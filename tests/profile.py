"""SCR21 integration checks. Mutates and cleans up only per-run generated accounts."""
import datetime
import html
import http.cookiejar
import json
import re
import sys
import time
import urllib.request
import uuid

import smoke
from auth_screens import api, check, post_form, sql_scalar

API, WEB = smoke.API, smoke.WEB


def email():
    value = f'scr21-{uuid.uuid4().hex}@example.test'
    smoke.test_emails.append(value)
    return value


def create():
    address = email()
    session = api(API + '/api/auth/register', 'POST', {
        'name': 'Khách kiểm thử SCR21', 'email': address, 'password': 'ProfilePass2026',
        'confirmPassword': 'ProfilePass2026', 'termsAccepted': True,
    }, expected=201)
    return address, session


def web_login(address):
    jar = http.cookiejar.CookieJar()
    web = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))
    page = web.open(WEB + '/Account/Login').read().decode()
    post_form(web, WEB + '/Account/Login', {'__RequestVerificationToken': smoke.antiforgery_token(page),
              'Email': address, 'Password': 'ProfilePass2026'})
    return web, jar


def form(web):
    page = web.open(WEB + '/Account/Profile').read().decode()
    proof = re.search(r'name="profileLoadToken" value="([^"]+)"', page)
    assert proof, 'A loaded profile must carry its protected load proof.'
    return {'__RequestVerificationToken': smoke.antiforgery_token(page),
            'profileLoadToken': html.unescape(proof.group(1))}, page


def snapshot():
    # Never print account data, password hashes, secrets, or this digest.
    return sql_scalar("""SELECT CONVERT(varchar(64), HASHBYTES('SHA2_256', (
        SELECT a.*, (SELECT r.MaVaiTro FROM TAIKHOAN_VAITRO r WHERE r.MaTK=a.MaTK ORDER BY r.MaVaiTro FOR JSON PATH) AS Roles
        FROM TAIKHOAN a ORDER BY a.MaTK FOR JSON PATH)), 2)""")


def run():
    original, session = create()
    other_email, other = create()
    token = session['token']
    account_id = session['user']['id']
    data = {'name': 'Khách đã cập nhật', 'email': original, 'phoneNumber': '012345678901234'}
    before_protected = sql_scalar(f"SELECT CONCAT(MatKhauBam, '|', TrangThai, '|', (SELECT STRING_AGG(CONVERT(varchar, MaVaiTro), ',') FROM TAIKHOAN_VAITRO WHERE MaTK={account_id})) FROM TAIKHOAN WHERE MaTK={account_id}")
    api(API + '/api/auth/profile', expected=401)
    api(API + '/api/auth/profile', 'PUT', data, expected=401)
    api(API + '/api/auth/profile', 'PUT', data, token=token)
    check(api(API + '/api/auth/profile', token=token)['phoneNumber'] == '012345678901234', 'Phone leading zero or 15-digit limit failed.')
    for invalid in [dict(name=' '), dict(name='A'), dict(name='N'*101), dict(email='invalid'),
                    dict(email='a'*255+'@test.example'), dict(email='é@example.test'),
                    dict(phoneNumber='+84901234567'), dict(phoneNumber='09abc'), dict(phoneNumber='1'*16), dict(phoneNumber='١٢٣')]:
        result = api(API + '/api/auth/profile', 'PUT', data | invalid, token=token, expected=400)
        if isinstance(result, str):
            result = json.loads(result)  # RFC problem+json from ValidationProblem.
        check(bool(result['errors']), 'API must return field validation errors.')
    api(API + '/api/auth/profile', 'PUT', data | {'email': other_email.upper()}, token=token, expected=409)
    check(api(API + '/api/auth/profile', token=other['token'])['name'] == other['user']['name'], 'Duplicate email changed the other account.')

    trip = api(API + '/api/itineraries', 'POST', {'name': 'SCR21 ownership', 'startDate': (datetime.date.today()+datetime.timedelta(days=1)).isoformat(), 'guests': 1, 'destinationIds': [1]}, token=token)
    changed = email()
    result = api(API + '/api/auth/profile', 'PUT', data | {'email': ' '+changed.upper()+' ', 'id': other['user']['id'], 'accountId': other['user']['id'], 'roles': ['Admin'], 'trangThai': 'Locked', 'password': 'Hacked2026'}, token=token)
    check(result['id'] == account_id and result['email'] == changed, 'Token identity / email normalization failed.')
    check(api(API + '/api/auth/profile', token=other['token'])['email'] == other_email, 'An injected ID changed another account.')
    check(api(API + '/api/itineraries', token=token) == [trip], 'Email change lost itinerary ownership.')
    reclaimed = api(API + '/api/auth/register', 'POST', {'name': 'Khách lấy email cũ', 'email': original, 'password': 'ProfilePass2026', 'confirmPassword': 'ProfilePass2026', 'termsAccepted': True}, expected=201)
    check(api(API + '/api/itineraries', token=reclaimed['token']) == [], 'A reclaimed email inherited another account itinerary.')
    api(API + '/api/auth/login', 'POST', {'email': changed, 'password': 'ProfilePass2026'})
    api(API + '/api/auth/profile', 'PUT', data | {'email': changed, 'phoneNumber': '  '}, token=token)
    check(sql_scalar(f"SELECT CASE WHEN SoDienThoai IS NULL THEN 'PASS' ELSE 'FAIL' END FROM TAIKHOAN WHERE MaTK={account_id}") == 'PASS', 'Cleared phone is not SQL NULL.')
    after_protected = sql_scalar(f"SELECT CONCAT(MatKhauBam, '|', TrangThai, '|', (SELECT STRING_AGG(CONVERT(varchar, MaVaiTro), ',') FROM TAIKHOAN_VAITRO WHERE MaTK={account_id})) FROM TAIKHOAN WHERE MaTK={account_id}")
    check(before_protected == after_protected, 'Password, status or roles changed during profile update.')

    web, jar = web_login(changed)
    fields, page = form(web)
    check('Hồ sơ tài khoản' in html.unescape(page), 'Profile heading missing.')
    # Direct HTTP posts exercise server validation without any JavaScript.
    _, invalid = post_form(web, WEB + '/Account/Profile', fields | {'Name': ' ', 'Email': 'invalid', 'PhoneNumber': 'abc'})
    check('Vui lòng nhập họ tên.' in html.unescape(invalid), 'No-JS server name validation failed.')
    _, duplicate = post_form(web, WEB + '/Account/Profile', fields | {'Name': 'Giữ dữ liệu nhập', 'Email': other_email, 'PhoneNumber': '0901'})
    check('Email đã được sử dụng.' in html.unescape(duplicate) and 'Giữ dữ liệu nhập' in html.unescape(duplicate), '409 did not retain form values / email field error.')
    post_form(web, WEB + '/Account/Profile', {'Name': 'Không antiforgery', 'Email': changed}, expected=400)
    # A POST without a valid load proof can only reload, never update SQL.
    post_form(web, WEB + '/Account/Profile', {'__RequestVerificationToken': fields['__RequestVerificationToken'], 'Name': 'Không được ghi', 'Email': changed})
    check(api(API + '/api/auth/profile', token=token)['name'] == data['name'], 'Unloaded form overwrote SQL.')
    post_form(web, WEB + '/Account/Profile', fields | {'profileLoadToken': 'tampered-proof', 'Name': 'Không được ghi', 'Email': changed})
    check(api(API + '/api/auth/profile', token=token)['name'] == data['name'], 'Tampered load proof overwrote SQL.')
    other_web, _ = web_login(other_email)
    other_fields, _ = form(other_web)
    post_form(other_web, WEB+'/Account/Profile', other_fields | {'profileLoadToken': fields['profileLoadToken'], 'Name': 'Không được ghi', 'Email': other_email})
    check(api(API+'/api/auth/profile', token=other['token'])['name'] == other['user']['name'], 'A load proof from another account was accepted.')
    _, success = post_form(web, WEB + '/Account/Profile', fields | {'Name': 'Tên mới trên header', 'Email': changed, 'PhoneNumber': ''})
    check('Đã lưu thay đổi hồ sơ tài khoản.' in html.unescape(success), 'Web save success missing.')
    check('Tên mới trên header' in html.unescape(web.open(WEB+'/Trips').read().decode()), 'Cookie name claim did not update.')
    check('SCR21 ownership' in html.unescape(web.open(WEB+'/Trips').read().decode()), 'Web trip link lost ownership.')
    legacy_phone = '090-123-4567'
    smoke.sql_non_query(f"UPDATE TAIKHOAN SET SoDienThoai={smoke.sql_literal(legacy_phone)} WHERE MaTK={account_id}")
    fields, legacy_page = form(web)
    check(legacy_phone in html.unescape(legacy_page), 'Existing formatted phone did not display.')
    check('Số cũ được giữ nguyên' in html.unescape(legacy_page), 'Legacy phone guidance missing.')
    _, legacy_saved = post_form(web, WEB+'/Account/Profile', fields | {'Name': 'Tên lưu cùng số cũ', 'Email': changed, 'PhoneNumber': legacy_phone})
    check('Đã lưu thay đổi hồ sơ' in html.unescape(legacy_saved), 'Saving another field with unchanged legacy phone failed.')
    check(api(API+'/api/auth/profile', token=token)['phoneNumber'] == legacy_phone, 'Unchanged legacy phone was rewritten.')
    invalid_legacy = api(API+'/api/auth/profile', 'PUT', {'name':'Tên mới','email':changed,'phoneNumber':'090-000-9999'}, token=token, expected=400)
    if isinstance(invalid_legacy, str): invalid_legacy = json.loads(invalid_legacy)
    check(bool(invalid_legacy['errors']), 'New formatted phone should be rejected.')
    api(API+'/api/auth/profile', 'PUT', {'name':'Tên mới','email':changed,'phoneNumber':'0001234567'}, token=token)
    check(api(API+'/api/auth/profile', token=token)['phoneNumber'] == '0001234567', 'Replacing legacy phone with digits failed.')
    api(API + '/api/auth/logout', 'POST', token=token, expected=204)
    return changed


def run_outage(processes):
    address, session = create()
    web, _ = web_login(address)
    fields, _ = form(web)
    processes[0].terminate()
    processes[0].wait(timeout=10)
    page = web.open(WEB + '/Account/Profile').read().decode()
    check('Chưa tải được hồ sơ' in html.unescape(page) and 'data-profile-form' not in page, 'Outage exposed a writable fallback form.')
    _, failed = post_form(web, WEB+'/Account/Profile', fields | {'Name': 'Không được lưu offline', 'Email': address, 'PhoneNumber': '012'})
    check('Chưa kết nối được API.' in html.unescape(failed), 'Outage POST message missing.')
    check(sql_scalar(f"SELECT CASE WHEN HoTen=N{smoke.sql_literal(session['user']['name'])} THEN 'PASS' ELSE 'FAIL' END FROM TAIKHOAN WHERE Email={smoke.sql_literal(address)}") == 'PASS', 'Outage wrote SQL.')


def run_expiry():
    address, _ = create()
    get_web, get_jar = web_login(address)
    post_web, post_jar = web_login(address)
    fields, _ = form(post_web)
    time.sleep(11)
    destination = get_web.open(WEB + '/Account/Profile').geturl()
    check('/Account/Login?' in destination and 'returnUrl=%2FAccount%2FProfile' in destination, 'Expired GET must redirect with internal returnUrl.')
    destination, _ = post_form(post_web, WEB+'/Account/Profile', fields | {'Name': 'Expired', 'Email': address})
    check('/Account/Login?' in destination, 'Expired POST must redirect to Login.')
    for jar in [get_jar, post_jar]:
        check(not any(cookie.name == 'DongNamTrip.Auth' for cookie in jar), '401 must end Web authentication cookie.')


if __name__ == '__main__':
    baseline = snapshot()
    try:
        with smoke.running_apps() as processes:
            run()
            run_outage(processes)
        smoke.cleanup_test_accounts()
        smoke.test_emails.clear()
        with smoke.running_apps(session_lifetime_seconds=10):
            run_expiry()
    finally:
        smoke.cleanup_test_accounts()
        check(snapshot() == baseline, 'Existing SQL accounts / roles changed or test cleanup failed.')
    import auth_screens
    print(f'PASS: {auth_screens.checks} SCR21 HTTP/SQL assertions; no existing account or role changes; generated test data cleaned.')
