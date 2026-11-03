USE master;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.symmetric_keys
    WHERE name = '##MS_DatabaseMasterKey##'
)
BEGIN
    CREATE MASTER KEY
    ENCRYPTION BY PASSWORD = '$(DMK_PASSWORD)';
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.certificates
    WHERE name = 'TutoringTdeCert'
)
BEGIN
    CREATE CERTIFICATE TutoringTdeCert
    FROM FILE =
        '/var/opt/mssql/tde-backup/TutoringTdeCert.cer'
    WITH PRIVATE KEY (
        FILE =
        '/var/opt/mssql/tde-backup/TutoringTdeCert.pvk',
        DECRYPTION BY PASSWORD = '$(CERT_PASSWORD)'
    );
END;
GO

IF DB_ID('TutoringDb') IS NULL
BEGIN
    EXEC('CREATE DATABASE TutoringDb');
END;
GO

USE TutoringDb;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.dm_database_encryption_keys
    WHERE database_id = DB_ID()
)
BEGIN
    EXEC('
        CREATE DATABASE ENCRYPTION KEY
        WITH ALGORITHM = AES_256
        ENCRYPTION BY SERVER CERTIFICATE TutoringTdeCert
    ');
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.databases
    WHERE name = 'TutoringDb'
      AND is_encrypted = 0
)
BEGIN
    ALTER DATABASE TutoringDb
    SET ENCRYPTION ON;
END;
GO

USE master;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.server_audits
    WHERE name = 'TutoringSecurityAudit'
)
BEGIN
    EXEC('
        CREATE SERVER AUDIT TutoringSecurityAudit
        TO FILE (
            FILEPATH = ''/var/opt/mssql/audit/'',
            MAXSIZE = 500 MB,
            MAX_ROLLOVER_FILES = 5
        )
        WITH (
            QUEUE_DELAY = 1000,
            ON_FAILURE = CONTINUE
        )
    ');
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.server_audits
    WHERE name = 'TutoringSecurityAudit'
      AND is_state_enabled = 0
)
BEGIN
    ALTER SERVER AUDIT TutoringSecurityAudit
    WITH (STATE = ON);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.server_audit_specifications
    WHERE name = 'TutoringAccessAudit'
)
BEGIN
    EXEC('
        CREATE SERVER AUDIT SPECIFICATION TutoringAccessAudit
        FOR SERVER AUDIT TutoringSecurityAudit
        ADD (FAILED_LOGIN_GROUP),
        ADD (SERVER_ROLE_MEMBER_CHANGE_GROUP),
        ADD (SERVER_PERMISSION_CHANGE_GROUP)
        WITH (STATE = ON)
    ');
END;
GO


USE TutoringDb;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_audit_specifications
    WHERE name = 'TutoringDataAudit'
)
BEGIN
    EXEC('
        CREATE DATABASE AUDIT SPECIFICATION TutoringDataAudit
        FOR SERVER AUDIT TutoringSecurityAudit
        ADD (
            SELECT, UPDATE, DELETE
            ON SCHEMA::dbo BY PUBLIC
        )
        WITH (STATE = ON)
    ');
END;
GO