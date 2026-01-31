import { postRouter } from "~/server/api/routers/post";
import { projectRouter } from "~/server/api/routers/project";
import { guildRouter } from "~/server/api/routers/guild";
import { shopRouter } from "~/server/api/routers/shop";
import { taskRouter } from "~/server/api/routers/task";
import { eventRouter } from "~/server/api/routers/event";
import { opportunityRouter } from "~/server/api/routers/opportunity";
import { governanceRouter } from "~/server/api/routers/governance";
import { roadmapRouter } from "~/server/api/routers/roadmap";
import { sprintRouter } from "~/server/api/routers/sprint";
import { epicRouter } from "~/server/api/routers/epic";
import { backlogRouter } from "~/server/api/routers/backlog";
import { taskBidRouter } from "~/server/api/routers/task-bid";
import { chatRouter } from "~/server/api/routers/chat";
import { createCallerFactory, createTRPCRouter } from "~/server/api/trpc";

/**
 * This is the primary router for your server.
 *
 * All routers added in /api/routers should be manually added here.
 */
export const appRouter = createTRPCRouter({
  post: postRouter,
  project: projectRouter,
  guild: guildRouter,
  shop: shopRouter,
  task: taskRouter,
  event: eventRouter,
  opportunity: opportunityRouter,
  governance: governanceRouter,
  roadmap: roadmapRouter,
  sprint: sprintRouter,
  epic: epicRouter,
  backlog: backlogRouter,
  taskBid: taskBidRouter,
  chat: chatRouter,
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
