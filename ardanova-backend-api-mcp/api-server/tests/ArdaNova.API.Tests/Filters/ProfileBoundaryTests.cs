using System.Security.Claims;
using System.Text.Json;
using ArdaNova.API.Filters;
using ArdaNova.API.Middleware;
using ArdaNova.API.Readiness;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace ArdaNova.API.Tests.Filters;

/// <summary>Profile-shaped data at the API edge: member rosters keep emails private, and contact/link fields refuse script-capable or malformed values.</summary>
public sealed class ProfileBoundaryTests
{
    private const string Member = "member-user-0001";
    private const string Colleague = "colleague-user-01";

    [Theory]
    [InlineData(null)]
    [InlineData(Colleague)]
    [InlineData(Member)]
    public void ProjectRoster_ShowsAMembersEmailOnlyToThatMember(string? viewer)
    {
        var roster = new List<ProjectMemberDto> { MemberRow(Member), MemberRow(Colleague) };

        var body = RunPrivacyFilter(roster, viewer);

        body.Should().Contain("Name of " + Member).And.Contain("Name of " + Colleague);
        body.Contains($"{Member}@example.test").Should().Be(viewer == Member);
        body.Contains($"{Colleague}@example.test").Should().Be(viewer == Colleague);
    }

    [Fact]
    public void SingleMemberResponse_IsRedactedForAnonymousViewers()
        => RunPrivacyFilter(MemberRow(Member), viewer: null).Should().NotContain("@");

    public static TheoryData<object, string> RefusedProfileValues => new()
    {
        { new CreateGuildDto { Name = "Guild", Description = "d", Email = "x@y.z", Website = "javascript:alert(document.domain)" }, "website" },
        { new CreateGuildDto { Name = "Guild", Description = "d", Email = "not-an-email" }, "email" },
        { new UpdateGuildDto { Logo = "data:text/html,<script>alert(1)</script>" }, "logo" },
        { new UpdateGuildDto { Email = "two@@example.test" }, "email" },
        { new UpdateUserDto { LinkedIn = "javascript:alert(1)" }, "linkedIn" },
        { new UpdateUserDto { Twitter = "java\tscript:alert(1)" }, "twitter" },
        { new UpdateUserDto { Website = "ftp://files.example.test" }, "website" },
        { new CreateGroupConversationDto { Name = "Team", AvatarUrl = "//evil.example.test/a.png", MemberUserIds = [] }, "avatarUrl" },
        { new CreateNotificationDto { UserId = Member, Type = default, Title = "t", Message = "m", ActionUrl = "vbscript:x" }, "actionUrl" },
    };

    [Theory]
    [MemberData(nameof(RefusedProfileValues))]
    public void DangerousOrMalformedProfileValues_AreRefusedOnTheirField(object request, string field)
        => Validate(request).Keys.Should().Equal(field);

    public static TheoryData<object> AcceptedProfileValues => new()
    {
        new CreateGuildDto { Name = "Guild", Description = "d", Email = "hello@guild.example.test", Website = "https://guild.example.test", Logo = "/uploads/logo.png" },
        new UpdateGuildDto { Website = "" },
        new UpdateUserDto { LinkedIn = "in/jade", Twitter = "@jade", Website = "https://jade.example.test", Image = "https://lh3.googleusercontent.com/a/x" },
        new UpdateUserDto { LinkedIn = "https://www.linkedin.com/in/jade" },
        new CreateNotificationDto { UserId = Member, Type = default, Title = "t", Message = "m", ActionUrl = "/projects/demo" },
    };

    [Theory]
    [MemberData(nameof(AcceptedProfileValues))]
    public void WhatTheFormsSend_IsAccepted(object request)
        => Validate(request).Should().BeEmpty();

    [Fact]
    public void ReadinessVerdict_IsReusedForFiveSecondsThenRecomputed()
    {
        var clock = new ManualClock();
        var cache = new ReadinessSummaryCache(clock);

        cache.TryGet(out _).Should().BeFalse("nothing has been computed yet");
        cache.Store(ready: false);
        cache.TryGet(out var cached).Should().BeTrue();
        cached.Should().BeFalse("a not-ready verdict is served as not-ready");

        clock.Advance(ReadinessSummaryCache.Lifetime);
        cache.TryGet(out _).Should().BeFalse("a stale verdict forces a fresh database probe");
    }

    private static ProjectMemberDto MemberRow(string userId) => new()
    {
        Id = $"pm-{userId}",
        ProjectId = "project-1",
        UserId = userId,
        User = new ProjectMemberUserDto { Id = userId, Name = "Name of " + userId, Email = $"{userId}@example.test" },
    };

    private static string RunPrivacyFilter(object value, string? viewer)
    {
        var http = new DefaultHttpContext();
        if (viewer is not null)
            http.User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, viewer),
                    new Claim("sub", viewer),
                    new Claim(ActorAssertionMiddleware.ClaimType, "v2"),
                ],
                ActorAssertionMiddleware.AuthenticationType));

        var result = new OkObjectResult(value);
        var context = new ResultExecutingContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()),
            [],
            result,
            controller: new object());
        new UserDirectoryPrivacyFilter().OnResultExecuting(context);
        return JsonSerializer.Serialize(result.Value, result.Value!.GetType());
    }

    private static IReadOnlyDictionary<string, string[]> Validate(object request)
    {
        var validator = typeof(IRequestValidator).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsClass: true } && typeof(IRequestValidator).IsAssignableFrom(type))
            .Select(type => (IRequestValidator)Activator.CreateInstance(type)!)
            .Single(v => v.RequestType == request.GetType());
        return validator.Validate(request);
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
