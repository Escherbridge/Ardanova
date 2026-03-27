namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Moq;

public class GovernanceServiceTests
{
    private readonly Mock<IRepository<Proposal>> _proposalRepositoryMock;
    private readonly Mock<IRepository<Vote>> _voteRepositoryMock;
    private readonly Mock<IRepository<ProposalExecution>> _executionRepositoryMock;
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly Mock<IRepository<Project>> _projectRepositoryMock;
    private readonly Mock<IRepository<ProposalComment>> _proposalCommentRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GovernanceService _sut;

    public GovernanceServiceTests()
    {
        _proposalRepositoryMock = new Mock<IRepository<Proposal>>();
        _voteRepositoryMock = new Mock<IRepository<Vote>>();
        _executionRepositoryMock = new Mock<IRepository<ProposalExecution>>();
        _userRepositoryMock = new Mock<IRepository<User>>();
        _projectRepositoryMock = new Mock<IRepository<Project>>();
        _proposalCommentRepositoryMock = new Mock<IRepository<ProposalComment>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new GovernanceService(
            _proposalRepositoryMock.Object,
            _voteRepositoryMock.Object,
            _executionRepositoryMock.Object,
            _userRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _proposalCommentRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task GetProposalByIdAsync_WhenProposalExists_ReturnsSuccessResult()
    {
        // Arrange
        var proposalId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var creatorId = Guid.NewGuid().ToString();
        var proposal = new Proposal
        {
            id = proposalId,
            projectId = projectId,
            creatorId = creatorId,
            type = ProposalType.GOVERNANCE,
            title = "Test Proposal",
            description = "A test proposal",
            options = "[\"Yes\",\"No\"]",
            quorum = 50,
            threshold = 51,
            status = ProposalStatus.DRAFT,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var project = new Project
        {
            id = projectId,
            title = "Test Project",
            slug = "test-project",
            description = "Test",
            problemStatement = "Test",
            solution = "Test",
            categories = "TECHNOLOGY",
            status = ProjectStatus.DRAFT,
            createdById = creatorId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var proposalDto = new ProposalDto { Id = proposalId, Title = "Test Proposal" };

        _proposalRepositoryMock.Setup(r => r.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);
        _voteRepositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Vote, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Vote>());
        _userRepositoryMock.Setup(r => r.GetByIdAsync(creatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _mapperMock.Setup(m => m.Map<ProposalDto>(proposal)).Returns(proposalDto);
        _mapperMock.Setup(m => m.Map<ProposalProjectDto>(project))
            .Returns(new ProposalProjectDto { Id = projectId, Title = "Test Project", Slug = "test-project" });

        // Act
        var result = await _sut.GetProposalByIdAsync(proposalId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Test Proposal");
    }

    [Fact]
    public async Task GetProposalByIdAsync_WhenProposalNotExists_ReturnsNotFound()
    {
        // Arrange
        var proposalId = Guid.NewGuid().ToString();
        _proposalRepositoryMock.Setup(r => r.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Proposal?)null);

        // Act
        var result = await _sut.GetProposalByIdAsync(proposalId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task CreateProposalAsync_WithValidDto_ReturnsCreatedProposal()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var creatorId = Guid.NewGuid().ToString();
        var project = new Project
        {
            id = projectId,
            title = "Test Project",
            slug = "test-project",
            description = "Test",
            problemStatement = "Test",
            solution = "Test",
            categories = "TECHNOLOGY",
            status = ProjectStatus.DRAFT,
            createdById = creatorId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new CreateProposalDto
        {
            ProjectId = projectId,
            CreatorId = creatorId,
            Type = ProposalType.GOVERNANCE,
            Title = "New Proposal",
            Description = "A new proposal",
            Options = "[\"Yes\",\"No\"]"
        };
        var proposalDto = new ProposalDto { Title = "New Proposal" };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _proposalRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Proposal p, CancellationToken _) => p);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _voteRepositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Vote, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Vote>());
        _userRepositoryMock.Setup(r => r.GetByIdAsync(creatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mapperMock.Setup(m => m.Map<ProposalDto>(It.IsAny<Proposal>())).Returns(proposalDto);
        _mapperMock.Setup(m => m.Map<ProposalProjectDto>(project))
            .Returns(new ProposalProjectDto { Id = projectId, Title = "Test Project", Slug = "test-project" });

        // Act
        var result = await _sut.CreateProposalAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("New Proposal");
    }

    [Fact]
    public async Task CreateProposalAsync_WhenProjectNotExists_ReturnsNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var dto = new CreateProposalDto
        {
            ProjectId = projectId,
            CreatorId = Guid.NewGuid().ToString(),
            Type = ProposalType.GOVERNANCE,
            Title = "New Proposal",
            Description = "Desc",
            Options = "[\"Yes\",\"No\"]"
        };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        // Act
        var result = await _sut.CreateProposalAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task DeleteProposalAsync_WhenDraft_ReturnsSuccess()
    {
        // Arrange
        var proposalId = Guid.NewGuid().ToString();
        var proposal = new Proposal
        {
            id = proposalId,
            projectId = Guid.NewGuid().ToString(),
            creatorId = Guid.NewGuid().ToString(),
            type = ProposalType.GOVERNANCE,
            title = "Test Proposal",
            description = "Desc",
            options = "[\"Yes\",\"No\"]",
            quorum = 50,
            threshold = 51,
            status = ProposalStatus.DRAFT,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _proposalRepositoryMock.Setup(r => r.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);
        _proposalRepositoryMock.Setup(r => r.DeleteAsync(proposal, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteProposalAsync(proposalId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProposalAsync_WhenNotDraft_ReturnsValidationError()
    {
        // Arrange
        var proposalId = Guid.NewGuid().ToString();
        var proposal = new Proposal
        {
            id = proposalId,
            projectId = Guid.NewGuid().ToString(),
            creatorId = Guid.NewGuid().ToString(),
            type = ProposalType.GOVERNANCE,
            title = "Test Proposal",
            description = "Desc",
            options = "[\"Yes\",\"No\"]",
            quorum = 50,
            threshold = 51,
            status = ProposalStatus.ACTIVE,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _proposalRepositoryMock.Setup(r => r.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);

        // Act
        var result = await _sut.DeleteProposalAsync(proposalId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task CastVoteAsync_WithValidRequest_ReturnsVote()
    {
        // Arrange
        var proposalId = Guid.NewGuid().ToString();
        var voterId = Guid.NewGuid().ToString();
        var proposal = new Proposal
        {
            id = proposalId,
            projectId = Guid.NewGuid().ToString(),
            creatorId = Guid.NewGuid().ToString(),
            type = ProposalType.GOVERNANCE,
            title = "Test Proposal",
            description = "Desc",
            options = "[\"Yes\",\"No\"]",
            quorum = 50,
            threshold = 51,
            status = ProposalStatus.ACTIVE,
            votingEnd = DateTime.UtcNow.AddDays(7),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new CastVoteDto
        {
            VoterId = voterId,
            Choice = 0,
            Weight = 1
        };
        var voter = new User { id = voterId, name = "Test Voter" };
        var voteDto = new VoteDto { ProposalId = proposalId, VoterId = voterId, Choice = 0 };
        var voterDto = new VoteUserDto { Id = voterId, Name = "Test Voter" };

        _proposalRepositoryMock.Setup(r => r.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);
        _voteRepositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Vote, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vote?)null);
        _voteRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Vote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vote v, CancellationToken _) => v);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(voterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(voter);
        _mapperMock.Setup(m => m.Map<VoteDto>(It.IsAny<Vote>())).Returns(voteDto);
        _mapperMock.Setup(m => m.Map<VoteUserDto>(voter)).Returns(voterDto);

        // Act
        var result = await _sut.CastVoteAsync(proposalId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CastVoteAsync_WhenProposalNotActive_ReturnsValidationError()
    {
        // Arrange
        var proposalId = Guid.NewGuid().ToString();
        var proposal = new Proposal
        {
            id = proposalId,
            projectId = Guid.NewGuid().ToString(),
            creatorId = Guid.NewGuid().ToString(),
            type = ProposalType.GOVERNANCE,
            title = "Test Proposal",
            description = "Desc",
            options = "[\"Yes\",\"No\"]",
            quorum = 50,
            threshold = 51,
            status = ProposalStatus.DRAFT,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new CastVoteDto
        {
            VoterId = Guid.NewGuid().ToString(),
            Choice = 0
        };

        _proposalRepositoryMock.Setup(r => r.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);

        // Act
        var result = await _sut.CastVoteAsync(proposalId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task CastVoteAsync_WhenVotingEnded_ReturnsValidationError()
    {
        // Arrange
        var proposalId = Guid.NewGuid().ToString();
        var proposal = new Proposal
        {
            id = proposalId,
            projectId = Guid.NewGuid().ToString(),
            creatorId = Guid.NewGuid().ToString(),
            type = ProposalType.GOVERNANCE,
            title = "Test Proposal",
            description = "Desc",
            options = "[\"Yes\",\"No\"]",
            quorum = 50,
            threshold = 51,
            status = ProposalStatus.ACTIVE,
            votingEnd = DateTime.UtcNow.AddDays(-1),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new CastVoteDto
        {
            VoterId = Guid.NewGuid().ToString(),
            Choice = 0
        };

        _proposalRepositoryMock.Setup(r => r.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);

        // Act
        var result = await _sut.CastVoteAsync(proposalId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task CastVoteAsync_WhenAlreadyVoted_ReturnsValidationError()
    {
        // Arrange
        var proposalId = Guid.NewGuid().ToString();
        var voterId = Guid.NewGuid().ToString();
        var proposal = new Proposal
        {
            id = proposalId,
            projectId = Guid.NewGuid().ToString(),
            creatorId = Guid.NewGuid().ToString(),
            type = ProposalType.GOVERNANCE,
            title = "Test Proposal",
            description = "Desc",
            options = "[\"Yes\",\"No\"]",
            quorum = 50,
            threshold = 51,
            status = ProposalStatus.ACTIVE,
            votingEnd = DateTime.UtcNow.AddDays(7),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var existingVote = new Vote { id = Guid.NewGuid().ToString(), proposalId = proposalId, voterId = voterId };
        var dto = new CastVoteDto
        {
            VoterId = voterId,
            Choice = 0
        };

        _proposalRepositoryMock.Setup(r => r.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);
        _voteRepositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Vote, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVote);

        // Act
        var result = await _sut.CastVoteAsync(proposalId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task ExecuteProposalAsync_WhenPassed_ExecutesProposal()
    {
        // Arrange
        var proposalId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var creatorId = Guid.NewGuid().ToString();
        var proposal = new Proposal
        {
            id = proposalId,
            projectId = projectId,
            creatorId = creatorId,
            type = ProposalType.GOVERNANCE,
            title = "Test Proposal",
            description = "Desc",
            options = "[\"Yes\",\"No\"]",
            quorum = 50,
            threshold = 51,
            status = ProposalStatus.PASSED,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var project = new Project
        {
            id = projectId,
            title = "Test Project",
            slug = "test-project",
            description = "Test",
            problemStatement = "Test",
            solution = "Test",
            categories = "TECHNOLOGY",
            status = ProjectStatus.DRAFT,
            createdById = creatorId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var proposalDto = new ProposalDto { Id = proposalId, Title = "Test Proposal", Status = ProposalStatus.EXECUTED };

        _proposalRepositoryMock.Setup(r => r.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);
        _proposalRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _executionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ProposalExecution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProposalExecution e, CancellationToken _) => e);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _voteRepositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Vote, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Vote>());
        _userRepositoryMock.Setup(r => r.GetByIdAsync(creatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _mapperMock.Setup(m => m.Map<ProposalDto>(It.IsAny<Proposal>())).Returns(proposalDto);
        _mapperMock.Setup(m => m.Map<ProposalProjectDto>(project))
            .Returns(new ProposalProjectDto { Id = projectId, Title = "Test Project", Slug = "test-project" });

        // Act
        var result = await _sut.ExecuteProposalAsync(proposalId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        proposal.status.Should().Be(ProposalStatus.EXECUTED);
    }

    [Fact]
    public async Task ExecuteProposalAsync_WhenNotPassed_ReturnsValidationError()
    {
        // Arrange
        var proposalId = Guid.NewGuid().ToString();
        var proposal = new Proposal
        {
            id = proposalId,
            projectId = Guid.NewGuid().ToString(),
            creatorId = Guid.NewGuid().ToString(),
            type = ProposalType.GOVERNANCE,
            title = "Test Proposal",
            description = "Desc",
            options = "[\"Yes\",\"No\"]",
            quorum = 50,
            threshold = 51,
            status = ProposalStatus.ACTIVE,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _proposalRepositoryMock.Setup(r => r.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);

        // Act
        var result = await _sut.ExecuteProposalAsync(proposalId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task CancelProposalAsync_WhenNotExecuted_CancelsProposal()
    {
        // Arrange
        var proposalId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var creatorId = Guid.NewGuid().ToString();
        var proposal = new Proposal
        {
            id = proposalId,
            projectId = projectId,
            creatorId = creatorId,
            type = ProposalType.GOVERNANCE,
            title = "Test Proposal",
            description = "Desc",
            options = "[\"Yes\",\"No\"]",
            quorum = 50,
            threshold = 51,
            status = ProposalStatus.ACTIVE,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var project = new Project
        {
            id = projectId,
            title = "Test Project",
            slug = "test-project",
            description = "Test",
            problemStatement = "Test",
            solution = "Test",
            categories = "TECHNOLOGY",
            status = ProjectStatus.DRAFT,
            createdById = creatorId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var proposalDto = new ProposalDto { Id = proposalId, Title = "Test Proposal", Status = ProposalStatus.CANCELLED };

        _proposalRepositoryMock.Setup(r => r.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);
        _proposalRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Proposal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _voteRepositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Vote, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Vote>());
        _userRepositoryMock.Setup(r => r.GetByIdAsync(creatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _mapperMock.Setup(m => m.Map<ProposalDto>(It.IsAny<Proposal>())).Returns(proposalDto);
        _mapperMock.Setup(m => m.Map<ProposalProjectDto>(project))
            .Returns(new ProposalProjectDto { Id = projectId, Title = "Test Project", Slug = "test-project" });

        // Act
        var result = await _sut.CancelProposalAsync(proposalId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        proposal.status.Should().Be(ProposalStatus.CANCELLED);
    }

    [Fact]
    public async Task CancelProposalAsync_WhenExecuted_ReturnsValidationError()
    {
        // Arrange
        var proposalId = Guid.NewGuid().ToString();
        var proposal = new Proposal
        {
            id = proposalId,
            projectId = Guid.NewGuid().ToString(),
            creatorId = Guid.NewGuid().ToString(),
            type = ProposalType.GOVERNANCE,
            title = "Test Proposal",
            description = "Desc",
            options = "[\"Yes\",\"No\"]",
            quorum = 50,
            threshold = 51,
            status = ProposalStatus.EXECUTED,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _proposalRepositoryMock.Setup(r => r.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);

        // Act
        var result = await _sut.CancelProposalAsync(proposalId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task GetVoteSummaryAsync_ReturnsCorrectSummary()
    {
        // Arrange
        var proposalId = Guid.NewGuid().ToString();
        var proposal = new Proposal
        {
            id = proposalId,
            projectId = Guid.NewGuid().ToString(),
            creatorId = Guid.NewGuid().ToString(),
            type = ProposalType.GOVERNANCE,
            title = "Test Proposal",
            description = "Desc",
            options = "[\"Yes\",\"No\"]",
            quorum = 50,
            threshold = 51,
            status = ProposalStatus.ACTIVE,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var votes = new List<Vote>
        {
            new Vote { id = Guid.NewGuid().ToString(), proposalId = proposalId, voterId = Guid.NewGuid().ToString(), choice = 0, weight = 30, createdAt = DateTime.UtcNow },
            new Vote { id = Guid.NewGuid().ToString(), proposalId = proposalId, voterId = Guid.NewGuid().ToString(), choice = 0, weight = 25, createdAt = DateTime.UtcNow },
            new Vote { id = Guid.NewGuid().ToString(), proposalId = proposalId, voterId = Guid.NewGuid().ToString(), choice = 1, weight = 20, createdAt = DateTime.UtcNow }
        };

        _proposalRepositoryMock.Setup(r => r.GetByIdAsync(proposalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proposal);
        _voteRepositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Vote, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(votes);

        // Act
        var result = await _sut.GetVoteSummaryAsync(proposalId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalVotes.Should().Be(3);
        result.Value.TotalVotingPower.Should().Be(75);
        result.Value.QuorumReached.Should().BeTrue();
        result.Value.OptionSummaries.Should().ContainKey(0);
        result.Value.OptionSummaries.Should().ContainKey(1);
        result.Value.OptionSummaries[0].VoteCount.Should().Be(2);
        result.Value.OptionSummaries[0].VotingPower.Should().Be(55);
    }
}
