# AGENTS.md · uvoluntapp-backend

Reglas para cualquier agente que trabaje en este repositorio. Las reglas de negocio están en `~/Documents/GitHub/coord/CONTEXT.md`; léelas antes de implementar.

## Stack
- .NET 10, ASP.NET Core, EF Core 9 con Npgsql (PostgreSQL 16), ASP.NET Core Identity con claves `Guid`, JWT con refresh tokens, Redis, xUnit.
- Solución `Uvoluntapp.Backend.slnx`, proyecto principal en `src/Uvoluntapp.Backend/` y pruebas en `tests/Uvoluntapp.Backend.Tests/`.

## Estructura

Dentro de `src/Uvoluntapp.Backend/`, cada tipo tiene un archivo con su nombre y su namespace sigue la carpeta. Crea las carpetas al añadir su primer tipo.

Estructura en capas tomada de `U-VoluntApp_Core`, sin modelos de persistencia duplicados ni mappers: EF Core mapea directamente las entidades de `Domain`. Dentro de cada carpeta, una subcarpeta por módulo (`Accounts`, `Administration`, `Audit`, `Authorization`, `Common`, `Profiles`, `Programs`, `Activities`).

- `Domain/Entities/<Módulo>/`: entidades y objetos de valor, con sus invariantes. Sin dependencias de EF ni de ASP.NET.
- `Domain/Policies/`: reglas que cruzan entidades (transiciones de estado, vinculación con Google, doble aprobación de ADMIN, asignación de líderes).
- `Domain/Utils/{Enums,Constants,Exceptions}/`: enums, constantes y excepciones del dominio.
- `Application/DTOs/<Módulo>/`: peticiones, respuestas y registros de lectura.
- `Application/Interfaces/<Módulo>/`: puertos (almacenes, proveedores, sesiones) que implementa `Infrastructure`.
- `Application/Services/<Módulo>/`: casos de uso y autorización por contexto.
- `Application/Validators/<Módulo>/` y `Application/Exceptions/<Módulo>/`.
- `Infrastructure/Persistence/`: `AppDbContext`, `Repositories/` (implementaciones de los puertos), `Records/`, `Seeders/` y `Migrations/`.
- `Infrastructure/{Auth,Email,Identity}/`: sesiones, OTP, Google y límites; envío de correo; usuario de Identity.
- `Presentation/{Controllers,Filters,Middleware,Helpers,Extensions}/`.

Las interfaces de los repositorios viven en `Application/Interfaces` (no en `Infrastructure` como en el núcleo) para que `Application` no dependa de `Infrastructure`.

## Comandos
- Compilar: `dotnet build Uvoluntapp.Backend.slnx`
- Pruebas: `dotnet test`
- Entorno local: `docker compose up -d postgres redis mailpit`

## Reglas obligatorias
1. Trabaja solo en los archivos que permite tu tarjeta de tarea (`coord/tasks/T-xxx.md`).
2. No modifiques migraciones, `AppDbContext*`, `src/Uvoluntapp.Backend/Program.cs`, `src/Uvoluntapp.Backend/Presentation/Extensions/DependencyInjectionExtensions.cs` ni `.github/`. Si los necesitas, descríbelo en tu informe; el integrador lo hace.
3. No hagas `git push` ni operaciones en Jira o GitHub. Commit local solamente.
4. Commits: `tipo(SCRUM-xx): descripción`. Sin líneas `Co-authored-by`, `Co-Authored-By` ni firmas de herramientas de IA en el mensaje. Solo las tarjetas sin historia de Jira (pruebas de humo, infraestructura) usan `tipo(alcance): descripción`, por ejemplo `test(smoke): ...`.
5. Toda regla de negocio nueva lleva al menos una prueba unitaria.
6. Autorización: nunca `[Authorize(Roles=...)]`. Usa el servicio de autorización por contexto.
7. Nada de credenciales en el código; configuración por variables de entorno.
8. Errores con Problem Details y los códigos de `CONTEXT.md`.
9. Entrega siempre `coord/reports/T-xxx.md` con la plantilla.
