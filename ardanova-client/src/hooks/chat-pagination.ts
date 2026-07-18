export const CHAT_CONVERSATION_PAGE_LIMIT = 20;

export function getNextConversationCursor(page: {
  nextCursor?: string | null;
}): string | undefined {
  return page.nextCursor ?? undefined;
}
