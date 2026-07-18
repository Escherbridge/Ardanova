ALTER TABLE public."TaskEscrow"
  ADD COLUMN IF NOT EXISTS "disputeReason" TEXT,
  ADD COLUMN IF NOT EXISTS "disputeDescription" TEXT,
  ADD COLUMN IF NOT EXISTS "disputedByUserId" TEXT,
  ADD COLUMN IF NOT EXISTS "disputedAt" TIMESTAMP(3);

-- IF NOT EXISTS must not silently bless incompatible pre-existing columns.
DO $$
BEGIN
  IF (
    SELECT COUNT(*) <> 4
      OR NOT bool_and(is_nullable = 'YES')
      OR NOT bool_and(
        CASE
          WHEN column_name = 'disputedAt'
            THEN data_type = 'timestamp without time zone' AND datetime_precision = 3
          ELSE data_type = 'text'
        END
      )
    FROM information_schema.columns
    WHERE table_schema = 'public'
      AND table_name = 'TaskEscrow'
      AND column_name IN (
        'disputeReason',
        'disputeDescription',
        'disputedByUserId',
        'disputedAt'
      )
  ) THEN
    RAISE EXCEPTION 'TaskEscrow dispute columns do not match the reviewed nullable text/timestamp(3) contract';
  END IF;
END $$;
