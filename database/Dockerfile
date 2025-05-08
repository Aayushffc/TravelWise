FROM mcr.microsoft.com/mssql/server:2022-latest

# Set environment variables
ENV ACCEPT_EULA=Y
ENV SA_PASSWORD=Pass@123
ENV MSSQL_PID=Express

# Expose SQL Server port
EXPOSE 1433

# Copy initialization script
COPY ./init-db.sql /docker-entrypoint-initdb.d/

# Create a shell script to run the initialization script
COPY ./init.sh /docker-entrypoint-initdb.d/
RUN chmod +x /docker-entrypoint-initdb.d/init.sh
