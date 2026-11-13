"""UC24 / SCR19 / SCR13 HTTP/SQL checks: users, roles, lock, password reset, audit, statistics (Administration module, phase 8).

Per-run accounts only (created staff accounts are registered with smoke.test_emails for cleanup).
Run: ``python tests/api/user_admin.py --start`` (development DB with V003 applied).
"""
import datetime
import sys
import uuid

import smoke
from content_screens import check, cleanup, grant_roles, sql_scalar

API = smoke.API
request = smoke.request


def run():
    suffix = uuid.uuid4().hex[:8]
    admin_email, admin_password, a = smoke.registration('Quản trị người dùng')
    _, _, customer = smoke.registration('Khách thường')
    target_email, target_password, target = smoke.registration('Tài khoản đích')
    grant_roles(admin_email, ['Admin'])
    admin_token = request(API + '/api/auth/login', 'POST', {'email': admin_email, 'password': admin_password})['token']
    my_id = request(API + '/api/auth/me', token=admin_token)['id']
    target_id = target['user']['id']
    adm = lambda path, **kw: request(API + '/api/admin' + path, **kw)

    # Chỉ Admin.
    adm('/users', expected=401)
    adm('/users', token=customer['token'], expected=403)
    adm('/audit', token=customer['token'], expected=403)
    adm('/stats', token=customer['token'], expected=403)

    # Danh sách, lọc, chi tiết.
    listing = adm('/users?role=Admin', token=admin_token)
    check(any(item['id'] == my_id for item in listing['items']) and all('Admin' in item['roles'] for item in listing['items']), 'Role filter must list admins only.')
    check(adm(f'/users?q={target_email}', token=admin_token)['total'] == 1, 'Search by email must find one account.')
    detail = adm(f'/users/{target_id}', token=admin_token)
    check(detail['user']['status'] == 'Active' and detail['activeSessions'] >= 1, 'User detail must show status and sessions.')
    adm('/users/999999999', token=admin_token, expected=404)

    # Tạo nhân viên: kiểm tra đầu vào, trùng email, mật khẩu tạm dùng được và không nằm trong nhật ký.
    staff_email = f'staff-{suffix}@example.test'
    smoke.test_emails.append(staff_email)
    form = {'email': staff_email, 'fullName': 'Nhân viên thử', 'phone': None, 'roles': ['Operator', 'Finance']}
    adm('/users', method='POST', token=admin_token, data={**form, 'roles': []}, expected=400)
    adm('/users', method='POST', token=admin_token, data={**form, 'roles': ['Hacker']}, expected=400)
    adm('/users', method='POST', token=admin_token, data={**form, 'email': 'khong-hop-le'}, expected=400)
    created = adm('/users', method='POST', token=admin_token, data=form, expected=201)
    temporary = created['temporaryPassword']
    check(len(temporary) == 12 and set(created['user']['roles']) == {'Operator', 'Finance'}, 'Temporary password and roles expected.')
    adm('/users', method='POST', token=admin_token, data=form, expected=409)
    # Hai yêu cầu đồng thời cùng email: một tạo được, một báo trùng (không lỗi 500).
    race_email = f'staff-race-{suffix}@example.test'
    smoke.test_emails.append(race_email)

    def _create(_):
        import json, urllib.request, urllib.error
        req = urllib.request.Request(API + '/api/admin/users', data=json.dumps({**form, 'email': race_email}).encode(), method='POST',
                                     headers={'Content-Type': 'application/json', 'Authorization': 'Bearer ' + admin_token})
        try:
            return urllib.request.urlopen(req, timeout=15).status
        except urllib.error.HTTPError as error:
            return error.code

    from concurrent.futures import ThreadPoolExecutor
    with ThreadPoolExecutor(max_workers=2) as pool:
        check(sorted(pool.map(_create, [0, 1])) == [201, 409], 'Concurrent staff creation with the same email must yield one 201 and one 409.')
    staff_id = created['user']['id']
    staff_token = request(API + '/api/auth/login', 'POST', {'email': staff_email, 'password': temporary})['token']
    check(set(request(API + '/api/auth/me', token=staff_token)['roles']) == {'Operator', 'Finance'}, 'New staff must log in with the temporary password.')
    check(int(sql_scalar(f"SELECT COUNT(*) FROM NHATKYTHAOTAC WHERE LoaiDoiTuong = 'TAIKHOAN' AND MaDoiTuong = '{staff_id}' AND (DuLieuSau LIKE '%{temporary}%' OR LyDo LIKE '%{temporary}%')")) == 0,
          'The temporary password must not be logged.')

    # Đổi vai trò xóa phiên; không tự gỡ Admin của mình.
    adm(f'/users/{staff_id}/roles', method='PUT', token=admin_token, data={'roles': []}, expected=400)
    adm(f'/users/{staff_id}/roles', method='PUT', token=admin_token, data={'roles': ['Nope']}, expected=400)
    adm('/users/999999999/roles', method='PUT', token=admin_token, data={'roles': ['Editor']}, expected=404)
    check(adm(f'/users/{staff_id}/roles', method='PUT', token=admin_token, data={'roles': ['Editor']})['roles'] == ['Editor'], 'Roles not replaced.')
    request(API + '/api/auth/me', token=staff_token, expected=401)
    adm(f'/users/{my_id}/roles', method='PUT', token=admin_token, data={'roles': ['Editor']}, expected=409)
    admins = adm('/users?role=Admin&status=Active', token=admin_token)['total']
    if admins == 1:
        check(True, 'Only one admin in this database: removing it is refused above.')

    # Khóa: tự khóa 409; khóa xóa phiên và chặn đăng nhập; mở khóa.
    adm(f'/users/{my_id}/status', method='POST', token=admin_token, data={'status': 'Locked'}, expected=409)
    adm(f'/users/{target_id}/status', method='POST', token=admin_token, data={'status': 'Bogus'}, expected=400)
    check(adm(f'/users/{target_id}/status', method='POST', token=admin_token, data={'status': 'Locked', 'reason': 'kiểm thử'})['status'] == 'Locked', 'Lock failed.')
    request(API + '/api/auth/me', token=target['token'], expected=401)
    request(API + '/api/auth/login', 'POST', {'email': target_email, 'password': target_password}, expected=401)
    check(adm(f'/users/{target_id}/status', method='POST', token=admin_token, data={'status': 'Active'})['status'] == 'Active', 'Unlock failed.')
    request(API + '/api/auth/login', 'POST', {'email': target_email, 'password': target_password})

    # Đặt lại mật khẩu: mật khẩu cũ hết hiệu lực, mật khẩu tạm dùng được.
    reset = adm(f'/users/{target_id}/reset-password', method='POST', token=admin_token)
    request(API + '/api/auth/login', 'POST', {'email': target_email, 'password': target_password}, expected=401)
    request(API + '/api/auth/login', 'POST', {'email': target_email, 'password': reset['password']})
    adm('/users/999999999/reset-password', method='POST', token=admin_token, expected=404)
    check(int(sql_scalar(f"SELECT COUNT(*) FROM NHATKYTHAOTAC WHERE LoaiDoiTuong = 'TAIKHOAN' AND MaDoiTuong = '{target_id}' AND (DuLieuSau LIKE '%{reset['password']}%' OR LyDo LIKE '%{reset['password']}%')")) == 0,
          'The reset password must not be logged.')

    # Nhật ký chỉ đọc, đủ nhãn.
    audit = adm(f'/audit?objectType=TAIKHOAN&actorId={my_id}&size=50', token=admin_token)
    actions = {row['action'] for row in audit['items']}
    check({'Tạo tài khoản nhân viên', 'Đổi vai trò', 'Khóa tài khoản', 'Mở khóa tài khoản', 'Đặt lại mật khẩu'} <= actions, f'Audit labels missing: {actions}.')
    check(all(row['actorName'] for row in audit['items']), 'Audit rows must carry the actor name.')
    adm('/audit', method='DELETE', token=admin_token, expected=405)
    request(API + '/api/admin/audit/1', 'PUT', {}, token=admin_token, expected=404)

    # Thống kê: khoảng sai, không dữ liệu trả 0, và khớp SQL.
    adm('/stats?from=2030-02-01&to=2030-01-01', token=admin_token, expected=400)
    adm('/stats?to=9999-12-31', token=admin_token, expected=400)  # AddDays(1) trên MaxValue từng gây 500
    adm('/stats?from=0001-01-01', token=admin_token, expected=400)
    adm('/audit?to=9999-12-31', token=admin_token, expected=400)
    adm('/audit?from=2030-02-01&to=2030-01-01', token=admin_token, expected=400)
    check(adm('/audit?page=2147483647', token=admin_token)['items'] == [], 'A huge audit page must be clamped, not overflow.')
    empty = adm('/stats?from=2000-01-01&to=2000-01-31', token=admin_token)
    check(empty['orders'] == [] and empty['orderValue'] == 0 and empty['received'] == 0 and empty['refunded'] == 0 and empty['requests'] == [], f'Empty stats: {empty}.')
    today = datetime.date.today()
    stats = adm(f'/stats?from={today - datetime.timedelta(days=3650)}&to={today}', token=admin_token)
    expected = sql_scalar("SELECT CAST(COALESCE(SUM(SoTien), 0) AS bigint) FROM GIAODICHTIEN WHERE LoaiGD = 'Receipt'")
    check(str(int(stats['received'])) == expected, f'Stats received {stats["received"]} must match SQL {expected}.')


if __name__ == '__main__':
    try:
        if '--start' in sys.argv:
            with smoke.running_apps(api_only=True):
                run()
        else:
            run()
        print(f'PASS: user administration (UC24) assertions (+{smoke.checks} request checks).')
    finally:
        cleanup()
