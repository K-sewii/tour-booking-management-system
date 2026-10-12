"""Launch built API/Web and run Playwright installed in the repository."""
import subprocess
from smoke import running_apps, ROOT
with running_apps():
    subprocess.run(['node', str(ROOT / 'tests/browser.cjs')], cwd=ROOT, check=True)
