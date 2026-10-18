using ArdaNova.API.Middleware;
using Microsoft.AspNetCore.Http;

namespace ArdaNova.API.Tests.Middleware;

/// <summary>Database faults the request caused answer 4xx; anything the server caused stays an opaque 500.</summary>
public sealed class ExceptionClassificationTests
{
    [Theory]
    [InlineData("22P02", StatusCodes.Status400BadRequest)] // invalid json for a jsonb column (POST /api/Achievements criteria)
    [InlineData("22001", StatusCodes.Status400BadRequest)] // value too long
    [InlineData("23505", StatusCodes.Status409Conflict)]
    [InlineData("23503", StatusCodes.Status400BadRequest)]
    [InlineData("23502", StatusCodes.Status500InternalServerError)] // a missing NOT NULL value is a server bug
    [InlineData("40P01", StatusCodes.Status500InternalServerError)]
    public void WrappedPostgresFault_MapsBySqlState(string sqlState, int expectedStatus)
    {
        var exception = new InvalidOperationException(
            "An error occurred while saving the entity changes.",
            new FakePostgresException(sqlState));

        var (status, message) = ExceptionHandlingMiddleware.Classify(exception);

        status.Should().Be(expectedStatus);
        message.Should().NotContain(sqlState).And.NotContain("entity changes");
    }

    private sealed class FakePostgresException(string sqlState) : Exception("23505: duplicate key value violates unique constraint \"Achievement_name_key\"")
    {
        public string SqlState { get; } = sqlState;
    }
}
