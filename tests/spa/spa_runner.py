"""Run the built SPA against the existing API and an isolated set of SQL test accounts."""
import os
import subprocess

import sys, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / 'api'))  # shared harness lives in tests/api
import smoke
from profile import create, snapshot
from spa_faults import run as fault_checks


baseline = snapshot()
try:
    with smoke.running_apps(spa=True):
        smoke.catalog_ids()
        first, _ = create()
        second, _ = create()
        registered = f'spa-{smoke.uuid.uuid4().hex}@example.test'
        smoke.test_emails.append(registered)
        env = os.environ.copy()
        env.update(SPA_FIRST=first, SPA_SECOND=second, SPA_REGISTERED=registered)
        subprocess.run(['node', str(smoke.ROOT / 'tests/spa/spa_browser.cjs')], cwd=smoke.ROOT, env=env, check=True)
    fault_checks(first)
finally:
    smoke.cleanup_test_accounts()
    assert snapshot() == baseline, 'SPA test changed existing accounts or left test data.'
