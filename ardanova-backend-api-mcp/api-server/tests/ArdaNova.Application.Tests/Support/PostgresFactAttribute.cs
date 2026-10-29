namespace ArdaNova.Application.Tests.Support;

/// <summary>The real-Postgres gate: <c>ARDANOVA_TEST_POSTGRES</c>, a server connection string with no database name.</summary>
public static class PostgresTestServer
{
    public const string ConnectionEnv = "ARDANOVA_TEST_POSTGRES";

    /// <summary>The server to create throwaway databases on, or null when none is configured.</summary>
    public static string? ConnectionString
        => Environment.GetEnvironmentVariable(ConnectionEnv) is { } value && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    /// <summary>Why a real-Postgres test cannot run with this connection setting, or null when it can.</summary>
    public static string? SkipReasonFor(string? connection)
        => string.IsNullOrWhiteSpace(connection)
            ? $"Needs a real Postgres server: set {ConnectionEnv} to a server connection string (CI sets it)."
            : null;
}

/// <summary>
/// A [Fact] that runs only against a real Postgres server and reports SKIPPED (never a silent pass) without one.
/// xunit 2.9 has no runtime skip, so the reason is set at discovery from the environment.
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        Skip = PostgresTestServer.SkipReasonFor(PostgresTestServer.ConnectionString);
    }
}

/// <summary>The [Theory] counterpart of <see cref="PostgresFactAttribute"/>.</summary>
public sealed class PostgresTheoryAttribute : TheoryAttribute
{
    public PostgresTheoryAttribute()
    {
        Skip = PostgresTestServer.SkipReasonFor(PostgresTestServer.ConnectionString);
    }
}
