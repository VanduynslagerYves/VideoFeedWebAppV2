DO $$
DECLARE
    start_date DATE := date_trunc('month', current_date);
    end_date DATE := (start_date + INTERVAL '1 month');
    partition_name TEXT := 'motion_images_' || to_char(start_date, 'YYYY_MM');
    drop_date DATE := current_date - INTERVAL '90 days';
    old_partition TEXT;
    -- Next month partition
    next_month_start DATE := date_trunc('month', current_date) + INTERVAL '1 month';
    next_month_end DATE := next_month_start + INTERVAL '1 month';
    next_partition_name TEXT := 'motion_images_' || to_char(next_month_start, 'YYYY_MM');
    -- Oldest partition to keep
    oldest_to_keep DATE := date_trunc('month', current_date) - INTERVAL '2 months';
    old_partition_name TEXT;
BEGIN
    -- Create current month partition
    EXECUTE format(
        'CREATE TABLE IF NOT EXISTS %I PARTITION OF motion_images FOR VALUES FROM (%L) TO (%L);',
        partition_name, start_date, end_date
    );

    -- Create next month's partition if not exists
    EXECUTE format(
        'CREATE TABLE IF NOT EXISTS %I PARTITION OF motion_images FOR VALUES FROM (%L) TO (%L);',
        next_partition_name, next_month_start, next_month_end
    );

    -- Drop old partitions
    FOR old_partition IN
        SELECT relname
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE relkind = 'r'
          AND relname LIKE 'motion_images_%'
          AND relname < ('motion_images_' || to_char(drop_date, 'YYYY_MM'))
    LOOP
        EXECUTE format('DROP TABLE IF EXISTS %I CASCADE;', old_partition);
    END LOOP;

    -- Drop partitions older than 90 days (i.e., older than 2 months before this month)
    FOR old_partition_name IN
        SELECT tablename
        FROM pg_tables
        WHERE tablename LIKE 'motion_images_%'
          AND to_date(substring(tablename, 14, 7), 'YYYY_MM') < oldest_to_keep
    LOOP
        EXECUTE format('DROP TABLE IF EXISTS %I CASCADE;', old_partition_name);
    END LOOP;
END $$;
