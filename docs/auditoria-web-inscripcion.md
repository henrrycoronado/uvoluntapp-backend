# Auditoría del backend para el recorrido web hasta inscripción

Corte del 23 de noviembre de 2026. La inscripción se apoya en cuentas activas, permisos por recurso y una actividad publicada. El contrato ejecutable por los clientes usa nombres JSON en camelCase. [OpenAPI](../contracts/openapi.yaml) · [PRD](https://docs.google.com/document/d/1a5zSNVWM9gzf1k3rIZngGFmPe8K1kyAjoqCv66cy3pw/edit) · [SRS](https://docs.google.com/document/d/1lYROYmwj3gNI8V_8Bcjw10n-ooYStg8v6hbtkNLlxI0/edit) · [TRD](https://docs.google.com/document/d/1qBdHuyp-wZhx30CRfNkeQ3k7aYrsPcNQQoFS2M8Q8E4/edit)

## Cuentas y autorización

| Auditoría Jira | Resultado del contraste de API | Evidencia en el repositorio |
| --- | --- | --- |
| PUCB-230 · registro | El teléfono pasó a ser obligatorio. La cuenta institucional confirma OTP. La externa completa la solicitud con prueba del buzón. Se conserva el perfil preexistente al reclamar una cuenta. | RegisterRequestValidatorTests, AuthFlowTests |
| PUCB-229 · sesión | Los intentos de acceso de cuentas Google sin contraseña devuelven el mismo 401 público que las credenciales inválidas. El código enviado al buzón permite crear contraseña. La renovación comprueba vigencia y revocación. | AccountSessions, AuthFlowTests |
| PUCB-228 y 227 · asignaciones y roles simultáneos | La autorización evalúa todas las asignaciones vigentes. La delegación operativa se hace sobre una actividad concreta y el equipo de un programa exige permiso sobre ese programa. | RoleAssignmentAuthorizationTests, LeaderAssignmentPolicyTests, PlanningFlowTests |
| PUCB-226 · perfil | La edición admite los campos propios autorizados. La foto personal tiene un recurso protegido que admite subida, sustitución y retiro. | ProfileApplicationService, ImagesController, ProfileAndApprovalTests |
| PUCB-225 · auditoría administrativa | Las operaciones de planificación conservan actor, recurso, fecha y cambios. La consulta global de auditoría exige ADMIN. Los rechazos de planificación se registran en un contexto separado. | PlanningFlowTests, AuditQueryServiceTests, AuditRecorderTests |
| PUCB-224 · suspensión | Se conservan cuenta, historial y asignaciones. La suspensión revoca sesiones. | AccountSuspensionServiceTests, AuthFlowTests |
| PUCB-223 · Google | El alta institucional solicita los datos de contacto que falten antes de emitir sesión. La vinculación con una cuenta existente exige buzones verificados. | GoogleAndRecoveryTests. La prueba usa identidades controladas. |
| PUCB-222 · recuperación | Enlace temporal de un solo uso. Los nuevos tokens se vinculan al correo destinatario y el consumo comprueba concurrencia. | RecoveryService, RecoveryAccountStore, AuthFlowTests |
| PUCB-221 y 220 · administración | Seed idempotente de dos fundadores. Las nuevas altas ADMIN pasan por aprobaciones independientes. | FounderAdminsTests, AdminGrantPolicyTests |
| PUCB-219 · cuentas externas | Rechazo y nueva solicitud mantienen decisiones separadas. Se corrigió el plazo a 12 horas y se probó el límite. | ProfileAndApprovalTests, AuthFlowTests |
| PUCB-218 · correos | Se retiró la verificación administrativa manual. El reemplazo excepcional exige motivo, revoca el acceso anterior y envía un enlace de recuperación al nuevo buzón. | AccountVerificationTests |

## Consola, catálogo e inscripción

| Auditoría o historia Jira | Comportamiento implementado | Evidencia |
| --- | --- | --- |
| PUCB-237 y 231 · programas | ADMIN crea el borrador. La gestión comprueba el programa asignado y el archivo preserva el historial. | CoreEntityTests, PlanningFlowTests |
| PUCB-236, 235 y 232 · actividades | El servidor asigna el periodo activo. Programa y periodo son inmutables. Se retiraron traslado y periodicidad. La migración conserva notas y vínculos históricos en el esquema planning_history. | ActivityService, migración ParticipationAndActivePeriod |
| PUCB-234 y 126 · participación | En borrador se configuran turnos, multirregistro, coordenadas del evento, radio y tolerancia. SIMPLE admite ubicación informativa. El cupo de una actividad grupal pertenece a cada turno. | CoreEntityTests, PlanningFlowTests |
| PUCB-233, 210 y 214 · operación | Reprogramar conserva antes, después, motivo y autor. Los conflictos de planificación generan advertencias. El inicio es automático y el cierre es manual. | PlanningFlowTests, CoreEntityTests |
| PUCB-206 y 216 · periodos | Las actividades determinan en qué periodos aparece un programa. ADMIN cierra el periodo y abre el sucesor en una transacción. Los borradores y las actividades en curso o publicadas bloquean el cierre. | AcademicPeriodClosureTests, PlanningFlowTests |
| PUCB-212 · catálogo | Consulta anónima de lista, calendario filtrable y ficha individual. Incluye PUBLISHED, IN_PROGRESS, FINISHED y CANCELLED. Omite borradores, datos personales y enlaces de reunión. | CatalogService, CatalogActivitySummary, PlanningFlowTests |
| PUCB-238, 239 y 241 · imágenes | R2 privado, validación de formato y dimensiones, regeneración de imagen y miniatura, y URL temporal tras comprobar visibilidad. | ImageProcessorTests, PlanningFlowTests. El almacenamiento de integración es un doble de R2. |
| PUCB-145 y 147 · inscripción | Sin grupos, el destino es la actividad. Con grupos se exige un turno válido. Se serializan solicitudes por usuario y actividad. La cancelación conserva el registro y libera el cupo. | PlanningFlowTests, EnrollmentStore |
| PUCB-143 y 127 · consulta y avisos | El participante consulta sus inscripciones y el responsable consulta el listado de su actividad. El alta y la cancelación no crean asistencia ni horas. Se intenta el aviso por correo después de guardar un cambio de horario o cancelación. | EnrollmentsController, ActivityNotices |

La migración necesita una decisión operativa si encuentra actividades pendientes en varios periodos heredados. Detiene la actualización para preservar esas actividades. El cierre comprueba los pendientes de los módulos existentes. Las evaluaciones de compromisos y la revisión de horas añadirán sus bloqueos cuando se implemente M5.

## Integración pendiente en el repositorio web

La inspección del cliente fue de lectura. Estos puntos identifican las adaptaciones para su siguiente entrega.

| Pantalla o archivo de uvoluntapp-web | Adaptación |
| --- | --- |
| planning/ActivityForm.tsx y planning/api.ts | Retirar periodicidad, traslado y selector libre de periodo. Guardar el borrador, configurar participación y publicar. |
| console/sections/RolesSection.tsx y console/api.ts | Enviar el identificador de actividad en la delegación operativa. Retirar el botón de verificación administrativa manual. |
| planning/CatalogExplorer.tsx y home/sections/ActivitiesSection.tsx | Mostrar las fichas y los turnos, abrir sesión para inscribirse y traducir los códigos de conflicto. Actualizar la vista de inscripciones tras alta o cancelación. |
| Consola y perfil | Consumir las rutas de imágenes, la edición del periodo activo y el cierre manual con nombre del sucesor. |
| Flujo Google | Si requiresProfileData es verdadero, completar el formulario y reiterar la solicitud con el token de Google vigente. |

La evidencia anterior corresponde al servidor. La validación en navegador con la web integrada, OAuth de producción y el bucket R2 real necesita el entorno de esos clientes y servicios.

## Verificación del 23 de noviembre

La compilación .NET 10 terminó con cero advertencias y la suite xUnit pasó 318 pruebas con PostgreSQL y Redis en contenedores. Se comprobó el formato de la solución y la coincidencia del modelo EF con la última migración. Redocly validó el documento OpenAPI 3.0.3. Las 79 combinaciones de método y ruta del contrato coinciden con las de los controladores bajo /api/v1.
