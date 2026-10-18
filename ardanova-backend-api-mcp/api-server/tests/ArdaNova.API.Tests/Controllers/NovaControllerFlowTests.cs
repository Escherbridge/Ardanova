namespace ArdaNova.API.Tests.Controllers;

using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ArdaNova.API.Controllers;
using ArdaNova.Application.DTOs.Nova;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Infrastructure.Ogai;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Nova AI project refinement driven through the controller, with only the upstream HTTP edge faked.</summary>
public class NovaControllerFlowTests
{
    private const string PartnerKey = "partner-key-0123456789abcdefghijklmnopqrstuv";
    private const string Idea = "A tool library run by a neighbourhood guild, lending power tools to members.";

    [Fact]
    public async Task MemberRefinesAnIdeaToAVerdictWithItsBrief()
    {
        var upstream = new FakePartnerApi();
        var nova = NovaFor(upstream);

        var first = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea });
        var turns = new List<NovaTurnDto>();
        var reply = first;
        while (reply.Type == "question")
        {
            turns.Add(new NovaTurnDto { Question = reply.Question!, Answer = "Forty households asked; they pay 5 a month." });
            reply = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea, Turns = turns, State = reply.State, SessionId = reply.SessionId });
        }

        first.Should().BeEquivalentTo(new { Type = "question", Assistant = "Nova AI", Mode = "live", Number = 1 });
        turns.Should().HaveCount(FakePartnerApi.QuestionsBeforeVerdict);
        reply.Assistant.Should().Be("Nova AI");
        reply.Verdict!.Outcome.Should().Be("adjust");
        reply.Brief!.WorkingName.Should().Be("Guild Tool Library");
        upstream.SeenAuthorization.Should().AllBe($"Bearer {PartnerKey}");
    }

    [Fact]
    public async Task ReplyNamingTheUpstreamProviderNeverReachesTheMember()
    {
        var nova = NovaFor(new FakePartnerApi { Leak = "Built with OVERSEASGENAI: who pays?" });

        var result = await nova.Controller.RefineProject(new NovaRefineRequestDto { Idea = Idea }, CancellationToken.None);

        StatusOf(result).Should().Be(StatusCodes.Status503ServiceUnavailable);
        JsonSerializer.Serialize(((ObjectResult)result).Value).Should().NotContainEquivalentOf("overseasgenai");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "", StatusCodes.Status503ServiceUnavailable)]
    [InlineData(HttpStatusCode.ServiceUnavailable, """{"error":"unavailable","reason":"spend_closed"}""", StatusCodes.Status503ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests, """{"error":"quota","reason":"daily_sessions"}""", StatusCodes.Status429TooManyRequests)]
    [InlineData(HttpStatusCode.BadRequest, """{"error":"Something went wrong. Please try again."}""", StatusCodes.Status400BadRequest)]
    public async Task UpstreamRefusalsMapToMemberStatusesAndNeverLogTheKey(HttpStatusCode upstreamStatus, string upstreamBody, int expected)
    {
        var nova = NovaFor(new FakePartnerApi { FailWith = (upstreamStatus, upstreamBody) });

        var result = await nova.Controller.RefineProject(new NovaRefineRequestDto { Idea = Idea }, CancellationToken.None);

        StatusOf(result).Should().Be(expected);
        nova.Logs.Should().NotContain(line => line.Contains(PartnerKey));
    }

    [Fact]
    public async Task DailyAllowanceStopsNewSessionsButAFailedStartIsGivenBack()
    {
        var upstream = new FakePartnerApi { FailWith = (HttpStatusCode.ServiceUnavailable, "") };
        var nova = NovaFor(upstream, dailySessions: 1);
        var start = new NovaRefineRequestDto { Idea = Idea };

        var outage = await nova.Controller.RefineProject(start, CancellationToken.None);
        upstream.FailWith = null;
        var afterOutage = await nova.Controller.RefineProject(start, CancellationToken.None);
        var overAllowance = await nova.Controller.RefineProject(start, CancellationToken.None);

        StatusOf(outage).Should().Be(StatusCodes.Status503ServiceUnavailable);
        StatusOf(afterOutage).Should().Be(StatusCodes.Status200OK);
        StatusOf(overAllowance).Should().Be(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public async Task AnotherMembersSessionReadsAsNotFoundAndSpendsNothingUpstream()
    {
        var upstream = new FakePartnerApi();
        var owner = NovaFor(upstream);
        var intruder = owner with { Controller = ControllerFor(owner.Service, "member-2") };
        var first = await RefineAsync(owner, new NovaRefineRequestDto { Idea = Idea });
        var continuation = new NovaRefineRequestDto
        {
            Idea = Idea,
            Turns = [new NovaTurnDto { Question = first.Question!, Answer = "Forty households asked." }],
            State = first.State,
            SessionId = first.SessionId,
        };
        var callsBefore = upstream.SeenAuthorization.Count;

        var hijack = await intruder.Controller.RefineProject(continuation, CancellationToken.None);
        var callsAfterHijack = upstream.SeenAuthorization.Count;
        var ownersTurn = await RefineAsync(owner, continuation);

        StatusOf(hijack).Should().Be(StatusCodes.Status404NotFound);
        callsAfterHijack.Should().Be(callsBefore);
        ownersTurn.Number.Should().Be(2);
    }

    [Fact]
    public async Task UnconfiguredEnvironmentRunsTheLabelledLocalPreview()
    {
        var nova = NovaFor(new LocalNovaPreviewClient());

        var reply = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea });
        var turns = new List<NovaTurnDto>();
        while (reply.Type == "question")
        {
            turns.Add(new NovaTurnDto { Question = reply.Question!, Answer = "Neighbours, mostly renters." });
            reply = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea, Turns = turns, State = reply.State, SessionId = reply.SessionId });
        }

        reply.Mode.Should().Be("local-preview");
        reply.Verdict!.Outcome.Should().Be(LocalNovaPreviewClient.PreviewOutcome);
        reply.Brief.Should().BeNull("the preview analyses nothing, so it proposes no brief");
    }

    private static async Task<NovaRefineResponseDto> RefineAsync(NovaHarness nova, NovaRefineRequestDto request)
    {
        var result = await nova.Controller.RefineProject(request, CancellationToken.None);
        StatusOf(result).Should().Be(StatusCodes.Status200OK, JsonSerializer.Serialize((result as ObjectResult)?.Value));
        return (NovaRefineResponseDto)((OkObjectResult)result).Value!;
    }

    private static int? StatusOf(IActionResult result) => (result as ObjectResult)?.StatusCode;

    private sealed record NovaHarness(NovaController Controller, INovaOrchestrationService Service, List<string> Logs);

    private static NovaHarness NovaFor(FakePartnerApi upstream, int dailySessions = 10)
    {
        var logs = new List<string>();
        var http = new HttpClient(upstream) { BaseAddress = new Uri("https://ogai.test/") };
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", PartnerKey);
        return NovaFor(new OgaiClient(http, new CapturingLogger<OgaiClient>(logs)), dailySessions, logs);
    }

    private static NovaHarness NovaFor(IOgaiClient client, int dailySessions = 10, List<string>? logs = null)
    {
        logs ??= [];
        var service = new NovaOrchestrationService(client, new NovaSessionAllowance(TimeProvider.System, dailySessions),
            new CapturingLogger<NovaOrchestrationService>(logs));
        return new NovaHarness(ControllerFor(service, "member-1"), service, logs);
    }

    private static NovaController ControllerFor(INovaOrchestrationService service, string actorId) => new(service)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actorId)], "test")),
            },
        },
    };

    /// <summary>Behaves like POST /api/partner/v1/validate: asks numbered questions, checks the state round-trip, then a verdict.</summary>
    private sealed class FakePartnerApi : HttpMessageHandler
    {
        public const int QuestionsBeforeVerdict = 2;
        public (HttpStatusCode Status, string Body)? FailWith { get; set; }
        public string? Leak { get; init; }
        public List<string> SeenAuthorization { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            SeenAuthorization.Add(request.Headers.Authorization?.ToString() ?? string.Empty);
            if (request.Method != HttpMethod.Post || request.RequestUri!.AbsolutePath != "/api/partner/v1/validate")
                return Json(HttpStatusCode.NotFound, "{}");
            if (FailWith is { } failure)
                return Json(failure.Status, failure.Body);

            var body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!;
            var answered = body["turns"]!.AsArray().Count;
            if (answered > 0 && body["state"]?.GetValue<string>() != $"state-{answered}")
                return Json(HttpStatusCode.BadRequest, """{"error":"Something went wrong. Please try again."}""");

            if (answered < QuestionsBeforeVerdict)
            {
                return Json(HttpStatusCode.OK, JsonSerializer.Serialize(new
                {
                    type = "question", assistant = "Nova AI", sessionId = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                    question = Leak ?? $"Question {answered + 1}: who pays?", number = answered + 1, maxQuestions = 8,
                    state = $"state-{answered + 1}",
                }));
            }
            return Json(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                type = "verdict", assistant = "Nova AI", sessionId = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                verdict = new
                {
                    outcome = "adjust", headline = "Promising, with one change.", summary = "Members want tools without buying them.",
                    strengths = new[] { "A clear community" }, risks = new[] { "Repair costs are untested" },
                    nextStep = "Ask twenty members what they would pay.", sources = Array.Empty<object>(),
                },
                vertical = "services",
                brief = new
                {
                    workingName = "Guild Tool Library", oneLiner = "Shared power tools for guild members.", customer = "Renters.",
                    problem = "Tools are costly.", offer = "Monthly lending.", pricing = "About 5 a month.",
                    channels = new[] { "Guild board" }, milestones = new[] { "Survey", "Pilot", "Launch" }, metrics = new[] { "Loans", "Members", "Repairs" },
                },
            }));
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string body)
            => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }

    private sealed class CapturingLogger<T>(List<string> lines) : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullLogger.Instance.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => lines.Add(formatter(state, exception));
    }
}
