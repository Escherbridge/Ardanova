namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Azoa.Quests;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

public class AzoaQuestAuthoringServiceTests
{
    private readonly Mock<IAzoaQuestNode> _nodeMock;
    private readonly AzoaQuestAuthoringService _sut;

    public AzoaQuestAuthoringServiceTests()
    {
        _nodeMock = new Mock<IAzoaQuestNode>();
        var loggerMock = new Mock<ILogger<AzoaQuestAuthoringService>>();
        _sut = new AzoaQuestAuthoringService(_nodeMock.Object, loggerMock.Object);
    }

    private void SetupHappyPath()
    {
        // Public definitions go to CreateTemplate; restricted ones to CreateQuest.
        _nodeMock
            .Setup(n => n.CreateTemplateAsync(It.IsAny<AzoaQuestDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AzoaQuestDefinition d, CancellationToken _) =>
                Result<AzoaQuestRef>.Success(new AzoaQuestRef($"tmpl-{d.Name}")));

        _nodeMock
            .Setup(n => n.CreateQuestAsync(It.IsAny<AzoaQuestDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AzoaQuestDefinition d, CancellationToken _) =>
                Result<AzoaQuestRef>.Success(new AzoaQuestRef($"quest-{d.Name}")));

        _nodeMock
            .Setup(n => n.ValidateQuestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaQuestValidation>.Success(new AzoaQuestValidation(true)));
    }

    [Fact]
    public async Task PublishLifecycleDefinitionsAsync_PublishesAndValidatesAllThreeDefinitions()
    {
        // Arrange
        SetupHappyPath();

        // Act
        var result = await _sut.PublishLifecycleDefinitionsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Published.Should().HaveCount(3);
        result.Value!.Published.Select(p => p.Name).Should().Contain(new[]
        {
            "Project Lifecycle", "Task Bounty", "Membership Credential",
        });
        result.Value!.Published.Should().OnlyContain(p => p.Validated);
        result.Value!.Published.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.QuestId));

        // Two public definitions → templates; one restricted → plain quest.
        _nodeMock.Verify(
            n => n.CreateTemplateAsync(It.IsAny<AzoaQuestDefinition>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _nodeMock.Verify(
            n => n.CreateQuestAsync(It.IsAny<AzoaQuestDefinition>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // Each published definition is validated once.
        _nodeMock.Verify(
            n => n.ValidateQuestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task PublishLifecycleDefinitionsAsync_PublicDefinitionsGoToTemplateEndpoint()
    {
        // Arrange
        SetupHappyPath();

        // Act
        var result = await _sut.PublishLifecycleDefinitionsAsync();

        // Assert — the two public definitions are flagged as templates, the
        // restricted Membership Credential is not.
        result.IsSuccess.Should().BeTrue();
        result.Value!.Published.Single(p => p.Name == "Project Lifecycle")
            .PublishedAsTemplate.Should().BeTrue();
        result.Value!.Published.Single(p => p.Name == "Task Bounty")
            .PublishedAsTemplate.Should().BeTrue();
        result.Value!.Published.Single(p => p.Name == "Membership Credential")
            .PublishedAsTemplate.Should().BeFalse();
    }

    [Fact]
    public async Task PublishLifecycleDefinitionsAsync_WhenValidationReportsInvalid_StillSucceedsButFlagsNotValidated()
    {
        // Arrange — publish succeeds, but the node reports the DAG invalid.
        _nodeMock
            .Setup(n => n.CreateTemplateAsync(It.IsAny<AzoaQuestDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaQuestRef>.Success(new AzoaQuestRef("tmpl-1")));
        _nodeMock
            .Setup(n => n.CreateQuestAsync(It.IsAny<AzoaQuestDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaQuestRef>.Success(new AzoaQuestRef("quest-1")));
        _nodeMock
            .Setup(n => n.ValidateQuestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaQuestValidation>.Success(new AzoaQuestValidation(false, "bad edge")));

        // Act
        var result = await _sut.PublishLifecycleDefinitionsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Published.Should().OnlyContain(p => !p.Validated);
    }

    [Fact]
    public async Task PublishLifecycleDefinitionsAsync_WhenCreateFails_PropagatesFailure()
    {
        // Arrange — the very first publish call fails.
        _nodeMock
            .Setup(n => n.CreateTemplateAsync(It.IsAny<AzoaQuestDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaQuestRef>.Failure("AZOA node unavailable"));

        // Act
        var result = await _sut.PublishLifecycleDefinitionsAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);

        // First definition (Project Lifecycle) is public → template fails → we
        // never reach validation.
        _nodeMock.Verify(
            n => n.ValidateQuestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PublishLifecycleDefinitionsAsync_WhenCreateForbidden_PropagatesForbidden()
    {
        // Arrange — node returns a Forbidden; the result type must survive.
        _nodeMock
            .Setup(n => n.CreateTemplateAsync(It.IsAny<AzoaQuestDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaQuestRef>.Forbidden("KYC_FORBIDDEN: not allowed"));

        // Act
        var result = await _sut.PublishLifecycleDefinitionsAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Forbidden);
    }

    [Fact]
    public async Task PublishLifecycleDefinitionsAsync_WhenValidationFails_PropagatesFailure()
    {
        // Arrange — publish succeeds but the validate transport call fails.
        SetupHappyPath();
        _nodeMock
            .Setup(n => n.ValidateQuestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaQuestValidation>.Failure("validate endpoint down"));

        // Act
        var result = await _sut.PublishLifecycleDefinitionsAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
    }

    [Fact]
    public async Task PublishLifecycleDefinitionsAsync_WhenNodeReturnsEmptyId_Fails()
    {
        // Arrange — publish "succeeds" but returns no id.
        _nodeMock
            .Setup(n => n.CreateTemplateAsync(It.IsAny<AzoaQuestDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaQuestRef>.Success(new AzoaQuestRef(string.Empty)));

        // Act
        var result = await _sut.PublishLifecycleDefinitionsAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
        _nodeMock.Verify(
            n => n.ValidateQuestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
