"""Inject transport faults between the real Web and API; never change SQL configuration."""
import contextlib
import http.client
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import html
import os
import threading
import time

import smoke
from auth_screens import api, check, post_form
from profile import create, form, snapshot, web_login


class Proxy(BaseHTTPRequestHandler):
    mode = 'normal'

    def log_message(self, *_):
        pass  # Do not log Authorization headers or request bodies.

    def forward(self):
        if self.headers.get('Transfer-Encoding', '').lower() == 'chunked':
            chunks = []
            while True:
                size = int(self.rfile.readline().split(b';', 1)[0].strip(), 16)
                if size == 0:
                    while self.rfile.readline().strip():
                        pass
                    break
                chunks.append(self.rfile.read(size))
                self.rfile.read(2)  # CRLF after each chunk.
            body = b''.join(chunks)
        else:
            body = self.rfile.read(int(self.headers.get('Content-Length', 0)))
        mode = type(self).mode
        if self.path == '/api/auth/profile':
            trigger = ((self.command == 'GET' and mode.startswith('get-'))
                       or (self.command == 'PUT' and mode.startswith('put-')))
            if trigger:
                if mode.endswith('slow'):
                    time.sleep(11)
                self.send_response(503 if self.command == 'GET' else 500)
                self.send_header('Content-Length', '0')
                self.end_headers()
                return
        connection = http.client.HTTPConnection('localhost', 5080, timeout=15)
        try:
            connection.request(self.command, self.path, body=body, headers={
                key: value for key, value in self.headers.items()
                if key.lower() in ['authorization', 'content-type', 'accept']
            })
            response = connection.getresponse()
            payload = response.read()
            self.send_response(response.status)
            self.send_header('Content-Type', response.getheader('Content-Type', 'application/json'))
            self.send_header('Content-Length', str(len(payload)))
            self.end_headers()
            self.wfile.write(payload)
        finally:
            connection.close()

    do_GET = do_POST = do_PUT = forward


@contextlib.contextmanager
def fault_proxy():
    server = ThreadingHTTPServer(('127.0.0.1', 5082), Proxy)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    previous = os.environ.get('ApiBaseUrl')
    os.environ['ApiBaseUrl'] = 'http://localhost:5082/'
    thread.start()
    try:
        yield
    finally:
        if previous is None:
            os.environ.pop('ApiBaseUrl', None)
        else:
            os.environ['ApiBaseUrl'] = previous
        server.shutdown()
        server.server_close()
        thread.join(timeout=5)


def run():
    address, session = create()
    web, _ = web_login(address)
    fields, _ = form(web)
    Proxy.mode = 'put-error'
    _, failed = post_form(web, smoke.WEB+'/Account/Profile', fields | {'Name': 'Dữ liệu được giữ lại', 'Email': address, 'PhoneNumber': '000123'})
    failed = html.unescape(failed)
    check('API chưa xử lý được yêu cầu.' in failed and 'Dữ liệu được giữ lại' in failed, 'API 500 must retain input and show a retryable error.')
    check(api(smoke.API+'/api/auth/profile', token=session['token'])['name'] == session['user']['name'], 'Injected 500 must not write SQL.')
    Proxy.mode = 'normal'
    _, success = post_form(web, smoke.WEB+'/Account/Profile', fields | {'Name': 'Thử lại thành công', 'Email': address, 'PhoneNumber': '000123'})
    check('Đã lưu thay đổi hồ sơ tài khoản.' in html.unescape(success), 'Retry after API 500 failed.')
    for mode, message in [('get-error', 'API chưa xử lý được yêu cầu.'), ('get-slow', 'API phản hồi chậm.')]:
        Proxy.mode = mode
        page = html.unescape(web.open(smoke.WEB+'/Account/Profile', timeout=15).read().decode())
        check(message in page and 'data-profile-form' not in page and 'Thử tải lại' in page, 'Failed/slow GET must show retry and no writable form.')
    Proxy.mode = 'normal'
    fields, page = form(web)
    check('Thử lại thành công' in html.unescape(page), 'GET retry did not restore complete API data.')
    Proxy.mode = 'put-slow'
    _, failed = post_form(web, smoke.WEB+'/Account/Profile', fields | {'Name': 'Chưa lưu khi timeout', 'Email': address, 'PhoneNumber': '000999'})
    check('API phản hồi chậm.' in html.unescape(failed) and 'Chưa lưu khi timeout' in html.unescape(failed), 'Timed-out PUT must retain input and allow retry.')
    Proxy.mode = 'normal'
    check(api(smoke.API+'/api/auth/profile', token=session['token'])['phoneNumber'] == '000123', 'Injected timeout wrote SQL.')


if __name__ == '__main__':
    baseline = snapshot()
    try:
        with fault_proxy():
            with smoke.running_apps():
                run()
    finally:
        smoke.cleanup_test_accounts()
        check(snapshot() == baseline, 'Fault tests changed existing accounts / roles or left data.')
    import auth_screens
    print(f'PASS: {auth_screens.checks} SCR21 fault assertions; injected API 500/503, GET/PUT timeout and retry against real Web/API/SQL.')
