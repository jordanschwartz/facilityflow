using FacilityFlow.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(ILogger<WebhooksController> logger)
    {
        _logger = logger;
    }

    [HttpPost("ses-inbound")]
    [AllowAnonymous]
    public async Task<IActionResult> SesInbound([FromServices] IInboundEmailService service)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        _logger.LogInformation("Received SES inbound webhook");

        // Fire and forget — return 200 quickly for SNS
        _ = service.ProcessInboundEmailAsync(body);

        return Ok();
    }
}
