# SQL Server least privilege

The API uses `tutoring_app`; EF Core migrations run separately as `tutoring_migrations`. Neither account is a server administrator or a database owner. SQL permissions complement API authorization: the shared application login still reads and writes CRM data on behalf of authorized users.

## Permissions and initialization

| Identity | Scope | Permissions |
| --- | --- | --- |
| `tutoring_app` via `tutoring_runtime` | `TutoringDb`, schemas `dbo` and `messaging` | `SELECT`, `INSERT`, `UPDATE`, `DELETE`; explicit denial of access to `dbo.__EFMigrationsHistory` |
| `tutoring_migrations` via `tutoring_schema_migrations` | `TutoringDb`, schemas `dbo` and `messaging` | `CREATE TABLE` in the database; `ALTER`, `REFERENCES` and CRUD in these schemas |
| `sa` | Instance provisioning and SQL health check | Used by the administrative initialization container and database health check; never passed to the API or migration service |

[init.sh](../../database/security/init.sh) first runs the existing TDE/audit initialization, then [least-privilege.sql](../../database/security/least-privilege.sql). The latter creates or updates login passwords, maps users and grants custom roles. It precreates the `messaging` schema and EF's standard migration history table so permissions can be set before the first migration. Existing migration history is preserved. Unexpected server roles, database roles, direct user grants or ownership stop initialization for manual review.

Runtime grants apply to future tables in these two schemas as well. Adding another schema or migration operations such as creating views/procedures requires a deliberate permission update. The migration account has broad schema-changing power inside these schemas and must remain a deployment credential.

The `full` Compose profile starts services in this order:

1. SQL Server becomes healthy.
2. `sql-security-init` configures TDE, audit and database identities.
3. `migrations` runs the API image with `--migrate` and exits successfully.
4. `api` starts with its runtime credential.

The [entry point](../../TutoringManagementSystem/Tutoring.Api/Program.cs) handles `--migrate` before registering HTTP/authentication/messaging services. Normal API startup, including Development, never runs migrations. A failed migration stops the dependent API from starting. Compose gives each process only its own database credential. Local environment files are excluded from the image build context.

## Local setup

Keep the existing TDE secret files described in [TDE](tde.md). Add distinct `TUTORING_APP_PASSWORD` and `TUTORING_MIGRATIONS_PASSWORD` values alongside `MSSQL_SA_PASSWORD` in the local Compose `.env`. Use random passwords of 16–128 characters satisfying SQL Server password policy. Double quotes, line breaks and `$(` are rejected by the initializer; quote literal dollar signs correctly in the local Compose environment file. See [.env.example](../../.env.example).

For an API running on the host, update its `ConnectionStrings__DefaultConnection` to use `tutoring_app`, the matching password, and `127.0.0.1,1433`. Never give that process the migration connection string. Keep deployment credentials out of the host API's environment file.

Start the full local stack from the repository root:

```powershell
docker compose --profile full up -d --build
```

For an API running separately on the host, provision the database and apply migrations first:

```powershell
docker compose --profile migrations up --build --abort-on-container-exit --exit-code-from migrations migrations
docker compose up -d rabbitmq mailpit
```

To apply later migrations, stop the API first and run a fresh migration process:

```powershell
docker compose stop api
docker compose --profile migrations run --rm --build migrations
docker compose --profile full up -d api
```

Review migrations and back up existing data before deployment. These commands do not remove volumes. The legacy Development database-reset endpoint needs database administration rights and is unsupported with the runtime account; perform intentional database maintenance outside the API.

For an existing installation, re-run initialization after changing passwords; merely changing `.env` does not rotate SQL logins stored in the persistent volume:

```powershell
docker compose stop api
docker compose run --rm sql-security-init
docker compose --profile migrations run --rm --build migrations
docker compose --profile full up -d --force-recreate api
```

## Network boundary

[docker-compose.yml](../../docker-compose.yml) publishes SQL only as `127.0.0.1:1433:1433` for host development. Containers connect to `sqlserver:1433` over their Compose network.

[docker-compose.production.yml](../../docker-compose.production.yml) removes SQL's host port with `!reset []` and sets the API environment to Production:

```powershell
docker compose -f docker-compose.yml -f docker-compose.production.yml --profile full config --quiet
docker compose -f docker-compose.yml -f docker-compose.production.yml --profile full up -d --build
```

This override supplies the SQL network restriction, not a complete production deployment. Supply production SQL licensing/image settings, trusted TLS certificates (`TrustServerCertificate=False` in both connection strings), messaging, SMTP and other deployment settings separately. The base file uses SQL Server Developer edition and local messaging/mail tools. Use a Compose version supporting `!reset`; validate the merged configuration before deployment.

## Verification

The [integration fixture](../../TutoringManagementSystem/Tutoring.IntegrationTests/Infrastructure/IntegrationTestFixture.cs) provisions a disposable SQL Server container with the actual permission script, reruns initialization, applies all migrations twice as `tutoring_migrations`, and runs every API integration test as `tutoring_app`. Only database creation and Respawn cleanup use the isolated container's administrator.

[SqlLeastPrivilegeTests](../../TutoringManagementSystem/Tutoring.IntegrationTests/Infrastructure/SqlLeastPrivilegeTests.cs) checks runtime CRUD grants in both schemas, rejected DDL/server changes/history access and denied access to another database. It also verifies that migrations finish without database or server ownership.

```powershell
dotnet test TutoringManagementSystem/Tutoring.IntegrationTests/Tutoring.IntegrationTests.csproj --filter FullyQualifiedName~SqlLeastPrivilegeTests
```

References: [Microsoft: migration deployment identities](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying), [Microsoft: schema permissions](https://learn.microsoft.com/en-us/sql/t-sql/statements/grant-schema-permissions-transact-sql?view=sql-server-ver16), [Docker: merge and reset Compose values](https://docs.docker.com/compose/how-tos/multiple-compose-files/merge/).
