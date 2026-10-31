"""HTTP/SQL smoke checks specific to SCR02 login and SCR03 registration.

The script only creates accounts with a per-run ``scr02scr03-`` prefix and
removes those exact accounts in ``finally``.  It deliberately never prints a
password, token, connection string, or password hash.
"""
from concurrent.futures import ThreadPoolExecutor
import html
import http.cookiejar
import json
import re
import subprocess
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid

import smoke


API, WEB = smoke.API, smoke.WEB
checks = 0


def check(condition, message):
    global checks
    assert condition, message
    checks += 1


def api(path, method="GET", data=None, token=None, expected=200):
    global checks
    headers = {}
    payload = None
    if data is not None:
        payload = json.dumps(data).encode()
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = "Bearer " + token
    request = urllib.request.Request(path, data=payload, method=method, headers=headers)
    try:
        response = urllib.request.urlopen(request, timeout=15)
    except urllib.error.HTTPError as error:
        response = error
    body = response.read().decode()
    check(response.status == expected, f"{method} {path}: expected {expected}, got {response.status}: {body[:150]}")
    return json.loads(body) if body and response.headers.get("Content-Type", "").startswith("application/json") else body


def post_form(opener, path, values, expected=200):
    global checks
    request = urllib.request.Request(
        path,
        data=urllib.parse.urlencode(values).encode(),
        method="POST",
        headers={"Content-Type": "application/x-www-form-urlencoded"},
    )
    try:
        response = opener.open(request, timeout=15)
    except urllib.error.HTTPError as error:
        response = error
    body = response.read().decode()
    check(response.status == expected, f"POST {path}: expected {expected}, got {response.status}: {body[:150]}")
    return response.geturl(), body


def new_email(label):
    email = f"scr02scr03-{label}-{uuid.uuid4().hex}@example.test"
    smoke.test_emails.append(email)
    return email


def registration_payload(initial_email, **overrides):
    value = {
        "name": "Khách kiểm thử SCR03",
        "email": initial_email,
        "password": "ScreenPass2026",
        "confirmPassword": "ScreenPass2026",
        "termsAccepted": True,
    }
    value.update(overrides)
    return value


def sql_scalar(statement):
    script = f'''$ProgressPreference = 'SilentlyContinue'
$connectionString = [Environment]::GetEnvironmentVariable('DONGNAMTRIP_TEST_CONNECTION')
$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
try {{
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = @'
{statement}
'@
    [Console]::Write($command.ExecuteScalar())
}}
finally {{
    if ($connection) {{ $connection.Dispose() }}
}}
'''
    encoded = smoke.base64.b64encode(script.encode("utf-16le")).decode()
    environment = smoke.os.environ.copy()
    environment["DONGNAMTRIP_TEST_CONNECTION"] = smoke.user_secrets_connection_string()
    completed = subprocess.run(
        ["powershell", "-NoProfile", "-NonInteractive", "-OutputFormat", "Text", "-EncodedCommand", encoded],
        cwd=smoke.ROOT,
        env=environment,
        check=True,
        capture_output=True,
        text=True,
    )
    return completed.stdout.strip()


def text(page):
    return html.unescape(page)


def form_token(opener, path):
    page = api(path, expected=200) if opener is None else opener.open(path, timeout=15).read().decode()
    return smoke.antiforgery_token(page), page


def run_api_checks():
    invalid_cases = [
        registration_payload("bad-empty@example.test", name="", email="", password="", confirmPassword="", termsAccepted=False),
        registration_payload("bad-space@example.test", name="   "),
        registration_payload("bad-long-name@example.test", name="N" * 101),
        registration_payload("bad-email@example.test", email="not-an-email"),
        registration_payload("bad-long-email@example.test", email=("a" * 245) + "@example.test"),
        registration_payload("bad-short-password@example.test", password="Abc1234", confirmPassword="Abc1234"),
        registration_payload("bad-letter-password@example.test", password="abcdefgh", confirmPassword="abcdefgh"),
        registration_payload("bad-digit-password@example.test", password="12345678", confirmPassword="12345678"),
        registration_payload("bad-confirm@example.test", confirmPassword="OtherPass2026"),
        registration_payload("bad-terms@example.test", termsAccepted=False),
        registration_payload("bad-phone-alpha@example.test", phoneNumber="09012abc"),
        registration_payload("bad-phone-long@example.test", phoneNumber="0" * 16),
        registration_payload("bad-phone-unicode@example.test", phoneNumber="١٢٣"),
    ]
    for payload in invalid_cases:
        api(API + "/api/auth/register", "POST", payload, expected=400)

    no_phone_email = new_email("null-phone")
    no_phone = api(API + "/api/auth/register", "POST", registration_payload(no_phone_email), expected=201)
    no_phone_profile = api(API + "/api/auth/profile", token=no_phone["token"])
    check(no_phone_profile["phoneNumber"] is None, "Empty registration phone must be SQL NULL.")

    phone_email = new_email("phone")
    phone = api(API + "/api/auth/register", "POST", registration_payload(phone_email, phoneNumber="012345678901234"), expected=201)
    phone_profile = api(API + "/api/auth/profile", token=phone["token"])
    check(phone_profile["phoneNumber"] == "012345678901234", "Phone must retain its leading zero.")

    injected_email = new_email("role-injection")
    injected = api(API + "/api/auth/register", "POST", registration_payload(
        injected_email, roles=["Admin"], role="Admin", trangThai="Locked"), expected=201)
    check(injected["user"]["roles"] == ["Customer"], "Public registration must only grant Customer.")
    injected_me = api(API + "/api/auth/me", token=injected["token"])
    check(injected_me["roles"] == ["Customer"], "Injected role must not appear in session claims.")
    check(sql_scalar(f"SELECT CASE WHEN TrangThai = 'Active' AND MatKhauBam <> 'ScreenPass2026' AND LEN(MatKhauBam) > 20 THEN 'PASS' ELSE 'FAIL' END FROM TAIKHOAN WHERE Email = {smoke.sql_literal(injected_email)}") == "PASS", "SQL account must be active and store a hash, not the clear password.")

    duplicate_email = new_email("duplicate")
    api(API + "/api/auth/register", "POST", registration_payload(duplicate_email), expected=201)
    api(API + "/api/auth/register", "POST", registration_payload(duplicate_email.upper()), expected=409)

    race_email = new_email("race")
    race_payload = registration_payload(race_email)
    with ThreadPoolExecutor(max_workers=2) as executor:
        statuses = sorted(executor.map(lambda _: smoke.request_status(API + "/api/auth/register", "POST", race_payload), range(2)))
    check(statuses == [201, 409], f"Concurrent registration must produce 201/409, got {statuses}.")
    check(sql_scalar(f"SELECT COUNT(*) FROM TAIKHOAN WHERE Email = {smoke.sql_literal(race_email)}") == "1", "Concurrent registration created more than one account.")

    api(API + "/api/auth/login", "POST", {"email": "unknown-scr02@example.test", "password": "WrongPass2026"}, expected=401)
    api(API + "/api/auth/login", "POST", {"email": no_phone_email, "password": "WrongPass2026"}, expected=401)
    logged_in = api(API + "/api/auth/login", "POST", {"email": no_phone_email.upper(), "password": "ScreenPass2026"})
    check(logged_in["user"]["email"] == no_phone_email, "Login must normalize email.")
    api(API + "/api/auth/logout", "POST", token=logged_in["token"], expected=204)
    api(API + "/api/auth/me", token=logged_in["token"], expected=401)

    locked_email = new_email("locked")
    locked = api(API + "/api/auth/register", "POST", registration_payload(locked_email), expected=201)
    smoke.sql_non_query(f"UPDATE TAIKHOAN SET TrangThai = 'Locked' WHERE Email = {smoke.sql_literal(locked_email)};")
    api(API + "/api/auth/login", "POST", {"email": locked_email, "password": "ScreenPass2026"}, expected=401)
    api(API + "/api/auth/me", token=locked["token"], expected=401)


def run_web_checks():
    jar = http.cookiejar.CookieJar()
    web = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))

    login_page = web.open(WEB + "/Account/Login?returnUrl=/Trips", timeout=15).read().decode()
    check('value="/Trips"' in login_page and "returnUrl=%2FTrips" in login_page, "Internal returnUrl must survive Login to Register.")
    login_token = smoke.antiforgery_token(login_page)
    _, blank_login = post_form(web, WEB + "/Account/Login", {
        "__RequestVerificationToken": login_token, "Email": "", "Password": ""
    })
    check("Vui lòng nhập email" in text(blank_login) and "Vui lòng nhập mật khẩu" in text(blank_login), "Login blank-field messages are missing.")

    login_page = web.open(WEB + "/Account/Login", timeout=15).read().decode()
    login_token = smoke.antiforgery_token(login_page)
    _, invalid_email = post_form(web, WEB + "/Account/Login", {
        "__RequestVerificationToken": login_token, "Email": "invalid", "Password": "ScreenPass2026"
    })
    check("Email không đúng định dạng." in text(invalid_email), "Invalid-email login message is missing.")

    login_page = web.open(WEB + "/Account/Login", timeout=15).read().decode()
    login_token = smoke.antiforgery_token(login_page)
    _, failed_login = post_form(web, WEB + "/Account/Login", {
        "__RequestVerificationToken": login_token, "Email": "unknown-scr02@example.test", "Password": "NeverEchoThis2026"
    })
    failed_login_text = text(failed_login)
    check("Email hoặc mật khẩu không đúng." in failed_login_text, "Authentication failure must remain generic.")
    check("NeverEchoThis2026" not in failed_login, "Failed login must clear password from rendered ModelState.")

    register_page = web.open(WEB + "/Account/Register?returnUrl=/Trips", timeout=15).read().decode()
    check('value="/Trips"' in register_page and "returnUrl=%2FTrips" in register_page, "Internal returnUrl must survive Register to Login.")
    register_token = smoke.antiforgery_token(register_page)
    _, terms_error = post_form(web, WEB + "/Account/Register", {
        "__RequestVerificationToken": register_token,
        "Name": "Khách điều khoản", "Email": "web-terms@example.test",
        "Password": "ScreenPass2026", "ConfirmPassword": "ScreenPass2026",
    })
    check("Vui lòng đồng ý với điều khoản." in text(terms_error), "Server must reject a direct registration without terms consent.")

    duplicate_email = new_email("web-duplicate")
    api(API + "/api/auth/register", "POST", registration_payload(duplicate_email), expected=201)
    register_page = web.open(WEB + "/Account/Register", timeout=15).read().decode()
    register_token = smoke.antiforgery_token(register_page)
    _, duplicate_page = post_form(web, WEB + "/Account/Register", {
        "__RequestVerificationToken": register_token,
        "Name": "Khách trùng", "Email": duplicate_email.upper(), "Password": "ScreenPass2026",
        "ConfirmPassword": "ScreenPass2026", "TermsAccepted": "true",
    })
    check("Email đã được đăng ký. Bạn có thể đăng nhập." in text(duplicate_page), "Web duplicate-email message is missing.")
    check("ScreenPass2026" not in duplicate_page, "Failed registration must clear password fields from rendered ModelState.")

    web_email = new_email("web-success")
    register_page = web.open(WEB + "/Account/Register?returnUrl=/Trips", timeout=15).read().decode()
    register_token = smoke.antiforgery_token(register_page)
    destination, signed_in_page = post_form(web, WEB + "/Account/Register?returnUrl=/Trips", {
        "__RequestVerificationToken": register_token,
        "Name": "Khách Web SCR03", "Email": web_email, "PhoneNumber": "0901234567",
        "Password": "ScreenPass2026", "ConfirmPassword": "ScreenPass2026", "TermsAccepted": "true",
    })
    check(destination == WEB + "/Trips", f"Registration should safely redirect internally to /Trips, got {destination}.")
    check("Khách Web SCR03" in text(signed_in_page), "Signed-in Web navigation must show the user name.")
    profile = api(API + "/api/auth/login", "POST", {"email": web_email, "password": "ScreenPass2026"})
    check(api(API + "/api/auth/profile", token=profile["token"])["phoneNumber"] == "0901234567", "Web registration must persist phone number to SQL.")

    logout_page = web.open(WEB + "/Trips", timeout=15).read().decode()
    logout_token = smoke.antiforgery_token(logout_page)
    destination, _ = post_form(web, WEB + "/Account/Logout", {"__RequestVerificationToken": logout_token})
    check(destination == WEB + "/", "Web logout must return to home.")

    external_jar = http.cookiejar.CookieJar()
    external_web = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(external_jar))
    page = external_web.open(WEB + "/Account/Login?returnUrl=https://example.invalid/", timeout=15).read().decode()
    check('value=""' in page, "External returnUrl must not be rendered into the form.")
    token = smoke.antiforgery_token(page)
    destination, _ = post_form(external_web, WEB + "/Account/Login?returnUrl=https://example.invalid/", {
        "__RequestVerificationToken": token, "Email": web_email, "Password": "ScreenPass2026"
    })
    check(destination == WEB + "/", f"External returnUrl must redirect home, got {destination}.")


def run():
    run_api_checks()
    run_web_checks()
    print(f"PASS: {checks} SCR02/SCR03 HTTP and SQL assertions.")


if __name__ == "__main__":
    try:
        if "--start" in sys.argv:
            with smoke.running_apps():
                run()
        else:
            run()
    finally:
        smoke.cleanup_test_accounts()
