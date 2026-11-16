"""UC09, UC10, UC17, UC18 HTTP/SQL checks: private-tour request, verification, quote, accept, expiry (Quotation module, phase 7).

Needs ``--start`` for the expiry check: smoke.running_apps sets Quotation__ExpiryIntervalMinutes=0.05 (3 s worker tick).
Per-run accounts only; ``content_screens.cleanup()`` removes them with their itineraries, requests, quotes and orders.
Run: ``python tests/api/quotation_flow.py --start`` (development DB with V003 applied).
"""
import datetime
import sys
import time

import smoke
from content_screens import check, cleanup, grant_roles, sql_scalar

API = smoke.API
request = smoke.request


def iso(**delta):
    return (datetime.datetime.now() + datetime.timedelta(**delta)).isoformat()


def run():
    op_email, _, _ = smoke.registration('Điều hành báo giá')
    _, _, c1 = smoke.registration('Khách báo giá 1')
    _, _, c2 = smoke.registration('Khách báo giá 2')
    grant_roles(op_email, ['Operator'])
    op = request(API + '/api/auth/login', 'POST', {'email': op_email, 'password': 'SmokePass!2026'})['token']
    grant_roles(op_email, ['Admin'])
    audit_token = request(API + '/api/auth/login', 'POST', {'email': op_email, 'password': 'SmokePass!2026'})['token']
    t1, t2 = c1['token'], c2['token']
    admin = lambda path, **kw: request(API + '/api/admin/quote-requests' + path, **kw)
    mine = lambda path, **kw: request(API + '/api/account' + path, **kw)

    places = request(API + '/api/destinations')
    start = (datetime.date.today() + datetime.timedelta(days=10)).isoformat()

    def itinerary(name):
        return request(API + '/api/itineraries', 'POST', {'name': name, 'startDate': start, 'guests': 2, 'destinationIds': [places[0]['id'], places[1]['id']]}, token=t1)['id']

    def submit(trip, expected=201, **over):
        body = {'itineraryId': trip, 'guests': 2, 'contact': '0900000000', 'needs': 'Xe và bữa trưa', **over}
        return mine('/quote-requests', method='POST', data=body, token=t1, expected=expected)

    quote_form = lambda **over: {'items': [{'description': 'Xe 16 chỗ', 'unit': 'ngày', 'quantity': 1, 'unitPrice': 2000000, 'includedInPackage': False},
                                           {'description': 'Bữa trưa', 'unit': 'khách', 'quantity': 2, 'unitPrice': 150000, 'includedInPackage': True}],
                                 'deposit': 500000, 'cancelPolicy': 'Hoàn 80% trước 3 ngày', 'validUntil': iso(days=3), **over}

    # Quyền và đầu vào.
    mine('/quote-requests', expected=401)
    admin('', token=t1, expected=403)
    trip = itinerary('SMOKE báo giá A')
    submit(trip, 400, guests=0)
    submit(trip, 400, contact=' ')
    submit(999999999, 404)
    mine('/quote-requests', method='POST', data={'itineraryId': trip, 'guests': 2, 'contact': 'x', 'needs': None}, token=t2, expected=404)

    # Gửi lặp trả cùng yêu cầu và không để lại phiên bản thừa.
    first = submit(trip)
    check(first['status'] == 'Submitted' and first['versionNumber'] == 1, f'New request must be Submitted v1: {first}.')
    again = submit(trip, 200)
    check(again['id'] == first['id'], 'Repeating must return the same open request.')
    check(sql_scalar(f'SELECT COUNT(*) FROM PHIENBANLICHTRINH WHERE MaLT = {trip}') == '1', 'A repeated request must not leave an extra version.')
    request(f'{API}/api/itineraries/{trip}', 'DELETE', token=t1, expected=409)
    rid = first['id']
    check(mine(f'/quote-requests/{rid}', token=t1)['request']['id'] == rid, 'Owner must read the request.')
    mine(f'/quote-requests/{rid}', token=t2, expected=404)
    check(any(item['id'] == rid for item in mine('/quote-requests', token=t1)), 'List must contain the request.')

    # Nhân viên: tiếp nhận, xác minh, báo giá.
    check(any(item['id'] == rid for item in admin('', token=op)['items']), 'Staff list must contain the request.')
    admin(f'/{rid}/quotes', method='POST', token=op, data=quote_form(), expected=409)
    check(admin(f'/{rid}/receive', method='POST', token=op)['status'] == 'Verifying', 'Receive must move to Verifying.')
    check(admin(f'/{rid}/receive', method='POST', token=op)['status'] == 'Verifying', 'Receiving twice must not fail.')
    admin(f'/{rid}/quotes', method='POST', token=op, data=quote_form(), expected=422)
    verification = {'item': 'Xe 16 chỗ', 'provider': 'Nhà xe thử', 'at': iso(), 'result': 'Available', 'note': None}
    admin(f'/{rid}/verifications', method='POST', token=op, data={**verification, 'result': 'Bogus'}, expected=400)
    admin(f'/{rid}/verifications', method='POST', token=op, data={**verification, 'result': 'Unavailable'}, expected=400)
    admin(f'/{rid}/verifications', method='POST', token=op, data=verification, expected=201)
    admin(f'/{rid}/verifications', method='POST', token=op, data={**verification, 'result': 'Substitute', 'note': 'Đề xuất xe 29 chỗ'}, expected=201)
    detail = admin(f'/{rid}', token=op)
    check(len(detail['verifications']) == 2 and 'fits' in detail['check'], 'Staff detail must show verifications and the schedule check.')
    admin(f'/{rid}/quotes', method='POST', token=op, data=quote_form(items=[]), expected=422)
    admin(f'/{rid}/quotes', method='POST', token=op, data=quote_form(deposit=99999999), expected=422)
    admin(f'/{rid}/quotes', method='POST', token=op, data=quote_form(validUntil=iso(days=-1)), expected=422)
    q1 = admin(f'/{rid}/quotes', method='POST', token=op, data=quote_form(), expected=201)
    staff_audit = request(API + '/api/admin/audit/search?actorType=staff&page=1', token=audit_token)
    check(any(row['objectType'] == 'BAOGIA' and row['objectId'] == str(q1['id']) and row['action'] == 'Phát hành báo giá' for row in staff_audit['items']), 'SCR19 must show staff quotation audit.')
    check(q1['status'] == 'Sent' and q1['total'] == 2000000, f'Included items must not be added twice: {q1}.')
    check(mine(f'/quote-requests/{rid}', token=t1)['request']['status'] == 'Quoted', 'Issuing must move the request to Quoted.')
    q2 = admin(f'/{rid}/quotes', method='POST', token=op, data=quote_form(deposit=600000), expected=201)
    statuses = {item['id']: item['status'] for item in admin(f'/{rid}', token=op)['quotes']}
    check(statuses[q1['id']] == 'Superseded' and statuses[q2['id']] == 'Sent', f'A new quote must supersede the old one: {statuses}.')
    check(any('báo giá' in note['title'] for note in mine('/notices', token=t1)), 'Customer must be notified about the quote.')

    # Khách chấp nhận: sai chủ 404, báo giá cũ 409; chấp nhận lặp trả cùng đơn.
    mine(f"/quotes/{q2['id']}/accept", method='POST', data={}, token=t2, expected=404)
    mine(f"/quotes/{q1['id']}/accept", method='POST', data={}, token=t1, expected=409)
    accepted = mine(f"/quotes/{q2['id']}/accept", method='POST', data={}, token=t1, expected=201)
    customer_audit = request(API + '/api/admin/audit/search?actorType=customer&page=1', token=audit_token)
    check(any(row['objectType'] == 'BAOGIA' and row['objectId'] == str(q2['id']) and row['action'] == 'Khách chấp nhận báo giá' and row['actorType'] == 'customer' for row in customer_audit['items']), 'SCR19 must classify customer quote acceptance.')
    check(accepted['amount'] == 2000000 and accepted['deposit'] == 600000, f'Accepted quote values: {accepted}.')
    repeat = mine(f"/quotes/{q2['id']}/accept", method='POST', data={}, token=t1, expected=200)
    check(repeat['orderId'] == accepted['orderId'], 'Accepting twice must return the same order.')
    order = mine(f"/bookings/{accepted['orderId']}", token=t1)
    check(order['booking']['source'] == 'Quote' and order['booking']['status'] == 'Pending' and order['booking']['tripDate'] == start
          and order['booking']['requiredDeposit'] == 600000 and order['booking']['holdUntil'] is None, f'Order from quote: {order}.')
    check(mine(f'/quote-requests/{rid}', token=t1)['orderId'] == accepted['orderId'], 'Request detail must expose the order.')
    stops = mine(f"/quotes/{q2['id']}/program", token=t1)
    check(len(stops) == 2 and all(stop['day'] == 1 and stop['place'] for stop in stops), f'Quote program must list the two stops: {stops}.')
    mine(f"/quotes/{q2['id']}/program", token=t2, expected=404)
    mine(f'/quote-requests/{rid}/cancel', method='POST', data={'reason': 'x'}, token=t1, expected=409)
    admin(f'/{rid}/cancel', method='POST', token=op, data={'reason': 'x'}, expected=409)

    # Đề nghị đổi báo giá: yêu cầu về Verifying, báo giá cũ Superseded, ghi chú hiện cho nhân viên.
    trip2 = itinerary('SMOKE báo giá B')
    r2 = submit(trip2)['id']
    admin(f'/{r2}/receive', method='POST', token=op)
    admin(f'/{r2}/verifications', method='POST', token=op, data=verification, expected=201)
    q3 = admin(f'/{r2}/quotes', method='POST', token=op, data=quote_form(), expected=201)
    mine(f"/quotes/{q3['id']}/change-request", method='POST', data={'note': ''}, token=t1, expected=400)
    mine(f"/quotes/{q3['id']}/change-request", method='POST', data={'note': 'Muốn thêm bữa tối'}, token=t2, expected=404)
    check(mine(f"/quotes/{q3['id']}/change-request", method='POST', data={'note': 'Muốn thêm bữa tối'}, token=t1)['status'] == 'Verifying', 'Change request must reopen verification.')
    customer_audit = request(API + '/api/admin/audit/search?actorType=customer&page=1', token=audit_token)
    check(any(row['objectType'] == 'YEUCAUTOURRIENG' and row['objectId'] == str(r2) and row['action'] == 'Khách đề nghị đổi báo giá' and row['actorType'] == 'customer' for row in customer_audit['items']), 'SCR19 must classify customer quote change request.')
    mine(f"/quotes/{q3['id']}/accept", method='POST', data={}, token=t1, expected=409)
    check('Muốn thêm bữa tối' in admin(f'/{r2}', token=op)['changeNotes'], 'Staff must see the change note.')
    q4 = admin(f'/{r2}/quotes', method='POST', token=op, data=quote_form(), expected=201)
    check(q4['status'] == 'Sent', 'Re-quoting after a change request must work.')
    check(mine(f'/quote-requests/{r2}/cancel', method='POST', data={'reason': 'đổi ý'}, token=t1)['status'] == 'Cancelled', 'Customer cancel failed.')
    mine(f"/quotes/{q4['id']}/accept", method='POST', data={}, token=t1, expected=409)
    request(f'{API}/api/itineraries/{trip2}', 'DELETE', token=t1, expected=409)

    def quoted(name):
        trip_id = itinerary(name)
        request_id = submit(trip_id)['id']
        admin(f'/{request_id}/receive', method='POST', token=op)
        admin(f'/{request_id}/verifications', method='POST', token=op, data=verification, expected=201)
        return trip_id, admin(f'/{request_id}/quotes', method='POST', token=op, data=quote_form(), expected=201)

    yesterday = (datetime.date.today() - datetime.timedelta(days=1)).isoformat()
    # Ngày đi của đơn là ngày trong ảnh chụp lúc gửi yêu cầu (QT05), kể cả khi khách sửa lịch trình sau đó.
    trip4, q6 = quoted('SMOKE báo giá D')
    smoke.sql_non_query(f'UPDATE LICHTRINH SET NgayDi = DATEADD(DAY, 3, NgayDi) WHERE MaLT = {trip4}')
    accepted6 = mine(f"/quotes/{q6['id']}/accept", method='POST', data={}, token=t1, expected=201)
    check(mine(f"/bookings/{accepted6['orderId']}", token=t1)['booking']['tripDate'] == start, 'Order trip date must come from the version snapshot, not the edited itinerary.')
    # Ngày đi trong ảnh chụp đã qua: báo giá còn hiệu lực vẫn không chấp nhận được (không tạo đơn không hủy được).
    trip5, q7 = quoted('SMOKE báo giá E')
    smoke.sql_non_query(f"UPDATE PHIENBANLICHTRINH SET NoiDungJson = JSON_MODIFY(NoiDungJson, '$.startDate', '{yesterday}') WHERE MaLT = {trip5}")
    mine(f"/quotes/{q7['id']}/accept", method='POST', data={}, token=t1, expected=409)
    check(sql_scalar(f"SELECT COUNT(*) FROM DONDAT WHERE MaBG = {q7['id']}") == '0', 'A past trip date must not create an order.')
    check(sql_scalar(f"SELECT TrangThai FROM BAOGIA WHERE MaBG = {q7['id']}") == 'Sent', 'The rejected accept must leave the quote Sent.')

    # Hết hạn báo giá (worker 3 giây): yêu cầu vẫn Quoted; chấp nhận 409; vẫn đề nghị đổi được.
    trip3 = itinerary('SMOKE báo giá C')
    r3 = submit(trip3)['id']
    admin(f'/{r3}/receive', method='POST', token=op)
    admin(f'/{r3}/verifications', method='POST', token=op, data=verification, expected=201)
    q5 = admin(f'/{r3}/quotes', method='POST', token=op, data=quote_form(validUntil=iso(seconds=6)), expected=201)
    for _ in range(40):
        if mine(f'/quote-requests/{r3}', token=t1)['quote']['status'] == 'Expired':
            break
        time.sleep(1)
    else:
        check(False, 'Quote expiry worker did not expire the quote (run with --start).')
    check(mine(f'/quote-requests/{r3}', token=t1)['request']['status'] == 'Quoted', 'Expired quote must leave the request Quoted.')
    mine(f"/quotes/{q5['id']}/accept", method='POST', data={}, token=t1, expected=409)
    check(any('hết hạn' in note['title'] for note in mine('/notices', token=t1)), 'Customer must be notified about expiry.')
    check(int(sql_scalar(f"SELECT COUNT(*) FROM NHATKYTHAOTAC WHERE LoaiDoiTuong = 'BAOGIA' AND MaDoiTuong = '{q5['id']}' AND MaTK IS NULL")) == 1, 'Expiry must be audited without an actor.')
    check(mine(f"/quotes/{q5['id']}/change-request", method='POST', data={'note': 'Gia hạn giúp'}, token=t1)['status'] == 'Verifying', 'Expired quote may still trigger a change request.')

    # Nhật ký thao tác nhân viên và khách.
    for action in ('Tiếp nhận yêu cầu', 'Ghi xác minh dịch vụ', 'Phát hành báo giá', 'Khách chấp nhận báo giá', 'Khách đề nghị đổi báo giá'):
        check(int(sql_scalar(f"SELECT COUNT(*) FROM NHATKYTHAOTAC WHERE HanhDong = N'{action}'")) >= 1, f'Audit label missing: {action}.')


if __name__ == '__main__':
    try:
        if '--start' in sys.argv:
            with smoke.running_apps(api_only=True):
                run()
        else:
            run()
        print(f'PASS: quotation flow (UC09, 10, 17, 18) assertions (+{smoke.checks} request checks).')
    finally:
        cleanup()
