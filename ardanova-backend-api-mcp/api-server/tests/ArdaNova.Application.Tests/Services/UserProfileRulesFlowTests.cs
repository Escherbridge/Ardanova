namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Mappings;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Tests.Support;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;
using FluentAssertions;
using Moq;

/// <summary>Profile edits driven through the real services; asserts on stored rows, so every caller of the API gets these rules.</summary>
public class UserProfileRulesFlowTests
{
    private const string Owner = "owner-user-0001";

    private readonly InMemoryRepository<User> _users = new();
    private readonly InMemoryRepository<UserSkill> _skills = new();
    private readonly InMemoryRepository<UserExperience> _experience = new();
    private readonly UserService _userService;
    private readonly UserSkillService _skillService;
    private readonly UserExperienceService _experienceService;

    public UserProfileRulesFlowTests()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
            MappingSecurityPolicy.Apply(cfg);
        }).CreateMapper();
        var unitOfWork = new Mock<IUnitOfWork>().Object;

        _users.Rows.Add(new User { id = Owner, email = "owner@example.test", name = "Owner", website = "https://owner.example" });
        _userService = new UserService(_users, unitOfWork, mapper);
        _skillService = new UserSkillService(_skills, unitOfWork, mapper);
        _experienceService = new UserExperienceService(_experience, unitOfWork, mapper);
    }

    [Theory]
    [InlineData("javascript:alert(document.cookie)", null, null)]
    [InlineData("  JavaScript:alert(1)", null, null)]
    [InlineData("data:text/html,<script>alert(1)</script>", null, null)]
    [InlineData("owner.example", null, null)]
    [InlineData(null, "javascript:alert(1)", null)]
    [InlineData(null, "//evil.example/x", null)]
    [InlineData(null, null, "vbscript:msgbox(1)")]
    public async Task ProfileUpdate_WithANonHttpLink_IsRejectedAndNothingIsStored(string? website, string? linkedIn, string? twitter)
    {
        var result = await _userService.UpdateAsync(Owner, new UpdateUserDto
        {
            Bio = "should not be saved",
            Website = website,
            LinkedIn = linkedIn,
            Twitter = twitter,
        });

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("http");
        var stored = _users.Rows.Single();
        stored.website.Should().Be("https://owner.example");
        stored.bio.Should().BeNull();
    }

    [Fact]
    public async Task ProfileUpdate_WithHttpsLinksAndAHandle_IsStoredTrimmed_AndAnEmptyWebsiteClearsIt()
    {
        var saved = await _userService.UpdateAsync(Owner, new UpdateUserDto
        {
            Website = " https://new.example/me ",
            LinkedIn = "https://www.linkedin.com/in/owner",
            Twitter = "@owner",
        });

        saved.IsSuccess.Should().BeTrue();
        _users.Rows.Single().website.Should().Be("https://new.example/me");
        _users.Rows.Single().twitter.Should().Be("@owner");

        var cleared = await _userService.UpdateAsync(Owner, new UpdateUserDto { Website = "" });

        cleared.IsSuccess.Should().BeTrue();
        _users.Rows.Single().website.Should().BeEmpty();
    }

    [Fact]
    public async Task AddSkill_StoresTrimmedSkill_ThenRefusesTheSameSkillInAnotherCase()
    {
        var first = await _skillService.CreateAsync(new CreateUserSkillDto { UserId = Owner, Skill = "  Rust ", Level = 7 });
        var duplicate = await _skillService.CreateAsync(new CreateUserSkillDto { UserId = Owner, Skill = "rust", Level = 3 });

        first.IsSuccess.Should().BeTrue();
        duplicate.Type.Should().Be(ResultType.ValidationError);
        _skills.Rows.Should().ContainSingle().Which.skill.Should().Be("Rust");
    }

    [Theory]
    [InlineData("Rust", 0)]
    [InlineData("Rust", 11)]
    [InlineData("Rust", 9999)]
    [InlineData("   ", 5)]
    public async Task AddSkill_OutsideTheRules_IsRejectedAndNotStored(string name, int level)
    {
        var result = await _skillService.CreateAsync(new CreateUserSkillDto { UserId = Owner, Skill = name, Level = level });

        result.Type.Should().Be(ResultType.ValidationError);
        _skills.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateSkill_ToALevelOffTheScale_KeepsTheStoredLevel()
    {
        _skills.Rows.Add(new UserSkill { id = "skill-1", userId = Owner, skill = "C#", level = 5 });

        var result = await _skillService.UpdateAsync(Owner, "skill-1", new UpdateUserSkillDto { Level = 42 });

        result.Type.Should().Be(ResultType.ValidationError);
        _skills.Rows.Single().level.Should().Be(5);
    }

    [Theory]
    [InlineData("2024-05-01", "2024-01-01", false, "before the start date")]
    [InlineData("2024-01-01", "2024-05-01", true, "no end date")]
    public async Task AddExperience_WithInconsistentDates_IsRejectedWithTheReason(
        string start, string end, bool isCurrent, string reason)
    {
        var result = await _experienceService.CreateAsync(new CreateUserExperienceDto
        {
            UserId = Owner,
            Title = "Engineer",
            Company = "Acme",
            StartDate = DateTime.Parse(start),
            EndDate = DateTime.Parse(end),
            IsCurrent = isCurrent,
        });

        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain(reason);
        _experience.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkingExperienceCurrent_ClearsItsEndDate()
    {
        _experience.Rows.Add(new UserExperience
        {
            id = "exp-1", userId = Owner, title = "Engineer", company = "Acme",
            startDate = new DateTime(2020, 1, 1), endDate = new DateTime(2022, 1, 1), isCurrent = false,
        });

        var result = await _experienceService.UpdateAsync(Owner, "exp-1", new UpdateUserExperienceDto { IsCurrent = true });

        result.IsSuccess.Should().BeTrue();
        var stored = _experience.Rows.Single();
        stored.isCurrent.Should().BeTrue();
        stored.endDate.Should().BeNull();
    }
}
