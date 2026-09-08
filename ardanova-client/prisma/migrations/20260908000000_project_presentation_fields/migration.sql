-- Project presentation fields (Wave 2, W2.4).
-- A project is presented with a one-line ask, uploaded media, and a funding goal whose
-- unit is recorded rather than assumed.
--
-- Defaults make every pre-existing row correct with no backfill pass: a project created
-- before this migration has no media ('[]') and any goal it already carries was always
-- expressed in platform units (ARDA), so the column states what was already true.
-- "summary" stays nullable: there is no honest value to invent for an existing project.
--
-- No index is added for "summary". The project search predicate
-- (ProjectQueryFilters.MatchesSearchTerm) is a case-insensitive substring match, which
-- Postgres cannot serve from a b-tree or a tsvector GIN index; an index here would look
-- like a search optimisation while changing no plan. Indexing that surface means moving
-- the predicate to full-text or pg_trgm first, which is a search change, not this one.
ALTER TABLE public."Project"
  ADD COLUMN IF NOT EXISTS "summary" TEXT,
  ADD COLUMN IF NOT EXISTS "mediaPaths" TEXT NOT NULL DEFAULT '[]',
  ADD COLUMN IF NOT EXISTS "fundingGoalUnit" VARCHAR(16) NOT NULL DEFAULT 'ARDA';
