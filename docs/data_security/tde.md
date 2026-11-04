# Transparent Data Encryption

SQL Server TDE encrypts database files and backups at rest. It does not encrypt query results or prevent an authorized SQL account from reading data.

## Configuration and initialization

[init.sql](../../database/security/init.sql) creates a database encryption key (DEK) for `TutoringDb` using AES-256 and protects it with the server certificate `TutoringTdeCert` in `master`. The database master key (DMK) in `master` protects the certificate's private key.

In [Docker Compose](../../docker-compose.yml), `sqlserver` starts first. Once its health check succeeds, the one-shot `sql-security-init` service runs [init.sh](../../database/security/init.sh), prepares audit-volume permissions and invokes `init.sql` with `sqlcmd`. The script creates missing objects, imports the existing certificate and private key, creates `TutoringDb` if absent, enables TDE and configures auditing and SQL accounts. The optional `api` waits for the migrations service, which waits for successful security initialization.

The initializer requires the following existing files under the repository's `.secrets/` directory, mounted read-only into both containers:

| File | Use |
| --- | --- |
| `TutoringTdeCert.cer` and `TutoringTdeCert.pvk` | Certificate and encrypted private key imported by SQL Server |
| `master-key-password.txt` | Password used when creating the DMK |
| `certificate-backup-password.txt` | Password used to decrypt the imported private key |

The script stops if any file is missing or empty; it does not generate or back up the certificate. `MSSQL_SA_PASSWORD` is supplied separately through Compose. Keep these files and cedentials private; [.gitignore](../../.gitignore) ignores `.secrets/`. The initializer passes the two file passwords to `sqlcmd` as variables. See [example passwords](../../.secrets_example/README.md).

If the SQL Server data volume is removed, initialization can create a new empty `TutoringDb` and import the same certificate when all required files remain available. It does not restore database contents; retain separate database backups and the matching certificate/private key and password for recovery. Do not remove volumes to perform the check below.

## Verification

Run against the SQL Server instance after initialization, using an account permitted to read the encryption DMV:

```sql
SELECT DB_NAME(database_id) AS database_name, encryption_state,
       key_algorithm, key_length
FROM sys.dm_database_encryption_keys
WHERE database_id = DB_ID('TutoringDb');
```

Expect `TutoringDb`, state `3` (encrypted), `AES` and `256`. State `2` means encryption is still in progress.
