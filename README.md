# Uvoluntapp Backend

API de voluntariado de la Pastoral Universitaria de la UCB Santa Cruz. El contrato de `/api/v1` cubre cuentas y roles contextuales (M1), así como periodos académicos, programas y actividades (M2). [Contrato OpenAPI](contracts/openapi.yaml)

## Arranque local

Necesitas el SDK de .NET 10 y Docker con Compose. Ejecuta los comandos desde la raíz del repositorio. En un checkout nuevo, prepara la configuración y las herramientas locales.

```bash
cp .env.example .env
dotnet tool restore
dotnet husky install
dotnet restore
```

En `.env`, elige una contraseña local para `POSTGRES_PASSWORD` y usa el mismo valor en `DB_CONNECTION_STRING`. La API se ejecuta en tu máquina, así que la cadena debe usar `Host=localhost` y el puerto publicado en `POSTGRES_PORT`. Si ya utilizas alguno de los puertos predeterminados, cámbialo en `.env`.

```bash
docker compose up -d --wait postgres redis mailpit
```

| Servicio | Acceso local predeterminado |
| --- | --- |
| PostgreSQL 16 | `localhost:5432` |
| Redis | `localhost:6379` |
| Mailpit | SMTP en `localhost:1025` y bandeja de entrada en `http://localhost:8025` |

Compose lee `.env`. Para que EF Core y la API usen la misma base de datos, exporta la cadena en la terminal donde ejecutarás ambos comandos. Si ya tienes definida `ConnectionStrings__DefaultConnection`, el host da prioridad a su valor.

```bash
export DB_CONNECTION_STRING="$(docker compose config --environment | sed -n 's/^DB_CONNECTION_STRING=//p')"
dotnet ef database update --project src/Uvoluntapp.Backend
dotnet run --project src/Uvoluntapp.Backend --launch-profile http
```

Desde otra terminal, comprueba que el host responde. El perfil `http` escucha en el puerto 5207 y `GET /health` devuelve `{"status":"ok"}`.

```bash
curl -fsS http://localhost:5207/health
```

Para terminar, detén la API con Ctrl+C y ejecuta `docker compose down`. Los datos se conservan en los volúmenes de Compose.

## Desarrollo y verificación

La solución está en `Uvoluntapp.Backend.slnx`. El proyecto en `src/Uvoluntapp.Backend/` contiene las entidades de dominio, la persistencia con EF Core 9 y Npgsql, y el host ASP.NET Core. Para las operaciones protegidas, usa el servicio de autorización por contexto. El permiso depende de la cuenta activa, la operación y el recurso, y se determina por titularidad o por una asignación vigente en el ámbito correspondiente. [Reglas del repositorio](AGENTS.md)

Las pruebas de integración de `tests/Uvoluntapp.Backend.Tests/` crean su propia base PostgreSQL 16 con Testcontainers y necesitan Docker en ejecución.

```bash
dotnet build Uvoluntapp.Backend.slnx
dotnet test
dotnet format --verify-no-changes
```

El hook de pre-commit comprueba el formato. Si señala cambios, ejecuta `dotnet format`, revisa el diff y añade los archivos corregidos al commit.
