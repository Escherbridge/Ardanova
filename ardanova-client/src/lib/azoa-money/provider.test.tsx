// @vitest-environment jsdom
import {
  act,
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  createKeyStore,
  MONEY_SCOPES,
  readSavedRequest,
  startCashOut,
} from "azoa-sdk/money";
import { useMoneyContext } from "azoa-sdk/react";
import { ArdaMoneyProvider } from "./provider";

const state = vi.hoisted(() => ({ subject: "ada", write: vi.fn() }));
vi.mock("next-auth/react", () => ({
  useSession: () => ({
    data: { user: { id: state.subject } },
    status: "authenticated",
  }),
}));
vi.mock("~/trpc/react", () => ({
  api: {
    useUtils: () => ({
      client: { money: { startCashOut: { mutate: state.write } } },
    }),
  },
}));

function Send() {
  const { transport, keyStore } = useMoneyContext();
  return (
    <button onClick={() => void startCashOut(transport, keyStore, 2500)}>
      Send saved request
    </button>
  );
}
beforeEach(() => {
  localStorage.clear();
  state.subject = "ada";
  state.write.mockReset();
});
afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

describe("a retired subject cannot send from a queued Web Lock", () => {
  it("drops the old host call after switching accounts and retains its subject-bound key", async () => {
    let release: (() => Promise<void>) | undefined;
    vi.stubGlobal("navigator", {
      locks: {
        request: (
          _name: string,
          _options: unknown,
          task: () => Promise<unknown>,
        ) =>
          new Promise((resolve) => {
            release = async () => {
              resolve(await task());
            };
          }),
      },
    });
    const view = render(
      <ArdaMoneyProvider>
        <Send />
      </ArdaMoneyProvider>,
    );
    fireEvent.click(
      await screen.findByRole("button", { name: "Send saved request" }),
    );
    await waitFor(() => expect(release).toBeTypeOf("function"));
    state.subject = "ben";
    view.rerender(
      <ArdaMoneyProvider>
        <Send />
      </ArdaMoneyProvider>,
    );
    await screen.findByRole("button", { name: "Send saved request" });
    await act(async () => {
      await release!();
    });
    expect(state.write).not.toHaveBeenCalled();
    expect(
      readSavedRequest(
        createKeyStore({ subject: "ada", storage: localStorage }),
        MONEY_SCOPES.cashOut,
      ),
    ).toMatchObject({ amountMinorUnits: 2500 });
    expect(
      readSavedRequest(
        createKeyStore({ subject: "ben", storage: localStorage }),
        MONEY_SCOPES.cashOut,
      ),
    ).toBeNull();
  });
});
