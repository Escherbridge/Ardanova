-- Historical asset decimals are evidence. Remove the fabricated default and
-- permit an explicit reconciliation window without manufacturing a value.
ALTER TABLE public."ProjectTokenConfig"
  ALTER COLUMN "assetScale" DROP DEFAULT,
  ALTER COLUMN "assetScale" DROP NOT NULL;
