-- Fail closed if the online IF NOT EXISTS migration retained an unsafe
-- same-named index. This migration validates only; it never replaces an index.
DO $migration$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_class AS index_class
        INNER JOIN pg_index AS index_metadata
            ON index_metadata.indexrelid = index_class.oid
        INNER JOIN pg_class AS table_class
            ON table_class.oid = index_metadata.indrelid
        INNER JOIN pg_namespace AS namespace
            ON namespace.oid = table_class.relnamespace
        WHERE namespace.nspname = 'public'
          AND table_class.relname = 'ActorAssertionReplay'
          AND index_class.relname = 'ActorAssertionReplay_expiresAt_jti_idx'
          AND index_metadata.indisvalid
          AND index_metadata.indisready
          AND NOT index_metadata.indisunique
          AND index_metadata.indpred IS NULL
          AND (
              SELECT string_agg(attribute.attname, ',' ORDER BY key_column.ordinality)
              FROM unnest(index_metadata.indkey) WITH ORDINALITY
                  AS key_column(attribute_number, ordinality)
              INNER JOIN pg_attribute AS attribute
                  ON attribute.attrelid = table_class.oid
                 AND attribute.attnum = key_column.attribute_number
              WHERE key_column.ordinality <= index_metadata.indnkeyatts
          ) = 'expiresAt,jti'
    ) THEN
        RAISE EXCEPTION USING
            ERRCODE = '55000',
            MESSAGE = 'ActorAssertionReplay cleanup index postcondition failed';
    END IF;
END
$migration$;
