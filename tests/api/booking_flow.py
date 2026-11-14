"""UC11-13, UC19, UC20 HTTP/SQL checks: booking lifecycle, seat holding, money, expiry worker (Booking module, phase 6).

Needs ``--start`` for the expiry checks: smoke.running_apps sets Booking__ExpiryIntervalMinutes=0.05 (3 s worker tick).
Test data uses the ``SMOKE tour `` prefix and per-run accounts; ``content_screens.cleanup()`` removes it.
Run: ``python tests/api/booking_flow.py --start`` (development DB with V003 applied).
"""
from concurrent.futures import ThreadPoolExecutor
import datetime
import sys
import time
import uuid

import smoke
from content_screens import check, cleanup, grant_roles, sql_scalar

API = smoke.API
request = smoke.request


def at(days, hour=8):
    return (datetime.datetime.now().replace(minute=0, second=0, microsecond=0, hour=hour) + datetime.timedelta(days=days)).isoformat()


def now_iso(minutes=-1):
    return (datetime.datetime.now() + datetime.timedelta(minutes=minutes)).isoformat()


def run():
    suffix = uuid.uuid4().hex[:8]
    op_email, _, _ = smoke.registration('Điều hành đơn')
    fin_email, _, _ = smoke.registration('Tài chính đơn')
    _, _, c1 = smoke.registration('Khách đơn 1')
    _, _, c2 = smoke.registration('Khách đơn 2')
    _, _, c3 = smoke.registration('Khách đơn 3')
    grant_roles(op_email, ['Operator'])
    grant_roles(fin_email, ['Operator', 'Finance'])
    login = lambda email: request(API + '/api/auth/login', 'POST', {'email': email, 'password': 'SmokePass!2026'})['token']
    op, fin = login(op_email), login(fin_email)
    t1, t2, t3 = c1['token'], c2['token'], c3['token']
    admin = lambda path, **kw: request(API + '/api/admin' + path, **kw)

    # Dữ liệu: một tour đã công bố, lịch sức chứa 1 (tranh chỗ) và lịch sức chứa 10.
    p1 = request(API + '/api/destinations')[0]['id']
    tour = admin('/tours', method='POST', token=op, expected=201, data={'name': f'SMOKE tour đơn {suffix}', 'region': 'Vũng Tàu', 'category': 'Biển',
                 'duration': '1 ngày', 'referencePrice': 500000, 'summary': 'Tour kiểm thử đặt chỗ', 'cancelPolicy': 'Hủy trước 3 ngày hoàn 80%.'})
    tid = tour['id']
    admin(f'/tours/{tid}/places', method='PUT', token=op, data=[{'destinationId': p1, 'day': 1, 'order': 1}])
    admin(f'/tours/{tid}/status', method='POST', token=op, data={'status': 'Published'})
    small = admin(f'/tours/{tid}/departures', method='POST', token=op, expected=201, data={'startsAt': at(20), 'price': 1000000, 'capacity': 1})['id']
    big = admin(f'/tours/{tid}/departures', method='POST', token=op, expected=201, data={'startsAt': at(21), 'price': 1000000, 'capacity': 10})['id']
    # Six order states on a fixed business day; direct fixtures are scoped to this test account.
    # UTC 17:00 is midnight in Vietnam, so the extra order just before it must be excluded.
    grant_roles(fin_email, ['Admin'])
    stats_token = login(fin_email)
    staff_audit = admin('/audit/search?actorType=staff&page=1', token=stats_token)
    check(any(row['objectType'] == 'TOUR' and row['objectId'] == str(tid) and row['action'] == 'Tạo tour' for row in staff_audit['items']), 'SCR19 must show staff tour audit.')
    marker = f'stats-{suffix}'
    owner_id = c3['user']['id']
    recorder_id = request(API + '/api/auth/me', token=stats_token)['id']
    try:
        smoke.sql_non_query(f"""
INSERT INTO DONDAT (MaTK, MaLKH, SoKhach, GiaChot, DieuKienHuy, TrangThai, NgayDat, LienHe)
SELECT {owner_id}, {big}, 1, 100, N'test', v.state, '2001-01-01T17:00:00', '{marker}'
FROM (VALUES ('Pending'),('Confirmed'),('CancelRequested'),('Cancelled'),('Expired'),('Completed')) v(state);
INSERT INTO DONDAT (MaTK, MaLKH, SoKhach, GiaChot, DieuKienHuy, TrangThai, NgayDat, LienHe)
VALUES ({owner_id}, {big}, 1, 500, N'test', 'Confirmed', '2001-01-01T16:59:59', '{marker}');
INSERT INTO GIAODICHTIEN (MaDon, LoaiGD, SoTien, MaThamChieu, ThoiDiem, MaTKGhiNhan)
SELECT MaDon, 'Receipt', 10, '{marker}-r', '2001-01-01T17:00:00', {recorder_id} FROM DONDAT WHERE LienHe='{marker}' AND TrangThai='Cancelled';
INSERT INTO GIAODICHTIEN (MaDon, LoaiGD, SoTien, MaThamChieu, ThoiDiem, MaTKGhiNhan)
SELECT MaDon, 'Refund', 4, '{marker}-f', '2001-01-01T17:00:00', {recorder_id} FROM DONDAT WHERE LienHe='{marker}' AND TrangThai='Cancelled';
""")
        summary = admin('/stats?from=2001-01-02&to=2001-01-02', token=stats_token)
        legacy = admin('/accounts/statistics?from=2001-01-02&to=2001-01-02', token=stats_token)
        states = {'Pending', 'Confirmed', 'CancelRequested', 'Cancelled', 'Expired', 'Completed'}
        check({x['status'] for x in summary['orders']} == states and sum(x['count'] for x in summary['orders']) == 6, f'All six states and UTC+7 boundary required: {summary}.')
        check(summary['orderValue'] == 400 and summary['received'] == 10 and summary['refunded'] == 4, f'Effective value and transaction totals mismatch: {summary}.')
        check(legacy['orderValue'] == summary['orderValue'] and legacy['collected'] == summary['received'] and legacy['refunded'] == summary['refunded'], 'Statistics APIs must use one calculation.')
        for state in states:
            filtered = admin(f'/stats?from=2001-01-02&to=2001-01-02&status={state}', token=stats_token)
            expected_value = 0 if state in {'Cancelled', 'Expired'} else 100
            check(filtered['orderValue'] == expected_value and len(filtered['orders']) == 1 and filtered['orders'][0]['count'] == 1 and filtered['received'] == 10 and filtered['refunded'] == 4, f'Status filter {state} must preserve transaction totals: {filtered}.')
    finally:
        smoke.sql_non_query(f"DELETE FROM GIAODICHTIEN WHERE MaDon IN (SELECT MaDon FROM DONDAT WHERE LienHe='{marker}'); DELETE FROM DONDAT WHERE LienHe='{marker}';")
    departure = lambda dep_id: next(item for item in admin(f'/tours/{tid}/departures', token=op) if item['id'] == dep_id)
    book = lambda token, dep_id, guests=1, expected=201: request(API + '/api/account/bookings', 'POST', {'departureId': dep_id, 'guests': guests, 'contact': '0900000000'}, token=token, expected=expected)

    # Quyền truy cập.
    request(API + '/api/account/bookings', expected=401)
    request(API + '/api/admin/bookings', token=t1, expected=403)
    request(API + '/api/admin/bookings', token=fin)
    check(request(API + '/api/admin/bookings?status=Pending', token=op)['page'] == 1, 'Staff list must page.')

    # Kiểm tra đầu vào.
    book(t1, big, 0, 400)
    book(t1, big, 41, 400)
    request(API + '/api/account/bookings', 'POST', {'departureId': big, 'guests': 1, 'contact': '  '}, token=t1, expected=400)
    book(t1, 999999999, 1, 404)

    # Tranh chỗ cuối: hai khách cùng đặt lịch còn 1 chỗ, chỉ một đơn được tạo.
    def _status(token):
        import json, urllib.request, urllib.error
        req = urllib.request.Request(API + '/api/account/bookings', data=json.dumps({'departureId': small, 'guests': 1, 'contact': '0900000000'}).encode(), method='POST',
                                     headers={'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token})
        try:
            return urllib.request.urlopen(req, timeout=15).status
        except urllib.error.HTTPError as error:
            return error.code

    with ThreadPoolExecutor(max_workers=2) as pool:
        statuses = sorted(pool.map(_status, [t1, t2]))
    check(statuses == [201, 409], f'Only one customer may take the last seat: {statuses}.')
    held = departure(small)
    check(held['held'] == 1 and held['status'] == 'Closed', 'Full departure must be Closed automatically.')
    check(small not in [item['id'] for item in request(API + f'/api/tours/{tid}/departures')], 'Closed departure must not be public.')
    winner_token = t1 if request(API + '/api/account/bookings', token=t1) else t2
    winner = request(API + '/api/account/bookings', token=winner_token)[0]
    request(f"{API}/api/account/bookings/{winner['id']}/cancel", 'POST', {'reason': 'đổi kế hoạch'}, token=winner_token)
    check(departure(small)['held'] == 1, 'Seat stays held while a cancel request is pending.')
    admin(f"/bookings/{winner['id']}/cancel-decision", method='POST', token=op, data={'approve': True, 'reason': 'đồng ý'})
    freed = departure(small)
    check(freed['held'] == 0 and freed['status'] == 'Open', 'Approved cancellation must free the seat and reopen the departure.')

    # Gửi lặp: cùng số khách trả cùng đơn (200) và chỉ giữ chỗ một lần; khác số khách 409.
    first = book(t1, big, 2)
    again = book(t1, big, 2, 200)
    check(first['id'] == again['id'] and first['status'] == 'Pending' and first['source'] == 'Departure', 'Repeat booking must return the same order.')
    book(t1, big, 3, 409)
    check(departure(big)['held'] == 2, 'Repeated booking must hold seats once.')
    order = first['id']
    check(first['agreedPrice'] == 2000000 and first['requiredDeposit'] == 600000, f'Price and deposit are computed on the server: {first}.')
    check(first['holdUntil'] is not None and first['tripDate'] is not None, 'Hold deadline and trip date expected.')
    today = datetime.date.today().isoformat()
    check(any(item['id'] == first['id'] for item in admin(f"/bookings?q={first['id']}&to={today}", token=op)['items']), 'The "to" date must include orders placed on that day.')
    pending = admin('/bookings?status=Pending', token=op)['items']
    holds = [item['holdUntil'] for item in pending if item['holdUntil']]
    check(any(item['id'] == first['id'] and item['holdUntil'] for item in pending) and holds == sorted(holds), 'Pending orders must expose the hold deadline, soonest first.')
    detail = request(f'{API}/api/account/bookings/{order}', token=t1)
    check(detail['outstanding'] == 2000000 and detail['paid'] == 0 and detail['cancelPolicy'], 'Booking detail money fields.')
    request(f'{API}/api/account/bookings/{order}', token=t2, expected=404)
    check(any(item['id'] == order for item in request(API + '/api/account/bookings', token=t1)), 'Booking list must contain the order.')
    request(f'{API}/api/account/bookings/{order}/cancel', 'POST', {'reason': ''}, token=t1, expected=400)
    request(f'{API}/api/account/bookings/{order}/cancel', 'POST', {'reason': 'x'}, token=t2, expected=404)

    # Xác nhận cần đủ khoản cần thu (422); chỉ Admin hoặc Finance ghi tiền.
    admin(f'/bookings/{order}/confirm', method='POST', token=op, expected=422)
    money = lambda token, data, expected=201: admin(f'/bookings/{order}/money', method='POST', token=token, data=data, expected=expected)
    ref = f'SMK-{suffix}-1'
    form = {'kind': 'Receipt', 'amount': 600000, 'reference': ref, 'at': now_iso()}
    money(op, form, 403)
    money(t1, form, 403)
    money(fin, {**form, 'amount': 2500000}, 422)
    money(fin, {**form, 'kind': 'Other'}, 400)
    money(fin, {**form, 'at': now_iso(600)}, 400)
    entry = money(fin, form)
    check(entry['kind'] == 'Receipt' and entry['amount'] == 600000, 'Receipt not recorded.')
    check(money(fin, form, 200)['id'] == entry['id'], 'Same reference on the same order must return the same transaction.')
    check(admin(f'/bookings/{order}', token=op)['detail']['paid'] == 600000, 'Paid must reflect receipts.')

    # Ghi thu đồng thời (khác mã tham chiếu): tổng thu không vượt giá chốt (còn phải thu 1.400.000, hai khoản 1.000.000).
    def _receipt(reference):
        import json, urllib.request, urllib.error
        body = {'kind': 'Receipt', 'amount': 1000000, 'reference': reference, 'at': now_iso()}
        req = urllib.request.Request(f'{API}/api/admin/bookings/{order}/money', data=json.dumps(body).encode(), method='POST',
                                     headers={'Content-Type': 'application/json', 'Authorization': 'Bearer ' + fin})
        try:
            return urllib.request.urlopen(req, timeout=15).status
        except urllib.error.HTTPError as error:
            return error.code

    with ThreadPoolExecutor(max_workers=2) as pool:
        results = sorted(pool.map(_receipt, [f'{ref}-A', f'{ref}-B']))
    check(results == [201, 422], f'Concurrent receipts must not exceed the amount due: {results}.')
    check(admin(f'/bookings/{order}', token=op)['detail']['paid'] == 1600000, 'Only one concurrent receipt may be recorded.')
    smoke.sql_non_query(f"DELETE FROM GIAODICHTIEN WHERE MaDon = {order} AND MaThamChieu IN ('{ref}-A', '{ref}-B')")

    # Xác nhận: cấp quyền nghe, thông báo khách.
    confirmed = admin(f'/bookings/{order}/confirm', method='POST', token=op)
    check(confirmed['status'] == 'Confirmed', 'Order must be confirmed.')
    staff_audit = admin('/audit/search?actorType=staff&page=1', token=stats_token)
    check(any(row['objectType'] == 'DONDAT' and row['objectId'] == str(order) and row['action'] == 'Xác nhận đơn' for row in staff_audit['items']), 'SCR19 must show staff booking audit.')
    check(sql_scalar(f'SELECT COUNT(*) FROM QUYENNGHE WHERE MaDon = {order}') == '1', 'Confirming must grant a listen right.')
    check(any('xác nhận' in note['title'] for note in request(API + '/api/account/notices', token=t1)), 'Customer must be notified about confirmation.')
    admin(f'/bookings/{order}/confirm', method='POST', token=op, expected=409)

    # Tham chiếu dùng cho đơn khác: 409. Hoàn tiền vượt mức: 422.
    other = book(t3, big, 1)['id']
    admin(f'/bookings/{other}/money', method='POST', token=fin, data={**form, 'amount': 300000}, expected=409)
    money(fin, {'kind': 'Refund', 'amount': 700000, 'reference': ref + 'R', 'at': now_iso()}, 422)

    # Điều chỉnh đơn đã xác nhận: cần lý do và ý kiến khách; đổi giá làm giảm khoản cần thu nếu vượt.
    adjust = lambda data, expected=200: admin(f'/bookings/{order}/adjust', method='POST', token=op, data=data, expected=expected)
    adjust({'change': 'đổi giờ', 'reason': 'khách yêu cầu', 'customerConsent': '', 'newPrice': None}, 400)
    adjust({'change': 'giảm giá', 'reason': 'ưu đãi', 'customerConsent': 'khách đồng ý', 'newPrice': 500000.5}, 400)
    adjusted = adjust({'change': 'giảm giá', 'reason': 'ưu đãi', 'customerConsent': 'khách đồng ý qua điện thoại', 'newPrice': 500000})
    check(adjusted['detail']['booking']['agreedPrice'] == 500000 and adjusted['detail']['booking']['requiredDeposit'] == 500000, f'Adjust must cap the deposit: {adjusted}.')
    customer_view = request(f'{API}/api/account/bookings/{order}', token=t1)
    check(any(stop['place'] for stop in customer_view['program']), f'Order detail must show the committed program: {customer_view["program"]}.')
    check(len(customer_view['adjustments']) == 1 and customer_view['adjustments'][0]['change'] == 'giảm giá' and customer_view['adjustments'][0]['price'] == 500000
          and customer_view['adjustments'][0]['reason'] == 'ưu đãi', f'Order detail must show the adjustment history: {customer_view["adjustments"]}.')
    check('khách đồng ý' not in str(customer_view['adjustments']), 'The customer consent note is not repeated in the history.')
    admin(f'/bookings/{other}/adjust', method='POST', token=op, data={'change': 'x', 'reason': 'y', 'customerConsent': 'z', 'newPrice': None}, expected=409)

    # Hủy: từ chối trả đúng trạng thái cũ; duyệt trả chỗ, thu hồi quyền nghe; sau đó vẫn hoàn được tiền.
    cancel = lambda token: request(f'{API}/api/account/bookings/{order}/cancel', 'POST', {'reason': 'không đi được'}, token=token)
    check(cancel(t1)['status'] == 'CancelRequested', 'Cancel request failed.')
    request(f'{API}/api/account/bookings/{order}/cancel', 'POST', {'reason': 'lại'}, token=t1, expected=409)
    check(request(f'{API}/api/account/bookings/{order}', token=t1)['cancelReason'] == 'không đi được', 'Cancel reason visible while pending.')
    admin(f'/bookings/{order}/cancel-decision', method='POST', token=op, data={'approve': False, 'reason': ''}, expected=400)
    check(admin(f'/bookings/{order}/cancel-decision', method='POST', token=op, data={'approve': False, 'reason': 'đã nhận cọc'})['status'] == 'Confirmed',
          'Rejecting a cancel request must restore the previous status.')
    cancel(t1)
    held_before = departure(big)['held']
    check(admin(f'/bookings/{order}/cancel-decision', method='POST', token=op, data={'approve': True, 'reason': 'đồng ý'})['status'] == 'Cancelled', 'Approve failed.')
    check(departure(big)['held'] == held_before - 2, 'Approving must release the held seats exactly once.')
    check(sql_scalar(f'SELECT COUNT(*) FROM QUYENNGHE WHERE MaDon = {order}') == '0', 'Approving must revoke the listen right.')
    money(fin, {'kind': 'Receipt', 'amount': 1, 'reference': ref + 'X', 'at': now_iso()}, 409)
    refund = money(fin, {'kind': 'Refund', 'amount': 480000, 'reference': ref + 'R2', 'at': now_iso()})
    check(refund['kind'] == 'Refund', 'Refund must be recorded after cancellation.')
    cancelled_detail = admin(f'/bookings/{order}', token=fin)['detail']
    check(cancelled_detail['paid'] == 600000 and cancelled_detail['refunded'] == 480000 and cancelled_detail['refundable'] == 120000, f'Money is shown separately: {cancelled_detail}.')
    request(f'{API}/api/account/bookings/{order}/cancel', 'POST', {'reason': 'x'}, token=t1, expected=409)

    # Hết hạn giữ chỗ (worker 3 giây): trả chỗ đúng một lần dù worker chạy nhiều lần; thu muộn rồi xác nhận lại.
    held_before = departure(big)['held']
    late = book(t2, big, 1)['id']
    check(departure(big)['held'] == held_before + 1, 'Booking must hold one seat.')
    smoke.sql_non_query(f'UPDATE DONDAT SET HanGiuCho = DATEADD(MINUTE, -5, SYSDATETIME()) WHERE MaDon = {late}')
    for _ in range(40):
        if request(f'{API}/api/account/bookings/{late}', token=t2)['booking']['status'] == 'Expired':
            break
        time.sleep(1)
    else:
        check(False, 'Hold expiry worker did not expire the order (run with --start).')
    time.sleep(7)  # hai chu kỳ nữa: không được trả chỗ lần hai
    check(departure(big)['held'] == held_before, 'Expiry must release seats exactly once.')
    check(any('hết hạn' in note['title'] for note in request(API + '/api/account/notices', token=t2)), 'Customer must be notified about expiry.')
    request(f'{API}/api/account/bookings/{late}/cancel', 'POST', {'reason': 'x'}, token=t2, expected=409)
    late_dep = admin(f'/bookings/{late}', token=op)['detail']['booking']['requiredDeposit']
    admin(f'/bookings/{late}/money', method='POST', token=fin, data={'kind': 'Receipt', 'amount': late_dep, 'reference': ref + 'L', 'at': now_iso()}, expected=201)
    check(admin(f'/bookings/{late}/confirm', method='POST', token=op)['status'] == 'Confirmed', 'Late payment must allow confirming an expired order.')
    check(departure(big)['held'] == held_before + 1, 'Late confirmation must hold the seat again.')

    # Hoàn thành: chỉ sau ngày đi; khách không hủy được đơn đã qua ngày đi.
    admin(f'/bookings/{late}/complete', method='POST', token=op, expected=409)
    smoke.sql_non_query(f'UPDATE DONDAT SET NgayDi = DATEADD(DAY, -1, CAST(SYSDATETIME() AS date)) WHERE MaDon = {late}')
    request(f'{API}/api/account/bookings/{late}/cancel', 'POST', {'reason': 'x'}, token=t2, expected=409)
    check(any(item['id'] == late for item in admin('/bookings?overdue=true', token=op)['items']), 'Overdue filter must list confirmed past-date orders.')
    check(admin(f'/bookings/{late}/complete', method='POST', token=op)['status'] == 'Completed', 'Complete failed.')
    admin(f'/bookings/{late}/complete', method='POST', token=op, expected=409)

    # Ngừng tour không làm mất chương trình của đơn đã đặt (QT09).
    admin(f'/tours/{tid}/status', method='POST', token=op, data={'status': 'Inactive'})
    after_stop = request(f'{API}/api/account/bookings/{order}', token=t1)
    check(after_stop['program'] and all(stop['place'] and not stop['place'].startswith('Địa điểm #') for stop in after_stop['program']),
          f'Order detail must still show the program after the tour is stopped: {after_stop["program"]}.')

    # Nhật ký thao tác nhân viên và tác vụ nền.
    check(int(sql_scalar(f"SELECT COUNT(*) FROM NHATKYTHAOTAC WHERE LoaiDoiTuong = 'DONDAT' AND MaDoiTuong = '{late}'")) >= 3, 'Order audit rows expected.')
    check(int(sql_scalar(f"SELECT COUNT(*) FROM NHATKYTHAOTAC WHERE LoaiDoiTuong = 'DONDAT' AND MaDoiTuong = '{late}' AND MaTK IS NULL")) == 1, 'Expiry must be audited without an actor.')
    check(int(sql_scalar("SELECT COUNT(*) FROM NHATKYTHAOTAC WHERE LoaiDoiTuong = 'GIAODICHTIEN' AND HanhDong IN (N'Ghi nhận thu', N'Ghi nhận hoàn')")) >= 3, 'Money audit rows expected.')


if __name__ == '__main__':
    try:
        if '--start' in sys.argv:
            with smoke.running_apps(api_only=True):
                run()
        else:
            run()
        print(f'PASS: booking flow (UC11-13, 19, 20) assertions (+{smoke.checks} request checks).')
    finally:
        cleanup()
