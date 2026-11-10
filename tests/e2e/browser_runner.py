"""Launch built API/Web and run Playwright installed in the repository."""
import subprocess
import os
import sys, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / 'api'))  # shared harness lives in tests/api
import smoke
from smoke import running_apps, ROOT
from profile import create, snapshot

baseline = snapshot()
try:
    with running_apps():
        smoke.catalog_ids()
        subprocess.run(['node', str(ROOT / 'tests/e2e/browser.cjs')], cwd=ROOT, check=True)
        address, _ = create()
        duplicate, _ = create()
        renamed = f'scr21-browser-{smoke.uuid.uuid4().hex}@example.test'
        smoke.test_emails.append(renamed)
        environment = os.environ.copy()
        environment.update(SCR21_EMAIL=address, SCR21_DUPLICATE=duplicate, SCR21_RENAMED=renamed)
        subprocess.run(['node', str(ROOT / 'tests/e2e/profile_browser.cjs')], cwd=ROOT, env=environment, check=True)
        subprocess.run(['node', str(ROOT / 'tests/e2e/account_area_browser.cjs')], cwd=ROOT, env=environment, check=True)
finally:
    smoke.cleanup_test_accounts()
    assert snapshot() == baseline, 'Browser tests changed existing accounts / roles or left test data.'
