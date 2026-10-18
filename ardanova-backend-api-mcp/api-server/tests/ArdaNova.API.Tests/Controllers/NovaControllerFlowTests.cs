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

    [Theory]
    [InlineData("Built with OVERSEASGENAI: who pays?")]
    [InlineData("Overseas Gen-AI wants to know: who pays?")]
    [InlineData("OVERSEAS\u200bGENAI asks: who pays?")]
    [InlineData("As OGAI I ask: who pays?")]
    [InlineData("Powered by OG-AI: who pays?")]
    [InlineData("O.G.A.I. asks: who pays?")]
    [InlineData("OG AI wants to know: who pays?")]
    public async Task ReplyNamingTheUpstreamProviderNeverReachesTheMemberNorCostsTheirAllowance(string leak)
    {
        var nova = NovaFor(new FakePartnerApi { Leak = leak });

        var result = await nova.Controller.RefineProject(new NovaRefineRequestDto { Idea = Idea }, CancellationToken.None);

        StatusOf(result).Should().Be(StatusCodes.Status503ServiceUnavailable);
        ErrorOf(result).Should().Be("nova_unavailable", "a fresh first turn mints a new upstream session, so retrying can succeed");
        JsonSerializer.Serialize(((ObjectResult)result).Value).Should().NotContainEquivalentOf("ogai").And.NotContainEquivalentOf("overseas");
        RemainingOf(nova).Should().Be(10, "ArdaNova refused the reply, so the member never got the refinement they paid for");
    }

    [Theory]
    [InlineData("Would yoga instructors in town pay for this?")]
    [InlineData("Is the log aisle in the hardware store your rival?")]
    public async Task OrdinaryWordsNearTheShortNameStillReachTheMember(string question)
    {
        var nova = NovaFor(new FakePartnerApi { Leak = question });

        var reply = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea });

        reply.Question.Should().Be(question);
    }

    [Fact]
    public async Task AContinuationArdaNovaDropsTellsTheMemberToStartAgainOnEveryRetry()
    {
        var upstream = new FakePartnerApi { Leak = "Powered by OG-AI: who pays?", LeakFromTurn = 2 };
        var nova = NovaFor(upstream);
        var first = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea });

        var dropped = await nova.Controller.RefineProject(Continuation(first), CancellationToken.None);
        var retried = await nova.Controller.RefineProject(Continuation(first), CancellationToken.None);
        var startedAgain = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea });

        ErrorOf(dropped).Should().Be("nova_reply_unusable", "the partner replays an answered turn, so 'try again' would loop forever");
        ErrorOf(retried).Should().Be("nova_reply_unusable");
        startedAgain.Number.Should().Be(1);
    }

    [Fact]
    public async Task AFirstTurnWhoseStateCannotRoundTripCostsTheMemberNothing()
    {
        var nova = NovaFor(new FakePartnerApi { StateLength = NovaSessionTickets.MaxUpstreamStateLength + 1 });

        var result = await nova.Controller.RefineProject(new NovaRefineRequestDto { Idea = Idea }, CancellationToken.None);

        StatusOf(result).Should().Be(StatusCodes.Status503ServiceUnavailable);
        RemainingOf(nova).Should().Be(10);
    }

    [Fact]
    public async Task AVerdictWithAnOutcomeNovaCannotLabelIsNeverShownRaw()
    {
        var nova = NovaFor(new FakePartnerApi { Outcome = "Ask OVERSEAS support" });
        var reply = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea });
        var turns = new List<NovaTurnDto>();
        IActionResult result;
        do
        {
            turns.Add(new NovaTurnDto { Question = reply.Question!, Answer = "Forty households asked." });
            result = await nova.Controller.RefineProject(new NovaRefineRequestDto { Idea = Idea, Turns = turns, State = reply.State, SessionId = reply.SessionId }, CancellationToken.None);
            if (result is OkObjectResult { Value: NovaRefineResponseDto next } && next.Type == "question")
                reply = next;
            else
                break;
        } while (true);

        ErrorOf(result).Should().Be("nova_reply_unusable");
    }

    [Fact]
    public async Task VerdictSourcesThatAreNotWebLinksAreDropped()
    {
        var nova = NovaFor(new FakePartnerApi
        {
            Sources = [("Survey", "https://example.org/survey"), ("Script", "javascript:alert(1)"), ("Data", "data:text/html,hi"), ("Relative", "/admin")],
        });
        var reply = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea });
        var turns = new List<NovaTurnDto>();
        while (reply.Type == "question")
        {
            turns.Add(new NovaTurnDto { Question = reply.Question!, Answer = "Forty households asked." });
            reply = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea, Turns = turns, State = reply.State, SessionId = reply.SessionId });
        }

        reply.Verdict!.Sources.Select(source => source.Url).Should().Equal("https://example.org/survey");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "", StatusCodes.Status503ServiceUnavailable, "nova_unavailable")]
    [InlineData(HttpStatusCode.ServiceUnavailable, """{"error":"unavailable","reason":"calls_full"}""", StatusCodes.Status503ServiceUnavailable, "nova_unavailable")]
    [InlineData(HttpStatusCode.ServiceUnavailable, """{"error":"unavailable","reason":"spend_closed"}""", StatusCodes.Status503ServiceUnavailable, "nova_capacity")]
    [InlineData(HttpStatusCode.TooManyRequests, """{"error":"quota","reason":"daily_sessions"}""", StatusCodes.Status503ServiceUnavailable, "nova_capacity")]
    [InlineData(HttpStatusCode.BadRequest, """{"error":"Something went wrong. Please try again."}""", StatusCodes.Status400BadRequest, null)]
    public async Task UpstreamRefusalsMapToMemberStatusesAndNeverLogTheKey(HttpStatusCode upstreamStatus, string upstreamBody, int expected, string? expectedError)
    {
        var nova = NovaFor(new FakePartnerApi { FailWith = (upstreamStatus, upstreamBody) });

        var result = await nova.Controller.RefineProject(new NovaRefineRequestDto { Idea = Idea }, CancellationToken.None);

        StatusOf(result).Should().Be(expected);
        if (expectedError is not null)
            ErrorOf(result).Should().Be(expectedError, "the partner's whole day being spent is capacity, never this member's own limit");
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
    public async Task ARefinementContinuesAfterARestartOrOnAnotherReplica()
    {
        var upstream = new FakePartnerApi();
        var clock = new ManualClock();
        var before = NovaFor(upstream, clock: clock);
        var first = await RefineAsync(before, new NovaRefineRequestDto { Idea = Idea });

        var after = NovaFor(upstream, clock: clock);
        var second = await RefineAsync(after, Continuation(first));

        second.Number.Should().Be(2, "ownership travels in the signed state, not in one process's memory");
    }

    [Fact]
    public async Task AnExpiredRefinementReadsAsNotFoundBeforeAnyUpstreamCall()
    {
        var upstream = new FakePartnerApi();
        var clock = new ManualClock();
        var nova = NovaFor(upstream, clock: clock);
        var first = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea });
        var callsBefore = upstream.SeenAuthorization.Count;

        clock.Advance(TimeSpan.FromHours(2));
        var late = await nova.Controller.RefineProject(Continuation(first), CancellationToken.None);

        StatusOf(late).Should().Be(StatusCodes.Status404NotFound);
        ErrorOf(late).Should().Be("nova_session_not_found");
        upstream.SeenAuthorization.Count.Should().Be(callsBefore, "a session past the upstream's lifetime is never sent there");
    }

    [Fact]
    public async Task UpstreamSessionExpiryTellsTheMemberToStartAgainNotToFixTheirInput()
    {
        var upstream = new FakePartnerApi();
        var nova = NovaFor(upstream);
        var first = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea });
        upstream.FailWith = (HttpStatusCode.BadRequest, """{"error":"session","reason":"state_expired"}""");

        var result = await nova.Controller.RefineProject(Continuation(first), CancellationToken.None);

        StatusOf(result).Should().Be(StatusCodes.Status404NotFound);
        ErrorOf(result).Should().Be("nova_session_not_found");
    }

    [Fact]
    public async Task ProductionWithoutTheServiceSaysOffAndNeverPretends()
    {
        var nova = NovaFor(new DisabledOgaiClient());

        var status = (NovaStatusDto)((OkObjectResult)nova.Controller.Status().Result!).Value!;
        var turn = await nova.Controller.RefineProject(new NovaRefineRequestDto { Idea = Idea }, CancellationToken.None);

        status.Refinement.Should().Be("off");
        StatusOf(turn).Should().Be(StatusCodes.Status503ServiceUnavailable);
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
        ((NovaStatusDto)((OkObjectResult)nova.Controller.Status().Result!).Value!).Refinement.Should().Be("local-preview");
        reply.Verdict!.Outcome.Should().Be(LocalNovaPreviewClient.PreviewOutcome);
        reply.Brief.Should().BeNull("the preview analyses nothing, so it proposes no brief");
    }

    [Fact]
    public async Task AMemberRunsOneTurnAtATimeSoNobodyHoldsTheSharedCapacity()
    {
        var upstream = new FakePartnerApi { Hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
        var nova = NovaFor(upstream);
        var start = new NovaRefineRequestDto { Idea = Idea };

        var slow = nova.Controller.RefineProject(start, CancellationToken.None);
        var parallel = await nova.Controller.RefineProject(start, CancellationToken.None);
        upstream.Hold.SetResult();
        var finished = await slow;
        var next = await nova.Controller.RefineProject(start, CancellationToken.None);

        StatusOf(parallel).Should().Be(StatusCodes.Status429TooManyRequests);
        ErrorOf(parallel).Should().Be("nova_turn_in_flight", "a turn already running is not the member's daily limit");
        StatusOf(finished).Should().Be(StatusCodes.Status200OK);
        StatusOf(next).Should().Be(StatusCodes.Status200OK, "the slot frees once the slow turn answers");
        ((NovaStatusDto)((OkObjectResult)nova.Controller.Status().Result!).Value!).RemainingToday.Should().Be(8, "the refused parallel start spent nothing");
    }

    [Fact]
    public async Task LiveRepliesAndStatusSayHowManyRefinementsAreLeftToday()
    {
        var nova = NovaFor(new FakePartnerApi(), dailySessions: 3);

        var before = (NovaStatusDto)((OkObjectResult)nova.Controller.Status().Result!).Value!;
        var first = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea });
        var second = await RefineAsync(nova, Continuation(first));

        before.Should().BeEquivalentTo(new { Refinement = "live", DailyLimit = 3, RemainingToday = 3 });
        first.RemainingToday.Should().Be(2);
        second.RemainingToday.Should().Be(2, "answering a question continues the same refinement");
    }

    [Fact]
    public async Task AStaleLocalPreviewTellsTheMemberToStartAgainNotToFixTheirInput()
    {
        var nova = NovaFor(new LocalNovaPreviewClient());
        var first = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea });

        var otherIdea = await nova.Controller.RefineProject(new NovaRefineRequestDto
        {
            Idea = Idea + " Now with bikes.",
            Turns = [new NovaTurnDto { Question = first.Question!, Answer = "Renters." }],
            State = first.State,
            SessionId = first.SessionId,
        }, CancellationToken.None);

        StatusOf(otherIdea).Should().Be(StatusCodes.Status404NotFound);
        ErrorOf(otherIdea).Should().Be("nova_session_not_found");
    }

    [Fact]
    public async Task LocalPreviewNeverSpendsTheMembersDailyNovaAiAllowance()
    {
        var nova = NovaFor(new LocalNovaPreviewClient(), dailySessions: 1);
        var start = new NovaRefineRequestDto { Idea = Idea };

        var first = await RefineAsync(nova, start);
        var second = await RefineAsync(nova, start);

        second.Mode.Should().Be("local-preview", "fixed practice questions cost nothing, so they never count toward the limit");
        first.RemainingToday.Should().BeNull();
        ((NovaStatusDto)((OkObjectResult)nova.Controller.Status().Result!).Value!).RemainingToday.Should().BeNull();
    }

    /// <summary>Partner wire contract pinned with OVERSEASGENAI (twin: its partner_contract_test.go): states up to 512 characters round-trip.</summary>
    [Theory]
    [InlineData(NovaSessionTickets.MaxUpstreamStateLength, StatusCodes.Status200OK)]
    [InlineData(NovaSessionTickets.MaxUpstreamStateLength + 1, StatusCodes.Status503ServiceUnavailable)]
    public async Task PartnerContractStateAtTheSharedLimitRoundTripsAndOneOverFailsAtOnce(int stateLength, int expected)
    {
        var nova = NovaFor(new FakePartnerApi { StateLength = stateLength });

        var first = await nova.Controller.RefineProject(new NovaRefineRequestDto { Idea = Idea }, CancellationToken.None);
        var outcome = expected == StatusCodes.Status200OK
            ? await nova.Controller.RefineProject(Continuation((NovaRefineResponseDto)((OkObjectResult)first).Value!), CancellationToken.None)
            : first;

        StatusOf(outcome).Should().Be(expected);
    }

    [Fact]
    public async Task PartnerContractCommunityProjectVerdictWithoutALaunchVerticalKeepsItsBrief()
    {
        var nova = NovaFor(new FakePartnerApi { Vertical = "none" });

        var reply = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea });
        var turns = new List<NovaTurnDto>();
        while (reply.Type == "question")
        {
            turns.Add(new NovaTurnDto { Question = reply.Question!, Answer = "Neighbours chip in 5 a month." });
            reply = await RefineAsync(nova, new NovaRefineRequestDto { Idea = Idea, Turns = turns, State = reply.State, SessionId = reply.SessionId });
        }

        reply.Vertical.Should().Be("none");
        reply.Brief!.WorkingName.Should().Be("Guild Tool Library");
    }

    private static async Task<NovaRefineResponseDto> RefineAsync(NovaHarness nova, NovaRefineRequestDto request)
    {
        var result = await nova.Controller.RefineProject(request, CancellationToken.None);
        StatusOf(result).Should().Be(StatusCodes.Status200OK, JsonSerializer.Serialize((result as ObjectResult)?.Value));
        return (NovaRefineResponseDto)((OkObjectResult)result).Value!;
    }

    private static int? RemainingOf(NovaHarness nova)
        => ((NovaStatusDto)((OkObjectResult)nova.Controller.Status().Result!).Value!).RemainingToday;

    private static int? StatusOf(IActionResult result) => (result as ObjectResult)?.StatusCode;

    private static string? ErrorOf(IActionResult result)
        => JsonSerializer.SerializeToNode((result as ObjectResult)?.Value)?["error"]?.GetValue<string>();

    private static NovaRefineRequestDto Continuation(NovaRefineResponseDto question) => new()
    {
        Idea = Idea,
        Turns = [new NovaTurnDto { Question = question.Question!, Answer = "Forty households asked." }],
        State = question.State,
        SessionId = question.SessionId,
    };

    private static readonly byte[] TicketKey = NovaSessionTickets.DeriveKey("nova-test-ticket-secret-0123456789abcdef");

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }

    private sealed record NovaHarness(NovaController Controller, INovaOrchestrationService Service, List<string> Logs);

    private static NovaHarness NovaFor(FakePartnerApi upstream, int dailySessions = 10, TimeProvider? clock = null)
    {
        var logs = new List<string>();
        var http = new HttpClient(upstream) { BaseAddress = new Uri("https://ogai.test/") };
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", PartnerKey);
        return NovaFor(new OgaiClient(http, new CapturingLogger<OgaiClient>(logs)), dailySessions, logs, clock);
    }

    private static NovaHarness NovaFor(IOgaiClient client, int dailySessions = 10, List<string>? logs = null, TimeProvider? clock = null)
    {
        logs ??= [];
        clock ??= TimeProvider.System;
        var service = new NovaOrchestrationService(client, new NovaSessionAllowance(clock, dailySessions),
            new NovaSessionTickets(TicketKey, clock), new CapturingLogger<NovaOrchestrationService>(logs));
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
        /// <summary>First question number (1-based) that carries <see cref="Leak"/>; earlier ones stay clean.</summary>
        public int LeakFromTurn { get; init; } = 1;
        public string Outcome { get; init; } = "adjust";
        public (string Title, string Url)[] Sources { get; init; } = [];
        public List<string> SeenAuthorization { get; } = [];
        /// <summary>When set, every turn waits for it, as a slow upstream turn does.</summary>
        public TaskCompletionSource? Hold { get; set; }
        /// <summary>Pads each issued state to this many characters; 0 keeps "state-N".</summary>
        public int StateLength { get; init; }
        public string Vertical { get; init; } = "services";

        private string StateFor(int number)
            => StateLength > 0 ? $"state-{number}".PadRight(StateLength, 's') : $"state-{number}";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            SeenAuthorization.Add(request.Headers.Authorization?.ToString() ?? string.Empty);
            if (request.Method != HttpMethod.Post || request.RequestUri!.AbsolutePath != "/api/partner/v1/validate")
                return Json(HttpStatusCode.NotFound, "{}");
            if (FailWith is { } failure)
                return Json(failure.Status, failure.Body);
            if (Hold is { } hold)
                await hold.Task.WaitAsync(ct);

            var body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!;
            var answered = body["turns"]!.AsArray().Count;
            if (answered > 0 && body["state"]?.GetValue<string>() != StateFor(answered))
                return Json(HttpStatusCode.BadRequest, """{"error":"Something went wrong. Please try again."}""");

            if (answered < QuestionsBeforeVerdict)
            {
                return Json(HttpStatusCode.OK, JsonSerializer.Serialize(new
                {
                    type = "question", assistant = "Nova AI", sessionId = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                    question = Leak is not null && answered + 1 >= LeakFromTurn ? Leak : $"Question {answered + 1}: who pays?", number = answered + 1, maxQuestions = 8,
                    state = StateFor(answered + 1),
                }));
            }
            return Json(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                type = "verdict", assistant = "Nova AI", sessionId = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                verdict = new
                {
                    outcome = Outcome, headline = "Promising, with one change.", summary = "Members want tools without buying them.",
                    strengths = new[] { "A clear community" }, risks = new[] { "Repair costs are untested" },
                    nextStep = "Ask twenty members what they would pay.", sources = Sources.Select(source => new { title = source.Title, url = source.Url }).ToArray(),
                },
                vertical = Vertical,
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
