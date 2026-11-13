    sql_non_query(f'''
INSERT INTO TAIKHOAN_VAITRO (MaTK, MaVaiTro)
SELECT account.MaTK, role.MaVaiTro
FROM TAIKHOAN AS account
CROSS JOIN VAITRO AS role
WHERE account.Email = {sql_literal(staff_email)}
  AND role.TenVaiTro IN ('Admin', 'Editor');
''')
    staff = request(API + '/api/auth/login', 'POST', {'email': staff_email, 'password': staff_password})
    assert set(staff['user']['roles']) == {'Admin', 'Customer', 'Editor'}
    assert set(request(API + '/api/auth/me', token=staff['token'])['roles']) == {'Admin', 'Customer', 'Editor'}
    # A role change through administration revokes existing sessions immediately.
    target_id = other['user']['id']
    request(API + f'/api/admin/users/{target_id}/roles', 'PUT', {'roles': ['Customer', 'Operator']}, token=staff['token'])
    request(API + '/api/auth/me', token=other['token'], expected=401)
    other = request(API + '/api/auth/login', 'POST', {'email': other_email, 'password': other_password})
    assert set(other['user']['roles']) == {'Customer', 'Operator'}
    request(API + f'/api/admin/users/{target_id}/roles', 'PUT', {'roles': ['Customer']}, token=staff['token'])
    request(API + '/api/auth/me', token=other['token'], expected=401)
    other = request(API + '/api/auth/login', 'POST', {'email': other_email, 'password': other_password})
    assert other['user']['roles'] == ['Customer']
    request(API + '/api/admin/quote-requests', token=other['token'], expected=403)
    request(API + f'/api/admin/accounts/{target_id}/roles', 'PUT', {'roles': ['Customer', 'Operator'], 'expectedRoles': ['Customer']}, token=staff['token'], expected=204)
    request(API + '/api/auth/me', token=other['token'], expected=401)
    other = request(API + '/api/auth/login', 'POST', {'email': other_email, 'password': other_password})
    assert set(other['user']['roles']) == {'Customer', 'Operator'}
    request(API + f'/api/admin/accounts/{target_id}/roles', 'PUT', {'roles': ['Customer'], 'expectedRoles': ['Customer']}, token=staff['token'], expected=409)
    request(API + f'/api/admin/accounts/{target_id}/roles', 'PUT', {'roles': ['Customer'], 'expectedRoles': ['Customer', 'Operator']}, token=staff['token'], expected=204)
    request(API + '/api/auth/me', token=other['token'], expected=401)
    other = request(API + '/api/auth/login', 'POST', {'email': other_email, 'password': other_password})
    assert other['user']['roles'] == ['Customer']
    request(API + f"/api/admin/users/{staff['user']['id']}/roles", 'PUT', {'roles': ['Customer']}, token=staff['token'], expected=409)
    planner = request(API + '/api/planner/catalog')
    assert len(planner['destinations']) >= 2
    request(API + f"/api/itineraries/{trip['id']}", token=other['token'], expected=404)
    request(API + f"/api/itineraries/{trip['id']}", 'PUT', itinerary, token=other['token'], expected=404)
    # Yêu cầu tour riêng và báo giá: xem quotation_flow.py.
    request(API + '/api/destinations', 'POST', {'name': 'Điểm kiểm tra quyền', 'region': 'Tây Ninh', 'category': 'Văn hóa', 'address': 'Chỉ bộ nhớ', 'description': 'Xác nhận Admin hoặc Editor được phép.', 'visitMinutes': 60}, token=staff['token'])
    request(API + '/api/admin/quote-requests', token=staff['token'])

    locked_email, locked_password, locked = registration('Tài khoản khóa')
    sql_non_query(f"UPDATE TAIKHOAN SET TrangThai = 'Locked' WHERE Email = {sql_literal(locked_email)};")
    request(API + '/api/auth/me', token=locked['token'], expected=401)
    request(API + '/api/auth/login', 'POST', {'email': locked_email, 'password': locked_password}, expected=401)

    jar = http.cookiejar.CookieJar()
    web = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))
    web_email = f'auth-test-web-{uuid.uuid4().hex}@example.test'
    test_emails.append(web_email)
    register_page = request(WEB + '/Account/Register', opener=web)
    register_token = antiforgery_token(register_page)
    form_request(WEB + '/Account/Register', {
        '__RequestVerificationToken': register_token,
        'Name': 'Khách Web', 'Email': web_email, 'Password': 'WebPass!2026', 'ConfirmPassword': 'WebPass!2026', 'TermsAccepted': 'true',
    }, opener=web)
    trips_page = request(WEB + '/Trips', opener=web)
    assert 'Chuyến đi của tôi' in html.unescape(trips_page)
    logout_token = antiforgery_token(trips_page)
    form_request(WEB + '/Account/Logout', {'__RequestVerificationToken': logout_token}, opener=web)
    login_page = request(WEB + '/Account/Login', opener=web)
    login_token = antiforgery_token(login_page)
    form_request(WEB + '/Account/Login', {
        '__RequestVerificationToken': login_token,
        'Email': web_email, 'Password': 'WebPass!2026',
    }, opener=web)
    assert 'Chuyến đi của tôi' in html.unescape(request(WEB + '/Trips', opener=web))

    request(API + '/api/auth/logout', 'POST', token=customer_token, expected=204)
    request(API + '/api/auth/me', token=customer_token, expected=401)
    print(f'PASS: {checks} functional HTTP checks; registration, role-change session revocation, lock, logout, ownership and Web login.')


def run_expiry_checks(session_lifetime_seconds):
    initial_checks = checks
    expiring_email, _, expiring = registration('Tài khoản hết phiên')
    me = request(API + '/api/auth/me', token=expiring['token'])
    assert me['email'] == expiring_email, 'The new token must be valid before expiry.'
    time.sleep(session_lifetime_seconds + 1)
    request(API + '/api/auth/me', token=expiring['token'], expected=401)
    assert expiring_email in test_emails
    print(f'PASS: {checks - initial_checks} expiry HTTP checks; the same SQL-account token returns 200 before expiry and 401 after expiry.')


if __name__ == '__main__':
    try:
        if '--start' in sys.argv:
            try:
                with running_apps():
                    run()
            finally:
                cleanup_test_accounts()
                test_emails.clear()
            # The functional fixture has stopped and released its ports before
            # starting a fresh API process with a short-lived, newly issued token.
            expiry_lifetime_seconds = 10
            with running_apps(session_lifetime_seconds=expiry_lifetime_seconds, api_only=True):
                run_expiry_checks(expiry_lifetime_seconds)
        else:
            run()
            print('SKIP: isolated expiry fixture requires smoke.py --start.')
    finally:
        cleanup_test_accounts()
