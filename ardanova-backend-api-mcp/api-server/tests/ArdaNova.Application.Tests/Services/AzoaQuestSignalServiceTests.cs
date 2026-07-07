namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

public class AzoaQuestSignalServiceTests
{
    private readonly Mock<IAzoaQuestNode> _nodeMock;
    private readonly AzoaQuestSignalService _sut;

    public AzoaQuestSignalServiceTests()
    {
        _nodeMock = new Mock<IAzoaQuestNode>();
        var loggerMock = new Mock<ILogger<AzoaQuestSignalService>>();
        _sut = new AzoaQuestSignalService(_nodeMock.Object, loggerMock.Object);
    }

    // ── SignalFundingGoalMetAsync ───────────────────────────────────────────────

    [Fact]
    public async Task SignalFundingGoalMetAsync_CallsPortSignalWithRunIdAndFundingGoalSignal()
    {
        // Arrange
        const string runId = "run-123";
        AzoaRunSignal? captured = null;
        _nodeMock
            .Setup(n => n.SignalRunAsync(runId, It.IsAny<AzoaRunSignal>(), It.IsAny<CancellationToken>()))
            .Callback<string, AzoaRunSignal, CancellationToken>((_, s, _) => captured = s)
            .ReturnsAsync(Result<AzoaSignalAck>.Success(new AzoaSignalAck(runId, true)));

        // Act
        var result = await _sut.SignalFundingGoalMetAsync(runId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RunId.Should().Be(runId);
        result.Value!.Accepted.Should().BeTrue();

        captured.Should().NotBeNull();
        captured!.SignalName.Should().Be("fundingGoalMet");

        _nodeMock.Verify(
            n => n.SignalRunAsync(runId, It.IsAny<AzoaRunSignal>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SignalTaskAcceptedAsync_SendsSubmissionAcceptedSignal()
    {
        // Arrange
        const string runId = "run-accept";
        AzoaRunSignal? captured = null;
        _nodeMock
            .Setup(n => n.SignalRunAsync(runId, It.IsAny<AzoaRunSignal>(), It.IsAny<CancellationToken>()))
            .Callback<string, AzoaRunSignal, CancellationToken>((_, s, _) => captured = s)
            .ReturnsAsync(Result<AzoaSignalAck>.Success(new AzoaSignalAck(runId, true)));

        // Act
        var result = await _sut.SignalTaskAcceptedAsync(runId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        captured!.SignalName.Should().Be("submissionAccepted");
    }

    [Fact]
    public async Task SignalTaskRejectedAsync_SendsSubmissionAcceptedSignal()
    {
        // Arrange
        const string runId = "run-reject";
        AzoaRunSignal? captured = null;
        _nodeMock
            .Setup(n => n.SignalRunAsync(runId, It.IsAny<AzoaRunSignal>(), It.IsAny<CancellationToken>()))
            .Callback<string, AzoaRunSignal, CancellationToken>((_, s, _) => captured = s)
            .ReturnsAsync(Result<AzoaSignalAck>.Success(new AzoaSignalAck(runId, true)));

        // Act
        var result = await _sut.SignalTaskRejectedAsync(runId);

        // Assert — same gate binding, drives the refund branch via payload.
        result.IsSuccess.Should().BeTrue();
        captured!.SignalName.Should().Be("submissionAccepted");
    }

    [Fact]
    public async Task SignalFundingGoalMetAsync_WhenRunIdMissing_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.SignalFundingGoalMetAsync("   ");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.BadRequest);
        _nodeMock.Verify(
            n => n.SignalRunAsync(It.IsAny<string>(), It.IsAny<AzoaRunSignal>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SignalFundingGoalMetAsync_WhenPortFails_PropagatesFailure()
    {
        // Arrange
        const string runId = "run-x";
        _nodeMock
            .Setup(n => n.SignalRunAsync(runId, It.IsAny<AzoaRunSignal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaSignalAck>.Failure("node down"));

        // Act
        var result = await _sut.SignalFundingGoalMetAsync(runId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
    }

    // ── GetRunExecutionStateAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetRunExecutionStateAsync_MapsAwaitingReconciliation_ToNonErrorPendingSettlement()
    {
        // Arrange
        const string runId = "run-settle";
        _nodeMock
            .Setup(n => n.GetExecutionStateAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaRunExecutionState>.Success(
                new AzoaRunExecutionState(runId, "AwaitingReconciliation", "transfer-bounty-reward")));

        // Act
        var result = await _sut.GetRunExecutionStateAsync(runId);

        // Assert — non-error pending-settlement state.
        result.IsSuccess.Should().BeTrue();
        result.Value!.State.Should().Be(AzoaRunState.AwaitingReconciliation);
        result.Value!.IsPendingSettlement.Should().BeTrue();
        result.Value!.IsTerminal.Should().BeFalse();
        result.Value!.RawStatus.Should().Be("AwaitingReconciliation");
        result.Value!.CurrentNodeId.Should().Be("transfer-bounty-reward");
    }

    [Theory]
    [InlineData("awaiting_reconciliation")]
    [InlineData("pending-settlement")]
    [InlineData("RECONCILING")]
    public async Task GetRunExecutionStateAsync_MapsReconciliationVariants_ToPendingSettlement(string raw)
    {
        // Arrange
        const string runId = "run-settle-variants";
        _nodeMock
            .Setup(n => n.GetExecutionStateAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaRunExecutionState>.Success(
                new AzoaRunExecutionState(runId, raw)));

        // Act
        var result = await _sut.GetRunExecutionStateAsync(runId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.State.Should().Be(AzoaRunState.AwaitingReconciliation);
        result.Value!.IsPendingSettlement.Should().BeTrue();
    }

    [Fact]
    public async Task GetRunExecutionStateAsync_MapsCompleted_ToTerminal()
    {
        // Arrange
        const string runId = "run-done";
        _nodeMock
            .Setup(n => n.GetExecutionStateAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaRunExecutionState>.Success(
                new AzoaRunExecutionState(runId, "Completed")));

        // Act
        var result = await _sut.GetRunExecutionStateAsync(runId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.State.Should().Be(AzoaRunState.Completed);
        result.Value!.IsTerminal.Should().BeTrue();
        result.Value!.IsPendingSettlement.Should().BeFalse();
    }

    [Fact]
    public async Task GetRunExecutionStateAsync_MapsUnknownStatus_ToUnknownAndPreservesRaw()
    {
        // Arrange
        const string runId = "run-weird";
        _nodeMock
            .Setup(n => n.GetExecutionStateAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaRunExecutionState>.Success(
                new AzoaRunExecutionState(runId, "QuantumFoam")));

        // Act
        var result = await _sut.GetRunExecutionStateAsync(runId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.State.Should().Be(AzoaRunState.Unknown);
        result.Value!.RawStatus.Should().Be("QuantumFoam");
    }

    [Fact]
    public async Task GetRunExecutionStateAsync_WhenPortFails_PropagatesFailure()
    {
        // Arrange
        const string runId = "run-fail";
        _nodeMock
            .Setup(n => n.GetExecutionStateAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AzoaRunExecutionState>.Failure("execution-state read failed"));

        // Act
        var result = await _sut.GetRunExecutionStateAsync(runId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Failure);
    }

    [Fact]
    public async Task GetRunExecutionStateAsync_WhenRunIdMissing_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetRunExecutionStateAsync("");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.BadRequest);
        _nodeMock.Verify(
            n => n.GetExecutionStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
