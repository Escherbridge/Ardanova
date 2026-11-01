---
type: reference
---

# app/chats: messages, and the draft a "Message" link opens (SR-03 Phase 3, Lane W)

Plan: `conductor/tracks/mvp-readiness-loop_20261026/SR-03-PHASE3-PLAN.md` §2 ("Message" is secondary; a draft until the
first send) and §10 (DMs have no block or consent check, so draft-until-send is the only guard).

## `?with={userId}`: draft until send

Linked from the feed's help card (`feedRoutes.chatWith`) and the task page's **Message {asker}**.

| Moment                       | Reads                            | Writes                                                   |
| ---------------------------- | -------------------------------- | -------------------------------------------------------- |
| load                         | `chat.getConversations` (page 1) | none                                                     |
| a DIRECT with them is loaded | that conversation + its messages | none; it opens instead of a draft                        |
| otherwise                    | `user.getById` for the header    | none; a draft opens ("Starts when you send")             |
| typing in the draft          | —                                | none (no typing indicator: there is no conversation yet) |
| first send                   | —                                | `chat.getOrCreateDirect`, then `chat.sendMessage`        |

- **Before this lane** `?with=` was ignored: the page opened on "Choose a conversation" and nothing was addressed. The
  only create path was **New** → pick a person (`components/chats/new-conversation-modal.tsx`), which creates the
  DIRECT conversation on the pick, before any message. That modal is not this lane's file and is unchanged.
- **Get-or-create on send, not on load.** The loaded list is one page, so an older conversation with the person may be
  missed and a draft shown; the send's get-or-create then returns that conversation server-side
  (`ChatService.GetOrCreateDirectConversationAsync`), so history is joined, never duplicated.
- **Partial failure.** If get-or-create fails, nothing exists and the words stay in the draft. If it succeeds and the
  send fails, the (now real, empty) conversation opens with the words still in the box and "Your message was not sent."
- The `with` value is consumed once per value (`consumedWithRef`), so "Back to chats" does not reopen the draft.
- **One send at a time (final-review L2).** `draftSendInFlightRef` guards the first send: `isPending` only flips on
  the next render, so a double Enter (or Enter + click) inside one render used to run two get-or-creates and send the
  message twice. On success only the sent text leaves the box (`withoutSentText`); words typed while it was sending
  stay. If the earlier text was edited mid-send it is all kept rather than guessed at.

## Deferred

- **New → pick a person still creates on pick.** Trigger: the first report of empty conversations from a mis-pick →
  make the modal hand back a user id and open this draft instead.
- **No per-sender limit on new DIRECT conversations** (plan §10). Trigger: the first abuse report.
