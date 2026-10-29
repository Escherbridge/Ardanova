namespace ArdaNova.Application.Tests.Support;

using System.Reflection;

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

/// <summary>
/// The policy both <c>RealPostgresTestGateTests</c> classes (one per test assembly that carries
/// real-Postgres flow tests) run: every <c>[Trait("Category", "Database")]</c> class's [Fact]/[Theory]
/// methods must be gated by <see cref="PostgresFactAttribute"/> or <see cref="PostgresTheoryAttribute"/>,
/// never a plain [Fact] that could early-return into a silent PASSED.
/// </summary>
public static class RealPostgresTestGate
{
    /// <summary>Types in <paramref name="assembly"/> carrying <c>[Trait("Category", "Database")]</c> on the class.</summary>
    public static List<Type> DatabaseCategoryClasses(Assembly assembly)
        => assembly.GetTypes()
            .Where(type => type.GetCustomAttributesData().Any(attribute =>
                attribute.AttributeType == typeof(TraitAttribute)
                && attribute.ConstructorArguments is [{ Value: "Category" }, { Value: "Database" }]))
            .ToList();

    /// <summary>
    /// Methods on <paramref name="databaseClasses"/> whose [Fact]/[Theory] is not one of the
    /// skipping attributes above - i.e. a plain [Fact] that would report PASSED without a server.
    /// </summary>
    public static List<string> UngatedMethods(IEnumerable<Type> databaseClasses)
        => databaseClasses
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttribute<FactAttribute>() is { } fact
                && fact is not PostgresFactAttribute and not PostgresTheoryAttribute)
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .ToList();
}
