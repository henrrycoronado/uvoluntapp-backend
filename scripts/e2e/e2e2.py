# Pruebas de extremo a extremo del módulo 1 contra la API local (Development).
# Requiere: API en http://localhost:5207, Mailpit en :8025 y DEV_ADMIN_* en el entorno.
import json, re, secrets, sys, time, uuid, urllib.request, urllib.error, http.cookiejar, os
BASE = "http://localhost:5207"; MAIL = "http://localhost:8025"
results = []
def call(method, path, body=None, token=None, headers=None):
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(BASE + path, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    if token: req.add_header("Authorization", "Bearer " + token)
    for k, v in (headers or {}).items(): req.add_header(k, v)
    try:
        with urllib.request.urlopen(req, timeout=20) as r:
            raw = r.read().decode() or "{}"
            return r.status, json.loads(raw), r.headers
    except urllib.error.HTTPError as e:
        raw = e.read().decode() or "{}"
        try: return e.code, json.loads(raw), e.headers
        except Exception: return e.code, {"raw": raw[:200]}, e.headers
def check(name, status, expected, extra=""):
    ok = status == expected; results.append(ok)
    print("PASS" if ok else "FAIL", name, f"-> {status} (esperado {expected})", extra)
    return ok
def mails_for(email):
    msgs = json.loads(urllib.request.urlopen(MAIL + "/api/v1/messages", timeout=10).read().decode())["messages"]
    out = []
    for m in msgs:
        if any(t["Address"].lower() == email.lower() for t in m["To"]):
            text = json.loads(urllib.request.urlopen(f"{MAIL}/api/v1/message/{m['ID']}", timeout=10).read().decode())["Text"]
            out.append(text)
    return out
def code_for(email):
    for text in mails_for(email):
        f = re.findall(r"\b(\d{6})\b", text)
        if f: return f[0]
def token_for(email):
    for text in mails_for(email):
        f = re.findall(r"token=([A-Za-z0-9_\-]+)", text) or re.findall(r"\b([A-Za-z0-9_\-]{40,})\b", text)
        if f: return f[0]
def login(email, pwd): return call("POST", "/api/v1/auth/login", {"email": email, "password": pwd})

sfx = uuid.uuid4().hex[:6]
import os
A1 = (os.environ["DEV_ADMIN_1_EMAIL"], os.environ["DEV_ADMIN_1_PASSWORD"])
A2 = (os.environ["DEV_ADMIN_2_EMAIL"], os.environ["DEV_ADMIN_2_PASSWORD"])
_, t1, _ = login(*A1); T1 = t1["accessToken"]
_, t2, _ = login(*A2); T2 = t2["accessToken"]

# --- Recuperación de contraseña
REC = f"recup.{sfx}@ucb.edu.bo"; OLD = "Recuperar#2026abc"; NEW = "Nueva#2026clave"
call("POST", "/api/v1/auth/register", {"email": REC, "firstName": "Rosa", "lastName": "Flores", "password": OLD})
call("POST", "/api/v1/auth/verify-otp", {"email": REC, "code": code_for(REC)})
s, _, _ = call("POST", "/api/v1/auth/forgot-password", {"email": REC}); check("olvidé mi contraseña (respuesta genérica)", s, 200)
s, _, _ = call("POST", "/api/v1/auth/forgot-password", {"email": f"no-existe.{sfx}@ucb.edu.bo"}); check("olvidé mi contraseña, correo inexistente (misma respuesta)", s, 200)
tok = token_for(REC)
s, _, _ = call("POST", "/api/v1/auth/reset-password", {"token": "token-falso-inventado-xxxxxxxxxxxxxxxxxxxxxxxxx", "newPassword": NEW}); check("token de recuperación inválido rechazado", 400 if s in (400, 422) else s, 400)
s, _, _ = call("POST", "/api/v1/auth/reset-password", {"token": tok, "newPassword": NEW}); check("restablecer contraseña con el enlace", s, 200)
s, _, _ = call("POST", "/api/v1/auth/reset-password", {"token": tok, "newPassword": NEW}); check("el enlace ya no se puede reutilizar", 400 if s in (400, 422) else s, 400)
s, _, _ = login(REC, NEW); check("login con la nueva contraseña", s, 200)

# --- Cambio de correo de acceso
CHG = f"cambio.{sfx}@ucb.edu.bo"; NEWMAIL = f"nuevo.{sfx}@ucb.edu.bo"
call("POST", "/api/v1/auth/register", {"email": CHG, "firstName": "Pedro", "lastName": "Soliz", "password": OLD})
call("POST", "/api/v1/auth/verify-otp", {"email": CHG, "code": code_for(CHG)})
_, tc, _ = login(CHG, OLD); TC = tc["accessToken"]
s, _, _ = call("POST", "/api/v1/profile/change-email", {"newEmail": NEWMAIL, "password": secrets.token_urlsafe(12)}, token=TC); check("cambio de correo con contraseña incorrecta", s, 401)
s, _, _ = call("POST", "/api/v1/profile/change-email", {"newEmail": NEWMAIL, "password": OLD}, token=TC); check("solicitar cambio de correo", s, 200)
s, _, _ = call("POST", "/api/v1/profile/verify-email-change", {"code": "000000"}, token=TC); check("código de cambio de correo incorrecto", s, 422)
s, _, _ = call("POST", "/api/v1/profile/verify-email-change", {"code": code_for(NEWMAIL)}, token=TC); check("confirmar cambio de correo", s, 200)
s, _, _ = login(NEWMAIL, OLD); check("login con el correo nuevo", s, 200)

# --- Correo institucional
INST = f"inst.{sfx}@ucb.edu.bo"
call("POST", "/api/v1/auth/register", {"email": f"base.{sfx}@ucb.edu.bo", "firstName": "Marta", "lastName": "Lima", "password": OLD})
call("POST", "/api/v1/auth/verify-otp", {"email": f"base.{sfx}@ucb.edu.bo", "code": code_for(f"base.{sfx}@ucb.edu.bo")})
_, tb, _ = login(f"base.{sfx}@ucb.edu.bo", OLD); TB = tb["accessToken"]
s, _, _ = call("POST", "/api/v1/profile/set-institutional-email", {"institutionalEmail": INST}, token=TB); check("solicitar correo institucional", s, 200)
s, _, _ = call("POST", "/api/v1/profile/verify-institutional-email", {"code": code_for(INST)}, token=TB); check("verificar correo institucional", s, 200)

# --- Cuenta externa: registro, OTP, datos y aprobación por ADMIN
EXT = f"externo.{sfx}@example.com"
jar = http.cookiejar.CookieJar(); opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))
def post_jar(path, body):
    req = urllib.request.Request(BASE + path, data=json.dumps(body).encode(), method="POST")
    req.add_header("Content-Type", "application/json")
    # La cookie de prueba es Secure: en http un cliente de Python no la reenvía, así que se envía explícitamente.
    proof = [c.value for c in jar if c.name == "uvoluntapp.registration"]
    if proof:
        req.add_header("Cookie", "uvoluntapp.registration=" + proof[-1])
    try:
        with opener.open(req, timeout=20) as r: return r.status, json.loads(r.read().decode() or "{}")
    except urllib.error.HTTPError as e: return e.code, {}
call("POST", "/api/v1/auth/register", {"email": EXT, "firstName": "Carla", "lastName": "Ruiz", "password": OLD})
s, _ = post_jar("/api/v1/auth/verify-otp", {"email": EXT, "code": code_for(EXT)}); check("OTP de cuenta externa", s, 200)
s, _ = post_jar("/api/v1/auth/complete-registration", {"email": EXT, "firstName": "Carla", "lastName": "Ruiz", "phone": "71234567", "relationWithPastoral": "Voluntaria externa"})
check("completar registro externo", s, 200)
s, _, _ = call("GET", "/api/v1/accounts/approval-requests?status=PENDING_APPROVAL", token=T1)
check("admin lista solicitudes externas", s, 200)
s, lst, _ = call("GET", "/api/v1/accounts/approval-requests?status=PENDING_APPROVAL", token=T1)
req_id = next((i["id"] for i in lst.get("items", []) if i.get("email") == EXT), None)
check("la solicitud externa aparece", 200 if req_id else 0, 200)
s, _, _ = call("POST", f"/api/v1/accounts/approval-requests/{req_id}/reject", {"reason": "Datos insuficientes"}, token=T2); check("admin 2 rechaza solicitud", s, 200)
s, _, _ = call("POST", f"/api/v1/accounts/approval-requests/{req_id}/approve", token=T1); check("solicitud ya resuelta no se aprueba", s, 409)
s, _, _ = login(EXT, OLD); check("cuenta rechazada no inicia sesión", 422 if s == 422 else s, 422)

print(); ok = sum(results); print(f"RESULTADO e2e2: {ok}/{len(results)} pasan"); sys.exit(0 if ok == len(results) else 1)
