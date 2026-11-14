"""HTTP/SQL checks for Vương's screens: SCR06/SCR07/SCR11 (khách) and SCR14/SCR15/SCR20 (nhân viên).

Covers places, activities, slots, narrations, translations, audio and listen rights (UC03, UC04, UC14, UC15,
UC21, UC22, UC23). Test data uses the ``SMOKE `` name prefix and per-run accounts; ``cleanup()`` removes exactly
that data. Run: ``python tests/api/content_screens.py --start`` (API must be built, DB must be a development DB).
"""
import base64
import datetime
import io
import subprocess
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid
import wave

import smoke

API = smoke.API
request = smoke.request
checks = 0


def check(condition, message):
    global checks
    assert condition, message
    checks += 1


def request_bytes(path, token=None, expected=200):
    headers = {'Authorization': 'Bearer ' + token} if token else {}
    try:
        response = urllib.request.build_opener().open(urllib.request.Request(path, headers=headers), timeout=15)
    except urllib.error.HTTPError as error:
        response = error
    body = response.read()
    check(response.status == expected, f'GET {path}: expected {expected}, got {response.status}: {body[:150]!r}')
    return body


def sql_scalar(statement):
    """Run one statement through the same PowerShell channel as smoke.sql_non_query and return the first value."""
    script = f'''$ProgressPreference = 'SilentlyContinue'
$connection = New-Object System.Data.SqlClient.SqlConnection ([Environment]::GetEnvironmentVariable('DONGNAMTRIP_TEST_CONNECTION'))
try {{
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = @'
{statement}
'@
    Write-Output ('SCALAR:' + [string]$command.ExecuteScalar())
}}
finally {{ if ($connection) {{ $connection.Dispose() }} }}
'''
    environment = smoke.os.environ.copy()
    environment['DONGNAMTRIP_TEST_CONNECTION'] = smoke.user_secrets_connection_string()
    output = subprocess.check_output(['powershell', '-NoProfile', '-NonInteractive', '-OutputFormat', 'Text', '-EncodedCommand',
                                      base64.b64encode(script.encode('utf-16le')).decode()], cwd=smoke.ROOT, env=environment, text=True)
    marker = output.find('SCALAR:')
    assert marker >= 0, 'SQL scalar query returned no value: ' + output[:200]
    return output[marker + 7:].strip().splitlines()[0]


def grant_roles(email, roles):
    names = ', '.join(smoke.sql_literal(role) for role in roles)
    smoke.sql_non_query(f'''
INSERT INTO TAIKHOAN_VAITRO (MaTK, MaVaiTro)
SELECT account.MaTK, role.MaVaiTro FROM TAIKHOAN AS account CROSS JOIN VAITRO AS role
WHERE account.Email = {smoke.sql_literal(email)} AND role.TenVaiTro IN ({names});
''')


def wav_base64():
    buffer = io.BytesIO()
    with wave.open(buffer, 'wb') as sound:
        sound.setnchannels(1)
        sound.setsampwidth(2)
        sound.setframerate(8000)
        sound.writeframes(b'\x00\x00' * 800)
    return base64.b64encode(buffer.getvalue()).decode()


def cleanup():
    """Delete data created by this script (names start with 'SMOKE '), then the per-run accounts."""
    if smoke.test_emails:
        emails = ', '.join(smoke.sql_literal(email) for email in smoke.test_emails)
        smoke.sql_non_query(f'''
DECLARE @owned TABLE (MaTK int PRIMARY KEY);
INSERT INTO @owned SELECT MaTK FROM TAIKHOAN WHERE Email IN ({emails});
DELETE FROM dbo.NHATKYTHAOTAC WHERE MaTK IN (SELECT MaTK FROM @owned);
DELETE FROM dbo.QUYENNGHE WHERE MaTK IN (SELECT MaTK FROM @owned);
DECLARE @routes TABLE (MaLT int PRIMARY KEY);
INSERT INTO @routes SELECT MaLT FROM dbo.LICHTRINH WHERE MaTK IN (SELECT MaTK FROM @owned);
DELETE FROM dbo.NHATKYTHAOTAC WHERE LoaiDoiTuong = 'DONDAT' AND MaDoiTuong IN (SELECT CAST(MaDon AS varchar(20)) FROM dbo.DONDAT WHERE MaTK IN (SELECT MaTK FROM @owned));
DELETE FROM dbo.GIAODICHTIEN WHERE MaDon IN (SELECT MaDon FROM dbo.DONDAT WHERE MaTK IN (SELECT MaTK FROM @owned)) OR MaTKGhiNhan IN (SELECT MaTK FROM @owned);
DELETE FROM dbo.QUYENNGHE WHERE MaDon IN (SELECT MaDon FROM dbo.DONDAT WHERE MaTK IN (SELECT MaTK FROM @owned));
DELETE FROM dbo.DONDAT WHERE MaTK IN (SELECT MaTK FROM @owned);
DELETE FROM dbo.NHATKYTHAOTAC WHERE LoaiDoiTuong IN ('BAOGIA', 'YEUCAUTOURRIENG', 'XACMINHDICHVU') AND MaTK IN (SELECT MaTK FROM @owned);
DELETE FROM dbo.NHATKYTHAOTAC WHERE LoaiDoiTuong = 'BAOGIA' AND MaDoiTuong IN (SELECT CAST(MaBG AS varchar(20)) FROM dbo.BAOGIA WHERE MaPB IN (SELECT MaPB FROM dbo.PHIENBANLICHTRINH WHERE MaLT IN (SELECT MaLT FROM @routes)));
DELETE FROM dbo.HANGMUCBAOGIA WHERE MaBG IN (SELECT MaBG FROM dbo.BAOGIA WHERE MaPB IN (SELECT MaPB FROM dbo.PHIENBANLICHTRINH WHERE MaLT IN (SELECT MaLT FROM @routes)));
DELETE FROM dbo.BAOGIA WHERE MaPB IN (SELECT MaPB FROM dbo.PHIENBANLICHTRINH WHERE MaLT IN (SELECT MaLT FROM @routes));
DELETE FROM dbo.XACMINHDICHVU WHERE MaYC IN (SELECT MaYC FROM dbo.YEUCAUTOURRIENG WHERE MaPB IN (SELECT MaPB FROM dbo.PHIENBANLICHTRINH WHERE MaLT IN (SELECT MaLT FROM @routes)));
DELETE FROM dbo.YEUCAUTOURRIENG WHERE MaPB IN (SELECT MaPB FROM dbo.PHIENBANLICHTRINH WHERE MaLT IN (SELECT MaLT FROM @routes));
DELETE FROM dbo.LICHTRINH_SUAT WHERE MaLT IN (SELECT MaLT FROM @routes);
DELETE FROM dbo.LICHTRINH_DIADIEM WHERE MaLT IN (SELECT MaLT FROM @routes);
DELETE FROM dbo.PHIENBANLICHTRINH WHERE MaLT IN (SELECT MaLT FROM @routes);
DELETE FROM dbo.LICHTRINH WHERE MaLT IN (SELECT MaLT FROM @routes);
DELETE FROM dbo.DONDAT WHERE MaTK IN (SELECT MaTK FROM @owned);
''')
    smoke.sql_non_query('''
DECLARE @tours TABLE (MaTour int PRIMARY KEY);
INSERT INTO @tours SELECT MaTour FROM dbo.TOUR WHERE TenTour LIKE N'SMOKE tour %';
DELETE FROM dbo.LICHKHOIHANH WHERE MaTour IN (SELECT MaTour FROM @tours);
DELETE FROM dbo.TOUR_DIADIEM WHERE MaTour IN (SELECT MaTour FROM @tours);
DELETE FROM dbo.TOUR WHERE MaTour IN (SELECT MaTour FROM @tours);
DECLARE @places TABLE (MaDD int PRIMARY KEY);
INSERT INTO @places SELECT MaDD FROM dbo.DIADIEM WHERE TenDD LIKE N'SMOKE %';
DECLARE @notes TABLE (MaTM int PRIMARY KEY);
INSERT INTO @notes SELECT MaTM FROM dbo.THUYETMINH WHERE MaDD IN (SELECT MaDD FROM @places);
DELETE FROM dbo.TEPAUDIO WHERE MaBD IN (SELECT MaBD FROM dbo.BANDICH WHERE MaTM IN (SELECT MaTM FROM @notes));
DELETE FROM dbo.BANDICH WHERE MaTM IN (SELECT MaTM FROM @notes);
DELETE FROM dbo.THUYETMINH WHERE MaTM IN (SELECT MaTM FROM @notes);
DELETE FROM dbo.LICHTRINH_SUAT WHERE MaDD IN (SELECT MaDD FROM @places);
DELETE FROM dbo.SUAT WHERE MaHD IN (SELECT MaHD FROM dbo.HOATDONG WHERE MaDD IN (SELECT MaDD FROM @places));
DELETE FROM dbo.HOATDONG WHERE MaDD IN (SELECT MaDD FROM @places);
DELETE FROM dbo.LICHTRINH_DIADIEM WHERE MaDD IN (SELECT MaDD FROM @places);
DELETE FROM dbo.TOUR_DIADIEM WHERE MaDD IN (SELECT MaDD FROM @places);
DELETE FROM dbo.DIADIEM WHERE MaDD IN (SELECT MaDD FROM @places);
''')
    smoke.cleanup_test_accounts()


def run():
    suffix = uuid.uuid4().hex[:8]

    def day(offset):
        return (datetime.date.today() + datetime.timedelta(days=offset)).isoformat()

    customer_email, _, customer = smoke.registration('Khách nghe thuyết minh')
    editor_email, _, editor = smoke.registration('Biên tập viên')
    publisher_email, _, publisher = smoke.registration('Người xuất bản')
    operator_email, _, operator = smoke.registration('Nhân viên điều hành')
    grant_roles(editor_email, ['Editor'])
    grant_roles(publisher_email, ['Publisher'])
    grant_roles(operator_email, ['Operator'])
    # Vai trò được nạp khi đăng nhập, nên đăng nhập lại để lấy phiên có vai trò mới.
    tokens = {}
    for key, email in (('editor', editor_email), ('publisher', publisher_email), ('operator', operator_email)):
        tokens[key] = request(API + '/api/auth/login', 'POST', {'email': email, 'password': 'SmokePass!2026'})['token']
    cu, ed, pub, op = customer['token'], tokens['editor'], tokens['publisher'], tokens['operator']

    # ---- Phân quyền các API quản trị ----
    request(API + '/api/admin/destinations', expected=401)
    request(API + '/api/admin/destinations', token=cu, expected=403)
    request(API + '/api/admin/destinations', token=op, expected=403)
    request(API + '/api/admin/destinations', token=pub, expected=403)
    request(API + '/api/admin/activities', token=cu, expected=403)
    request(API + '/api/admin/activities', token=op)
    request(API + '/api/admin/narrations', token=cu, expected=403)
    request(API + '/api/admin/narrations', token=op, expected=403)
    request(API + '/api/admin/narrations', token=pub)
    languages = request(API + '/api/admin/languages', token=ed)
    check({'vi', 'en', 'ko'} <= {item['code'] for item in languages}, 'Seed languages vi/en/ko are missing.')

    # ---- SCR14: địa điểm ----
    name = f'SMOKE Điểm {suffix}'
    form = {'name': name, 'region': 'Tây Ninh', 'category': 'Văn hóa', 'address': 'Địa chỉ thử nghiệm',
            'description': 'Địa điểm kiểm thử SCR06/07/14.', 'visitMinutes': 60}
    places = API + '/api/admin/destinations'
    request(places, 'POST', {**form, 'name': ''}, token=ed, expected=400)
    request(places, 'POST', {**form, 'visitMinutes': 5}, token=ed, expected=400)
    request(places, 'POST', {**form, 'region': 'Hà Nội'}, token=ed, expected=400)
    request(places, 'POST', {**form, 'openTime': '17:00:00', 'closeTime': '08:00:00'}, token=ed, expected=400)
    request(places, 'POST', {**form, 'latitude': 10.5}, token=ed, expected=400)
    place = request(places, 'POST', form, token=ed)
    place_id = place['id']
    check(place['status'] == 'Draft' and place['name'] == name, 'A new place must start as Draft.')
    request(places, 'POST', form, token=ed, expected=409)
    request(places, 'POST', form, token=cu, expected=403)
    edited = request(f'{places}/{place_id}', 'PUT', {**form, 'description': 'Mô tả đã sửa.'}, token=ed)
    check(edited['description'] == 'Mô tả đã sửa.', 'Edit did not persist.')
    request(f'{places}/999999999', 'PUT', {**form, 'name': name + ' khác'}, token=ed, expected=404)

    # SCR06/SCR07: bản nháp không hiển thị cho khách
    check(place_id not in [item['id'] for item in request(API + '/api/destinations')], 'Draft place leaked to guests.')
    request(f'{API}/api/destinations/{place_id}', expected=404)
    request(f'{places}/{place_id}/status', 'POST', {'status': 'Draft'}, token=ed, expected=400)
    check(request(f'{places}/{place_id}/status', 'POST', {'status': 'Published'}, token=ed)['status'] == 'Published', 'Publish failed.')
    check(place_id in [item['id'] for item in request(API + '/api/destinations')], 'Published place is not listed.')
    found = request(f'{API}/api/destinations/search?q={urllib.parse.quote(name)}')
    check(found['total'] == 1 and found['items'][0]['id'] == place_id, 'Search must find the published place.')
    detail = request(f'{API}/api/destinations/{place_id}')
    check(detail['name'] == name and detail['activities'] == [], 'Detail must have no activities yet.')

    # Không xóa được địa điểm đã công bố; xóa được bản nháp
    check(request(f'{places}/{place_id}', 'DELETE', token=ed, expected=409)['deleted'] is False, 'Published place must not be deletable.')
    spare = request(places, 'POST', {**form, 'name': name + ' nháp'}, token=ed)
    check(request(f'{places}/{spare["id"]}', 'DELETE', token=ed)['deleted'] is True, 'Draft place must be deletable.')
    check(all(item['id'] != spare['id'] for item in request(places, token=ed)), 'Deleted draft still listed.')
    kept = request(places, 'POST', {**form, 'name': name + ' giữ lại'}, token=ed)

    # Ngừng công bố rồi công bố lại
    check(request(f'{places}/{place_id}/status', 'POST', {'status': 'Inactive'}, token=ed)['status'] == 'Inactive', 'Unpublish failed.')
    request(f'{API}/api/destinations/{place_id}', expected=404)
    check(request(f'{places}/{place_id}/status', 'POST', {'status': 'Published'}, token=ed)['status'] == 'Published', 'Republish failed.')

    # ---- SCR15: hoạt động và suất ----
    activities = API + '/api/admin/activities'
    activity_form = {'destinationId': place_id, 'name': f'Hoạt động {suffix}', 'description': 'Mô tả hoạt động', 'durationMinutes': 60,
                     'conditions': 'Người lớn đi cùng trẻ em', 'referenceFee': 150000}
    request(activities, 'POST', {**activity_form, 'name': ''}, token=ed, expected=400)
    request(activities, 'POST', {**activity_form, 'destinationId': 0}, token=ed, expected=400)
    request(activities, 'POST', {**activity_form, 'destinationId': 999999999}, token=ed, expected=400)
    request(activities, 'POST', {**activity_form, 'referenceFee': 100.5}, token=ed, expected=400)
    activity = request(activities, 'POST', activity_form, token=op)
    activity_id = activity['id']
    check(activity['status'] == 'Draft', 'A new activity must start as Draft.')
    request(activities, 'POST', activity_form, token=ed, expected=409)
    request(f'{activities}/{activity_id}', 'PUT', {**activity_form, 'description': 'Mô tả đã sửa'}, token=ed)
    check(any(item['id'] == activity_id for item in request(f'{activities}?destinationId={place_id}', token=op)), 'Activity filter by place failed.')
    check(request(f'{API}/api/destinations/{place_id}')['activities'] == [], 'Draft activity leaked to guests.')
    kept_activity = request(activities, 'POST', {**activity_form, 'destinationId': kept['id'], 'name': 'Hoạt động giữ địa điểm'}, token=ed)
    request(f'{activities}/{kept_activity["id"]}/status', 'POST', {'status': 'Published'}, token=ed, expected=409)
    check(request(f'{places}/{kept["id"]}', 'DELETE', token=ed, expected=409)['deleted'] is False, 'Place with activities must not be deletable.')
    check(request(f'{activities}/{activity_id}/status', 'POST', {'status': 'Published'}, token=op)['status'] == 'Done', 'Activity publish failed.')

    slots = f'{activities}/{activity_id}/slots'
    good = {'date': day(2), 'start': '09:00:00', 'end': '11:00:00'}
    request(slots, 'POST', {**good, 'end': '09:00:00'}, token=ed, expected=400)
    request(slots, 'POST', {**good, 'end': '09:30:00'}, token=ed, expected=400)
    request(slots, 'POST', {**good, 'date': day(-3)}, token=ed, expected=400)
    slot = request(slots, 'POST', good, token=ed)
    slot_id = slot['id']
    check(slot['status'] == 'Published', 'A new slot must be published.')
    request(slots, 'POST', {**good, 'start': '10:00:00', 'end': '11:30:00'}, token=ed, expected=409)
    request(slots, 'POST', {'date': day(3), 'start': '14:00:00', 'end': '15:30:00'}, token=ed)
    request(f'{API}/api/admin/slots/{slot_id}', 'PUT', {**good, 'end': '11:30:00'}, token=ed)

    shown = next(item for item in request(f'{API}/api/destinations/{place_id}')['activities'] if item['id'] == activity_id)
    check(shown['referenceFee'] == 150000 and shown['conditions'] == 'Người lớn đi cùng trẻ em', 'Activity detail mismatch.')
    check([item['date'] for item in shown['slots']] == [day(2), day(3)], 'Slots must be listed by date.')
    paused = request(f'{API}/api/admin/slots/{slot_id}/status', 'POST', {'status': 'Inactive'}, token=ed)
    check(paused['status'] == 'Done' and paused['affectedItineraries'] == 0, 'Pausing an unused slot must not warn.')
    check(request(f'{API}/api/admin/slots/{slot_id}/status', 'POST', {'status': 'Published'}, token=ed)['status'] == 'Done', 'Slot republish failed.')

    # Cảnh báo hồ sơ bị ảnh hưởng: lịch trình của khách đang dùng suất này
    smoke.sql_non_query(f'''
DECLARE @lt int;
INSERT INTO dbo.LICHTRINH (MaTK, TenLT, NgayDi, SoKhach)
SELECT MaTK, N'SMOKE lich trinh', DATEADD(day, 3, CAST(SYSUTCDATETIME() AS date)), 1 FROM dbo.TAIKHOAN WHERE Email = {smoke.sql_literal(customer_email)};
SET @lt = SCOPE_IDENTITY();
INSERT INTO dbo.LICHTRINH_DIADIEM (MaLT, MaDD, NgayThu, ThuTu) VALUES (@lt, {place_id}, 1, 1);
INSERT INTO dbo.LICHTRINH_SUAT (MaLT, MaSuat, MaDD) VALUES (@lt, {slot_id}, {place_id});
''')
    request(f'{API}/api/admin/slots/{slot_id}', 'PUT', {**good, 'start': '08:00:00', 'end': '10:30:00'}, token=ed, expected=409)
    warning = request(f'{API}/api/admin/slots/{slot_id}/status', 'POST', {'status': 'Inactive'}, token=ed, expected=409)
    check(warning['status'] == 'NeedConfirm' and warning['affectedItineraries'] == 1, 'Expected a NeedConfirm warning with 1 itinerary.')
    check(len(next(i for i in request(f'{API}/api/destinations/{place_id}')['activities'] if i['id'] == activity_id)['slots']) == 2, 'Unconfirmed pause must not change the slot.')
    confirmed = request(f'{API}/api/admin/slots/{slot_id}/status?confirm=true', 'POST', {'status': 'Inactive'}, token=ed)
    check(confirmed['status'] == 'Done' and confirmed['affectedItineraries'] == 1, 'Confirmed pause must report the affected itinerary.')
    warning = request(f'{activities}/{activity_id}/status', 'POST', {'status': 'Inactive'}, token=op, expected=409)
    check(warning['status'] == 'NeedConfirm' and warning['affectedItineraries'] == 1, 'Activity pause must warn about the itinerary.')
    check(request(f'{activities}/{activity_id}/status?confirm=true', 'POST', {'status': 'Inactive'}, token=op)['status'] == 'Done', 'Confirmed activity pause failed.')
    check(request(f'{API}/api/destinations/{place_id}')['activities'] == [], 'Paused activity leaked to guests.')
    check(request(f'{activities}/{activity_id}/status', 'POST', {'status': 'Published'}, token=op)['status'] == 'Done', 'Activity republish failed.')

    # ---- SCR20: thuyết minh, bản dịch, audio ----
    narrations = API + '/api/admin/narrations'
    narration_form = {'destinationId': place_id, 'title': f'Thuyết minh {suffix}', 'source': 'Nội dung thuyết minh gốc để kiểm thử.'}
    request(narrations, 'POST', narration_form, token=pub, expected=403)
    request(narrations, 'POST', {**narration_form, 'title': ''}, token=ed, expected=400)
    narration = request(narrations, 'POST', narration_form, token=ed)
    narration_id = narration['id']
    check(narration['version'] == 1 and narration['status'] == 'Draft', 'A new narration must be version 1 Draft.')
    status_url = f'{narrations}/{narration_id}/status'
    request(status_url, 'POST', {'status': 'Pending'}, token=ed, expected=409)
    translations = f'{narrations}/{narration_id}/translations'
    request(translations, 'PUT', {'languageCode': 'xx', 'content': 'Nội dung'}, token=ed, expected=400)
    request(translations, 'PUT', {'languageCode': 'vi', 'content': 'Nội dung tiếng Việt'}, token=pub, expected=403)
    narration = request(translations, 'PUT', {'languageCode': 'vi', 'content': 'Xin chào, đây là thuyết minh kiểm thử.'}, token=ed)
    translation_id = narration['translations'][0]['id']
    check(narration['translations'][0]['languageCode'] == 'vi', 'Translation language mismatch.')
    check(request(status_url, 'POST', {'status': 'Pending'}, token=ed)['status'] == 'Pending', 'Submit for review failed.')
    request(status_url, 'POST', {'status': 'Approved'}, token=ed, expected=403)
    check(request(status_url, 'POST', {'status': 'Approved'}, token=pub)['status'] == 'Approved', 'Approve failed.')
    # Hoàn duyệt (Approved -> Draft) chỉ dành cho Publisher/Admin; Editor bị từ chối.
    request(status_url, 'POST', {'status': 'Draft'}, token=ed, expected=403)
    check(request(status_url, 'POST', {'status': 'Draft'}, token=pub)['status'] == 'Draft', 'Publisher must be able to withdraw an approval.')
    check(request(status_url, 'POST', {'status': 'Pending'}, token=ed)['status'] == 'Pending', 'Resubmit after withdrawal failed.')
    check(request(status_url, 'POST', {'status': 'Approved'}, token=pub)['status'] == 'Approved', 'Re-approve failed.')
    translation_status = f'{API}/api/admin/translations/{translation_id}/status'
    request(translation_status, 'POST', {'status': 'Pending'}, token=ed)
    request(translation_status, 'POST', {'status': 'Approved'}, token=ed, expected=403)
    request(translation_status, 'POST', {'status': 'Approved'}, token=pub)
    request(translation_status, 'POST', {'status': 'Draft'}, token=ed, expected=403)
    request(translation_status, 'POST', {'status': 'Draft'}, token=pub)
    request(translation_status, 'POST', {'status': 'Pending'}, token=ed)
    request(translation_status, 'POST', {'status': 'Approved'}, token=pub)

    audio_url = f'{API}/api/admin/translations/{translation_id}/audio'
    request(audio_url, 'POST', {'voice': 'Test Voice', 'wavBase64': '###'}, token=ed, expected=400)
    request(audio_url, 'POST', {'voice': 'Test Voice', 'wavBase64': base64.b64encode(b'this is not a wav file').decode()}, token=ed, expected=400)
    request(audio_url, 'POST', {'voice': 'Test Voice', 'wavBase64': wav_base64()}, token=pub, expected=403)
    failed = request(audio_url, 'POST', {'voice': 'Test Voice', 'error': 'TTS loi gia lap'}, token=ed)
    check(failed['translations'][0]['audios'][0]['status'] == 'Failed', 'A TTS error must be stored as Failed.')
    uploaded = request(audio_url, 'POST', {'voice': 'Test Voice', 'wavBase64': wav_base64()}, token=ed)
    audios = uploaded['translations'][0]['audios']
    check(any(item['status'] == 'Failed' for item in audios), 'The Failed audio must be kept.')
    audio_id = next(item['id'] for item in audios if item['status'] == 'Pending')
    audio_status = f'{API}/api/admin/audio/{audio_id}/status'
    request(audio_status, 'POST', {'status': 'Published'}, token=ed, expected=403)
    published = request(audio_status, 'POST', {'status': 'Published'}, token=pub)
    check(published['status'] == 'Published' and published['translations'][0]['status'] == 'Published', 'Publishing audio must publish the translation.')
    check(request_bytes(f'{API}/api/admin/audio/{audio_id}/file', token=pub)[:4] == b'RIFF', 'Editor preview must return a WAV file.')
    request_bytes(f'{API}/api/admin/audio/{audio_id}/file', token=cu, expected=403)
    version = request(f'{narrations}/{narration_id}/versions', 'POST', {**narration_form, 'source': 'Nội dung phiên bản 2.'}, token=ed)
    check(version['version'] == 2 and version['status'] == 'Draft', 'A new version must be version 2 Draft.')
    only_source = request(f'{narrations}/{narration_id}/versions', 'POST', {'source': 'Chỉ gửi nội dung.'}, token=ed)
    check(only_source['version'] == 3 and only_source['status'] == 'Draft', 'A new version must only need the source text.')
    request(f'{narrations}/{narration_id}/versions', 'POST', {'source': ''}, token=ed, expected=400)
    request(f'{narrations}/{narration_id}', 'PUT', {**narration_form, 'source': 'Sửa trực tiếp'}, token=ed, expected=409)

    # ---- SCR11: nghe thuyết minh (chuyến đi, thời hạn nghe, quyền truy cập tệp) ----
    request(API + '/api/listen', expected=401)
    request(f'{API}/api/listen/audio/{audio_id}', expected=401)
    listing = request(API + '/api/listen', token=cu)
    check(listing['hasAccess'] is False and listing['tracks'] == [], 'A guest without a confirmed order must have no access.')
    request(f'{API}/api/listen/audio/{audio_id}', token=cu, expected=403)
    order_id = int(sql_scalar(f'''
DECLARE @tour int, @lkh int, @don int;
INSERT INTO dbo.TOUR (TenTour, KhuVuc, LoaiHinh, ThoiLuong, GiaThamKhao, TomTat, TrangThai)
VALUES (N'SMOKE tour {suffix}', N'Tay Ninh', N'Van hoa', N'1 ngay', 1000, N'Tour kiem thu nghe thuyet minh', 'Draft');
SET @tour = SCOPE_IDENTITY();
INSERT INTO dbo.TOUR_DIADIEM (MaTour, MaDD, NgayThu, ThuTu) VALUES (@tour, {place_id}, 1, 1);
INSERT INTO dbo.LICHKHOIHANH (MaTour, NgayGioKhoiHanh, Gia, SucChua, SoChoDaGiu, TrangThai)
VALUES (@tour, DATEADD(day, 3, SYSUTCDATETIME()), 1000, 10, 1, 'Inactive');
SET @lkh = SCOPE_IDENTITY();
INSERT INTO dbo.DONDAT (MaTK, MaLKH, SoKhach, GiaChot, DieuKienHuy, TrangThai)
SELECT MaTK, @lkh, 1, 1000, N'Kiem thu', 'Confirmed' FROM dbo.TAIKHOAN WHERE Email = {smoke.sql_literal(customer_email)};
SET @don = SCOPE_IDENTITY();
SELECT @don;
'''))
    now = datetime.datetime.now(datetime.timezone.utc)

    def iso(moment):
        return moment.strftime('%Y-%m-%dT%H:%M:%SZ')

    rights = API + '/api/internal/listen-rights'
    grant = {'orderId': order_id, 'accountId': customer['user']['id'], 'from': iso(now - datetime.timedelta(hours=1)), 'to': iso(now + datetime.timedelta(days=2))}
    request(rights, 'POST', grant, token=cu, expected=403)
    request(rights, 'POST', {**grant, 'to': grant['from']}, token=op, expected=400)
    request(rights, 'POST', {**grant, 'accountId': editor['user']['id']}, token=op, expected=400)
    request(rights, 'POST', {**grant, 'orderId': 999999999}, token=op, expected=404)
    request(rights, 'POST', grant, token=op)
    request(rights, 'POST', grant, token=op)  # cấp lại phải thành công, không tạo bản trùng
    listing = request(API + '/api/listen', token=cu)
    check(listing['hasAccess'] is True and any(item['audioId'] == audio_id for item in listing['tracks']), 'The confirmed order must unlock the place narration.')
    check(request_bytes(f'{API}/api/listen/audio/{audio_id}', token=cu)[:4] == b'RIFF', 'The guest must be able to download the audio file.')
    request(f'{API}/api/listen/audio/{audio_id}', token=ed, expected=403)
    request(f'{rights}/{order_id}', 'DELETE', token=op, expected=204)
    check(request(API + '/api/listen', token=cu)['hasAccess'] is False, 'Revoking must remove access.')
    request(f'{API}/api/listen/audio/{audio_id}', token=cu, expected=403)
    request(rights, 'POST', {**grant, 'from': iso(now - datetime.timedelta(days=3)), 'to': iso(now - datetime.timedelta(days=1))}, token=op)
    expired = request(API + '/api/listen', token=cu)
    check(expired['hasAccess'] is False and 'hết hạn' in expired['message'], 'An expired right must say so.')
    request(f'{API}/api/listen/audio/{audio_id}', token=cu, expected=403)
    # Quyền chưa tới hạn: báo ngày bắt đầu theo giờ Việt Nam (UTC+7), không phải "hết hạn".
    start = (now + datetime.timedelta(days=2)).replace(microsecond=0)
    request(rights, 'POST', {**grant, 'from': iso(start), 'to': iso(start + datetime.timedelta(days=3))}, token=op)
    upcoming = request(API + '/api/listen', token=cu)
    expected_day = (start + datetime.timedelta(hours=7)).strftime('%d/%m/%Y')
    check(upcoming['hasAccess'] is False and 'hết hạn' not in upcoming['message'] and expected_day in upcoming['message'], 'A not-yet-valid right must announce its start date in Vietnam time.')
    request(f'{API}/api/listen/audio/{audio_id}', token=cu, expected=403)


if __name__ == '__main__':
    try:
        if '--start' in sys.argv:
            with smoke.running_apps(api_only=True):
                run()
        else:
            run()
        print(f'PASS: {checks} SCR06/07/11/14/15/20 HTTP and SQL assertions (+{smoke.checks} request checks).')
    finally:
        cleanup()
