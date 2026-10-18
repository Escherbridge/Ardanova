import { describe, expect, it } from "vitest";
import {
  ballotStandingMessage,
  quorumProgressPercent,
  readProposalOptions,
} from "./proposal-tally";

describe("ballotStandingMessage", () => {
  const base = {
    isSignedIn: true,
    ballotLoading: false,
    windowHasEnded: false,
  };
  const notConfirmed = "Only confirmed members of this project can vote on it";
  it.each([
    {
      name: "a cast ballot is reported by its option label, even after the window ends",
      input: {
        ...base,
        windowHasEnded: true,
        ballot: { canVote: false, choice: 0 },
        choiceLabel: "For",
      },
      expected: "You voted For. Ballots are final.",
    },
    {
      name: "choice 0 is a real ballot, not an absent one",
      input: { ...base, ballot: { canVote: false, choice: 0 } },
      expected: "You voted option 0. Ballots are final.",
    },
    {
      name: "an ended window says so instead of the eligibility reason",
      input: {
        ...base,
        windowHasEnded: true,
        ballot: { canVote: false, reason: notConfirmed },
      },
      expected: "Voting has closed; the result is being counted.",
    },
    {
      name: "a signed-out viewer is asked to sign in",
      input: { ...base, isSignedIn: false },
      expected: "Sign in to vote on this proposal.",
    },
    {
      name: "an ineligible member sees the server's own refusal reason",
      input: { ...base, ballot: { canVote: false, reason: notConfirmed } },
      expected: notConfirmed,
    },
  ])("$name", ({ input, expected }) => {
    expect(ballotStandingMessage(input)).toBe(expected);
  });
});

describe("readProposalOptions", () => {
  it.each([
    {
      name: "string options take their index as the choice",
      options: '["Yes","No"]',
      votes: { "0": 3, "1": 1 },
      expected: [
        { label: "Yes", choice: 0, votes: 3, percentage: 75 },
        { label: "No", choice: 1, votes: 1, percentage: 25 },
      ],
    },
    {
      name: "object options keep their declared choice numbers",
      options: '[{"label":"Approve","choice":7},{"label":"Reject","choice":9}]',
      votes: { "9": 2 },
      expected: [
        { label: "Approve", choice: 7, votes: 0, percentage: 0 },
        { label: "Reject", choice: 9, votes: 2, percentage: 100 },
      ],
    },
    {
      name: "an unlabelled object option is named by position, not 'Option'",
      options: '[{"choice":0},{"label":"No"}]',
      votes: undefined,
      expected: [
        { label: "Option 1", choice: 0, votes: 0, percentage: 0 },
        { label: "No", choice: 1, votes: 0, percentage: 0 },
      ],
    },
  ])("$name", ({ options, votes, expected }) => {
    expect(readProposalOptions(options, votes)).toEqual(expected);
  });

  it.each(["not json", '{"a":1}', '["Yes", 3]', '["Yes", ""]', ""])(
    "never invents options when they cannot be read: %s",
    (options) => {
      expect(readProposalOptions(options, { "0": 1 })).toEqual([]);
    },
  );
});

describe("quorumProgressPercent", () => {
  it.each([
    { cast: 1, needed: 2, expected: 50 },
    { cast: 3, needed: 2, expected: 100 },
    { cast: 1, needed: 0, expected: 0 },
  ])("$cast of $needed -> $expected%", ({ cast, needed, expected }) => {
    expect(quorumProgressPercent(cast, needed)).toBe(expected);
  });
});
