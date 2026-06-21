using Microsoft.AspNetCore.Mvc;

namespace Hive.MicroServices.Tests;

[ApiController]
[Route("api/[controller]")]
public sealed class PingController : ControllerBase
{
  [HttpGet]
  public IActionResult Get() => Ok(new { ping = "pong" });
}