-- Keep this migration outside an explicit transaction so PostgreSQL can build online.
CREATE INDEX CONCURRENTLY IF NOT EXISTS
  "ActorAssertionReplay_expiresAt_jti_idx"
ON public."ActorAssertionReplay" ("expiresAt", "jti");
