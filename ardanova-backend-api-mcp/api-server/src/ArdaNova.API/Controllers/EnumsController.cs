namespace ArdaNova.API.Controllers;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EnumsController : ControllerBase
{
    private readonly IEnumLookupService _enumLookupService;

    public EnumsController(IEnumLookupService enumLookupService)
    {
        _enumLookupService = enumLookupService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var result = _enumLookupService.GetAllEnumNames();
        return ToActionResult(result);
    }

    [HttpGet("{name}")]
    public IActionResult GetByName(string name)
    {
        var result = _enumLookupService.GetEnumValues(name);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Type switch
        {
            ResultType.NotFound => NotFound(new { error = result.Error }),
            ResultType.ValidationError => BadRequest(new { error = result.Error }),
            ResultType.Unauthorized => Unauthorized(new { error = result.Error }),
            ResultType.Forbidden => Forbid(),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
