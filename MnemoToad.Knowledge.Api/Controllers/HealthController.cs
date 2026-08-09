using Microsoft.AspNetCore.Mvc;

namespace MnemoToad.Knowledge.Api.Controllers;

/// <summary>Reports whether the API is up, for uptime monitoring and deploy health checks.</summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    /// <summary>Checks API health.</summary>
    /// <response code="200">The API is running.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() =>
        Ok(new { status = "pass" });
}
