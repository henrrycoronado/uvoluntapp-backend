# Pruebas de extremo a extremo de los permisos de líderes (módulo 1).
# Requiere: API en http://localhost:5207, Mailpit en :8025, DEV_ADMIN_* en el entorno y al menos dos programas.
import json, os, re, secrets, sys, uuid, urllib.request, urllib.error
# Clave de las cuentas desechables de esta corrida, generada al azar.
LEADER_KEY = secrets.token_urlsafe(12) + "A1#"
BASE = "http://localhost:5207"; MAIL = "http://localhost:8025"
results = []
def call(method, path, body=None, token=None):
    req = urllib.request.Request(BASE + path, data=json.dumps(body).encode() if body is not None else None, method=method)
    req.add_header("Content-Type", "application/json")
    if token: req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req, timeout=20) as r: return r.status, json.loads(r.read().decode() or "{}")
    except urllib.error.HTTPError as e:
        raw = e.read().decode() or "{}"
        try: return e.code, json.loads(raw)
        except Exception: return e.code, {}
def check(name, status, expected, extra=""):
    ok = status == expected; results.append(ok)
    print("PASS" if ok else "FAIL", name, f"-> {status} (esperado {expected})", extra)
def code_for(email):
    msgs = json.loads(urllib.request.urlopen(MAIL + "/api/v1/messages", timeout=10).read().decode())["messages"]
    for m in msgs:
        if any(t["Address"].lower() == email.lower() for t in m["To"]):
            text = json.loads(urllib.request.urlopen(f"{MAIL}/api/v1/message/{m['ID']}", timeout=10).read().decode())["Text"]
            f = re.findall(r"\b(\d{6})\b", text)
            if f: return f[0]
def new_active_user(prefix):
    email = f"{prefix}.{uuid.uuid4().hex[:6]}@ucb.edu.bo"
    call("POST", "/api/v1/auth/register", {"email": email, "firstName": prefix.title(), "lastName": "Prueba", "password": LEADER_KEY})
    call("POST", "/api/v1/auth/verify-otp", {"email": email, "code": code_for(email)})
    _, t = call("POST", "/api/v1/auth/login", {"email": email, "password": LEADER_KEY})
    _, p = call("GET", "/api/v1/profile", token=t["accessToken"])
    return p["id"], t["accessToken"]

_, a = call("POST", "/api/v1/auth/login", {"email": os.environ["DEV_ADMIN_1_EMAIL"], "password": os.environ["DEV_ADMIN_1_PASSWORD"]})
ADMIN = a["accessToken"]
s, progs = call("GET", "/api/v1/programs", token=ADMIN); check("admin lista todos los programas", s, 200, f"total={progs.get('meta',{}).get('totalCount')}")
ids = [p["id"] for p in progs.get("items", [])]
if len(ids) < 2: print("Se necesitan al menos dos programas"); sys.exit(1)
PA, PB = ids[0], ids[1]

leader_id, LEADER = new_active_user("lider")
op_id, OP = new_active_user("operativo")
other_id, OTHER = new_active_user("otro")
nolead_id, NOLEAD = new_active_user("sinprograma")

s, vol_programs = call("GET", "/api/v1/programs", token=OTHER)
only_active = all(p["status"] == "ACTIVE" for p in vol_programs.get("items", []))
check("voluntario solo ve programas activos", 200 if (s == 200 and only_active) else s, 200)
s, _ = call("GET", "/api/v1/users", token=OTHER); check("voluntario no consulta usuarios", s, 403)

s, pl = call("POST", "/api/v1/role-assignments", {"userId": leader_id, "role": "PROGRAM_LEADER", "scope": "PROGRAM", "scopeId": PA}, token=ADMIN)
check("admin asigna líder de programa en A", s, 201)
s, ol = call("POST", "/api/v1/role-assignments", {"userId": op_id, "role": "OPERATIONAL_LEADER", "scope": "PROGRAM", "scopeId": PA}, token=LEADER)
check("líder asigna líder operativo en su programa A", s, 201)
s, _ = call("POST", "/api/v1/role-assignments", {"userId": op_id, "role": "OPERATIONAL_LEADER", "scope": "PROGRAM", "scopeId": PB}, token=LEADER)
check("líder NO asigna en el programa B que no lidera", s, 403)
s, _ = call("POST", "/api/v1/role-assignments", {"userId": other_id, "role": "PROGRAM_LEADER", "scope": "PROGRAM", "scopeId": PA}, token=LEADER)
check("líder NO crea otro líder de programa", s, 403)
s, lst = call("GET", "/api/v1/role-assignments", token=LEADER)
scoped = all(i.get("scopeId") == PA for i in lst.get("items", []))
check("líder solo ve asignaciones de su programa", 200 if (s == 200 and scoped) else s, 200, f"items={len(lst.get('items', []))}")
s, _ = call("GET", "/api/v1/users", token=OP); check("líder operativo revisa usuarios", s, 200)
s, _ = call("POST", f"/api/v1/users/{other_id}/suspend", {"reason": "no permitido"}, token=OP); check("líder operativo NO suspende", s, 403)
s, _ = call("POST", f"/api/v1/users/{other_id}/suspend", {"reason": "no permitido"}, token=LEADER); check("líder de programa NO suspende", s, 403)
s, _ = call("GET", "/api/v1/accounts/approval-requests", token=LEADER); check("líder NO ve solicitudes de cuentas", s, 403)
s, _ = call("POST", f"/api/v1/role-assignments/{ol['id']}/revoke", token=LEADER); check("líder revoca líder operativo de su programa", s, 200)
s, pl2 = call("POST", "/api/v1/role-assignments", {"userId": other_id, "role": "PROGRAM_LEADER", "scope": "PROGRAM", "scopeId": PA}, token=ADMIN)
check("admin asigna otro líder de programa en A", s, 201)
s, _ = call("POST", f"/api/v1/role-assignments/{pl2['id']}/revoke", token=LEADER); check("líder NO revoca a otro líder de programa", s, 403)
s, _ = call("POST", f"/api/v1/role-assignments/{pl2['id']}/revoke", token=ADMIN); check("admin revoca cualquier rol", s, 200)

s, _ = call("POST", "/api/v1/role-assignments", {"userId": nolead_id, "role": "PROGRAM_LEADER", "scope": "GLOBAL", "scopeId": None}, token=ADMIN)
check("admin crea líder sin programa", s, 201)
s, _ = call("POST", "/api/v1/role-assignments", {"userId": op_id, "role": "OPERATIONAL_LEADER", "scope": "PROGRAM", "scopeId": PA}, token=NOLEAD)
check("líder sin programa NO asigna", s, 403)
s, _ = call("GET", "/api/v1/role-assignments", token=NOLEAD); check("líder sin programa NO lista asignaciones", s, 403)
s, _ = call("POST", f"/api/v1/users/{other_id}/suspend", {"reason": "Prueba de baja de voluntario"}, token=ADMIN); check("admin da de baja a un voluntario", s, 200)

print(); ok = sum(results); print(f"RESULTADO líderes: {ok}/{len(results)} pasan"); sys.exit(0 if ok == len(results) else 1)
