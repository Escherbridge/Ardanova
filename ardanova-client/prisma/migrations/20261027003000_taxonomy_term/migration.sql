-- Domain-neutral taxonomy: sectors, skills and project kinds (SR-03 phase 0).
-- Lands AFTER 20261027002000_post_engagement_uniqueness.
--
-- ArdaNova hosts construction crews, kitchens, care co-ops and clinics as well as software
-- projects, so the words people pick from live in one table instead of tech-shaped enums.
-- A record keeps storing a plain code ("Project"."categories" is comma-separated TEXT,
-- "Project"."projectType" is the ProjectType enum); "TaxonomyTerm" owns that code's label,
-- description, parent and order, keyed by UNIQUE ("kind", "slug") with slug = the stored code.
--
-- The ten legacy ProjectCategory codes are seeded verbatim as SECTOR slugs, so every category
-- already stored resolves with no backfill. Every ProjectType value (old and new) is seeded as a
-- PROJECT_TYPE term. Each SKILL points at its SECTOR through "parentId", looked up by slug rather
-- than by a hard-coded id, so a sector that already existed under another id still links.
--
-- Strictly additive: one enum, one table, its unique index and self foreign key, and seed rows.
-- Seed ids are deterministic ('taxonomy_<kind>_<slug>') so every environment names a term the
-- same way. ON CONFLICT ("kind", "slug") DO NOTHING never overwrites a term an operator has since
-- relabelled or deactivated, and makes a re-run insert nothing. The client mirrors these labels
-- in src/lib/taxonomy/labels.ts; labels.test.ts fails if the two drift.
--
-- Generated with `prisma migrate diff`, then guarded and seeded by hand.

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'TaxonomyKind') THEN
    CREATE TYPE "TaxonomyKind" AS ENUM ('SECTOR', 'SKILL', 'PROJECT_TYPE');
  END IF;
END $$;

CREATE TABLE IF NOT EXISTS "TaxonomyTerm" (
    "id" TEXT NOT NULL,
    "kind" "TaxonomyKind" NOT NULL,
    "slug" TEXT NOT NULL,
    "label" TEXT NOT NULL,
    "description" TEXT,
    "parentId" TEXT,
    "sortOrder" INTEGER NOT NULL DEFAULT 0,
    "isActive" BOOLEAN NOT NULL DEFAULT true,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "TaxonomyTerm_pkey" PRIMARY KEY ("id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "TaxonomyTerm_kind_slug_key" ON "TaxonomyTerm"("kind", "slug");

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'TaxonomyTerm_parentId_fkey') THEN
    ALTER TABLE "TaxonomyTerm" ADD CONSTRAINT "TaxonomyTerm_parentId_fkey" FOREIGN KEY ("parentId") REFERENCES "TaxonomyTerm"("id") ON DELETE SET NULL ON UPDATE CASCADE;
  END IF;
END $$;

-- SECTOR: includes the ten legacy ProjectCategory codes verbatim. OTHER sorts last.
INSERT INTO "TaxonomyTerm" ("id", "kind", "slug", "label", "description", "sortOrder", "updatedAt")
SELECT 'taxonomy_sector_' || lower(term.slug), 'SECTOR'::"TaxonomyKind", term.slug, term.label,
       term.description, term.sort_order, CURRENT_TIMESTAMP
FROM (VALUES
  ('AGRICULTURE', 'Agriculture', 'Farming, growing and producing food', 10),
  ('ARTS_CULTURE', 'Arts and culture', 'Art, music, performance and heritage', 20),
  ('BUSINESS', 'Business', 'Starting and running a business', 30),
  ('CARE_SERVICES', 'Care services', 'Looking after children, older people and anyone who needs support', 40),
  ('CIVIC', 'Civic life', 'Local government, public services and neighborhood groups', 50),
  ('CONSTRUCTION', 'Construction', 'Building, renovating and repairing structures', 60),
  ('EDUCATION', 'Education', 'Teaching, training and learning', 70),
  ('ENERGY', 'Energy', 'Producing, saving and supplying energy', 80),
  ('ENVIRONMENT', 'Environment', 'Conservation, restoration and climate work', 90),
  ('FINANCE', 'Finance', 'Money, accounting and financial services', 100),
  ('FOOD_BEVERAGE', 'Food and drink', 'Cooking, baking, brewing and serving food and drink', 110),
  ('HEALTHCARE', 'Healthcare', 'Medical care and health services', 120),
  ('HOSPITALITY', 'Hospitality', 'Hotels, restaurants, cafes and guest services', 130),
  ('HOUSING', 'Housing', 'Homes, tenancy and property upkeep', 140),
  ('LEGAL_SERVICES', 'Legal services', 'Legal advice, contracts and representation', 150),
  ('MANUFACTURING', 'Manufacturing', 'Making and assembling goods', 160),
  ('MEDIA', 'Media', 'Writing, photography, video and publishing', 170),
  ('RESEARCH', 'Research', 'Studying a question and sharing what is found', 180),
  ('RETAIL', 'Retail', 'Shops, markets and selling to the public', 190),
  ('SOCIAL_IMPACT', 'Social impact', 'Work whose main aim is a social benefit', 200),
  ('SPORTS_RECREATION', 'Sports and recreation', 'Sport, fitness and leisure', 210),
  ('TECHNOLOGY', 'Technology', 'Software, hardware and digital services', 220),
  ('TOURISM', 'Tourism', 'Travel, tours and visitor services', 230),
  ('TRANSPORT_LOGISTICS', 'Transport and logistics', 'Moving people and goods', 240),
  ('WATER_SANITATION', 'Water and sanitation', 'Clean water, sanitation and waste', 250),
  ('OTHER', 'Other', 'Anything not listed here', 990)
) AS term(slug, label, description, sort_order)
ON CONFLICT ("kind", "slug") DO NOTHING;

-- PROJECT_TYPE: one term per ProjectType value, old and new.
INSERT INTO "TaxonomyTerm" ("id", "kind", "slug", "label", "description", "sortOrder", "updatedAt")
SELECT 'taxonomy_project_type_' || lower(term.slug), 'PROJECT_TYPE'::"TaxonomyKind", term.slug, term.label,
       term.description, term.sort_order, CURRENT_TIMESTAMP
FROM (VALUES
  ('TEMPORARY', 'Temporary', 'Short-term project with a defined end date', 10),
  ('LONG_TERM', 'Long term', 'Ongoing project without a strict end date', 20),
  ('FOUNDATION', 'Foundation', 'Non-profit organization or foundation', 30),
  ('BUSINESS', 'Business', 'Business venture or startup', 40),
  ('PRODUCT', 'Product', 'A product people can help develop', 50),
  ('OPEN_SOURCE', 'Open source', 'Open source project or tool', 60),
  ('COMMUNITY', 'Community', 'Community-driven initiative', 70),
  ('SERVICE', 'Service', 'Regular work done for people, such as repairs, cleaning or care', 80),
  ('PHYSICAL_BUILD', 'Physical build', 'Building or renovating something physical, such as a home or workshop', 90),
  ('VENUE', 'Venue', 'Opening or running a place people visit, such as a cafe or hall', 100),
  ('PROGRAM', 'Program', 'A recurring program such as classes, training or mentoring', 110),
  ('RESEARCH', 'Research', 'Studying a question and sharing what is found', 120),
  ('COOPERATIVE', 'Cooperative', 'A business owned and run by its members', 130)
) AS term(slug, label, description, sort_order)
ON CONFLICT ("kind", "slug") DO NOTHING;

-- SKILL: trades and professions, each under the sector it is most often found in.
INSERT INTO "TaxonomyTerm" ("id", "kind", "slug", "label", "parentId", "sortOrder", "updatedAt")
SELECT 'taxonomy_skill_' || lower(term.slug), 'SKILL'::"TaxonomyKind", term.slug, term.label,
       sector."id", term.sort_order, CURRENT_TIMESTAMP
FROM (VALUES
  ('CARPENTRY', 'Carpentry', 'CONSTRUCTION', 10),
  ('ELECTRICAL', 'Electrical work', 'CONSTRUCTION', 20),
  ('PLUMBING', 'Plumbing', 'CONSTRUCTION', 30),
  ('MASONRY', 'Masonry', 'CONSTRUCTION', 40),
  ('PAINTING_DECORATING', 'Painting and decorating', 'CONSTRUCTION', 50),
  ('ROOFING', 'Roofing', 'CONSTRUCTION', 60),
  ('WELDING', 'Welding', 'MANUFACTURING', 70),
  ('MACHINE_OPERATION', 'Machine operation', 'MANUFACTURING', 80),
  ('SEWING_TAILORING', 'Sewing and tailoring', 'MANUFACTURING', 90),
  ('COOKING', 'Cooking', 'FOOD_BEVERAGE', 100),
  ('BAKING', 'Baking', 'FOOD_BEVERAGE', 110),
  ('FOOD_SAFETY', 'Food safety', 'FOOD_BEVERAGE', 120),
  ('FRONT_OF_HOUSE', 'Front of house', 'HOSPITALITY', 130),
  ('CLEANING', 'Cleaning', 'HOSPITALITY', 140),
  ('EVENT_PLANNING', 'Event planning', 'HOSPITALITY', 150),
  ('LOGISTICS', 'Logistics', 'TRANSPORT_LOGISTICS', 160),
  ('DRIVING', 'Driving', 'TRANSPORT_LOGISTICS', 170),
  ('WAREHOUSE_WORK', 'Warehouse work', 'TRANSPORT_LOGISTICS', 180),
  ('BOOKKEEPING', 'Bookkeeping', 'FINANCE', 190),
  ('ACCOUNTING', 'Accounting', 'FINANCE', 200),
  ('NURSING', 'Nursing', 'HEALTHCARE', 210),
  ('FIRST_AID', 'First aid', 'HEALTHCARE', 220),
  ('CAREGIVING', 'Caregiving', 'CARE_SERVICES', 230),
  ('CHILDCARE', 'Childcare', 'CARE_SERVICES', 240),
  ('TEACHING', 'Teaching', 'EDUCATION', 250),
  ('TUTORING', 'Tutoring', 'EDUCATION', 260),
  ('CURRICULUM_DESIGN', 'Curriculum design', 'EDUCATION', 270),
  ('GRANT_WRITING', 'Grant writing', 'SOCIAL_IMPACT', 280),
  ('FUNDRAISING', 'Fundraising', 'SOCIAL_IMPACT', 290),
  ('VOLUNTEER_COORDINATION', 'Volunteer coordination', 'SOCIAL_IMPACT', 300),
  ('COMMUNITY_ORGANIZING', 'Community organizing', 'CIVIC', 310),
  ('PUBLIC_SPEAKING', 'Public speaking', 'CIVIC', 320),
  ('LEGAL', 'Legal work', 'LEGAL_SERVICES', 330),
  ('MARKETING', 'Marketing', 'BUSINESS', 340),
  ('SALES', 'Sales', 'BUSINESS', 350),
  ('PROJECT_MANAGEMENT', 'Project management', 'BUSINESS', 360),
  ('CUSTOMER_SERVICE', 'Customer service', 'BUSINESS', 370),
  ('PHOTOGRAPHY', 'Photography', 'MEDIA', 380),
  ('VIDEO_PRODUCTION', 'Video production', 'MEDIA', 390),
  ('WRITING_EDITING', 'Writing and editing', 'MEDIA', 400),
  ('DESIGN', 'Design', 'ARTS_CULTURE', 410),
  ('MUSIC_PERFORMANCE', 'Music and performance', 'ARTS_CULTURE', 420),
  ('SOFTWARE', 'Software development', 'TECHNOLOGY', 430),
  ('DATA', 'Data analysis', 'TECHNOLOGY', 440),
  ('IT_SUPPORT', 'IT support', 'TECHNOLOGY', 450),
  ('FARMING', 'Farming', 'AGRICULTURE', 460),
  ('GARDENING_LANDSCAPING', 'Gardening and landscaping', 'AGRICULTURE', 470),
  ('CONSERVATION', 'Conservation work', 'ENVIRONMENT', 480),
  ('SOLAR_INSTALLATION', 'Solar installation', 'ENERGY', 490),
  ('PROPERTY_MAINTENANCE', 'Property maintenance', 'HOUSING', 500),
  ('TOUR_GUIDING', 'Tour guiding', 'TOURISM', 510),
  ('COACHING', 'Coaching', 'SPORTS_RECREATION', 520),
  ('STOCK_INVENTORY', 'Stock and inventory', 'RETAIL', 530),
  ('WASTE_RECYCLING', 'Waste and recycling', 'WATER_SANITATION', 540)
) AS term(slug, label, sector_slug, sort_order)
JOIN "TaxonomyTerm" AS sector ON sector."kind" = 'SECTOR' AND sector."slug" = term.sector_slug
ON CONFLICT ("kind", "slug") DO NOTHING;
