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
DELETE assignment
FROM TAIKHOAN_VAITRO AS assignment
INNER JOIN TAIKHOAN AS account ON account.MaTK = assignment.MaTK
WHERE account.Email IN ({emails});
DELETE FROM TAIKHOAN WHERE Email IN ({emails});
''')


@contextlib.contextmanager
def running_apps():
    assert API_EXE.exists() and WEB_EXE.exists(), 'Build the solution before running smoke.py --start.'
    environment = os.environ.copy()
    environment['ASPNETCORE_ENVIRONMENT'] = 'Development'
    environment['ASPNETCORE_URLS'] = 'http://localhost:5080'
    environment['Authentication__SessionLifetimeSeconds'] = '10'
    processes, logs = [], []
    try:
        for executable, working_directory in ((API_EXE, API_EXE.parent), (WEB_EXE, WEB_EXE.parent)):
            log = tempfile.TemporaryFile()
            logs.append(log)
            process_environment = environment.copy()
            process_environment['ASPNETCORE_URLS'] = 'http://localhost:5080' if executable == API_EXE else 'http://localhost:5081'
            processes.append(subprocess.Popen([str(executable)], cwd=working_directory, stdout=log, stderr=log, env=process_environment))
        for url in (API + '/health', WEB + '/Account/Login'):
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
    itinerary = {'name': 'Lịch trình SQL identity', 'startDate': tomorrow, 'guests': 2, 'destinationIds': [1, 6]}
    trip = request(API + '/api/itineraries', 'POST', itinerary, token=customer_token)
    assert trip['userId'] == customer['user']['id']
    other_email, _, other = registration('Khách khác')
    other_items = request(API + '/api/itineraries', token=other['token'])
    assert all(item['id'] != trip['id'] for item in other_items), 'A SQL account must not inherit another account\'s demo itinerary.'

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
    expiring_email, _, expiring = registration('Tài khoản hết phiên')
    time.sleep(11)
    request(API + '/api/auth/me', token=expiring['token'], expected=401)
    assert expiring_email in test_emails
    print(f'PASS: {checks} HTTP checks; SQL registration, duplicate email, password, dynamic roles, lock, logout, expiry, ownership and Web login.')


if __name__ == '__main__':
    try:
        if '--start' in sys.argv:
            with running_apps():
                run()
        else:
            run()
    finally:
        cleanup_test_accounts()
