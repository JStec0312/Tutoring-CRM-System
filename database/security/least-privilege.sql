-- Administrator-only provisioning. The database must already exist.
USE master;
GO

IF SUSER_ID(N'tutoring_app') IS NULL
    CREATE LOGIN tutoring_app WITH PASSWORD = '$(APP_PASSWORD_SQL)',
        CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = TutoringDb;
ELSE
    ALTER LOGIN tutoring_app WITH PASSWORD = '$(APP_PASSWORD_SQL)';

IF SUSER_ID(N'tutoring_migrations') IS NULL
    CREATE LOGIN tutoring_migrations WITH PASSWORD = '$(MIGRATIONS_PASSWORD_SQL)',
        CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = TutoringDb;
ELSE
    ALTER LOGIN tutoring_migrations WITH PASSWORD = '$(MIGRATIONS_PASSWORD_SQL)';
GO

-- Refuse to reuse logins with existing server-wide grants or role membership.
IF EXISTS (
    SELECT 1 FROM sys.server_role_members
    WHERE member_principal_id IN (SUSER_ID(N'tutoring_app'), SUSER_ID(N'tutoring_migrations'))
) OR EXISTS (
    SELECT 1 FROM sys.server_permissions
    WHERE grantee_principal_id IN (SUSER_ID(N'tutoring_app'), SUSER_ID(N'tutoring_migrations'))
      AND permission_name <> 'CONNECT SQL'
      AND state IN ('G', 'W')
)
    THROW 51000, 'Application or migration login has unexpected server privileges. Review manually.', 1;
GO

USE TutoringDb;
GO

IF SCHEMA_ID(N'messaging') IS NULL
    EXEC(N'CREATE SCHEMA messaging AUTHORIZATION dbo');

IF DATABASE_PRINCIPAL_ID(N'tutoring_app') IS NULL
    CREATE USER tutoring_app FOR LOGIN tutoring_app WITH DEFAULT_SCHEMA = dbo;
IF DATABASE_PRINCIPAL_ID(N'tutoring_migrations') IS NULL
    CREATE USER tutoring_migrations FOR LOGIN tutoring_migrations WITH DEFAULT_SCHEMA = dbo;

IF DATABASE_PRINCIPAL_ID(N'tutoring_runtime') IS NULL
    CREATE ROLE tutoring_runtime AUTHORIZATION dbo;
IF DATABASE_PRINCIPAL_ID(N'tutoring_schema_migrations') IS NULL
    CREATE ROLE tutoring_schema_migrations AUTHORIZATION dbo;
GO

-- Fixed roles such as db_owner/db_ddladmin must never be inherited by these users.
IF EXISTS (
    SELECT 1 FROM sys.database_role_members AS members
    JOIN sys.database_principals AS roles ON roles.principal_id = members.role_principal_id
    WHERE members.member_principal_id IN (
        USER_ID(N'tutoring_app'), USER_ID(N'tutoring_migrations'),
        USER_ID(N'tutoring_runtime'), USER_ID(N'tutoring_schema_migrations'))
      AND NOT (
          (members.member_principal_id = USER_ID(N'tutoring_app') AND roles.name = N'tutoring_runtime')
          OR (members.member_principal_id = USER_ID(N'tutoring_migrations') AND roles.name = N'tutoring_schema_migrations')
      )
)
    THROW 51000, 'Unexpected database role membership. Review manually.', 1;

IF EXISTS (
    SELECT 1 FROM sys.database_permissions
    WHERE grantee_principal_id IN (USER_ID(N'tutoring_app'), USER_ID(N'tutoring_migrations'))
      AND NOT (class = 0 AND permission_name = 'CONNECT' AND state = 'G')
) OR EXISTS (
    SELECT 1 FROM sys.database_permissions
    WHERE grantee_principal_id IN (USER_ID(N'tutoring_runtime'), USER_ID(N'tutoring_schema_migrations'))
      AND NOT (
          (state = 'G' AND class = 3
              AND major_id IN (SCHEMA_ID(N'dbo'), SCHEMA_ID(N'messaging'))
              AND (permission_name IN ('SELECT', 'INSERT', 'UPDATE', 'DELETE')
                  OR (grantee_principal_id = USER_ID(N'tutoring_schema_migrations')
                      AND permission_name IN ('ALTER', 'REFERENCES'))))
          OR (state = 'G' AND class = 0 AND permission_name = 'CREATE TABLE'
              AND grantee_principal_id = USER_ID(N'tutoring_schema_migrations'))
          OR (state = 'D' AND class = 1 AND major_id = OBJECT_ID(N'dbo.__EFMigrationsHistory')
              AND permission_name IN ('SELECT', 'INSERT', 'UPDATE', 'DELETE')
              AND grantee_principal_id = USER_ID(N'tutoring_runtime'))
      )
) OR EXISTS (
    SELECT 1 FROM sys.schemas
    WHERE principal_id IN (USER_ID(N'tutoring_app'), USER_ID(N'tutoring_migrations'),
        USER_ID(N'tutoring_runtime'), USER_ID(N'tutoring_schema_migrations'))
) OR EXISTS (
    SELECT 1 FROM sys.databases
    WHERE database_id = DB_ID()
      AND owner_sid IN (SUSER_SID(N'tutoring_app'), SUSER_SID(N'tutoring_migrations'))
)
    THROW 51000, 'Unexpected direct permissions or ownership. Review manually.', 1;

IF IS_ROLEMEMBER(N'tutoring_runtime', N'tutoring_app') <> 1
    ALTER ROLE tutoring_runtime ADD MEMBER tutoring_app;
IF IS_ROLEMEMBER(N'tutoring_schema_migrations', N'tutoring_migrations') <> 1
    ALTER ROLE tutoring_schema_migrations ADD MEMBER tutoring_migrations;

GRANT CONNECT TO tutoring_app;
GRANT CONNECT TO tutoring_migrations;

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO tutoring_runtime;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::messaging TO tutoring_runtime;

-- Precreate EF's standard history table so runtime access can be denied even
-- before the first migration. EF recognizes and reuses this table.
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__EFMigrationsHistory (
        MigrationId nvarchar(150) NOT NULL,
        ProductVersion nvarchar(32) NOT NULL,
        CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
    );
END;
DENY SELECT, INSERT, UPDATE, DELETE ON OBJECT::dbo.__EFMigrationsHistory TO tutoring_runtime;

-- Current migrations create/alter/drop tables and indexes and backfill data
-- only in dbo and messaging. No instance, database creation, or security rights.
GRANT CREATE TABLE TO tutoring_schema_migrations;
GRANT ALTER, REFERENCES, SELECT, INSERT, UPDATE, DELETE
    ON SCHEMA::dbo TO tutoring_schema_migrations;
GRANT ALTER, REFERENCES, SELECT, INSERT, UPDATE, DELETE
    ON SCHEMA::messaging TO tutoring_schema_migrations;
GO
