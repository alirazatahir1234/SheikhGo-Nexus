using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;

namespace SheikhGoNexusERP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/farm")]
public class FarmController(IFarmService svc) : ControllerBase
{
    [HttpGet("farms")]
    public async Task<ActionResult<IReadOnlyList<FarmDto>>> Farms(CancellationToken ct) =>
        Ok(await svc.GetFarmsAsync(ct));

    [HttpPost("farms")]
    public async Task<ActionResult<FarmDto>> CreateFarm([FromBody] FarmUpsertDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.CreateFarmAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("units")]
    public async Task<ActionResult<FarmUnitDto>> CreateUnit([FromBody] FarmUnitUpsertDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.CreateUnitAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("flocks")]
    public async Task<ActionResult<IReadOnlyList<FlockDto>>> Flocks(CancellationToken ct) =>
        Ok(await svc.GetFlocksAsync(ct));

    [HttpGet("flocks/{id:guid}")]
    public async Task<ActionResult<FlockDto>> FlockById(Guid id, CancellationToken ct)
    {
        var f = await svc.GetFlockByIdAsync(id, ct);
        return f is null ? NotFound() : Ok(f);
    }

    [HttpPost("flocks")]
    public async Task<ActionResult<FlockDto>> CreateFlock([FromBody] FlockCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.CreateFlockAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("flocks/{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct) =>
        await svc.CloseFlockAsync(id, ct) ? NoContent() : NotFound();

    [HttpGet("flocks/{id:guid}/mortality")]
    public async Task<ActionResult<IReadOnlyList<MortalityDto>>> Mortalities(Guid id, CancellationToken ct) =>
        Ok(await svc.GetMortalitiesAsync(id, ct));

    [HttpPost("flocks/{id:guid}/mortality")]
    public async Task<ActionResult<MortalityDto>> AddMortality(Guid id, [FromBody] MortalityCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.AddMortalityAsync(id, dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("flocks/{id:guid}/events")]
    public async Task<ActionResult<IReadOnlyList<FarmEventDto>>> Events(Guid id, CancellationToken ct) =>
        Ok(await svc.GetEventsAsync(id, ct));

    [HttpPost("flocks/{id:guid}/events")]
    public async Task<ActionResult<FarmEventDto>> AddEvent(Guid id, [FromBody] FarmEventCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.AddEventAsync(id, dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("feed-issues")]
    public async Task<ActionResult<IReadOnlyList<FeedIssueDto>>> FeedIssues(CancellationToken ct) =>
        Ok(await svc.GetFeedIssuesAsync(ct));

    [HttpPost("feed-issues")]
    public async Task<ActionResult<FeedIssueDto>> CreateFeed([FromBody] FeedIssueCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.CreateFeedIssueAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("feed-issues/{id:guid}/{actionName}")]
    public async Task<ActionResult<FeedIssueDto>> TransitionFeed(Guid id, string actionName, CancellationToken ct)
    {
        try
        {
            var r = await svc.TransitionFeedIssueAsync(id, actionName, ct);
            return r is null ? NotFound() : Ok(r);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("treatments")]
    public async Task<ActionResult<IReadOnlyList<FarmTreatmentDto>>> Treatments([FromQuery] Guid? flockId, CancellationToken ct) =>
        Ok(await svc.GetTreatmentsAsync(flockId, ct));

    [HttpPost("treatments")]
    public async Task<ActionResult<FarmTreatmentDto>> CreateTreatment([FromBody] FarmTreatmentCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.CreateTreatmentAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("expenses")]
    public async Task<ActionResult<IReadOnlyList<FarmExpenseDto>>> Expenses(CancellationToken ct) =>
        Ok(await svc.GetExpensesAsync(ct));

    [HttpPost("expenses")]
    public async Task<ActionResult<FarmExpenseDto>> CreateExpense([FromBody] FarmExpenseCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.CreateExpenseAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("expenses/{id:guid}/{actionName}")]
    public async Task<ActionResult<FarmExpenseDto>> TransitionExpense(Guid id, string actionName, CancellationToken ct)
    {
        try
        {
            var r = await svc.TransitionExpenseAsync(id, actionName, ct);
            return r is null ? NotFound() : Ok(r);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("performance/{flockId:guid}")]
    public async Task<ActionResult<FarmPerformanceDto>> Performance(Guid flockId, CancellationToken ct)
    {
        var r = await svc.GetPerformanceAsync(flockId, ct);
        return r is null ? NotFound() : Ok(r);
    }
}

[ApiController]
[Authorize]
[Route("api/lab")]
public class LabController(ILabService svc) : ControllerBase
{
    [HttpGet("test-types")]
    [HttpGet("tests")]
    public async Task<ActionResult<IReadOnlyList<LabTestTypeDto>>> TestTypes(CancellationToken ct) =>
        Ok(await svc.GetTestTypesAsync(ct));

    [HttpPost("test-types")]
    [HttpPost("tests")]
    public async Task<ActionResult<LabTestTypeDto>> UpsertTest([FromBody] LabTestTypeUpsertDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.UpsertTestTypeAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("samples")]
    public async Task<ActionResult<IReadOnlyList<LabSampleDto>>> Samples(CancellationToken ct) =>
        Ok(await svc.GetSamplesAsync(ct));

    [HttpPost("samples")]
    public async Task<ActionResult<LabSampleDto>> CreateSample([FromBody] LabSampleCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.CreateSampleAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("results")]
    public async Task<ActionResult<IReadOnlyList<LabTestResultDto>>> Results(CancellationToken ct) =>
        Ok(await svc.GetResultsAsync(ct));

    [HttpPost("results")]
    public async Task<ActionResult<LabTestResultDto>> CreateResult([FromBody] LabTestResultCreateDto dto, CancellationToken ct)
    {
        try { return Ok(await svc.CreateResultAsync(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("results/{id:guid}/approve")]
    public async Task<ActionResult<LabTestResultDto>> Approve(Guid id, CancellationToken ct)
    {
        try
        {
            var r = await svc.ApproveResultAsync(id, ct);
            return r is null ? NotFound() : Ok(r);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
