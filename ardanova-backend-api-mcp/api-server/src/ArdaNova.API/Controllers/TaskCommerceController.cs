namespace ArdaNova.API.Controllers;

using System.Security.Claims;
using ArdaNova.API.Middleware;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/task-commerce")]
public class TaskCommerceController : ControllerBase
{
    private readonly ITaskCommerceService _taskCommerceService;

    public TaskCommerceController(ITaskCommerceService taskCommerceService)
    {
        _taskCommerceService = taskCommerceService;
    }

    [HttpGet("{taskId}")]
    [Authorize(Policy = AuthorizationPolicies.ActorAssertion)]
    public async Task<IActionResult> GetByTaskId(string taskId, CancellationToken ct)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _taskCommerceService.GetByTaskIdAsync(taskId, actorId, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : result.Type switch
            {
                ResultType.NotFound => NotFound(new { error = result.Error }),
                ResultType.Forbidden => Forbid(),
                _ => BadRequest(new { error = result.Error }),
            };
    }
}
