"""Launch built API/Web and run Playwright UI smoke checks."""
import os
import subprocess
import uuid

import smoke


try:
    with smoke.running_apps():
        subprocess.run(['node', str(smoke.ROOT / 'tests/browser.cjs')], cwd=smoke.ROOT, check=True)

        customer_email = f'auth-test-nam-ui-{uuid.uuid4().hex}@example.test'
        smoke.test_emails.append(customer_email)
        staff_email, _, staff = smoke.registration('Nhân viên kiểm thử giao diện')
        smoke.sql_non_query(f'''
INSERT INTO TAIKHOAN_VAITRO (MaTK, MaVaiTro)
SELECT account.MaTK, role.MaVaiTro
FROM TAIKHOAN AS account
CROSS JOIN VAITRO AS role
WHERE account.Email = {smoke.sql_literal(staff_email)}
  AND role.TenVaiTro = 'Admin';
''')
        environment = os.environ.copy()
        environment['NAM_UI_CUSTOMER_EMAIL'] = customer_email
        environment['NAM_UI_STAFF_TOKEN'] = staff['token']
        subprocess.run(['node', str(smoke.ROOT / 'tests/nam_browser.cjs')], cwd=smoke.ROOT, env=environment, check=True)
finally:
    smoke.cleanup_test_accounts()
    smoke.test_emails.clear()
