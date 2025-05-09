FROM mcr.microsoft.com/mssql/server:2022-latest

ENV ACCEPT_EULA=Y
ENV SA_PASSWORD=Pass@123
ENV MSSQL_PID=Express

USER root

# Create and set permissions for SQL Server directories
RUN mkdir -p /var/opt/mssql/data && \
    mkdir -p /var/opt/mssql/log && \
    chown -R mssql:mssql /var/opt/mssql/data && \
    chown -R mssql:mssql /var/opt/mssql/log && \
    chmod -R 755 /var/opt/mssql/data && \
    chmod -R 755 /var/opt/mssql/log

USER mssql

EXPOSE 1433

CMD [ "/opt/mssql/bin/sqlservr" ]