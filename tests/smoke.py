"""SQL-auth API/Web smoke checks. Uses and removes only generated test accounts."""
import base64
import contextlib
from concurrent.futures import ThreadPoolExecutor
import datetime
import html
import http.cookiejar
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[1]
API, WEB = 'http://localhost:5080', 'http://localhost:5081'
API_EXE = ROOT / 'src' / 'DongNamTrip.Api' / 'bin' / 'Debug' / 'net10.0' / 'DongNamTrip.Api.exe'
WEB_EXE = ROOT / 'src' / 'DongNamTrip.Web' / 'bin' / 'Debug' / 'net10.0' / 'DongNamTrip.Web.exe'
checks = 0
test_emails = []
test_connection_string = None


def request(path, method='GET', data=None, token=None, expected=200, opener=None):
    global checks
    headers = {}
    if data is not None:
        data = json.dumps(data).encode()
        headers['Content-Type'] = 'application/json'
    if token:
        headers['Authorization'] = 'Bearer ' + token
    req = urllib.request.Request(path, data=data, method=method, headers=headers)
    try:
        response = (opener or urllib.request.build_opener()).open(req, timeout=15)
    except urllib.error.HTTPError as error:
        response = error
    body = response.read().decode()
    assert response.status == expected, f'{method} {path}: expected {expected}, got {response.status}: {body[:150]}'
    checks += 1
    return json.loads(body) if response.headers.get('Content-Type', '').startswith('application/json') and body else body


def form_request(path, values, expected=200, opener=None):
    global checks
    body = urllib.parse.urlencode(values).encode()
    req = urllib.request.Request(path, data=body, method='POST', headers={'Content-Type': 'application/x-www-form-urlencoded'})
    try:
        response = (opener or urllib.request.build_opener()).open(req, timeout=15)
    except urllib.error.HTTPError as error:
        response = error
    text = response.read().decode()
    assert response.status == expected, f'POST {path}: expected {expected}, got {response.status}: {text[:150]}'
    checks += 1
    return text


def request_status(path, method, data):
    global checks
    req = urllib.request.Request(path, data=json.dumps(data).encode(), method=method, headers={'Content-Type': 'application/json'})
    try:
        response = urllib.request.build_opener().open(req, timeout=15)
    except urllib.error.HTTPError as error:
        response = error
    response.read()
    checks += 1
    return response.status


def antiforgery_token(page):
    match = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)
    assert match, 'Antiforgery token is missing from the Web form.'
    return html.unescape(match.group(1))


def user_secrets_connection_string():
    global test_connection_string
    if test_connection_string is not None:
        return test_connection_string
    output = subprocess.check_output(
        ['dotnet', 'user-secrets', 'list', '--project', 'src/DongNamTrip.Api'],
        cwd=ROOT,
        text=True,
    )
    match = re.search(r'^ConnectionStrings:DongNamTrip = (.+)$', output, re.MULTILINE)
    assert match, 'ConnectionStrings:DongNamTrip is not configured in User Secrets.'
    test_connection_string = match.group(1).strip()
    return test_connection_string


def sql_non_query(statement):
    script = f'''$ProgressPreference = 'SilentlyContinue'
$connectionString = [Environment]::GetEnvironmentVariable('DONGNAMTRIP_TEST_CONNECTION')
$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
try {{
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = @'
{statement}
'@
    [void]$command.ExecuteNonQuery()
}}
finally {{
    if ($connection) {{ $connection.Dispose() }}
}}
'''
    encoded = base64.b64encode(script.encode('utf-16le')).decode()
    environment = os.environ.copy()
    environment['DONGNAMTRIP_TEST_CONNECTION'] = user_secrets_connection_string()
    subprocess.run(['powershell', '-NoProfile', '-NonInteractive', '-OutputFormat', 'Text', '-EncodedCommand', encoded], cwd=ROOT, env=environment, check=True)


def sql_literal(value):
    return "'" + value.replace("'", "''") + "'"


def cleanup_test_accounts():
    if not test_emails:
        return
    emails = ', '.join(sql_literal(email) for email in test_emails)
    sql_non_query(f'''
DELETE [line] FROM HANGMUCBAOGIA AS [line]
JOIN BAOGIA AS quote ON quote.MaBG = [line].MaBG
JOIN YEUCAUTOURRIENG AS req ON req.MaYC = quote.MaYC
JOIN PHIENBANLICHTRINH AS version ON version.MaPB = req.MaPB
JOIN LICHTRINH AS trip ON trip.MaLT = version.MaLT
JOIN TAIKHOAN AS account ON account.MaTK = trip.MaTK
WHERE account.Email IN ({emails});
DELETE [order] FROM DONDAT AS [order]
JOIN BAOGIA AS quote ON quote.MaBG = [order].MaBG
JOIN YEUCAUTOURRIENG AS req ON req.MaYC = quote.MaYC
JOIN PHIENBANLICHTRINH AS version ON version.MaPB = req.MaPB
JOIN LICHTRINH AS trip ON trip.MaLT = version.MaLT
JOIN TAIKHOAN AS account ON account.MaTK = trip.MaTK
WHERE account.Email IN ({emails});
DELETE quote FROM BAOGIA AS quote
JOIN YEUCAUTOURRIENG AS req ON req.MaYC = quote.MaYC
JOIN PHIENBANLICHTRINH AS version ON version.MaPB = req.MaPB
JOIN LICHTRINH AS trip ON trip.MaLT = version.MaLT
JOIN TAIKHOAN AS account ON account.MaTK = trip.MaTK
WHERE account.Email IN ({emails});
DELETE verification FROM XACMINHDICHVU AS verification
JOIN YEUCAUTOURRIENG AS req ON req.MaYC = verification.MaYC
JOIN PHIENBANLICHTRINH AS version ON version.MaPB = req.MaPB
JOIN LICHTRINH AS trip ON trip.MaLT = version.MaLT
JOIN TAIKHOAN AS account ON account.MaTK = trip.MaTK
WHERE account.Email IN ({emails});
DELETE req FROM YEUCAUTOURRIENG AS req
JOIN PHIENBANLICHTRINH AS version ON version.MaPB = req.MaPB
JOIN LICHTRINH AS trip ON trip.MaLT = version.MaLT
JOIN TAIKHOAN AS account ON account.MaTK = trip.MaTK
WHERE account.Email IN ({emails});
DELETE version FROM PHIENBANLICHTRINH AS version
JOIN LICHTRINH AS trip ON trip.MaLT = version.MaLT
JOIN TAIKHOAN AS account ON account.MaTK = trip.MaTK
WHERE account.Email IN ({emails});
DELETE slot FROM LICHTRINH_SUAT AS slot
JOIN LICHTRINH AS trip ON trip.MaLT = slot.MaLT
JOIN TAIKHOAN AS account ON account.MaTK = trip.MaTK
WHERE account.Email IN ({emails});
DELETE place FROM LICHTRINH_DIADIEM AS place
JOIN LICHTRINH AS trip ON trip.MaLT = place.MaLT
JOIN TAIKHOAN AS account ON account.MaTK = trip.MaTK
WHERE account.Email IN ({emails});
DELETE trip FROM LICHTRINH AS trip
JOIN TAIKHOAN AS account ON account.MaTK = trip.MaTK
WHERE account.Email IN ({emails});
DELETE assignment
FROM TAIKHOAN_VAITRO AS assignment
INNER JOIN TAIKHOAN AS account ON account.MaTK = assignment.MaTK
WHERE account.Email IN ({emails});
DELETE FROM TAIKHOAN WHERE Email IN ({emails});
''')


@contextlib.contextmanager
def running_apps(session_lifetime_seconds=300, *, api_only=False):
    if not isinstance(session_lifetime_seconds, int) or not 1 <= session_lifetime_seconds <= 86_400:
        raise ValueError('Test session lifetime must be an integer between 1 and 86400 seconds.')
    apps = [(API_EXE, API + '/health')]
    if not api_only:
        apps.append((WEB_EXE, WEB + '/Account/Login'))
    assert all(executable.exists() for executable, _ in apps), 'Build the solution before running smoke.py --start.'
    environment = os.environ.copy()
    environment['ASPNETCORE_ENVIRONMENT'] = 'Development'
    environment['ASPNETCORE_URLS'] = 'http://localhost:5080'
    environment['Authentication__SessionLifetimeSeconds'] = str(session_lifetime_seconds)
    environment['BootstrapAdmin__Enabled'] = 'false'
    environment['DevelopmentTestAccounts__Enabled'] = 'false'
    processes, logs = [], []
    try:
        for executable, _ in apps:
            log = tempfile.TemporaryFile()
            logs.append(log)
            process_environment = environment.copy()
            process_environment['ASPNETCORE_URLS'] = 'http://localhost:5080' if executable == API_EXE else 'http://localhost:5081'
            processes.append(subprocess.Popen([str(executable)], cwd=executable.parent, stdout=log, stderr=log, env=process_environment))
        for _, url in apps:
            for _ in range(80):
                try:
                    urllib.request.urlopen(url, timeout=1).close()
                    break
                except (urllib.error.URLError, TimeoutError):
                    if any(process.poll() is not None for process in processes):
                        raise RuntimeError('An application exited before readiness.')
                    time.sleep(.25)
            else:
                raise RuntimeError('Application did not become ready: ' + url)
        yield
    except Exception:
        for log in logs:
            log.seek(0)
            print(log.read().decode(errors='replace')[-5000:], file=sys.stderr)
        raise
    finally:
        for process in processes:
            process.terminate()
        for process in processes:
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()
        for log in logs:
            log.close()


def registration(name, password='SmokePass!2026'):
    email = f'auth-test-{uuid.uuid4().hex}@example.test'
    test_emails.append(email)
    response = request(API + '/api/auth/register', 'POST', {
        'name': name,
        'email': email,
        'password': password,
        'confirmPassword': password,
        'termsAccepted': True,
    }, expected=201)
    assert response['user']['email'] == email
    assert response['user']['roles'] == ['Customer']
    assert 'matKhauBam' not in json.dumps(response).lower()
    return email, password, response


def run():
    health = request(API + '/health/db')
    assert health['tableCount'] == 25 and health['vaiTroCount'] == 6 and health['ngonNguCount'] >= 1
    places = request(API + '/api/destinations')
    assert len(places) >= 6
    request(API + '/api/itineraries', expected=401)

    customer_email, customer_password, customer = registration('Khách kiểm tra')
    customer_token = customer['token']
    me = request(API + '/api/auth/me', token=customer_token)
    assert me['email'] == customer_email and me['roles'] == ['Customer']
    request(API + '/api/auth/register', 'POST', {
        'name': 'Khách trùng', 'email': customer_email.upper(), 'password': customer_password, 'confirmPassword': customer_password, 'termsAccepted': True,
    }, expected=409)
    race_email = f'auth-test-race-{uuid.uuid4().hex}@example.test'
    test_emails.append(race_email)
    race_request = {'name': 'Khách đồng thời', 'email': race_email, 'password': 'RacePass!2026', 'confirmPassword': 'RacePass!2026', 'termsAccepted': True}
    with ThreadPoolExecutor(max_workers=2) as executor:
        race_statuses = sorted(executor.map(lambda _: request_status(API + '/api/auth/register', 'POST', race_request), range(2)))
    assert race_statuses == [201, 409], f'Concurrent registration must create one account only: {race_statuses}'
    request(API + '/api/auth/login', 'POST', {'email': customer_email, 'password': 'WrongPassword!2026'}, expected=401)
    logged_in = request(API + '/api/auth/login', 'POST', {'email': customer_email.upper(), 'password': customer_password})
    request(API + '/api/destinations', 'POST', {'name': 'Không được phép', 'region': 'Tây Ninh', 'category': 'Văn hóa', 'address': 'Test', 'description': 'Không được lưu', 'visitMinutes': 60}, token=logged_in['token'], expected=403)

    tomorrow = (datetime.date.today() + datetime.timedelta(days=1)).isoformat()
    planner_catalog = request(API + '/api/planner/catalog')
    assert len(planner_catalog['destinations']) >= 2
    planner_ids = [item['id'] for item in planner_catalog['destinations'][:2]]
    itinerary = {'name': 'Lịch trình SQL identity', 'startDate': tomorrow, 'guests': 2,
                 'destinationIds': planner_ids, 'departurePoint': 'TP. Hồ Chí Minh',
                 'transport': 'Ô tô', 'budget': 2000000}
    trip = request(API + '/api/itineraries', 'POST', itinerary, token=customer_token)
    assert trip['userId'] == customer['user']['id']
    assert trip['destinationIds'] == planner_ids and trip['departurePoint'] == 'TP. Hồ Chí Minh'
    assert request(API + f"/api/itineraries/{trip['id']}", token=customer_token)['id'] == trip['id']
    other_email, _, other = registration('Khách khác')
    other_items = request(API + '/api/itineraries', token=other['token'])
    assert all(item['id'] != trip['id'] for item in other_items), 'A SQL account must not inherit another account\'s demo itinerary.'
    request(API + f"/api/itineraries/{trip['id']}", token=other['token'], expected=404)

    staff_email, _, staff = registration('Nhân viên đa vai trò')
    sql_non_query(f'''
INSERT INTO TAIKHOAN_VAITRO (MaTK, MaVaiTro)
SELECT account.MaTK, role.MaVaiTro
FROM TAIKHOAN AS account
CROSS JOIN VAITRO AS role
WHERE account.Email = {sql_literal(staff_email)}
  AND role.TenVaiTro IN ('Admin', 'Editor');
''')
    staff_me = request(API + '/api/auth/me', token=staff['token'])
    assert set(staff_me['roles']) == {'Admin', 'Customer', 'Editor'}
    request(API + '/api/destinations', 'POST', {'name': 'Điểm kiểm tra quyền', 'region': 'Tây Ninh', 'category': 'Văn hóa', 'address': 'Chỉ bộ nhớ', 'description': 'Xác nhận Admin hoặc Editor được phép.', 'visitMinutes': 60}, token=staff['token'])

    tour_request = request(API + f"/api/itineraries/{trip['id']}/requests", 'POST',
                           {'contact': customer_email, 'serviceNeeds': 'Hướng dẫn viên'}, token=customer_token)
    assert tour_request['snapshot']['destinationIds'] == planner_ids
    assert request(API + f"/api/itineraries/{trip['id']}/requests", 'POST',
                   {'contact': customer_email, 'serviceNeeds': 'Hướng dẫn viên'}, token=customer_token)['id'] == tour_request['id']
    request(API + f"/api/private-tour-requests/{tour_request['id']}", token=other['token'], expected=404)
    request(API + f"/api/private-tour-requests/{tour_request['id']}/quotes/1/accept", 'POST', {}, token=other['token'], expected=404)
    assert any(item['id'] == tour_request['id'] for item in request(API + '/api/staff/private-tour-requests', token=staff['token']))
    changed = {**itinerary, 'name': 'Lịch trình đã sửa', 'destinationIds': [planner_ids[0]]}
    request(API + f"/api/itineraries/{trip['id']}", 'PUT', changed, token=customer_token)
    assert request(API + f"/api/private-tour-requests/{tour_request['id']}", token=customer_token)['snapshot']['name'] == itinerary['name']
    quote_payload = {'expiresAt': (datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(days=2)).isoformat(),
                     'amountDue': 300000, 'cancellationTerms': 'Hủy trước 48 giờ.',
                     'lines': [{'description': 'Dịch vụ tour riêng', 'unit': 'khách', 'quantity': 2, 'unitPrice': 500000}]}
    request(API + f"/api/staff/private-tour-requests/{tour_request['id']}/verifications", 'POST',
            {'item': 'Hướng dẫn viên', 'supplier': 'Đối tác thử nghiệm', 'result': 'Available'}, token=staff['token'])
    old_quote = request(API + f"/api/staff/private-tour-requests/{tour_request['id']}/quotes", 'POST', quote_payload, token=staff['token'])['quotes'][-1]
    request(API + f"/api/private-tour-requests/{tour_request['id']}/quotes/{old_quote['id']}/accept", 'POST', {}, token=customer_token, expected=409)
    updated_request = request(API + f"/api/itineraries/{trip['id']}/requests", 'POST',
                              {'contact': customer_email, 'serviceNeeds': 'Hướng dẫn viên'}, token=customer_token)
    assert updated_request['id'] != tour_request['id'] and updated_request['version'] > tour_request['version']
    tour_request = updated_request
    request(API + f"/api/staff/private-tour-requests/{tour_request['id']}/quotes", 'POST', quote_payload, token=staff['token'], expected=409)
    request(API + f"/api/staff/private-tour-requests/{tour_request['id']}/verifications", 'POST',
            {'item': 'Hướng dẫn viên', 'supplier': 'Đối tác thử nghiệm', 'result': 'Available', 'note': 'Đã xác minh'}, token=staff['token'])
    first = request(API + f"/api/staff/private-tour-requests/{tour_request['id']}/quotes", 'POST', quote_payload, token=staff['token'])['quotes'][-1]
    assert first['total'] == 1000000
    second = request(API + f"/api/staff/private-tour-requests/{tour_request['id']}/quotes", 'POST', quote_payload, token=staff['token'])['quotes'][-1]
    request(API + f"/api/private-tour-requests/{tour_request['id']}/quotes/{first['id']}/accept", 'POST', {}, token=customer_token, expected=409)
    order = request(API + f"/api/private-tour-requests/{tour_request['id']}/quotes/{second['id']}/accept", 'POST', {}, token=customer_token)
    assert order['quoteId'] == second['id'] and order['agreedPrice'] == 1000000
    assert request(API + f"/api/private-tour-requests/{tour_request['id']}/quotes/{second['id']}/accept", 'POST', {}, token=customer_token)['id'] == order['id']
    assert any(item['id'] == order['id'] for item in request(API + '/api/private-tour-orders', token=customer_token))
    sql_non_query(f'''
DELETE assignment
FROM TAIKHOAN_VAITRO AS assignment
INNER JOIN TAIKHOAN AS account ON account.MaTK = assignment.MaTK
INNER JOIN VAITRO AS role ON role.MaVaiTro = assignment.MaVaiTro
WHERE account.Email = {sql_literal(staff_email)} AND role.TenVaiTro IN ('Admin', 'Editor');
''')
    request(API + '/api/destinations', 'POST', {'name': 'Quyền đã bị gỡ', 'region': 'Tây Ninh', 'category': 'Văn hóa', 'address': 'Không lưu', 'description': 'Role bị gỡ phải có hiệu lực ngay.', 'visitMinutes': 60}, token=staff['token'], expected=403)

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
    planner_page = request(WEB + '/Planner', opener=web)
    planner_token = antiforgery_token(planner_page)
    web_trip = json.loads(form_request(WEB + '/Planner/Save', {
        '__RequestVerificationToken': planner_token, 'Name': 'Web lưu SQL', 'StartDate': tomorrow,
        'Guests': '2', 'DeparturePoint': 'TP. Hồ Chí Minh', 'Transport': 'Ô tô',
        'DestinationIds': str(planner_ids[0]), 'Stops[0].DestinationId': str(planner_ids[0]),
        'Stops[0].Day': '1', 'Stops[0].Order': '1', 'Stops[0].Note': 'Kiểm thử Web',
    }, opener=web))
    assert web_trip['id'] > 0 and 'Web lưu SQL' in html.unescape(request(WEB + '/Trips', opener=web))
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
    assert 'Web lưu SQL' in html.unescape(request(WEB + '/Trips', opener=web))

    request(API + '/api/auth/logout', 'POST', token=customer_token, expected=204)
    request(API + '/api/auth/me', token=customer_token, expected=401)
    print(f'PASS: {checks} functional HTTP checks; SQL registration, duplicate email, password, dynamic roles, lock, logout, ownership and Web login.')


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
