"""SCR22–SCR25 real API/SQL checks with per-run accounts and exact cleanup."""
import html
import http.cookiejar
import time
import urllib.request

import smoke
from auth_screens import api, check, post_form
from profile import snapshot

API, WEB = smoke.API, smoke.WEB


def web_login(address, password):
    jar = http.cookiejar.CookieJar()
    web = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))
    page = web.open(WEB + '/Account/Login').read().decode()
    post_form(web, WEB + '/Account/Login', {'__RequestVerificationToken': smoke.antiforgery_token(page), 'Email': address, 'Password': password})
    return web


def run_first():
    address, password, a = smoke.registration('SCR22 25 chủ tài khoản')
    other_address, _, b = smoke.registration('SCR22 25 tài khoản khác')
    token, other = a['token'], b['token']
    second = api(API+'/api/auth/login', 'POST', {'email':address,'password':password})['token']
    sessions = api(API+'/api/auth/sessions', token=token)
    check(len(sessions) == 2 and sum(row['isCurrent'] for row in sessions) == 1, 'Active sessions not identified.')
    api(API+'/api/auth/sessions/'+sessions[0]['id'], 'DELETE', token=other, expected=404)
    check(len(api(API+'/api/auth/sessions', token=token)) == 2, 'Other account revoked a session.')

    check(api(API+'/api/account/payments', token=token) == [], 'Empty real payment list expected.')
    api(API+'/api/account/payments/999999', token=token, expected=404)
    check(api(API+'/api/account/payment-choice', token=token)['methodCode'] is None, 'Payment choice should begin empty.')
    api(API+'/api/account/payment-choice', expected=401)
    api(API+'/api/account/payment-choice', 'PUT', {'methodCode':'card'}, token=token, expected=400)
    api(API+'/api/account/payment-choice', 'PUT', {'methodCode':'bank_transfer'}, token=token)
    check(api(API+'/api/account/payment-choice', token=token)['methodCode'] == 'bank_transfer', 'Payment choice did not save.')
    check(api(API+'/api/account/payment-choice', token=other)['methodCode'] is None, 'Payment choice crossed accounts.')
    api(API+'/api/account/payment-choice', 'PUT', {'methodCode':'cash'}, token=other)
    check(api(API+'/api/account/payment-choice', token=token)['methodCode'] == 'bank_transfer', 'Other account changed payment choice.')
    for path in ['collections','notices','preferences']:
        api(API+'/api/account/'+path, expected=401)
    check(api(API+'/api/account/collections', token=token)['favorites'] == [], 'Favorites not empty initially.')
    check(api(API+'/api/account/notices', token=token) == [], 'Notices not empty initially.')

    api(API+'/api/account/favorites/tour/1', 'PUT', token=token, expected=204)
    api(API+'/api/account/favorites/destination/2', 'PUT', token=token, expected=204)
    api(API+'/api/account/favorites/tour/99999', 'PUT', token=token, expected=404)
    check(len(api(API+'/api/account/collections', token=token)['favorites']) == 2, 'Favorites did not persist to SQL.')
    check(api(API+'/api/account/collections', token=other)['favorites'] == [], 'Favorites crossed accounts.')
    api(API+'/api/account/favorites/tour/1', 'DELETE', token=other, expected=204)
    check(len(api(API+'/api/account/collections', token=token)['favorites']) == 2, 'Other account deleted favorite.')
    api(API+'/api/account/activity', 'POST', {'kind':'search','query':'núi xanh'}, token=token, expected=204)
    api(API+'/api/account/activity', 'POST', {'kind':'tour','itemId':1}, token=token, expected=204)
    api(API+'/api/account/activity', 'POST', {'kind':'destination','itemId':2}, token=token, expected=204)
    for invalid in [{'kind':'search','query':' '},{'kind':'tour','itemId':9999},{'kind':'unknown','itemId':1}]:
        api(API+'/api/account/activity', 'POST', invalid, token=token, expected=400)
    collection = api(API+'/api/account/collections', token=token)
    check(len(collection['searches']) == 1 and len(collection['viewed']) == 2, 'Activity tabs incomplete.')
    check(api(API+'/api/account/collections', token=other)['viewed'] == [], 'Activity crossed accounts.')
    api(API+'/api/account/activity/'+str(collection['viewed'][0]['id']), 'DELETE', token=other, expected=204)
    check(len(api(API+'/api/account/collections', token=token)['viewed']) == 2, 'Other account deleted viewed history.')

    api(API+'/api/account/preferences', 'PUT', {'securityNotices':False}, token=token)
    check(api(API+'/api/account/preferences', token=token)['securityNotices'] is False, 'Preference not saved.')
    check(api(API+'/api/account/preferences', token=other)['securityNotices'] is True, 'Preference crossed accounts.')
    api(API+'/api/auth/password', 'POST', {'currentPassword':'wrong','newPassword':'NewPass2026','confirmNewPassword':'NewPass2026'}, token=token, expected=422)
    api(API+'/api/auth/password', 'POST', {'currentPassword':password,'newPassword':'short','confirmNewPassword':'short'}, token=token, expected=400)
    api(API+'/api/auth/password', 'POST', {'currentPassword':password,'newPassword':'NewPass2026','confirmNewPassword':'NewPass2026'}, token=token, expected=204)
    api(API+'/api/auth/me', token=token, expected=401)
    api(API+'/api/auth/me', token=second, expected=401)
    api(API+'/api/auth/login', 'POST', {'email':address,'password':password}, expected=401)
    token = api(API+'/api/auth/login', 'POST', {'email':address,'password':'NewPass2026'})['token']
    check(api(API+'/api/account/notices', token=token) == [], 'Disabled security notices still generated an event.')
    api(API+'/api/account/preferences', 'PUT', {'securityNotices':True}, token=token)
    api(API+'/api/auth/password', 'POST', {'currentPassword':'NewPass2026','newPassword':'NewestPass2026','confirmNewPassword':'NewestPass2026'}, token=token, expected=204)
    token = api(API+'/api/auth/login', 'POST', {'email':address,'password':'NewestPass2026'})['token']
    notices = api(API+'/api/account/notices', token=token)
    check(len(notices) == 1 and notices[0]['readAt'] is None, 'Real password event did not generate unread notice.')
    api(API+'/api/account/notices/'+str(notices[0]['id'])+'/read', 'PUT', token=other, expected=404)
    check(api(API+'/api/account/notices', token=token)[0]['readAt'] is None, 'Other account marked notice read.')
    api(API+'/api/account/notices/'+str(notices[0]['id'])+'/read', 'PUT', token=token, expected=204)
    check(api(API+'/api/account/notices', token=token)[0]['readAt'] is not None, 'Read state not persisted.')

    web = web_login(address, 'NewestPass2026')
    for page in ['Security','Payments','Favorites','Notices']:
        body = html.unescape(web.open(WEB+'/Account/'+page).read().decode())
        check('account-layout' in body and '<h1' in body, 'Account page failed to render: '+page)
    security = web.open(WEB+'/Account/Security').read().decode()
    _, invalid = post_form(web, WEB+'/Account/Security', {'__RequestVerificationToken':smoke.antiforgery_token(security), 'CurrentPassword':'wrong','NewPassword':'AgainPass2026','ConfirmNewPassword':'AgainPass2026'})
    check('Mật khẩu hiện tại không đúng.' in html.unescape(invalid), 'Web current password error missing.')
    check('AgainPass2026' not in invalid, 'Web echoed a submitted password.')
    return address, other_address


def run_after_restart(address, other_address):
    token = api(API+'/api/auth/login', 'POST', {'email':address,'password':'NewestPass2026'})['token']
    other = api(API+'/api/auth/login', 'POST', {'email':other_address,'password':'SmokePass!2026'})['token']
    collections = api(API+'/api/account/collections', token=token)
    check(len(collections['favorites']) == 2 and len(collections['viewed']) == 2 and len(collections['searches']) == 1, 'SQL collection not durable after API restart.')
    check(api(API+'/api/account/collections', token=other)['favorites'] == [], 'Restart leaked favorites to another account.')
    check(api(API+'/api/account/preferences', token=token)['securityNotices'] is True, 'Preference not durable.')
    check(api(API+'/api/account/payment-choice', token=token)['methodCode'] == 'bank_transfer', 'Payment choice not durable.')
    check(api(API+'/api/account/payment-choice', token=other)['methodCode'] == 'cash', 'Other owner choice not durable.')
    check(api(API+'/api/account/notices', token=token)[0]['readAt'] is not None, 'Read notice not durable.')
    api(API+'/api/account/activity?kind=search','DELETE',token=token,expected=204)
    api(API+'/api/account/activity?kind=viewed','DELETE',token=token,expected=204)
    api(API+'/api/account/favorites/tour/1','DELETE',token=token,expected=204)
    api(API+'/api/account/favorites/destination/2','DELETE',token=token,expected=204)
    check(api(API+'/api/account/collections', token=token)['favorites'] == [], 'Clear favorites failed.')


def run_outage(address, processes):
    browsers = [(page, web_login(address, 'NewestPass2026')) for page in ['Security','Payments','Favorites','Notices']]
    processes[0].terminate()
    processes[0].wait(timeout=10)
    for page, web in browsers:
        body = html.unescape(web.open(WEB+'/Account/'+page).read().decode())
        check('Thử lại' in body and 'Chưa tải được' in body, page+' did not show an outage/retry state.')


def run_expiry(address):
    browsers = [(page, web_login(address, 'NewestPass2026')) for page in ['Security','Payments','Favorites','Notices']]
    time.sleep(11)
    for page, web in browsers:
        destination = web.open(WEB+'/Account/'+page).geturl()
        check('/Account/Login?' in destination and 'returnUrl=%2FAccount%2F'+page in destination, page+' did not end an expired session safely.')


if __name__ == '__main__':
    baseline = snapshot()
    try:
        with smoke.running_apps():
            address, other_address = run_first()
        with smoke.running_apps():
            run_after_restart(address, other_address)
        with smoke.running_apps() as processes:
            run_outage(address, processes)
        with smoke.running_apps(session_lifetime_seconds=10):
            run_expiry(address)
    finally:
        smoke.cleanup_test_accounts()
        check(snapshot() == baseline, 'SCR22–SCR25 changed existing accounts/roles or left test data.')
    import auth_screens
    print(f'PASS: {auth_screens.checks} SCR22–SCR25 API/Web/SQL assertions; two owners, password/session revocation, SQL persistence after restart, cleanup.')
