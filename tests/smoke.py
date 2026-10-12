"""API/Web smoke checks. Run after starting apps, or use --start (build first)."""
import contextlib
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

ROOT = Path(__file__).resolve().parents[1]
API, WEB = 'http://localhost:5080', 'http://localhost:5081'
checks = 0

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
    return json.loads(body) if response.headers.get('Content-Type','').startswith('application/json') else body

@contextlib.contextmanager
def running_apps():
    dotnet = os.environ.get('DOTNET_EXE', 'dotnet')
    processes, logs = [], []
    try:
        for name in ('Api', 'Web'):
            log = tempfile.TemporaryFile(); logs.append(log)
            processes.append(subprocess.Popen([dotnet,'run','--no-build','--project',f'src/DongNamTrip.{name}'], cwd=ROOT, stdout=log, stderr=log))
        for url in (API+'/health', WEB+'/Account/Login'):
            for attempt in range(80):
                try:
                    urllib.request.urlopen(url, timeout=1).close(); break
                except (urllib.error.URLError, TimeoutError):
                    if any(p.poll() is not None for p in processes):
                        raise RuntimeError('An application exited before readiness.')
                    time.sleep(.25)
            else:
                raise RuntimeError('Application did not become ready: '+url)
        yield
    except Exception:
        for log in logs:
            log.seek(0); print(log.read().decode(errors='replace')[-5000:], file=sys.stderr)
        raise
    finally:
        for process in processes:
            process.terminate()
        for process in processes:
            try: process.wait(timeout=10)
            except subprocess.TimeoutExpired: process.kill(); process.wait()
        for log in logs: log.close()

def run():
    request(API+'/health')
    places = request(API+'/api/destinations'); assert len(places) >= 6
    request(API+'/api/itineraries', expected=401)
    request(API+'/api/auth/login','POST',{'email':'khach@dongnamtrip.local','password':'wrong'},expected=401)
    def login(email):
        return request(API+'/api/auth/login','POST',{'email':email,'password':'Demo@123'})['token']
    admin, customer, other = (login(x+'@dongnamtrip.local') for x in ('admin','khach','khach2'))
    place = {'name':'Điểm kiểm thử','region':'Tây Ninh','category':'Văn hóa','address':'Địa chỉ mẫu','description':'Nội dung phục vụ smoke test.','visitMinutes':60}
    request(API+'/api/destinations','POST',place,expected=401)
    request(API+'/api/destinations','POST',place,customer,expected=403)
    request(API+'/api/destinations','POST',{**place,'visitMinutes':-1},admin,400)
    request(API+'/api/destinations','POST',{**place,'region':'Ngoài danh mục'},admin,400)
    saved = request(API+'/api/destinations','POST',place,admin)
    updated = request(API+f'/api/destinations/{saved["id"]}','PUT',{**place,'name':'Điểm đã chỉnh sửa'},admin)
    assert updated['name'] == 'Điểm đã chỉnh sửa'
    assert any(x['id']==saved['id'] for x in request(API+'/api/destinations'))
    tomorrow = (datetime.date.today()+datetime.timedelta(days=1)).isoformat()
    itinerary = {'name':'Chuyến kiểm thử','startDate':tomorrow,'guests':2,'destinationIds':[1,6]}
    trip = request(API+'/api/itineraries','POST',itinerary,customer)
    assert trip['destinationIds'] == [1,6]
    assert any(x['id']==trip['id'] for x in request(API+'/api/itineraries',token=customer))
    assert all(x['id']!=trip['id'] for x in request(API+'/api/itineraries',token=other))
    for invalid in ({'destinationIds':[]},{'destinationIds':None},{'destinationIds':[1,1]},{'destinationIds':[99999]},{'guests':0},{'startDate':'2000-01-01'}):
        request(API+'/api/itineraries','POST',{**itinerary,**invalid},customer,400)
    for path in ('/','/Tours','/Tours?page=2','/Tours/Details/1','/Destinations','/Destinations/Details/1','/Planner','/Account/Login','/Account/Register'):
        page=request(WEB+path); assert '<html lang="vi">' in page
    page = html.unescape(request(WEB+'/Tours?region='+urllib.parse.quote('Vũng Tàu')))
    assert '1 hành trình' in page and 'Vũng Tàu: một ngày cùng biển' in page
    assert 'Điểm đã chỉnh sửa' in html.unescape(request(WEB+'/Destinations/Details/'+str(saved['id'])))
    request(WEB+'/Tours/Details/99999',expected=404)
    request(WEB+'/Account/Login','POST',{},expected=400)  # Antiforgery
    request(API+'/api/auth/logout','POST',token=customer,expected=204)
    request(API+'/api/itineraries',token=customer,expected=401)
    again=login('khach@dongnamtrip.local')
    assert any(x['id']==trip['id'] for x in request(API+'/api/itineraries',token=again))
    for token in (admin,other,again): request(API+'/api/auth/logout','POST',token=token,expected=204)
    print(f'PASS: {checks} HTTP checks; authorization, validation, ownership, logout, shared catalog, MVC routes and CSRF.')

if __name__=='__main__':
    if '--start' in sys.argv:
        with running_apps(): run()
    else: run()
