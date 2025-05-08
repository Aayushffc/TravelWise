#!/bin/bash
# Wait for SQL Server to be up and running
echo "Waiting for SQL Server to start..."
sleep 30s

echo "Running initialization SQL script..."
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Pass@123" -i /init-db.sql