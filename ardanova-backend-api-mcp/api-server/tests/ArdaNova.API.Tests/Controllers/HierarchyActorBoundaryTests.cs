namespace ArdaNova.API.Tests.Controllers;

using System.Reflection;
using ArdaNova.API.Controllers;
using ArdaNova.API.Middleware;
using Microsoft.AspNetCore.Authorization;

public class HierarchyActorBoundaryTests
{
    public static TheoryData<Type, string> MutatingActions => new()
    {
        { typeof(EpicsController), nameof(EpicsController.Create) },
        { typeof(EpicsController), nameof(EpicsController.Update) },
        { typeof(EpicsController), nameof(EpicsController.Delete) },
        { typeof(EpicsController), nameof(EpicsController.Assign) },
        { typeof(EpicsController), nameof(EpicsController.UpdateStatus) },
        { typeof(EpicsController), nameof(EpicsController.UpdatePriority) },
        { typeof(MilestoneEpicsController), nameof(MilestoneEpicsController.ReorderEpics) },
        { typeof(SprintsController), nameof(SprintsController.Create) },
        { typeof(SprintsController), nameof(SprintsController.Update) },
        { typeof(SprintsController), nameof(SprintsController.Delete) },
        { typeof(SprintsController), nameof(SprintsController.Start) },
        { typeof(SprintsController), nameof(SprintsController.Complete) },
        { typeof(SprintsController), nameof(SprintsController.Cancel) },
        { typeof(SprintsController), nameof(SprintsController.UpdateStatus) },
        { typeof(FeaturesController), nameof(FeaturesController.Create) },
        { typeof(FeaturesController), nameof(FeaturesController.Update) },
        { typeof(FeaturesController), nameof(FeaturesController.Delete) },
        { typeof(FeaturesController), nameof(FeaturesController.Assign) },
        { typeof(FeaturesController), nameof(FeaturesController.UpdateStatus) },
        { typeof(FeaturesController), nameof(FeaturesController.UpdatePriority) },
        { typeof(ProductBacklogItemsController), nameof(ProductBacklogItemsController.Create) },
        { typeof(ProductBacklogItemsController), nameof(ProductBacklogItemsController.Update) },
        { typeof(ProductBacklogItemsController), nameof(ProductBacklogItemsController.Delete) },
        { typeof(ProductBacklogItemsController), nameof(ProductBacklogItemsController.Assign) },
        { typeof(ProductBacklogItemsController), nameof(ProductBacklogItemsController.UpdateStatus) },
        { typeof(ProductBacklogItemsController), nameof(ProductBacklogItemsController.UpdatePriority) },
        { typeof(ProductBacklogItemsController), nameof(ProductBacklogItemsController.ReorderPbis) },
        { typeof(TasksController), nameof(TasksController.Create) },
        { typeof(TasksController), nameof(TasksController.Update) },
        { typeof(TasksController), nameof(TasksController.UpdateStatus) },
        { typeof(TasksController), nameof(TasksController.Delete) },
        { typeof(ProjectsController), nameof(ProjectsController.Create) },
        { typeof(ProjectsController), nameof(ProjectsController.Update) },
        { typeof(ProjectsController), nameof(ProjectsController.Delete) },
        { typeof(ProjectsController), nameof(ProjectsController.Publish) },
        { typeof(ProjectsController), nameof(ProjectsController.SetFeatured) },
        { typeof(ProjectsController), nameof(ProjectsController.CreateResource) },
        { typeof(ProjectsController), nameof(ProjectsController.UpdateResource) },
        { typeof(ProjectsController), nameof(ProjectsController.DeleteResource) },
        { typeof(ProjectsController), nameof(ProjectsController.MarkResourceObtained) },
        { typeof(ProjectsController), nameof(ProjectsController.CreateMilestone) },
        { typeof(ProjectsController), nameof(ProjectsController.UpdateMilestone) },
        { typeof(ProjectsController), nameof(ProjectsController.DeleteMilestone) },
        { typeof(ProjectsController), nameof(ProjectsController.CompleteMilestone) },
        { typeof(ProjectsController), nameof(ProjectsController.AddMember) },
        { typeof(ProjectsController), nameof(ProjectsController.UpdateMember) },
        { typeof(ProjectsController), nameof(ProjectsController.RemoveMember) },
        { typeof(ProjectsController), nameof(ProjectsController.SubmitApplication) },
        { typeof(ProjectsController), nameof(ProjectsController.AcceptApplication) },
        { typeof(ProjectsController), nameof(ProjectsController.RejectApplication) },
        { typeof(ProjectsController), nameof(ProjectsController.WithdrawApplication) },
        { typeof(ProjectsController), nameof(ProjectsController.DeleteApplication) },
        { typeof(ProjectsController), nameof(ProjectsController.CreateComment) },
        { typeof(ProjectsController), nameof(ProjectsController.UpdateComment) },
        { typeof(ProjectsController), nameof(ProjectsController.DeleteComment) },
        { typeof(ProjectsController), nameof(ProjectsController.CreateUpdate) },
        { typeof(ProjectsController), nameof(ProjectsController.DeleteUpdate) },
        { typeof(ProjectsController), nameof(ProjectsController.CreateSupport) },
        { typeof(ProjectsController), nameof(ProjectsController.ToggleSupport) },
        { typeof(ProjectsController), nameof(ProjectsController.DeleteSupport) },
        { typeof(ProjectsController), nameof(ProjectsController.CreateProposal) },
        { typeof(ProjectsController), nameof(ProjectsController.ExecuteProposal) },
        { typeof(ProjectsController), nameof(ProjectsController.CancelProposal) },
        { typeof(ProjectsController), nameof(ProjectsController.PublishProposal) },
        { typeof(ProjectsController), nameof(ProjectsController.UpdateProposal) },
        { typeof(ProjectsController), nameof(ProjectsController.CreateProposalComment) },
        { typeof(ProjectsController), nameof(ProjectsController.CastVote) },
        { typeof(ProjectsController), nameof(ProjectsController.CreateInvitation) },
        { typeof(ProjectsController), nameof(ProjectsController.AcceptInvitation) },
        { typeof(ProjectsController), nameof(ProjectsController.RejectInvitation) },
        { typeof(ProjectsController), nameof(ProjectsController.DeleteInvitation) }
    };

    [Theory]
    [MemberData(nameof(MutatingActions))]
    public void DirectMutationsRequireActorAssertion(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(method);
        var authorization = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorization);
        Assert.Equal(AuthorizationPolicies.ActorAssertion, authorization!.Policy);
    }
}
