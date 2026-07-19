namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Application.DTOs;
using FluentAssertions;

public class HierarchyAssignmentContractTests
{
    [Theory]
    [InlineData(typeof(UpdateEpicDto), "AssigneeId")]
    [InlineData(typeof(UpdateSprintDto), "AssigneeId")]
    [InlineData(typeof(UpdateFeatureDto), "AssigneeId")]
    [InlineData(typeof(UpdateProductBacklogItemDto), "AssigneeId")]
    [InlineData(typeof(UpdateTaskDto), "AssignedToId")]
    public void GenericUpdateDto_DoesNotExposeAssignment(Type dtoType, string propertyName)
    {
        dtoType.GetProperty(propertyName).Should().BeNull();
    }
}
