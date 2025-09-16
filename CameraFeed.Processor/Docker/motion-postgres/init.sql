-- Parent table
CREATE TABLE IF NOT EXISTS motion_images (
    id BIGSERIAL,
    camera_id INT NOT NULL,
    event_time TIMESTAMPTZ NOT NULL DEFAULT now(),
    image_data BYTEA NOT NULL,
    PRIMARY KEY (id, event_time)
) PARTITION BY RANGE (event_time);

-- First partition for current month
DO $$
DECLARE
    start_date DATE := date_trunc('month', current_date);
    end_date DATE := (start_date + INTERVAL '1 month');
BEGIN
    EXECUTE format(
        'CREATE TABLE IF NOT EXISTS motion_images_%s PARTITION OF motion_images FOR VALUES FROM (%L) TO (%L);',
        to_char(start_date, 'YYYY_MM'), start_date, end_date
    );
END $$;

-- Index
CREATE INDEX IF NOT EXISTS idx_motion_images_camera_time
    ON motion_images (camera_id, event_time DESC);
