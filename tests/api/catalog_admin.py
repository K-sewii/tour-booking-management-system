"""UC16 / SCR16 HTTP and SQL checks: tour and departure administration (Catalog module, phase 4).

Test data uses the ``SMOKE tour `` name prefix and per-run accounts; ``content_screens.cleanup()`` removes it.
Seat holding (TryHold/Release, auto Closed/Open) is exercised through bookings in booking_flow.py.
Run: ``python tests/api/catalog_admin.py --start`` (API must be built, DB must be a development DB with V003 applied).
"""
import datetime
import sys
import uuid

import smoke
from content_screens import check, cleanup, grant_roles

API = smoke.API
request = smoke.request


def at(days, hour=8):
    return (datetime.datetime.now().replace(minute=0, second=0, microsecond=0, hour=hour) + datetime.timedelta(days=days)).isoformat()


def run():
    suffix = uuid.uuid4().hex[:8]
    operator_email, _, _ = smoke.registration('Điều hành tour')
    editor_email, _, _ = smoke.registration('Biên tập tour')
    customer_email, _, customer = smoke.registration('Khách tour')
    grant_roles(operator_email, ['Operator'])
    grant_roles(editor_email, ['Editor'])
    login = lambda email: request(API + '/api/auth/login', 'POST', {'email': email, 'password': 'SmokePass!2026'})['token']
    operator, editor = login(operator_email), login(editor_email)

    # Quyền: khách và biên tập viên không vào được quản trị tour.
    request(API + '/api/admin/tours', expected=401)
    request(API + '/api/admin/tours', token=customer['token'], expected=403)
    request(API + '/api/admin/tours', token=editor, expected=403)
    check(isinstance(request(API + '/api/admin/tours', token=operator), list), 'Operator cannot list tours.')

    # Điểm dừng cần địa điểm đã công bố.
    places = request(API + '/api/destinations')
    check(len(places) >= 2, 'Need two published destinations.')
    p1, p2 = places[0]['id'], places[1]['id']

    name = f'SMOKE tour {suffix}'
    form = {'name': name, 'region': 'Vũng Tàu', 'category': 'Biển', 'duration': '1 ngày', 'referencePrice': 500000, 'summary': 'Tour kiểm thử', 'cancelPolicy': None}
    request(API + '/api/admin/tours', 'POST', {**form, 'region': 'Hà Nội'}, token=operator, expected=400)
    request(API + '/api/admin/tours', 'POST', {**form, 'category': 'Lạ'}, token=operator, expected=400)
    tour = request(API + '/api/admin/tours', 'POST', form, token=operator, expected=201)
    check(tour['status'] == 'Draft' and tour['stops'] == 0, 'New tour must be Draft without stops.')
    tour_id = tour['id']
    # Trùng tên không phân biệt hoa thường và dấu.
    request(API + '/api/admin/tours', 'POST', {**form, 'name': name.upper()}, token=operator, expected=409)
    request(API + f'/api/admin/tours/{tour_id}', 'PUT', {**form, 'referencePrice': 600000}, token=operator)
    request(API + '/api/admin/tours/999999', 'PUT', form, token=operator, expected=404)
    check(request(API + f'/api/admin/tours/{tour_id}', token=operator)['tour']['referencePrice'] == 600000, 'Tour update did not persist.')

    # Chưa có điểm: không công bố được (422); tour Draft không hiện công khai.
    request(API + f'/api/admin/tours/{tour_id}/status', 'POST', {'status': 'Published'}, token=operator, expected=422)
    request(API + f'/api/tours/{tour_id}', expected=404)
    request(API + f'/api/tours/{tour_id}/departures', expected=404)
    request(API + f'/api/admin/tours/{tour_id}/departures', 'POST', {'startsAt': at(20), 'price': 500000, 'capacity': 10}, token=operator, expected=422)

    # Điểm dừng: lặp địa điểm, trùng thứ tự, địa điểm không có.
    request(API + f'/api/admin/tours/{tour_id}/places', 'PUT', [{'destinationId': p1, 'day': 1, 'order': 1}, {'destinationId': p1, 'day': 1, 'order': 2}], token=operator, expected=400)
    request(API + f'/api/admin/tours/{tour_id}/places', 'PUT', [{'destinationId': p1, 'day': 1, 'order': 1}, {'destinationId': p2, 'day': 1, 'order': 1}], token=operator, expected=400)
    request(API + f'/api/admin/tours/{tour_id}/places', 'PUT', [{'destinationId': 999999, 'day': 1, 'order': 1}], token=operator, expected=400)
    request(API + '/api/admin/tours/999999/places', 'PUT', [], token=operator, expected=404)
    detail = request(API + f'/api/admin/tours/{tour_id}/places', 'PUT', [{'destinationId': p1, 'day': 1, 'order': 1}, {'destinationId': p2, 'day': 1, 'order': 2}], token=operator)
    check(len(detail['stops']) == 2 and detail['tour']['stops'] == 2, 'Stops not replaced.')

    # Công bố, chuyển trạng thái không hợp lệ.
    request(API + f'/api/admin/tours/{tour_id}/status', 'POST', {'status': 'Bogus'}, token=operator, expected=400)
    check(request(API + f'/api/admin/tours/{tour_id}/status', 'POST', {'status': 'Published'}, token=operator)['status'] == 'Published', 'Publish failed.')
    request(API + f'/api/admin/tours/{tour_id}/status', 'POST', {'status': 'Published'}, token=operator, expected=409)
    request(API + f'/api/tours/{tour_id}')

    # Lịch khởi hành.
    request(API + f'/api/admin/tours/{tour_id}/departures', 'POST', {'startsAt': at(-1), 'price': 500000, 'capacity': 10}, token=operator, expected=400)
    request(API + f'/api/admin/tours/{tour_id}/departures', 'POST', {'startsAt': at(20), 'price': 500000, 'capacity': 0}, token=operator, expected=400)
    request(API + '/api/admin/tours/999999/departures', 'POST', {'startsAt': at(20), 'price': 500000, 'capacity': 5}, token=operator, expected=404)
    departure = request(API + f'/api/admin/tours/{tour_id}/departures', 'POST', {'startsAt': at(20), 'price': 500000, 'capacity': 10}, token=operator, expected=201)
    check(departure['status'] == 'Open' and departure['held'] == 0, 'New departure must be Open.')
    dep_id = departure['id']
    public = request(API + f'/api/tours/{tour_id}/departures')
    check([item['id'] for item in public] == [dep_id] and public[0]['remaining'] == 10, 'Public departures wrong.')
    updated = request(API + f'/api/admin/departures/{dep_id}', 'PUT', {'startsAt': at(21), 'price': 550000, 'capacity': 8}, token=operator)
    check(updated['capacity'] == 8 and updated['price'] == 550000, 'Departure update did not persist.')

    # Đã có chỗ được giữ: không đổi ngày, không giảm sức chứa dưới số chỗ giữ, không sửa điểm dừng.
    smoke.sql_non_query(f'UPDATE LICHKHOIHANH SET SoChoDaGiu = 3 WHERE MaLKH = {dep_id}')
    request(API + f'/api/admin/departures/{dep_id}', 'PUT', {'startsAt': at(21), 'price': 550000, 'capacity': 2}, token=operator, expected=409)
    request(API + f'/api/admin/departures/{dep_id}', 'PUT', {'startsAt': at(25), 'price': 550000, 'capacity': 8}, token=operator, expected=409)
    check(request(API + f'/api/admin/departures/{dep_id}', 'PUT', {'startsAt': at(21), 'price': 560000, 'capacity': 3}, token=operator)['status'] == 'Closed',
          'Capacity equal to held seats must close the departure.')
    request(API + f'/api/admin/tours/{tour_id}/places', 'PUT', [{'destinationId': p1, 'day': 1, 'order': 1}], token=operator, expected=409)
    check(request(API + f'/api/admin/departures/{dep_id}', 'PUT', {'startsAt': at(21), 'price': 560000, 'capacity': 9}, token=operator)['status'] == 'Open',
          'Raising capacity must reopen the departure.')
    smoke.sql_non_query(f'UPDATE LICHKHOIHANH SET SoChoDaGiu = 0 WHERE MaLKH = {dep_id}')

    # Ngừng lịch (một chiều) và ngừng tour không đụng lịch cũ.
    check(request(API + f'/api/admin/departures/{dep_id}/deactivate', 'POST', token=operator)['status'] == 'Inactive', 'Deactivate failed.')
    request(API + f'/api/admin/departures/{dep_id}/deactivate', 'POST', token=operator, expected=409)
    request(API + '/api/admin/departures/999999/deactivate', 'POST', token=operator, expected=404)
    check(request(API + f'/api/tours/{tour_id}/departures') == [], 'Inactive departure still public.')
    check(request(API + f'/api/admin/tours/{tour_id}/status', 'POST', {'status': 'Inactive', 'reason': 'kiểm thử'}, token=operator)['status'] == 'Inactive', 'Deactivate tour failed.')
    request(API + f'/api/tours/{tour_id}', expected=404)
    request(API + f'/api/admin/tours/{tour_id}/status', 'POST', {'status': 'Published'}, token=operator)

    audit = int(__import__('content_screens').sql_scalar(
        f"SELECT COUNT(*) FROM NHATKYTHAOTAC WHERE LoaiDoiTuong IN ('TOUR','LICHKHOIHANH') AND MaDoiTuong IN ('{tour_id}','{dep_id}')"))
    check(audit >= 8, f'Expected tour and departure audit rows, got {audit}.')


if __name__ == '__main__':
    try:
        if '--start' in sys.argv:
            with smoke.running_apps(api_only=True):
                run()
        else:
            run()
        print(f'PASS: catalog admin (UC16) assertions (+{smoke.checks} request checks).')
    finally:
        cleanup()
