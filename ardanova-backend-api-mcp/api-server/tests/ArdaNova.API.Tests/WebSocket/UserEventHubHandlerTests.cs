using ArdaNova.API.EventBus.Events;
using ArdaNova.API.WebSocket.Clients;
using ArdaNova.API.WebSocket.Handlers;
using ArdaNova.API.WebSocket.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArdaNova.API.Tests.WebSocket;

public class UserEventHubHandlerTests
{
    [Fact]
    public async Task HandleAsync_UserCreatedEvent_ProjectsPiiOnlyToPrivateUserGroup()
    {
        var hubContext = new Mock<IHubContext<ArdaNovaHub, IArdaNovaHubClient>>();
        var clients = new Mock<IHubClients<IArdaNovaHubClient>>();
        var privateClient = new Mock<IArdaNovaHubClient>();
        var requestedGroups = new List<string>();

        hubContext.Setup(context => context.Clients).Returns(clients.Object);
        clients.Setup(group => group.Group(It.IsAny<string>()))
            .Returns((string groupName) =>
            {
                requestedGroups.Add(groupName);
                return privateClient.Object;
            });

        var handler = new UserEventHubHandler(
            hubContext.Object,
            NullLogger<UserEventHubHandler>.Instance);

        await handler.HandleAsync(new UserCreatedEvent(
            "user-1",
            "private@example.test",
            "Private Person"));

        requestedGroups.Should().OnlyContain(groupName => groupName == "user:user-1");
        requestedGroups.Should().NotContain("all");
        privateClient.Verify(client => client.UserCreated(It.IsAny<object>()), Times.Once);
        privateClient.Verify(
            client => client.ReceiveEvent("user.created", It.IsAny<object>()),
            Times.Once);
    }
}
