"""Read-only SCR19 authorization and empty statistics checks against an isolated API process."""
import json
import subprocess
import urllib.error
import urllib.request

BASE = "http://127.0.0.1:5098"
lines = subprocess.check_output(["dotnet", "user-secrets", "list", "--project", "src/Backend/DongNamTrip.Api"], text=True)
secrets = dict(line.split(" = ", 1) for line in lines.splitlines() if " = " in line)

def request(path, token=None, body=None, method=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(BASE + path, headers=headers, data=json.dumps(body).encode() if body is not None else None, method=method)
    try:
        response = urllib.request.urlopen(req, timeout=15)
    except urllib.error.HTTPError as exc:
        response = exc
    data = response.read()
    return response.status, json.loads(data) if data else None

def login(role):
    code, result = request("/api/auth/login", body={"email": role + "@dongnamtrip.test", "password": secrets["DevelopmentTestAccounts:Passwords:" + role]})
    assert code == 200, f"{role} login returned {code}"
    return result["token"]

assert request("/api/admin/stats")[0] == 401
for name in ("operator", "customer1"):
    token = login(name)
    assert request("/api/admin/stats", token)[0] == 403, name
    assert request("/api/admin/audit/search?actorType=staff", token)[0] == 403, name
admin = login("admin")
assert request("/api/admin/users")[0] == 401
assert request("/api/admin/users", admin)[0] == 200
assert request("/api/admin/users/grouped?group=staff")[0] == 401
assert request("/api/admin/users/grouped?group=invalid", admin)[0] == 400
assert request("/api/admin/users/grouped?group=customer&status=Active", admin)[0] == 200
assert request("/api/admin/users/roles", admin)[0] == 200
assert request("/api/admin/audit/search?page=1", admin)[0] == 200
assert request("/api/admin/audit/search?actorType=staff")[0] == 401
assert request("/api/admin/audit/search?actorType=invalid", admin)[0] == 400
assert request("/api/admin/audit/search?from=2026-12-31&to=2026-01-01", admin)[0] == 400
assert request("/api/admin/audit/search?to=9999-12-31", admin)[0] == 400
code, audit = request("/api/admin/audit/search?from=2026-01-01&to=2026-12-31", admin)
assert code == 200 and audit["total"] >= len(audit["items"]) and audit["totalPages"] == max(1, (audit["total"] + 49) // 50)
assert all("2025-12-31T17:00:00" <= row["time"][:19] < "2026-12-31T17:00:00" for row in audit["items"])
code, audit = request("/api/admin/audit/search?query=SeedCatalogPublicData", admin)
assert code == 200 and all(row["action"] == "SeedCatalogPublicData" or "SeedCatalogPublicData" in (row["actor"] or "") for row in audit["items"])
for kind in ("staff", "customer", "system"):
    code, audit = request("/api/admin/audit/search?actorType=" + kind, admin)
    assert code == 200 and all(row["actorType"] == kind for row in audit["items"]), (kind, audit)
    assert len(audit["items"]) <= 50
code, stats = request("/api/admin/stats?from=2026-01-01&to=2026-12-31", admin)
assert code == 200 and stats["orderValue"] == 0 and stats["received"] == 0 and stats["refunded"] == 0 and stats["orders"] == [], stats
assert request("/api/admin/stats?from=2026-12-31&to=2026-01-01", admin)[0] == 400
assert request("/api/admin/stats?from=0001-01-01&to=0001-01-01", admin)[0] == 400
assert request("/api/admin/stats?from=9999-12-31&to=9999-12-31", admin)[0] == 400
assert request("/api/admin/accounts", admin)[0] == 404
print("SCR19 read-only HTTP checks passed.")
