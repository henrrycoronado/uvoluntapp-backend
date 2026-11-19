# AGENTS.md · uvoluntapp-backend

Reglas para cualquier agente que trabaje en este repositorio. Las reglas de negocio están en `~/Documents/GitHub/coord/CONTEXT.md`; léelas antes de implementar.

## Stack
- .NET 10, ASP.NET Core, EF Core 9 con Npgsql (PostgreSQL 16), ASP.NET Core Identity con claves `Guid`, JWT con refresh tokens, Redis, xUnit.
- Proyecto principal `U-VoluntApp_Core.csproj` (se renombrará), pruebas en `tests/`.

## Estructura
- `Src/Domain`: entidades, invariantes, transiciones de estado. Sin dependencias de EF ni de ASP.NET.
- `Src/Application`: casos de uso, DTOs, interfaces (puertos), autorización por contexto.
- `Src/Infrastructure`: EF Core, repositorios, Identity, proveedores (Resend, R2, Google, Redis).
- `Src/Presentation`: controladores o endpoints, middleware, extensiones de arranque.

## Comandos
- Compilar: `dotnet build`
- Pruebas: `dotnet test tests/tests.csproj`
- Entorno local: `docker compose up -d postgres redis mailpit`

## Reglas obligatorias
1. Trabaja solo en los archivos que permite tu tarjeta de tarea (`coord/tasks/T-xxx.md`).
2. No modifiques migraciones, `AppDbContext*`, `Program.cs`, `Src/Presentation/Extensions/DependencyInjectionExtensions.cs` ni `.github/`. Si los necesitas, descríbelo en tu informe; el integrador lo hace.
3. No hagas `git push` ni operaciones en Jira o GitHub. Commit local solamente.
4. Commits: `tipo(SCRUM-xx): descripción`. Sin líneas `Co-authored-by`, `Co-Authored-By` ni firmas de herramientas de IA en el mensaje. Solo las tarjetas sin historia de Jira (pruebas de humo, infraestructura) usan `tipo(alcance): descripción`, por ejemplo `test(smoke): ...`.
5. Toda regla de negocio nueva lleva al menos una prueba unitaria.
6. Autorización: nunca `[Authorize(Roles=...)]`. Usa el servicio de autorización por contexto.
7. Nada de credenciales en el código; configuración por variables de entorno.
8. Errores con Problem Details y los códigos de `CONTEXT.md`.
9. Entrega siempre `coord/reports/T-xxx.md` con la plantilla.
