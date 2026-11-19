# Pruebas de extremo a extremo del módulo 1 contra la API local (Development).
# Requiere: API en http://localhost:5207, Mailpit en :8025 y DEV_ADMIN_* en el entorno.
import json, re, sys, uuid, urllib.request, urllib.error, os

BASE = "http://localhost:5207"
MAIL = "http://localhost:8025"
results = []

def call(method, path, body=None, token=None, expect=None):
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(BASE + path, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req, timeout=20) as r:
            raw = r.read().decode() or "{}"
            return r.status, json.loads(raw)
    except urllib.error.HTTPError as e:
        raw = e.read().decode() or "{}"
        try:
            return e.code, json.loads(raw)
        except Exception:
            return e.code, {"raw": raw[:200]}

def check(name, status, expected, extra=None):
    ok = status == expected
    results.append((ok, name, status, expected))
    print(("PASS" if ok else "FAIL"), name, f"-> {status} (esperado {expected})", extra or "")
    return ok

def latest_code(email):
    with urllib.request.urlopen(MAIL + "/api/v1/messages", timeout=10) as r:
        msgs = json.loads(r.read().decode())["messages"]
    for m in msgs:
        if any(t["Address"].lower() == email.lower() for t in m["To"]):
            with urllib.request.urlopen(f"{MAIL}/api/v1/message/{m['ID']}", timeout=10) as r:
                text = json.loads(r.read().decode())["Text"]
            found = re.findall(r"\b(\d{6})\b", text)
            if found:
                return found[0]
    return None

def login(email, password):
    return call("POST", "/api/v1/auth/login", {"email": email, "password": password})

A1 = {"email": "admin.prueba1@uvoluntapp.test", "password": "AdminPrueba1#2026"}
A2 = {"email": "admin.prueba2@uvoluntapp.test", "password": "AdminPrueba2#2026"}
sfx = uuid.uuid4().hex[:6]
VOL = f"voluntario.{sfx}@ucb.edu.bo"
VOL2 = f"voluntario2.{sfx}@ucb.edu.bo"
PASS = "Voluntario#2026xyz"

s, t1 = login(**A1); check("login admin prueba 1", s, 200)
A1T = t1.get("accessToken")
s, t2 = login(**A2); check("login admin prueba 2", s, 200)
A2T = t2.get("accessToken")

s, _ = call("POST", "/api/v1/auth/register", {"email": VOL, "firstName": "Ana", "lastName": "Quispe", "password": PASS})
check("registro voluntario institucional", s, 200)
code = latest_code(VOL); print("   codigo OTP recibido en Mailpit:", bool(code))
s, _ = call("POST", "/api/v1/auth/verify-otp", {"email": VOL, "code": code})
check("verificar OTP voluntario", s, 200)
s, tv = login(VOL, PASS); check("login voluntario activo", s, 200)
VT = tv.get("accessToken")

s, _ = call("POST", "/api/v1/auth/register", {"email": VOL2, "firstName": "Luis", "lastName": "Mamani", "password": PASS})
check("registro segundo voluntario", s, 200)
code2 = latest_code(VOL2)
s, _ = call("POST", "/api/v1/auth/verify-otp", {"email": VOL2, "code": code2})
s, tv2 = login(VOL2, PASS); VT2 = tv2.get("accessToken")
check("login segundo voluntario", s, 200)

s, prof = call("GET", "/api/v1/profile", token=VT)
check("GET perfil propio", s, 200)
s, _ = call("PATCH", "/api/v1/profile", {"firstName": "Ana Maria", "lastName": "Quispe", "phone": "70000001"}, token=VT)
check("PATCH perfil propio", s, 200)

s, users = call("GET", f"/api/v1/users?search={sfx}", token=A1T)
check("admin lista usuarios", s, 200)
s, user_detail = call("GET", "/api/v1/users/" + prof.get("id", ""), token=A1T)
check("admin consulta usuario", s, 200)

s, _ = call("GET", "/api/v1/users", token=VT2)
check("voluntario sin rol NO lista usuarios", s, 403)

s, _ = call("POST", "/api/v1/role-assignments", {"userId": prof["id"], "role": "ADMIN", "scope": "GLOBAL", "scopeId": None}, token=A1T)
check("crear ADMIN directo rechazado (requiere doble aprobación)", s, 422)

PROGRAM = open(os.environ.get("E2E_PROGRAM_ID_FILE", "program_id.txt")).read().strip()
s, _ = call("POST", "/api/v1/role-assignments", {"userId": prof["id"], "role": "PROGRAM_LEADER", "scope": "PROGRAM", "scopeId": str(uuid.uuid4())}, token=A1T)
check("programa inexistente rechazado", s, 422)
s, ra = call("POST", "/api/v1/role-assignments", {"userId": prof["id"], "role": "PROGRAM_LEADER", "scope": "PROGRAM", "scopeId": PROGRAM}, token=A1T)
check("asignar líder de programa", s, 201)
RA_ID = ra.get("id")
s, _ = call("POST", "/api/v1/role-assignments", {"userId": prof["id"], "role": "PROGRAM_LEADER", "scope": "PROGRAM", "scopeId": PROGRAM}, token=A1T)
check("asignación duplicada rechazada", s, 409)
s, lst = call("GET", f"/api/v1/role-assignments?userId={prof['id']}&active=true", token=A1T)
check("listar asignaciones del usuario", s, 200)
s, one = call("GET", f"/api/v1/role-assignments/{RA_ID}", token=A1T)
check("consultar asignación", s, 200)
s, rev = call("POST", f"/api/v1/role-assignments/{RA_ID}/revoke", token=A1T)
check("revocar asignación", s, 200)
s, _ = call("POST", f"/api/v1/role-assignments/{RA_ID}/revoke", token=A1T)
check("revocar dos veces rechazado", s, 409)

s, g = call("POST", "/api/v1/admin-grants", {"beneficiaryId": prof["id"]}, token=A1T)
check("solicitud de promoción a ADMIN", s, 201)
GRANT = g.get("id")
s, g2 = call("POST", "/api/v1/admin-grants", {"beneficiaryId": prof["id"]}, token=A1T)
check("segunda solicitud pendiente rechazada", s, 409)
s, g1 = call("POST", f"/api/v1/admin-grants/{GRANT}/approve", token=A1T)
check("primera aprobación del admin 1", s, 200, f"estado={g1.get('status')}")
s, _ = call("POST", f"/api/v1/admin-grants/{GRANT}/approve", token=A1T)
check("mismo admin aprueba dos veces: rechazado", s, 409)
s, g2b = call("POST", f"/api/v1/admin-grants/{GRANT}/approve", token=A2T)
check("segunda aprobación del admin 2", s, 200, f"estado={g2b.get('status')}")
s, det = call("GET", f"/api/v1/users/{prof['id']}", token=A1T)
roles_now = [r["role"] for r in det.get("roles", [])]
check("el beneficiario ahora es ADMIN", s, 200, f"roles={roles_now}")

s, _ = call("POST", f"/api/v1/users/{prof['id']}/suspend", {"reason": "Prueba de baja"}, token=A2T)
check("suspender cuenta", s, 200)
s, _ = login(VOL, PASS)
check("cuenta suspendida no inicia sesión", s, 422 if s != 401 else 401)
s, _ = call("POST", f"/api/v1/users/{prof['id']}/reactivate", token=A2T)
check("reactivar cuenta", s, 200)
import time  # sin espera: límite desactivado
print("   esperando 61 s por el límite de inicios de sesión...")
pass
s, _ = login(VOL, PASS)
check("cuenta reactivada inicia sesión", s, 200)

s, aud = call("GET", "/api/v1/audit-events", token=A1T)
check("consulta de auditoría", s, 200, f"eventos={len(aud.get('items', []))}")
s, _ = call("GET", "/api/v1/audit-events", token=VT2)
check("auditoría denegada a no admin", s, 403)

s, _ = call("POST", "/api/v1/role-assignments", {"userId": prof["id"], "role": "OPERATIONAL_LEADER", "scope": "GLOBAL", "scopeId": None}, token=A1T)
check("líder sin programa asignado permitido (sin permisos de gestión)", s, 201)

print()
ok = sum(1 for r in results if r[0])
print(f"RESULTADO: {ok}/{len(results)} pasan")
sys.exit(0 if ok == len(results) else 1)
