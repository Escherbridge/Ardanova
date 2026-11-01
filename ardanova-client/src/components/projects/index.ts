export { default as OverviewTab } from "./overview-tab";
export { default as TeamTab } from "./team-tab";
export { default as ProposalsTab } from "./proposals-tab";
export { default as CommentsTab } from "./comments-tab";
export { default as OpportunitiesTab } from "./opportunities-tab";
export { default as FundingTab } from "./funding-tab";
export { default as MoneyTab } from "./money-tab";
export { default as EquityTab } from "./equity-tab";
export { default as TasksTab } from "./tasks-tab";
export { default as RoadmapTab } from "./roadmap-tab";
export { default as SprintsTab } from "./sprints-tab";

// SR-03 Phase 3 project page (Lane H); see ./AGENTS.md §Project page.
export {
  ProjectFeedTab,
  ProjectFollowButton,
  useProjectFollow,
  type ProjectFeedTabProps,
  type ProjectFollow,
} from "./project-feed";
export {
  DEFAULT_PROJECT_TAB,
  PROJECT_TABS,
  projectLedeOf,
  projectTabFrom,
  projectTagsOf,
  type ProjectTab,
} from "./project-page-model";
