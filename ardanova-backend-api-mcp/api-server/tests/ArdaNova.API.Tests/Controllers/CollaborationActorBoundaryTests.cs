namespace ArdaNova.API.Tests.Controllers;

using System.Reflection;
using System.Security.Claims;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public class CollaborationActorBoundaryTests
{
    [Theory]
    [InlineData(typeof(ChatController))]
    [InlineData(typeof(NotificationsController))]
    public void CollaborationControllers_RequireActorAssertion(Type controllerType)
    {
        controllerType.GetCustomAttribute<AuthorizeAttribute>()?.Policy
            .Should().Be(AuthorizationPolicies.ActorAssertion);
    }

    [Fact]
    public async Task ChatSendMessage_UsesTheSignedActorInsteadOfARequestIdentity()
    {
        var chat = new Mock<IChatService>();
        chat.Setup(service => service.SendMessageAsync(
                "actor-1",
                It.Is<SendMessageDto>(dto => dto.ConversationId == "conversation-1" && dto.Message == "Hello"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ChatMessageDto>.Success(new ChatMessageDto
            {
                Id = "message-1",
                ConversationId = "conversation-1",
                UserFromId = "actor-1",
                UserFromName = "Actor",
                Status = MessageStatus.SENT,
            }));
        var controller = WithActor(new ChatController(chat.Object), "actor-1");

        var result = await controller.SendMessage(
            new SendMessageDto { ConversationId = "conversation-1", Message = "Hello" },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        chat.VerifyAll();
    }

    [Fact]
    public async Task ChatTypingIndicator_ReplacesCallerSuppliedIdentityWithTheSignedActor()
    {
        var chat = new Mock<IChatService>();
        chat.Setup(service => service.SendTypingIndicatorAsync(
                "actor-1",
                It.Is<TypingIndicatorDto>(dto => dto.UserId == "actor-1" && dto.UserName == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        var controller = WithActor(new ChatController(chat.Object), "actor-1");

        var result = await controller.SendTypingIndicator(
            new TypingIndicatorDto
            {
                ConversationId = "conversation-1",
                UserId = "other-user",
                UserName = "Other User",
                IsTyping = true,
            },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        chat.VerifyAll();
    }

    [Fact]
    public async Task NotificationRead_ForeignNotificationIsForbiddenBeforeMutation()
    {
        var notifications = new Mock<INotificationService>(MockBehavior.Strict);
        notifications.Setup(service => service.GetByIdAsync("notification-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<NotificationDto>.Success(Notification("notification-1", "other-user")));
        var controller = WithActor(new NotificationsController(notifications.Object), "actor-1");

        var result = await controller.MarkAsRead("notification-1", CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        notifications.Verify(service => service.MarkAsReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        notifications.VerifyAll();
    }

    [Fact]
    public async Task NotificationRead_ActorOwnedNotificationCanMutate()
    {
        var notifications = new Mock<INotificationService>();
        var notification = Notification("notification-1", "actor-1");
        notifications.Setup(service => service.GetByIdAsync("notification-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<NotificationDto>.Success(notification));
        notifications.Setup(service => service.MarkAsReadAsync("notification-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<NotificationDto>.Success(notification with { IsRead = true }));
        var controller = WithActor(new NotificationsController(notifications.Object), "actor-1");

        var result = await controller.MarkAsRead("notification-1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        notifications.VerifyAll();
    }

    [Fact]
    public async Task NotificationUnreadList_UsesTheSignedActor()
    {
        var notifications = new Mock<INotificationService>();
        notifications.Setup(service => service.GetUnreadByUserIdAsync("actor-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<NotificationDto>>.Success([]));
        var controller = WithActor(new NotificationsController(notifications.Object), "actor-1");

        var result = await controller.GetUnreadMine(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        notifications.VerifyAll();
    }

    [Fact]
    public void NotificationCreate_IsUnavailableToPublicRequests()
    {
        var notifications = new Mock<INotificationService>(MockBehavior.Strict);
        var controller = WithActor(new NotificationsController(notifications.Object), "actor-1");

        var result = controller.Create(new CreateNotificationDto
        {
            UserId = "other-user",
            Type = NotificationType.PROJECT_UPDATE,
            Title = "Project update",
            Message = "A caller must not be able to create arbitrary notifications.",
        }, CancellationToken.None);

        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(StatusCodes.Status501NotImplemented);
        notifications.VerifyNoOtherCalls();
    }

    private static NotificationDto Notification(string id, string userId)
        => new()
        {
            Id = id,
            UserId = userId,
            Type = NotificationType.PROJECT_UPDATE,
            Title = "Project update",
            Message = "A project changed.",
            CreatedAt = DateTime.UtcNow,
        };

    private static T WithActor<T>(T controller, string actorId)
        where T : ControllerBase
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, actorId)],
                "ActorAssertion"))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }
}
