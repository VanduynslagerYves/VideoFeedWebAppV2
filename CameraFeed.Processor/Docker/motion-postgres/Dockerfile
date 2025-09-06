FROM postgres:16

# Install cron
RUN apt-get update && apt-get install -y cron && rm -rf /var/lib/apt/lists/*

# Copy SQL scripts
COPY init.sql /docker-entrypoint-initdb.d/init.sql
COPY partition_manager.sql /docker-entrypoint-initdb.d/partition_manager.sql
COPY run_partition_manager.sh /docker-entrypoint-initdb.d/run_partition_manager.sh
RUN chmod +x /docker-entrypoint-initdb.d/run_partition_manager.sh

# Add cron job to run partition manager daily at 1:00 AM
RUN echo "0 1 * * * /docker-entrypoint-initdb.d/run_partition_manager.sh >> /var/log/cron.log 2>&1" > /etc/cron.d/partition_manager \
    && chmod 0644 /etc/cron.d/partition_manager \
    && crontab /etc/cron.d/partition_manager

# Start cron + Postgres
CMD bash -c "cron && docker-entrypoint.sh postgres"
