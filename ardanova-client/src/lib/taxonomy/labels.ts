// Human labels for taxonomy codes. See src/lib/AGENTS.md §Taxonomy labels.
import {
  isProjectType,
  type ProjectType,
} from "~/lib/contracts/project-contract";

export interface ProjectTypeCopy {
  label: string;
  description: string;
}

/** PROJECT_TYPE terms; mirrors prisma/migrations/20261027003000_taxonomy_term. */
export const PROJECT_TYPE_TERMS: Readonly<
  Record<ProjectType, ProjectTypeCopy>
> = {
  TEMPORARY: {
    label: "Temporary",
    description: "Short-term project with a defined end date",
  },
  LONG_TERM: {
    label: "Long term",
    description: "Ongoing project without a strict end date",
  },
  FOUNDATION: {
    label: "Foundation",
    description: "Non-profit organization or foundation",
  },
  BUSINESS: { label: "Business", description: "Business venture or startup" },
  PRODUCT: {
    label: "Product",
    description: "A product people can help develop",
  },
  OPEN_SOURCE: {
    label: "Open source",
    description: "Open source project or tool",
  },
  COMMUNITY: {
    label: "Community",
    description: "Community-driven initiative",
  },
  SERVICE: {
    label: "Service",
    description:
      "Regular work done for people, such as repairs, cleaning or care",
  },
  PHYSICAL_BUILD: {
    label: "Physical build",
    description:
      "Building or renovating something physical, such as a home or workshop",
  },
  VENUE: {
    label: "Venue",
    description:
      "Opening or running a place people visit, such as a cafe or hall",
  },
  PROGRAM: {
    label: "Program",
    description: "A recurring program such as classes, training or mentoring",
  },
  RESEARCH: {
    label: "Research",
    description: "Studying a question and sharing what is found",
  },
  COOPERATIVE: {
    label: "Cooperative",
    description: "A business owned and run by its members",
  },
};

/** SECTOR terms; a project's category codes are sector slugs. Mirrors the same migration. */
export const SECTOR_LABELS: Readonly<Record<string, string>> = {
  AGRICULTURE: "Agriculture",
  ARTS_CULTURE: "Arts and culture",
  BUSINESS: "Business",
  CARE_SERVICES: "Care services",
  CIVIC: "Civic life",
  CONSTRUCTION: "Construction",
  EDUCATION: "Education",
  ENERGY: "Energy",
  ENVIRONMENT: "Environment",
  FINANCE: "Finance",
  FOOD_BEVERAGE: "Food and drink",
  HEALTHCARE: "Healthcare",
  HOSPITALITY: "Hospitality",
  HOUSING: "Housing",
  LEGAL_SERVICES: "Legal services",
  MANUFACTURING: "Manufacturing",
  MEDIA: "Media",
  RESEARCH: "Research",
  RETAIL: "Retail",
  SOCIAL_IMPACT: "Social impact",
  SPORTS_RECREATION: "Sports and recreation",
  TECHNOLOGY: "Technology",
  TOURISM: "Tourism",
  TRANSPORT_LOGISTICS: "Transport and logistics",
  WATER_SANITATION: "Water and sanitation",
  OTHER: "Other",
};

/** SKILL terms. Mirrors the same migration. */
export const SKILL_LABELS: Readonly<Record<string, string>> = {
  CARPENTRY: "Carpentry",
  ELECTRICAL: "Electrical work",
  PLUMBING: "Plumbing",
  MASONRY: "Masonry",
  PAINTING_DECORATING: "Painting and decorating",
  ROOFING: "Roofing",
  WELDING: "Welding",
  MACHINE_OPERATION: "Machine operation",
  SEWING_TAILORING: "Sewing and tailoring",
  COOKING: "Cooking",
  BAKING: "Baking",
  FOOD_SAFETY: "Food safety",
  FRONT_OF_HOUSE: "Front of house",
  CLEANING: "Cleaning",
  EVENT_PLANNING: "Event planning",
  LOGISTICS: "Logistics",
  DRIVING: "Driving",
  WAREHOUSE_WORK: "Warehouse work",
  BOOKKEEPING: "Bookkeeping",
  ACCOUNTING: "Accounting",
  NURSING: "Nursing",
  FIRST_AID: "First aid",
  CAREGIVING: "Caregiving",
  CHILDCARE: "Childcare",
  TEACHING: "Teaching",
  TUTORING: "Tutoring",
  CURRICULUM_DESIGN: "Curriculum design",
  GRANT_WRITING: "Grant writing",
  FUNDRAISING: "Fundraising",
  VOLUNTEER_COORDINATION: "Volunteer coordination",
  COMMUNITY_ORGANIZING: "Community organizing",
  PUBLIC_SPEAKING: "Public speaking",
  LEGAL: "Legal work",
  MARKETING: "Marketing",
  SALES: "Sales",
  PROJECT_MANAGEMENT: "Project management",
  CUSTOMER_SERVICE: "Customer service",
  PHOTOGRAPHY: "Photography",
  VIDEO_PRODUCTION: "Video production",
  WRITING_EDITING: "Writing and editing",
  DESIGN: "Design",
  MUSIC_PERFORMANCE: "Music and performance",
  SOFTWARE: "Software development",
  DATA: "Data analysis",
  IT_SUPPORT: "IT support",
  FARMING: "Farming",
  GARDENING_LANDSCAPING: "Gardening and landscaping",
  CONSERVATION: "Conservation work",
  SOLAR_INSTALLATION: "Solar installation",
  PROPERTY_MAINTENANCE: "Property maintenance",
  TOUR_GUIDING: "Tour guiding",
  COACHING: "Coaching",
  STOCK_INVENTORY: "Stock and inventory",
  WASTE_RECYCLING: "Waste and recycling",
};

const STORED_CODE = /^[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)*$/;

/** "PHYSICAL_BUILD" -> "Physical build"; anything not shaped like a code is returned trimmed. */
export function humanizeCode(code: string): string {
  const trimmed = code.trim();
  if (!STORED_CODE.test(trimmed)) return trimmed;
  const sentence = trimmed.toLowerCase().split("_").join(" ");
  return sentence.charAt(0).toUpperCase() + sentence.slice(1);
}

function readLabel(labels: Readonly<Record<string, string>>, code: string) {
  return Object.hasOwn(labels, code) ? labels[code] : humanizeCode(code);
}

/** Label for a ProjectType value, including one a newer API added. */
export function projectTypeLabel(code: string): string {
  return isProjectType(code)
    ? PROJECT_TYPE_TERMS[code].label
    : humanizeCode(code);
}

/** One-line description of a ProjectType value; undefined when this client has none. */
export function projectTypeDescription(code: string): string | undefined {
  return isProjectType(code) ? PROJECT_TYPE_TERMS[code].description : undefined;
}

/** Label for a stored project category: a sector code, or a person's own free-text category. */
export function categoryLabel(code: string): string {
  return readLabel(SECTOR_LABELS, code);
}

/** Label for a skill code. */
export function skillLabel(code: string): string {
  return readLabel(SKILL_LABELS, code);
}
