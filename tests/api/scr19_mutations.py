"""SCR19 tests against one unique temporary account; exact-account cleanup in finally."""
import uuid
from concurrent.futures import ThreadPoolExecutor
import smoke
from scr19_readonly import request, login

email = 'scr19-' + uuid.uuid4().hex + '@example.test'
password = 'ScreenPass2026'
smoke.test_emails.append(email)
admin = login('admin')
try:
    code, result = request('/api/auth/register', body={'name': 'SCR19 test', 'email': email, 'password': password, 'confirmPassword': password, 'termsAccepted': True})
    assert code == 201, code
    customer_token = result['token']
    account = result['user']['id']
    path = f'/api/admin/accounts/{account}'
    def groups():
        result = []
        for group in ('staff', 'customer'):
            code, page = request('/api/admin/accounts/paged?group=' + group + '&query=' + email, admin)
            assert code == 200
            result.append([x['id'] for x in page['items']])
        return result
    assert groups() == [[], [account]]
    def roles(new, expected):
        return request(path + '/roles', admin, {'roles': new, 'expectedRoles': expected}, 'PUT')[0]
    assert roles([], ['Customer']) == 400
    assert roles(['Customer', 'Operator'], ['Customer']) == 204
    assert groups() == [[account], []]
    assert roles(['Customer'], ['Customer']) == 409
    assert request('/api/auth/me', customer_token)[0] == 401
    code, updated = request('/api/auth/login', body={'email': email, 'password': password})
    assert code == 200 and 'Operator' in updated['user']['roles']
    assert roles(['Customer'], ['Customer', 'Operator']) == 204
    assert groups() == [[], [account]]
    assert request('/api/auth/me', updated['token'])[0] == 401
    code, current = request('/api/auth/login', body={'email': email, 'password': password})
    assert code == 200 and current['user']['roles'] == ['Customer']
    assert request(path + '/status', admin, {'status': 'Locked', 'expectedStatus': 'Active', 'reason': 'Kiểm thử alias'}, 'PUT')[0] == 204
    assert request('/api/auth/me', customer_token)[0] == 401
    assert request('/api/auth/me', updated['token'])[0] == 401
    assert request('/api/auth/me', current['token'])[0] == 401
    assert request('/api/auth/login', body={'email': email, 'password': password})[0] == 401
    assert request(path + '/status', admin, {'status': 'Active', 'expectedStatus': 'Locked'}, 'PUT')[0] == 204
    code, unlocked = request('/api/auth/login', body={'email': email, 'password': password})
    assert code == 200 and unlocked['user']['roles'] == ['Customer']
    entries = request('/api/admin/accounts/audit?page=1', admin)[1]['items']
    changes = [x for x in entries if x['objectType'] == 'TAIKHOAN' and x['objectId'] == str(account)]
    assert len(changes) == 4, len(changes)
    assert any(x['reason'] == 'Kiểm thử alias' for x in changes)
    staff_entries = request('/api/admin/accounts/audit?actorType=staff&page=1', admin)[1]['items']
    assert len([x for x in staff_entries if x['objectType'] == 'TAIKHOAN' and x['objectId'] == str(account) and x['actorType'] == 'staff']) == 4
    # Both API surfaces write through the same role rules and stale-write check.
    assert request(f'/api/admin/users/{account}/roles', admin, {'roles': ['Customer', 'Operator']}, 'PUT')[0] == 200
    assert request('/api/auth/me', unlocked['token'])[0] == 401
    assert roles(['Customer'], ['Customer']) == 409
    code, operator_session = request('/api/auth/login', body={'email': email, 'password': password})
    assert code == 200 and set(operator_session['user']['roles']) == {'Customer', 'Operator'}
    assert roles(['Customer'], ['Customer', 'Operator']) == 204
    assert request('/api/auth/me', operator_session['token'])[0] == 401
    assert roles(['Customer', 'Admin'], ['Customer']) == 204
    code, temp_admin = request('/api/auth/login', body={'email': email, 'password': password})
    assert code == 200
    assert request(path + '/roles', temp_admin['token'], {'roles': ['Customer'], 'expectedRoles': ['Admin', 'Customer']}, 'PUT')[0] == 409
    with ThreadPoolExecutor(max_workers=2) as pool:
        results = list(pool.map(lambda _: roles(['Customer', 'Operator'], ['Admin', 'Customer']), range(2)))
    assert sorted(results) == [204, 409], results
    print('SCR19 temporary-account mutation checks passed.')
finally:
    smoke.cleanup_test_accounts()
