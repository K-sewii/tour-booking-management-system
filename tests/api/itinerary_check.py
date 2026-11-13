"""UC06-UC08 HTTP/SQL checks: schedule check (draft and saved), delete (Itinerary module, phase 5).

Test data uses the ``SMOKE `` name prefix; ``content_screens.cleanup()`` removes it and the per-run accounts.
Itinerary versions are exercised through quote requests in quotation_flow.py.
Run: ``python tests/api/itinerary_check.py --start`` (development DB with V003 applied).
"""
import datetime
import sys
import uuid

import smoke
from content_screens import check, cleanup, grant_roles

API = smoke.API
request = smoke.request


def day(offset):
    return (datetime.date.today() + datetime.timedelta(days=offset)).isoformat()


def run():
    suffix = uuid.uuid4().hex[:8]
    editor_email, _, _ = smoke.registration('Biên tập lịch')
    _, _, customer = smoke.registration('Khách lịch trình')
    _, _, other = smoke.registration('Khách khác')
    grant_roles(editor_email, ['Editor'])
    ed = request(API + '/api/auth/login', 'POST', {'email': editor_email, 'password': 'SmokePass!2026'})['token']
    cu, ot = customer['token'], other['token']

    # Địa điểm mở 08:00-17:00 có hai hoạt động; hoạt động còn lại ở địa điểm không có giờ mở cửa.
    def place(name, hours):
        form = {'name': f'SMOKE Lịch {name} {suffix}', 'region': 'Tây Ninh', 'category': 'Văn hóa', 'address': 'Thử nghiệm',
                'description': 'Địa điểm kiểm tra lịch.', 'visitMinutes': 60, **hours}
        created = request(API + '/api/admin/destinations', 'POST', form, token=ed)
        request(f"{API}/api/admin/destinations/{created['id']}/status", 'POST', {'status': 'Published'}, token=ed)
        return created['id']

    def slot(place_id, name, date, start, end):
        activity = request(API + '/api/admin/activities', 'POST', {'destinationId': place_id, 'name': f'Hoạt động {name} {suffix}', 'description': 'Kiểm thử',
                                                                    'durationMinutes': 60, 'conditions': 'Không', 'referenceFee': 100000}, token=ed)
        request(f"{API}/api/admin/activities/{activity['id']}/status", 'POST', {'status': 'Published'}, token=ed)
        return request(f"{API}/api/admin/activities/{activity['id']}/slots", 'POST', {'date': date, 'start': start, 'end': end}, token=ed)['id']

    a = place('A', {'openTime': '08:00:00', 'closeTime': '17:00:00'})
    b = place('B', {'openTime': '08:00:00', 'closeTime': '17:00:00'})
    c = place('C', {})
    start = day(5)
    s_a = slot(a, 'a', start, '09:00:00', '10:00:00')
    s_b = slot(b, 'b', start, '10:10:00', '11:10:00')       # cách s_a 10 phút < đệm 30 phút: Overlap
    s_early = slot(a, 'sớm', start, '06:00:00', '07:00:00')  # ngoài giờ mở cửa: OutOfHours
    s_wrong = slot(b, 'lệch', day(7), '09:00:00', '10:00:00')  # ngày khác ngày thứ 1: WrongDate
    s_c = slot(c, 'c', start, '14:00:00', '15:00:00')

    def draft(stops):
        return {'name': f'SMOKE lịch trình {suffix}', 'startDate': start, 'guests': 2,
                'stops': [{'destinationId': p, 'day': 1, 'order': i + 1, 'slotIds': slots} for i, (p, slots) in enumerate(stops)]}

    def codes(result):
        return sorted({w['code'] for w in result['warnings']})

    # POST /check công khai (nháp tạm), chỉ cảnh báo.
    ok = request(API + '/api/itineraries/check', 'POST', {'draft': draft([(a, [s_a])])})
    check(ok['fits'] is True and ok['complete'] is True and ok['warnings'] == [], f'Clean draft must fit: {ok}.')
    clash = request(API + '/api/itineraries/check', 'POST', {'draft': draft([(a, [s_a]), (b, [s_b])])})
    check('Overlap' in codes(clash) and clash['fits'] is False, f'Expected Overlap: {clash}.')
    bad = request(API + '/api/itineraries/check', 'POST', {'draft': draft([(a, [s_early]), (b, [s_wrong])])})
    check({'OutOfHours', 'WrongDate'} <= set(codes(bad)), f'Expected OutOfHours and WrongDate: {bad}.')
    dup = request(API + '/api/itineraries/check', 'POST', {'draft': draft([(a, [s_a])]), 'picks': [{'destinationId': a, 'slotId': s_a, 'note': None}]})
    check('Duplicate' in codes(dup), f'Expected Duplicate: {dup}.')
    unknown = request(API + '/api/itineraries/check', 'POST', {'draft': draft([(c, [s_c])])})
    check(unknown['complete'] is False and unknown['fits'] is True and 'Incomplete' in codes(unknown), f'Missing hours must be Incomplete: {unknown}.')
    request(API + '/api/itineraries/check', 'POST', {'draft': draft([(a, []), (a, [])])}, expected=400)
    request(API + '/api/itineraries/check', 'POST', {'draft': draft([(999999999, [])])}, expected=400)

    # Lịch trình đã lưu: chỉ chủ xem được kiểm tra; người khác nhận 404.
    trip = request(API + '/api/itineraries', 'POST', draft([(a, [s_a]), (b, [s_b])]), token=cu)
    check(any('trùng giờ' in w for w in trip['warnings']), 'Saved itinerary must keep string warnings for compatibility.')
    saved = request(f"{API}/api/itineraries/{trip['id']}/check", token=cu)
    check('Overlap' in codes(saved), 'Saved check must warn Overlap.')
    request(f"{API}/api/itineraries/{trip['id']}/check", token=ot, expected=404)
    request(f"{API}/api/itineraries/{trip['id']}/check", expected=401)

    # Xóa: người khác 404, chủ 204; xóa rồi không còn.
    request(f"{API}/api/itineraries/{trip['id']}", 'DELETE', token=ot, expected=404)
    request(f"{API}/api/itineraries/{trip['id']}", 'DELETE', token=cu, expected=204)
    request(f"{API}/api/itineraries/{trip['id']}", token=cu, expected=404)
    request(f"{API}/api/itineraries/{trip['id']}", 'DELETE', token=cu, expected=404)


if __name__ == '__main__':
    try:
        if '--start' in sys.argv:
            with smoke.running_apps(api_only=True):
                run()
        else:
            run()
        print(f'PASS: itinerary check (UC06-08) assertions (+{smoke.checks} request checks).')
    finally:
        cleanup()
