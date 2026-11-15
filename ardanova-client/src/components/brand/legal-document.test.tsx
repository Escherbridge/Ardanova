// @vitest-environment jsdom
import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { OperatorContact } from "./legal-document";

describe("operator contact", () => {
  it("equals the city and public email form", () => {
    const { container } = render(<OperatorContact />);
    const mail = container.querySelector("a[href^='mailto:']");
    const location = container.querySelector("address > p:last-of-type");

    expect(`${location?.textContent ?? ""} ${mail?.textContent ?? ""}`).toBe(
      "Boise, Idaho contact@escherbridge.com",
    );
    expect(mail?.getAttribute("href")).toBe("mailto:contact@escherbridge.com");
  });
});
