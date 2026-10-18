using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;
using FluentAssertions;
using Moq;

public sealed class ProjectMemberServiceTests
{
    [Theory]
    [MemberData(nameof(DirectAllocationChanges))]
    public async Task UpdateAsync_DirectShareOrVotingAllocationFailsBeforeRepositoryAccess(
        UpdateProjectMemberDto dto)
    {
        var members = new Mock<IRepository<ProjectMember>>(MockBehavior.Strict);
        var users = new Mock<IRepository<User>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        var service = new ProjectMemberService(
            members.Object,
            users.Object,
            new Mock<IRepository<ProjectTask>>(MockBehavior.Strict).Object,
            unitOfWork.Object,
            mapper.Object);

        var result = await service.UpdateAsync("member-1", dto, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("allocation is unavailable");
        members.VerifyNoOtherCalls();
        users.VerifyNoOtherCalls();
        unitOfWork.VerifyNoOtherCalls();
        mapper.VerifyNoOtherCalls();
    }

    public static TheoryData<UpdateProjectMemberDto> DirectAllocationChanges =>
    [
        new UpdateProjectMemberDto { ShareBalance = 1m },
        new UpdateProjectMemberDto { VotingPower = 1m },
    ];
}
