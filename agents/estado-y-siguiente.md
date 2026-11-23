# Estado del backend y siguientes pasos

Corte: 23 de noviembre de 2026 · Rama de trabajo `fix/m2-validacion` · Jira: proyecto PUCB (Pastoral-UCB).

## Cómo trabajar aquí

- La fuente de verdad son los tickets de Jira y `AGENTS.md`. Este archivo resume el estado; no lo sustituye.
- No hacer `git push`, crear PRs ni cambiar estados o fechas en Jira sin confirmación explícita.
- Commits `tipo(alcance): descripción`, con la descripción de 80 caracteres como máximo (lo exige el hook de `.husky`). Sin firmas de herramientas de IA.
- Un cambio en endpoints o DTOs se refleja en `contracts/openapi.yaml`. La web regenera su cliente con `npm run gen:api`.
- Los topes de longitud de datos de cuenta y perfil viven en `Domain/Utils/Constants/FieldLimits.cs`. La web los repite en `LIMITS` (`src/features/auth/validation.ts`); si cambian aquí, cambian allí.
- Antes de cerrar: `dotnet format --verify-no-changes`, `dotnet build Uvoluntapp.Backend.slnx` y `dotnet test`.
- Solo pasa a Done en Jira lo que está completo, con pruebas y evidencia.

## Ya hecho

| Módulo | Alcance entregado | Estado en Jira |
| --- | --- | --- |
| M1 · Usuarios y roles | Registro con OTP, sesión con refresh, acceso con Google, recuperación de contraseña, perfil, suspensión, aprobación de cuentas externas, verificación y cambio de correo, administradores fundadores con doble aprobación, roles contextuales por recurso y auditoría | 13 de 13 HU |
| M2 · Programas y actividades | Fichas de programa, actividades con periodo activo, reprogramación con historial, conflictos, catálogo público, equipos de programa, delegación operativa y cierre y sucesión de periodos | 14 de 14 HU |
| M3 · Inscripción previa | Condiciones de participación (HU-M3-01), cupos (HU-M3-03) e inscripción con turnos, cancelación y consulta propia (API de HU-M3-02, PUCB-147) | HU-M3-01 y 03 Done. HU-M3-02 sigue en To Do porque falta la parte móvil (PUCB-149) |
| Imágenes | Almacenamiento privado en R2, validación, miniaturas y URL temporal para programa, actividad y perfil (PUCB-238 a 242) | Done |
| Límites de campos | Topes de correo, contraseña, nombres, teléfono y relación en los DTOs de cuenta y perfil, con pruebas y contrato actualizado | Sin ticket |

Verificación al corte: compilación sin advertencias, formato correcto y 327 pruebas xUnit pasando contra PostgreSQL y Redis reales. El contrato tiene 79 combinaciones de método y ruta; el detalle está en `docs/auditoria-web-inscripcion.md`.

Avance de Jira (campo Story point estimate): por HU 108 de 278 SP (38,8 %) y por tasks 206 de 595 SP (34,6 %). De esas tasks, 56 SP son auditorías y 30 SP documentación y varios; el código entregado de API y web suma 120 SP.

## Sprints

- Sprints 0, 1 y 2: cerrados, sin tickets arrastrados.
- Sprint 3 (activo, del 23/11 al 14/12): 16 de 23 tickets ya están Done. Quedan 19 SP.

## Sigue

Pendiente del Sprint 3, en este orden de riesgo:

1. PUCB-143 · estado inscrito, confirmado, cancelado y asistente (3 SP, en curso). El estado "asistente" depende de la asistencia del Sprint 4.
2. PUCB-127 · avisos por correo de cambios y cancelaciones, con envío real verificado en Mailpit y en el proveedor (3 SP).
3. PUCB-125 · pulido de la consola M2, auditoría legible y datos de prueba (3 SP).
4. PUCB-28 · validar con Pastoral las reglas de programas y responsables (2 SP).
5. Verificar el CORS del bucket R2 para la vista previa de imágenes.

Sprint 4 (24 tickets, 110 SP): modalidades de marcación y tolerancia (PUCB-140), códigos QR temporales (PUCB-138), validación de sesión y duplicados en el autorregistro (PUCB-136), geolocalización (PUCB-134) y la base móvil. Sprint 5 en adelante: registro y corrección de asistencia, horas y compromisos (M5), presupuestos (M4), informes y auditoría (M6), IA local y despliegue.

## Riesgos y notas abiertas

- El `.env` local contiene credenciales de administradores de prueba. Los scripts de `scripts/e2e` las leen del entorno; no copiarlas a código, documentos ni commits.
- El envío real de correo y el bucket R2 de producción no están verificados; las pruebas usan Mailpit y un doble de R2.
- El estado "asistente" y todo lo de QR y geolocalización son del Sprint 4; no adelantarlos sin ajustar el sprint.
