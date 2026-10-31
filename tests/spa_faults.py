"""Exercise expiry and API outage at the SPA's cookie and antiforgery bridge."""
import http.cookiejar
import json
import time
import urllib.error
import urllib.request

import smoke


def run(email):
    jar = http.cookiejar.CookieJar()
    opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))

    def call(path, method='GET', payload=None, csrf=None):
        headers = {'Accept': 'application/json'}
        if csrf:
            headers['RequestVerificationToken'] = csrf
        if payload is not None:
            headers['Content-Type'] = 'application/json'
        req = urllib.request.Request(smoke.WEB + '/spa-api/' + path,
                                     data=json.dumps(payload).encode() if payload is not None else None,
                                     method=method, headers=headers)
        try:
            response = opener.open(req, timeout=15)
        except urllib.error.HTTPError as error:
            response = error
        body = response.read().decode()
        return response.status, json.loads(body) if body and response.headers.get('Content-Type', '').startswith('application/json') else body

    with smoke.running_apps(session_lifetime_seconds=2, spa=True) as processes:
        csrf = call('csrf')[1]['token']
        assert call('login', 'POST', {'email': email, 'password': 'ProfilePass2026'}, csrf)[0] == 200
        assert call('profile')[0] == 200
        time.sleep(2.5)
        assert call('profile')[0] == 401
        assert call('session')[1]['user'] is None
        csrf = call('csrf')[1]['token']
        assert call('login', 'POST', {'email': email, 'password': 'ProfilePass2026'}, csrf)[0] == 200
        processes[0].terminate()
        processes[0].wait(timeout=10)
        assert call('session')[0] == 503
        assert call('profile')[0] == 503
    print('SPA bridge PASS: expired cookie/session, API outage and profile load blocked.')
