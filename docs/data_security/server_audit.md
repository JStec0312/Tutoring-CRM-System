# SQL Server Audit

SQL Server Audit records configured server and database events to an audit target. This configuration writes audit files on the SQL Server instance.

## Scope and configuration

[init.sql](../../database/security/init.sql) creates and enables:

| Object | Recorded events |
| --- | --- |
| Server Audit `TutoringSecurityAudit` | File target `/var/opt/mssql/audit/` |
| Server Audit Specification `TutoringAccessAudit` | `FAILED_LOGIN_GROUP`, `SERVER_ROLE_MEMBER_CHANGE_GROUP`, `SERVER_PERMISSION_CHANGE_GROUP` |
| Database Audit Specification `TutoringDataAudit` in `TutoringDb` | `SELECT`, `UPDATE`, `DELETE` on `SCHEMA::dbo` by `PUBLIC`, across objects in that schema |

The database specification does not explicitly include `INSERT` or other schemas. Audit events identify SQL principals; CRM user identification depends on application logging and is not supplied by SQL Audit. The Compose API connection uses the shared `tutoring_app` SQL account.

| Server Audit setting | Value | Meaning |
| --- | --- | --- |
| `MAXSIZE` | `500 MB` | Maximum size of an individual audit file. |
| `MAX_ROLLOVER_FILES` | `5` | Maximum number of rollover files retained before older files are overwritten. |
| `QUEUE_DELAY` | `1000` | Audit processing is queued with a 1000 ms delay. |
| `ON_FAILURE` | `CONTINUE` | SQL Server continues if writing audit events fails, so events may be lost. |

In [Docker Compose](../../docker-compose.yml), `sql-security-init` waits for healthy `sqlserver`, prepares permissions on the `sql-audit-data` volume and runs [init.sh](../../database/security/init.sh) and `init.sql`. The same named volume is mounted by SQL Server at `/var/opt/mssql/audit`, where `.sqlaudit` files persist independently of the SQL Server data volume. The optional API waits for migrations, which wait for initialization.

The script re-enables an existing disabled Server Audit, but only creates missing specifications; it does not re-enable existing disabled specifications.

## Verification

Run with sufficient SQL Server permissions. The audit and both specifications should be enabled; the first query also reports the runtime status.

```sql
SELECT a.name, a.is_state_enabled, s.status_desc
FROM sys.server_audits AS a
LEFT JOIN sys.dm_server_audit_status AS s ON s.audit_id = a.audit_id
WHERE a.name = 'TutoringSecurityAudit';
SELECT name, is_state_enabled FROM sys.server_audit_specifications
WHERE name = 'TutoringAccessAudit';
USE TutoringDb;
SELECT name, is_state_enabled FROM sys.database_audit_specifications
WHERE name = 'TutoringDataAudit';
```

Read available events from the server-side path (results depend on recorded activity and file availability):

```sql
SELECT TOP (20) event_time, action_id, succeeded, server_principal_name,
       database_name, schema_name, object_name, statement
FROM sys.fn_get_audit_file('/var/opt/mssql/audit/*.sqlaudit', DEFAULT, DEFAULT)
ORDER BY event_time DESC;
```
