import { render, screen } from "@testing-library/react";
import Home from "./page";

describe("Home", () => {
  it("renders core clinic ui", () => {
    render(<Home />);

    expect(screen.getByRole("heading", { name: "Clinic POS" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Create Patient" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Refresh" })).toBeInTheDocument();
  });
});
