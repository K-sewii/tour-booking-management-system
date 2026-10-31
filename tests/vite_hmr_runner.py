"""Launch API/Web plus Vite and verify its browser development path."""
import subprocess
import tempfile
import time
import urllib.error
import urllib.request

import smoke


vite_cli = smoke.ROOT / 'src' / 'DongNamTrip.Spa' / 'node_modules' / 'vite' / 'bin' / 'vite.js'
assert vite_cli.exists(), 'Run npm ci in src/DongNamTrip.Spa before the Vite HMR check.'

with smoke.running_apps(spa=True):
    with tempfile.TemporaryFile() as log:
        vite = subprocess.Popen(
            ['node', str(vite_cli), '--host', '127.0.0.1'],
            cwd=smoke.ROOT / 'src' / 'DongNamTrip.Spa', stdout=log, stderr=log,
        )
        try:
            for _ in range(80):
                if vite.poll() is not None:
                    raise RuntimeError('Vite exited before readiness.')
                try:
                    urllib.request.urlopen('http://127.0.0.1:5173/Tours/Details/1', timeout=1).close()
                    break
                except (urllib.error.URLError, TimeoutError):
                    time.sleep(.25)
            else:
                raise RuntimeError('Vite did not become ready on port 5173.')
            subprocess.run(['node', str(smoke.ROOT / 'tests' / 'vite_hmr.cjs')], cwd=smoke.ROOT, check=True)
        except Exception:
            log.seek(0)
            print(log.read().decode(errors='replace')[-5000:])
            raise
        finally:
            vite.terminate()
            try:
                vite.wait(timeout=10)
            except subprocess.TimeoutExpired:
                vite.kill()
                vite.wait()
