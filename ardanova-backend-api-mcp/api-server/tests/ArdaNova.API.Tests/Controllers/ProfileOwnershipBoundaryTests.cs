namespace ArdaNova.API.Tests.Controllers;

using System.Reflection;
using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class ProfileOwnershipBoundaryTests
{
    [Theory]
    [InlineData(typeof(UserSkillsController), "Create")]
    [InlineData(typeof(UserSkillsController), "Update")]
    [InlineData(typeof(UserSkillsController), "Delete")]
    [InlineData(typeof(UserExperienceController), "Create")]
    [InlineData(typeof(UserExperienceController), "Update")]
    [InlineData(typeof(UserExperienceController), "Delete")]
    public void ProfileWrites_RequireActorAssertion(Type controllerType, string action)
    {
        controllerType.GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(AuthorizationPolicies.ActorAssertion);
    }

    [Fact]
    public async Task ExperienceUpdate_ThroughAnotherUsersRoute_IsForbiddenBeforeAnyLookup()
    {
        var experience = new Mock<IUserExperienceService>(MockBehavior.Strict);
        var controller = WithActor(new UserExperienceController(experience.Object), "actor-1");

        var result = await controller.Update(
            "some-other-user", "experience-1", new UpdateUserExperienceDto { Title = "Renamed" }, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task SkillDelete_ThroughAnotherUsersRoute_IsForbiddenBeforeAnyLookup()
    {
        var skills = new Mock<IUserSkillService>(MockBehavior.Strict);
        var controller = WithActor(new UserSkillsController(skills.Object), "actor-1");

        var result = await controller.Delete("some-other-user", "skill-1", CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task SkillUpdate_OnOwnProfile_ReturnsTheUpdatedSkill()
    {
        var skills = new Mock<IUserSkillService>(MockBehavior.Strict);
        skills.Setup(service => service.UpdateAsync("actor-1", "skill-1", It.IsAny<UpdateUserSkillDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserSkillDto>.Success(new UserSkillDto { Id = "skill-1", UserId = "actor-1", Skill = "C#", Level = 7 }));
        var controller = WithActor(new UserSkillsController(skills.Object), "actor-1");

        var result = await controller.Update("actor-1", "skill-1", new UpdateUserSkillDto { Level = 7 }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<UserSkillDto>()
            .Which.Level.Should().Be(7);
    }

    [Fact]
    public void SelfVerification_IsRetired()
    {
        var controller = WithActor(
            new UsersController(Mock.Of<IUserService>(MockBehavior.Strict), Mock.Of<IUserFollowService>(MockBehavior.Strict)),
            "actor-1");

        var result = controller.Verify("actor-1");

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    private static T WithActor<T>(T controller, string actorId)
        where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, actorId)],
                    "ActorAssertion"))
            }
        };
        return controller;
    }
}
