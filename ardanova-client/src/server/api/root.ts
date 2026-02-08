import { adminRouter } from "~/server/api/routers/admin";
import { postRouter } from "~/server/api/routers/post";
import { projectRouter } from "~/server/api/routers/project";
import { guildRouter } from "~/server/api/routers/guild";
import { taskRouter } from "~/server/api/routers/task";
import { eventRouter } from "~/server/api/routers/event";
import { opportunityRouter } from "~/server/api/routers/opportunity";
import { governanceRouter } from "~/server/api/routers/governance";
import { sprintRouter } from "~/server/api/routers/sprint";
import { epicRouter } from "~/server/api/routers/epic";
import { backlogRouter } from "~/server/api/routers/backlog";
import { chatRouter } from "~/server/api/routers/chat";
import { enumRouter } from "~/server/api/routers/enum";
import { featureRouter } from "~/server/api/routers/feature";
import { productRouter } from "~/server/api/routers/product";
import { opportunityBidRouter } from "~/server/api/routers/opportunity-bid";
import { membershipCredentialRouter } from "~/server/api/routers/membership-credential";
import { credentialUtilityRouter } from "~/server/api/routers/credential-utility";
import { profileRouter } from "~/server/api/routers/profile";
import { xpRouter } from "~/server/api/routers/xp";
import { streakRouter } from "~/server/api/routers/streak";
import { achievementRouter } from "~/server/api/routers/achievement";
import { leaderboardRouter } from "~/server/api/routers/leaderboard";
import { referralRouter } from "~/server/api/routers/referral";
import { kycRouter } from "~/server/api/routers/kyc";
import { createCallerFactory, createTRPCRouter } from "~/server/api/trpc";

/**
 * This is the primary router for your server.
 *
 * All routers added in /api/routers should be manually added here.
 */
export const appRouter = createTRPCRouter({
  admin: adminRouter,
  post: postRouter,
  project: projectRouter,
  guild: guildRouter,
  task: taskRouter,
  event: eventRouter,
  opportunity: opportunityRouter,
  governance: governanceRouter,
  sprint: sprintRouter,
  epic: epicRouter,
  backlog: backlogRouter,
  chat: chatRouter,
  enum: enumRouter,
  feature: featureRouter,
  product: productRouter,
  opportunityBid: opportunityBidRouter,
  membershipCredential: membershipCredentialRouter,
  credentialUtility: credentialUtilityRouter,
  profile: profileRouter,
  xp: xpRouter,
  streak: streakRouter,
  achievement: achievementRouter,
  leaderboard: leaderboardRouter,
  referral: referralRouter,
  kyc: kycRouter,
});

// export type definition of API
export type AppRouter = typeof appRouter;

/**
 * Create a server-side caller for the tRPC API.
 * @example
 * const trpc = createCaller(createContext);
 * const res = await trpc.post.all();
 *       ^? Post[]
 */
export const createCaller = createCallerFactory(appRouter);
