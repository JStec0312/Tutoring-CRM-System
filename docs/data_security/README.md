# Data security

These mechanisms support protection of CRM data through access control, encryption at rest, and database activity recording.

| Mechanism | Purpose | Level | Details |
| --- | --- | --- | --- |
| JWT | Authenticate requests and enforce endpoint roles | API | [JWT](jwt.md) |
| Least privilege | Separate application CRUD from migrations and restrict SQL networking | SQL Server | [Least privilege](least_privilege.md) |
| TDE | Encrypt database data at rest | SQL Server database | [TDE](tde.md) |
| SQL Server Audit | Record configured database and security events | SQL Server and database | [Server audit](server_audit.md) |

JWT controls API access, TDE protects database files at rest, and SQL Server Audit records selected SQL activity. They address different parts of the data lifecycle and do not replace one another.
