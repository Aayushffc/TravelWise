FROM mcr.microsoft.com/mssql/server:2022-latest

# Set environment variables
ENV ACCEPT_EULA=Y
ENV SA_PASSWORD=Pass@123
ENV MSSQL_PID=Express

# Expose SQL Server port
EXPOSE 1433

# Copy scripts to a common location
COPY ./init-db.sql /init-db.sql
# COPY ./init.sh /init.sh

# Use CMD to launch SQL Server and init script at runtime (no chmod needed)
CMD /bin/bash -c "/opt/mssql/bin/sqlservr & sleep 30 && /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Pass@123' -i /init-db.sql"