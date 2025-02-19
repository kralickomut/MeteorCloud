using MeteorCloud.Shared.ApiResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeteorCloud.API.Controllers;


[ApiController]
[Authorize]
[Route("api/test")]
public class TestController : ControllerBase
{

    [HttpPost("test")]
    public IActionResult Test()
    {
        return Ok(new ApiResult<object>(default, true, "Test successful"));
    }
}