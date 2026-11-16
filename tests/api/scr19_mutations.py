"""SCR19 mutation checks using temporary accounts and the smoke API fixture."""
import sys
import uuid
from concurrent.futures import ThreadPoolExecutor
import smoke
from scr19_readonly import request, login, setup_accounts

def run():
    email = 'scr19-' + uuid.uuid4().hex + '@example.test'
    password = 'ScreenPass2026'
    smoke.test_emails.append(email)
    try:
        setup_accounts()
        admin = login('admin')
        code, result = request('/api/auth/register', body={'name': 'SCR19 test', 'email': email, 'password': password, 'confirmPassword': password, 'termsAccepted': True})
        assert code == 201, code
        customer_token = result['token']
        account = result['user']['id']
        path = f'/api/admin/users/{account}'
        def groups():
            result = []
            for group in ('staff', 'customer'):
                code, page = request('/api/admin/users/grouped?group=' + group + '&q=' + email, admin)
                assert code == 200
                result.append([x['id'] for x in page['items']])
            return result
        assert groups() == [[], [account]]
        def roles(new, expected):
            return request(path + '/roles', admin, {'roles': new, 'expectedRoles': expected}, 'PUT')[0]
        assert roles([], ['Customer']) == 400
        assert roles(['Customer', 'Operator'], ['Customer']) == 200
        assert groups() == [[account], []]
        assert roles(['Customer'], ['Customer']) == 409
        assert request('/api/auth/me', customer_token)[0] == 401
        code, updated = request('/api/auth/login', body={'email': email, 'password': password})
        assert code == 200 and 'Operator' in updated['user']['roles']
        assert roles(['Customer'], ['Customer', 'Operator']) == 200
        assert groups() == [[], [account]]
        assert request('/api/auth/me', updated['token'])[0] == 401
        code, current = request('/api/auth/login', body={'email': email, 'password': password})
        assert code == 200 and current['user']['roles'] == ['Customer']
        assert request(path + '/status', admin, {'status': 'Locked', 'expectedStatus': 'Active', 'reason': 'Kiểm thử khóa'}, 'POST')[0] == 200
        assert request('/api/auth/me', customer_token)[0] == 401
        assert request('/api/auth/me', updated['token'])[0] == 401
        assert request('/api/auth/me', current['token'])[0] == 401
        assert request('/api/auth/login', body={'email': email, 'password': password})[0] == 401
        assert request(path + '/status', admin, {'status': 'Active', 'expectedStatus': 'Locked'}, 'POST')[0] == 200
        code, unlocked = request('/api/auth/login', body={'email': email, 'password': password})
        assert code == 200 and unlocked['user']['roles'] == ['Customer']
        entries = request('/api/admin/audit/search?page=1', admin)[1]['items']
        changes = [x for x in entries if x['objectType'] == 'TAIKHOAN' and x['objectId'] == str(account)]
        assert len(changes) == 4, len(changes)
        assert any(x['reason'] == 'Kiểm thử khóa' for x in changes)
        staff_entries = request('/api/admin/audit/search?actorType=staff&page=1', admin)[1]['items']
        assert len([x for x in staff_entries if x['objectType'] == 'TAIKHOAN' and x['objectId'] == str(account) and x['actorType'] == 'staff']) == 4
        # Both role operations use the same service and stale-write check.
        assert request(f'/api/admin/users/{account}/roles', admin, {'roles': ['Customer', 'Operator']}, 'PUT')[0] == 200
        assert request('/api/auth/me', unlocked['token'])[0] == 401
        assert roles(['Customer'], ['Customer']) == 409
        code, operator_session = request('/api/auth/login', body={'email': email, 'password': password})
        assert code == 200 and set(operator_session['user']['roles']) == {'Customer', 'Operator'}
        assert roles(['Customer'], ['Customer', 'Operator']) == 200
        assert request('/api/auth/me', operator_session['token'])[0] == 401
        assert roles(['Customer', 'Admin'], ['Customer']) == 200
        code, temp_admin = request('/api/auth/login', body={'email': email, 'password': password})
        assert code == 200
        assert request(path + '/roles', temp_admin['token'], {'roles': ['Customer'], 'expectedRoles': ['Admin', 'Customer']}, 'PUT')[0] == 409
        with ThreadPoolExecutor(max_workers=2) as pool:
            results = list(pool.map(lambda _: roles(['Customer', 'Operator'], ['Admin', 'Customer']), range(2)))
        assert sorted(results) == [200, 409], results
        print('SCR19 temporary-account mutation checks passed.')
    finally:
        smoke.cleanup_test_accounts()

if __name__ == '__main__':
    if '--start' not in sys.argv:
        raise SystemExit('Run with --start to use the isolated API fixture.')
    with smoke.running_apps(api_only=True):
        run()
