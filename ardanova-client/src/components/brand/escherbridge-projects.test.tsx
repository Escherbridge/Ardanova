// @vitest-environment jsdom
import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { NoteBody } from "~/app/notes/articles";

import {
  EscherbridgeProjects,
  ESCHERBRIDGE_PROJECTS,
} from "./escherbridge-projects";

function followHrefs(container: HTMLElement): string[] {
  return [...container.querySelectorAll("a")].flatMap((anchor) => {
    expect(anchor.getAttribute("rel") ?? "").not.toMatch(/nofollow/i);
    const href = anchor.getAttribute("href");
    return href ? [href] : [];
  });
}

describe("Escherbridge project footer", () => {
  it("lists each sister on its own domain and omits ArdaNova", () => {
    const { container } = render(<EscherbridgeProjects />);
    const hrefs = followHrefs(container);
    expect(hrefs).toEqual(ESCHERBRIDGE_PROJECTS.map((site) => site.href));
    expect(hrefs).not.toContain("https://ardanova.com");
    expect(container.textContent).toContain("An Escherbridge project");
    for (const site of ESCHERBRIDGE_PROJECTS) {
      expect(container.textContent).toContain(site.label);
    }
  });
});

describe("public notes", () => {
  it("keeps follow links to PrimusNeo and AZOA", () => {
    const record = render(<NoteBody slug="a-record-is-not-a-right" />);
    const recordHrefs = followHrefs(record.container);
    expect(recordHrefs).toContain("https://primusneo.com");
    expect(recordHrefs).toContain("https://azoa.ardanova.com");

    const agreement = render(<NoteBody slug="two-records-one-agreement" />);
    expect(followHrefs(agreement.container)).toContain("https://primusneo.com");
  });
});
