/** The published limit on records. Kept next to any pattern or note index. */
export function SimulationNotice({
  includeAzoa = false,
}: {
  includeAzoa?: boolean;
}) {
  return (
    <div className="space-y-3">
      <p>The economy in this workspace is simulated.</p>
      <p>
        Platform records do not automatically create legal ownership, equity, or
        payment rights.
      </p>
      {includeAzoa ? (
        <p>
          <a
            className="underline underline-offset-4"
            href="https://azoa.ardanova.com"
          >
            AZOA, the planned workflow engine, also still a simulation
          </a>
          .
        </p>
      ) : null}
    </div>
  );
}
