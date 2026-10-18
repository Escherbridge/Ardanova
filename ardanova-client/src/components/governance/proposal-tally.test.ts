import { describe, expect, it } from "vitest";
import { quorumProgressPercent, readProposalOptions } from "./proposal-tally";

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
