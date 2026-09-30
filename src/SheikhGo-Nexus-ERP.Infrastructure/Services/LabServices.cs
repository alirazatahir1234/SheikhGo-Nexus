using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class LabService(
    AppDbContext db,
    IAuditService audit,
    IPermissionService permissions) : ILabService
{
    public async Task<IReadOnlyList<LabTestTypeDto>> GetTestTypesAsync(CancellationToken ct = default)
    {
        var rows = await db.LabTestTypes.OrderBy(x => x.Code).ToListAsync(ct);
        return rows.Select(MapType).ToList();
    }

    public async Task<LabTestTypeDto> UpsertTestTypeAsync(LabTestTypeUpsertDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Lab.TestType.Create", ct);
        var code = dto.Code.Trim().ToUpperInvariant();
        var existing = await db.LabTestTypes.FirstOrDefaultAsync(x => x.Code == code, ct);
        if (existing is null)
        {
            existing = new LabTestType
            {
                Code = code,
                Name = dto.Name.Trim(),
                Unit = dto.Unit,
                MinValue = dto.MinValue,
                MaxValue = dto.MaxValue,
                IsActive = dto.IsActive
            };
            db.LabTestTypes.Add(existing);
        }
        else
        {
            existing.Name = dto.Name.Trim();
            existing.Unit = dto.Unit;
            existing.MinValue = dto.MinValue;
            existing.MaxValue = dto.MaxValue;
            existing.IsActive = dto.IsActive;
        }
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(LabTestType), existing.Id, "Upserted", null, existing.Code, ct);
        return MapType(existing);
    }

    public async Task<IReadOnlyList<LabSampleDto>> GetSamplesAsync(CancellationToken ct = default)
    {
        var rows = await db.LabSamples
            .Include(x => x.Product).Include(x => x.Warehouse)
            .Include(x => x.Results).ThenInclude(r => r.TestType)
            .OrderByDescending(x => x.SampleDate).ToListAsync(ct);
        return rows.Select(MapSample).ToList();
    }

    public async Task<LabSampleDto> CreateSampleAsync(LabSampleCreateDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Lab.Sample.Create", ct);
        if (!Enum.TryParse<LabSampleSourceType>(dto.SourceType, true, out var source))
            source = LabSampleSourceType.Inventory;

        var no = await NextSampleNoAsync(ct);
        var sample = new LabSample
        {
            SampleNo = no,
            SampleDate = (dto.SampleDate ?? DateTime.UtcNow).Date,
            ProductId = dto.ProductId,
            WarehouseId = dto.WarehouseId,
            SourceType = source,
            SourceEntityId = dto.SourceEntityId,
            FlockId = dto.FlockId,
            Status = LabSampleStatus.Draft,
            Remarks = dto.Remarks
        };
        db.LabSamples.Add(sample);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(LabSample), sample.Id, "Created", null, no, ct);
        return (await GetSamplesAsync(ct)).First(x => x.Id == sample.Id);
    }

    public async Task<IReadOnlyList<LabTestResultDto>> GetResultsAsync(CancellationToken ct = default)
    {
        var rows = await db.LabTestResults
            .Include(x => x.Sample).Include(x => x.TestType)
            .OrderByDescending(x => x.TestedAt).ToListAsync(ct);
        return rows.Select(MapResult).ToList();
    }

    public async Task<LabTestResultDto> CreateResultAsync(LabTestResultCreateDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Lab.Result.Create", ct);
        var sample = await db.LabSamples.FirstOrDefaultAsync(x => x.Id == dto.SampleId, ct)
            ?? throw new InvalidOperationException("Sample not found.");
        var type = await db.LabTestTypes.FirstOrDefaultAsync(x => x.Id == dto.TestTypeId, ct)
            ?? throw new InvalidOperationException("Test type not found.");

        var decision = Evaluate(type, dto.Value);
        var result = new LabTestResult
        {
            SampleId = sample.Id,
            TestTypeId = type.Id,
            TestedAt = dto.TestedAt ?? DateTime.UtcNow,
            Value = dto.Value,
            Decision = decision,
            Status = LabResultStatus.Draft,
            Narration = dto.Narration
        };
        sample.Status = LabSampleStatus.InTest;
        db.LabTestResults.Add(result);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(LabTestResult), result.Id, "Created", null, $"{type.Code}={dto.Value}→{decision}", ct);
        return MapResult(await db.LabTestResults.Include(x => x.Sample).Include(x => x.TestType).FirstAsync(x => x.Id == result.Id, ct));
    }

    public async Task<LabTestResultDto?> ApproveResultAsync(Guid id, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("Lab.Result.Approve", ct);
        var result = await db.LabTestResults
            .Include(x => x.Sample).Include(x => x.TestType)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (result is null) return null;
        if (result.Status == LabResultStatus.Approved) return MapResult(result);

        // Re-evaluate so FAIL cannot silently become PASS
        result.Decision = Evaluate(result.TestType!, result.Value);
        result.Status = LabResultStatus.Approved;
        result.ApprovedAt = DateTime.UtcNow;
        result.Sample!.Status = LabSampleStatus.Completed;

        if (result.Decision is LabDecision.Fail or LabDecision.Hold
            && result.Sample.ProductId is Guid pid
            && result.Sample.WarehouseId is Guid wid)
        {
            var stock = await db.WarehouseStocks.FirstOrDefaultAsync(x => x.ProductId == pid && x.WarehouseId == wid, ct);
            if (stock is not null)
            {
                stock.QualityStatus = result.Decision == LabDecision.Fail
                    ? StockQualityStatus.Rejected
                    : StockQualityStatus.Hold;
                await permissions.EnsureAsync(
                    result.Decision == LabDecision.Hold ? "Lab.Quality.Hold" : "Lab.Quality.Hold", ct);
            }
        }
        else if (result.Decision == LabDecision.Pass
                 && result.Sample.ProductId is Guid pid2
                 && result.Sample.WarehouseId is Guid wid2)
        {
            var stock = await db.WarehouseStocks.FirstOrDefaultAsync(x => x.ProductId == pid2 && x.WarehouseId == wid2, ct);
            if (stock is not null && stock.QualityStatus != StockQualityStatus.Unrestricted)
            {
                await permissions.EnsureAsync("Lab.Quality.Release", ct);
                stock.QualityStatus = StockQualityStatus.Unrestricted;
            }
        }

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(LabTestResult), result.Id, "Approved", null, result.Decision.ToString(), ct);
        return MapResult(result);
    }

    private static LabDecision Evaluate(LabTestType type, decimal value)
    {
        if (type.MaxValue is decimal max && value > max) return LabDecision.Fail;
        if (type.MinValue is decimal min && value < min) return LabDecision.Fail;
        return LabDecision.Pass;
    }

    private async Task<string> NextSampleNoAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var count = await db.LabSamples.CountAsync(ct);
        return $"LS-{year}-{(count + 1):D6}";
    }

    private static LabTestTypeDto MapType(LabTestType t) =>
        new(t.Id, t.Code, t.Name, t.Unit, t.MinValue, t.MaxValue, t.IsActive);

    private static LabSampleDto MapSample(LabSample s) => new(
        s.Id, s.SampleNo, s.SampleDate, s.ProductId, s.Product?.Name, s.WarehouseId, s.Warehouse?.Name,
        s.SourceType.ToString(), s.SourceEntityId, s.FlockId, s.Status.ToString(), s.Remarks,
        s.Results.Select(MapResult).ToList());

    private static LabTestResultDto MapResult(LabTestResult r) => new(
        r.Id, r.SampleId, r.Sample?.SampleNo, r.TestTypeId, r.TestType?.Code, r.TestType?.Name,
        r.TestedAt, r.Value, r.Decision.ToString(), r.Status.ToString(), r.Narration);
}
