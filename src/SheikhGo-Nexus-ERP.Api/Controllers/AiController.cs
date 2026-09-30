using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;

namespace SheikhGoNexusERP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ai")]
public class AiController(IAiAssistantService ai) : ControllerBase
{
    [HttpPost("chat")]
    [HttpPost("query")]
    public async Task<ActionResult<AiChatResponse>> Chat([FromBody] AiChatRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await ai.ChatAsync(request, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<IReadOnlyList<AiConversationListItemDto>>> Conversations(CancellationToken ct)
    {
        try { return Ok(await ai.ListConversationsAsync(ct)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<ActionResult<AiConversationDetailDto>> Conversation(Guid id, CancellationToken ct)
    {
        try
        {
            var detail = await ai.GetConversationAsync(id, ct);
            return detail is null ? NotFound() : Ok(detail);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpGet("actions/{id:guid}")]
    public async Task<ActionResult<AiActionPreviewDto>> GetAction(Guid id, CancellationToken ct)
    {
        try
        {
            var action = await ai.GetActionAsync(id, ct);
            return action is null ? NotFound() : Ok(action);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPatch("actions/{id:guid}")]
    public async Task<ActionResult<AiActionPreviewDto>> UpdateAction(Guid id, [FromBody] AiActionUpdateRequest request, CancellationToken ct)
    {
        try { return Ok(await ai.UpdateActionAsync(id, request, ct)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("actions/{id:guid}/confirm")]
    public async Task<ActionResult<AiActionConfirmResponse>> ConfirmAction(Guid id, [FromBody] AiActionConfirmRequest? request, CancellationToken ct)
    {
        try { return Ok(await ai.ConfirmActionAsync(id, request, ct)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("actions/{id:guid}/cancel")]
    public async Task<ActionResult<AiActionPreviewDto>> CancelAction(Guid id, CancellationToken ct)
    {
        try { return Ok(await ai.CancelActionAsync(id, ct)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("agents")]
    public async Task<ActionResult<IReadOnlyList<AiAgentRunDto>>> Agents(CancellationToken ct)
    {
        try { return Ok(await ai.ListAgentRunsAsync(ct)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpGet("agents/{id:guid}")]
    public async Task<ActionResult<AiAgentRunDto>> Agent(Guid id, CancellationToken ct)
    {
        try
        {
            var run = await ai.GetAgentRunAsync(id, ct);
            return run is null ? NotFound() : Ok(run);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPost("agents/{id:guid}/confirm")]
    public async Task<ActionResult<AiAgentConfirmResponse>> ConfirmAgent(Guid id, [FromBody] AiAgentConfirmRequest? request, CancellationToken ct)
    {
        try { return Ok(await ai.ConfirmAgentRunAsync(id, request, ct)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("agents/{id:guid}/cancel")]
    public async Task<ActionResult<AiAgentRunDto>> CancelAgent(Guid id, CancellationToken ct)
    {
        try { return Ok(await ai.CancelAgentRunAsync(id, ct)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
