#!/usr/bin/env bash
set -euo pipefail

: "${TUTORING_APP_PASSWORD:?Set TUTORING_APP_PASSWORD}"
: "${TUTORING_MIGRATIONS_PASSWORD:?Set TUTORING_MIGRATIONS_PASSWORD}"

# SQL login passwords are limited to 128 characters. Exclude sqlcmd expansion,
# line breaks, and connection-string quote delimiters; other punctuation is supported.
for password in "$TUTORING_APP_PASSWORD" "$TUTORING_MIGRATIONS_PASSWORD"; do
    if (( ${#password} < 16 || ${#password} > 128 )) ||
       [[ "$password" == *'$('* || "$password" == *$'\n'* ||
          "$password" == *$'\r'* || "$password" == *'"'* ]]; then
        echo 'SQL application passwords must be 16-128 characters without double quotes, line breaks, or $(.' >&2
        exit 1
    fi
done
if [[ "$TUTORING_APP_PASSWORD" == "$TUTORING_MIGRATIONS_PASSWORD" ||
      "$TUTORING_APP_PASSWORD" == "$SQLCMDPASSWORD" ||
      "$TUTORING_MIGRATIONS_PASSWORD" == "$SQLCMDPASSWORD" ]]; then
    echo 'SQL administrator, application, and migration passwords must be distinct.' >&2
    exit 1
fi

for file in \
    TutoringTdeCert.cer \
    TutoringTdeCert.pvk \
    master-key-password.txt \
    certificate-backup-password.txt
do
    if [ ! -s "/run/tde/$file" ]; then
        echo "Missing TDE secret: $file" >&2
        exit 1
    fi
done

chown 10001:0 /audit-volume
chmod 770 /audit-volume

DMK_PASSWORD=$(tr -d '\r\n' < /run/tde/master-key-password.txt)
CERT_PASSWORD=$(tr -d '\r\n' < /run/tde/certificate-backup-password.txt)

# Escaping apostrofów w literałach T-SQL
DMK_PASSWORD=${DMK_PASSWORD//\'/\'\'}
CERT_PASSWORD=${CERT_PASSWORD//\'/\'\'}

echo "Configuring SQL Server security..."

 /opt/mssql-tools18/bin/sqlcmd \
    -S sqlserver \
    -U sa \
    -C \
    -b \
    -i /scripts/init.sql \
    -v "DMK_PASSWORD=$DMK_PASSWORD" \
       "CERT_PASSWORD=$CERT_PASSWORD"

# sqlcmd reads variables from the environment; do not put login passwords in argv.
export APP_PASSWORD_SQL=${TUTORING_APP_PASSWORD//\'/\'\'}
export MIGRATIONS_PASSWORD_SQL=${TUTORING_MIGRATIONS_PASSWORD//\'/\'\'}
/opt/mssql-tools18/bin/sqlcmd \
    -S sqlserver -U sa -C -b -r 1 \
    -i /scripts/least-privilege.sql

echo "SQL Server security initialized."
