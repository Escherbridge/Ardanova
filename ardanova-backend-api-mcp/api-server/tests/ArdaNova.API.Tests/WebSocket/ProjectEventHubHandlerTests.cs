using ArdaNova.API.EventBus.Events;
using ArdaNova.API.WebSocket.Clients;
using ArdaNova.API.WebSocket.Handlers;
using ArdaNova.API.WebSocket.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArdaNova.API.Tests.WebSocket;

public class ProjectEventHubHandlerTests
{
    private readonly Mock<IHubContext<ArdaNovaHub, IArdaNovaHubClient>> _mockHubContext;
    private readonly Mock<IHubClients<IArdaNovaHubClient>> _mockClients;
    private readonly Mock<IArdaNovaHubClient> _mockClientProxy;
    private readonly ProjectEventHubHandler _handler;
    private readonly Dictionary<string, Mock<IArdaNovaHubClient>> _groupClients;

    public ProjectEventHubHandlerTests()
    {
        _mockHubContext = new Mock<IHubContext<ArdaNovaHub, IArdaNovaHubClient>>();
        _mockClients = new Mock<IHubClients<IArdaNovaHubClient>>();
        _mockClientProxy = new Mock<IArdaNovaHubClient>();
        _groupClients = new Dictionary<string, Mock<IArdaNovaHubClient>>();

        _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);

        // Setup Group to return tracked client proxies
        _mockClients.Setup(c => c.Group(It.IsAny<string>()))
            .Returns((string groupName) =>
            {
                if (!_groupClients.ContainsKey(groupName))
                {
                    _groupClients[groupName] = new Mock<IArdaNovaHubClient>();
                }
                return _groupClients[groupName].Object;
            });

        _handler = new ProjectEventHubHandler(
            _mockHubContext.Object,
            NullLogger<ProjectEventHubHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ProjectCreatedEvent_NotifiesOwnerAndAllGroups()
    {
        // Arrange
        var ownerId = Guid.NewGuid().ToString();
        var projectId = Guid.NewGuid().ToString();
        var @event = new ProjectCreatedEvent(projectId, ownerId, "Test Project", "test-project");

        // Act
        await _handler.HandleAsync(@event);

        // Assert - Owner group should receive ProjectCreated and ReceiveEvent
        var ownerGroup = _groupClients[$"user:{ownerId}"];
        ownerGroup.Verify(c => c.ProjectCreated(It.Is<object>(o =>
            o.GetType().GetProperty("projectId")!.GetValue(o)!.Equals(projectId))), Times.Once);
        ownerGroup.Verify(c => c.ReceiveEvent("project.created", It.IsAny<object>()), Times.Once);

        // Assert - "all" group should receive ProjectCreated
        var allGroup = _groupClients["all"];
        allGroup.Verify(c => c.ProjectCreated(It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ProjectUpdatedEvent_NotifiesProjectGroup()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var @event = new ProjectUpdatedEvent(projectId, "Updated Title");

        // Act
        await _handler.HandleAsync(@event);

        // Assert
        var projectGroup = _groupClients[$"project:{projectId}"];
        projectGroup.Verify(c => c.ProjectUpdated(It.Is<object>(o =>
            o.GetType().GetProperty("projectId")!.GetValue(o)!.Equals(projectId) &&
            o.GetType().GetProperty("title")!.GetValue(o)!.Equals("Updated Title"))), Times.Once);
        projectGroup.Verify(c => c.ReceiveEvent("project.updated", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ProjectStatusChangedEvent_NotifiesProjectGroup()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var @event = new ProjectStatusChangedEvent(projectId, "DRAFT", "ACTIVE");

        // Act
        await _handler.HandleAsync(@event);

        // Assert
        var projectGroup = _groupClients[$"project:{projectId}"];
        projectGroup.Verify(c => c.ProjectStatusChanged(It.Is<object>(o =>
            o.GetType().GetProperty("oldStatus")!.GetValue(o)!.Equals("DRAFT") &&
            o.GetType().GetProperty("newStatus")!.GetValue(o)!.Equals("ACTIVE"))), Times.Once);
        projectGroup.Verify(c => c.ReceiveEvent("project.status_changed", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ProjectDeletedEvent_NotifiesProjectGroup()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var @event = new ProjectDeletedEvent(projectId);

        // Act
        await _handler.HandleAsync(@event);

        // Assert
        var projectGroup = _groupClients[$"project:{projectId}"];
        projectGroup.Verify(c => c.ReceiveEvent("project.deleted", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ProjectTaskCompletedEvent_NotifiesProjectAndAssignee()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var taskId = Guid.NewGuid().ToString();
        var assigneeId = Guid.NewGuid().ToString();
        var @event = new ProjectTaskCompletedEvent(projectId, taskId, assigneeId, "Complete Feature X");

        // Act
        await _handler.HandleAsync(@event);

        // Assert - Project group gets TaskCompleted
        var projectGroup = _groupClients[$"project:{projectId}"];
        projectGroup.Verify(c => c.TaskCompleted(It.Is<object>(o =>
            o.GetType().GetProperty("taskId")!.GetValue(o)!.Equals(taskId))), Times.Once);
        projectGroup.Verify(c => c.ReceiveEvent("project.task_completed", It.IsAny<object>()), Times.Once);

        // Assert - Assignee group gets TaskCompleted
        var assigneeGroup = _groupClients[$"user:{assigneeId}"];
        assigneeGroup.Verify(c => c.TaskCompleted(It.Is<object>(o =>
            o.GetType().GetProperty("taskId")!.GetValue(o)!.Equals(taskId))), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ProjectTaskCompletedEvent_WithNoAssignee_DoesNotNotifyUserGroup()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var taskId = Guid.NewGuid().ToString();
        var @event = new ProjectTaskCompletedEvent(projectId, taskId, null, "Unassigned Task");

        // Act
        await _handler.HandleAsync(@event);

        // Assert - Only project group should be notified (no user: groups)
        _groupClients.Keys.Where(k => k.StartsWith("user:")).Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ProjectMemberAddedEvent_NotifiesProjectAndNewMember()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var @event = new ProjectMemberAddedEvent(projectId, userId, "CONTRIBUTOR");

        // Act
        await _handler.HandleAsync(@event);

        // Assert - Project group gets notification
        var projectGroup = _groupClients[$"project:{projectId}"];
        projectGroup.Verify(c => c.ReceiveEvent("project.member_added", It.IsAny<object>()), Times.Once);

        // Assert - New member gets notification
        var userGroup = _groupClients[$"user:{userId}"];
        userGroup.Verify(c => c.ReceiveEvent("project.member_added", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ProjectMemberRemovedEvent_NotifiesProjectAndRemovedMember()
    {
        // Arrange
        var projectId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var @event = new ProjectMemberRemovedEvent(projectId, userId);

        // Act
        await _handler.HandleAsync(@event);

        // Assert - Project group gets notification
        var projectGroup = _groupClients[$"project:{projectId}"];
        projectGroup.Verify(c => c.ReceiveEvent("project.member_removed", It.IsAny<object>()), Times.Once);

        // Assert - Removed member gets notification
        var userGroup = _groupClients[$"user:{userId}"];
        userGroup.Verify(c => c.ReceiveEvent("project.member_removed", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public void ProjectCreatedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var @event = new ProjectCreatedEvent(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "Test", null);

        // Assert
        @event.EventType.Should().Be("project.created");
    }

    [Fact]
    public void ProjectUpdatedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var @event = new ProjectUpdatedEvent(Guid.NewGuid().ToString(), "Test");

        // Assert
        @event.EventType.Should().Be("project.updated");
    }

    [Fact]
    public void ProjectStatusChangedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var @event = new ProjectStatusChangedEvent(Guid.NewGuid().ToString(), "OLD", "NEW");

        // Assert
        @event.EventType.Should().Be("project.status_changed");
    }

    [Fact]
    public void ProjectDeletedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var @event = new ProjectDeletedEvent(Guid.NewGuid().ToString());

        // Assert
        @event.EventType.Should().Be("project.deleted");
    }

    [Fact]
    public void ProjectTaskCompletedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var @event = new ProjectTaskCompletedEvent(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), null, "Test");

        // Assert
        @event.EventType.Should().Be("project.task_completed");
    }

    [Fact]
    public void ProjectMemberAddedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var @event = new ProjectMemberAddedEvent(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "CONTRIBUTOR");

        // Assert
        @event.EventType.Should().Be("project.member_added");
    }

    [Fact]
    public void ProjectMemberRemovedEvent_HasCorrectEventType()
    {
        // Arrange & Act
        var @event = new ProjectMemberRemovedEvent(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        // Assert
        @event.EventType.Should().Be("project.member_removed");
    }
}
