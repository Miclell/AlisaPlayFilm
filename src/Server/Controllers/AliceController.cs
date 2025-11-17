using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers;

[ApiController]
[Route("api/alice")]
[Produces("application/json")]
public class AliceController(
    IAliceService aliceService,
    ILogger<AliceController> logger)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AliceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AliceResponse>> Post([FromBody] AliceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Session ID: {SessionId}", request.Session?.SessionId);
            logger.LogDebug("User ID: {UserId}", request.Session?.UserId);
            logger.LogDebug("Message ID: {MessageId}", request.Session?.MessageId);
            logger.LogDebug("Request Type: {Type}", request.Request?.Type);
            logger.LogDebug("Command: {Command}", request.Request?.Command);
            logger.LogDebug("Original Utterance: {OriginalUtterance}", request.Request?.OriginalUtterance);
            logger.LogDebug("Version: {Version}", request.Version);
            logger.LogDebug("Full Request JSON: {RequestJson}", JsonSerializer.Serialize(request));

            var response = await aliceService.ProcessRequestAsync(request, cancellationToken);

            logger.LogDebug("Response Text: {ResponseText}", response.Response?.Text);
            logger.LogDebug("Full Response JSON: {ResponseJson}", JsonSerializer.Serialize(response));

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Alice request");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}