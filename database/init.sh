#!/bin/bash
# Wait for SQL Server to be ready
sleep 30s

# Run the initialization script
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Pass@123" -i /docker-entrypoint-initdb.d/init-db.sql 