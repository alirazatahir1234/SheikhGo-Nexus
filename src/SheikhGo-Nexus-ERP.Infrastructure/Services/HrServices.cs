using Microsoft.EntityFrameworkCore;
using SheikhGoNexusERP.Application.DTOs;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Domain.Entities;
using SheikhGoNexusERP.Domain.Enums;
using SheikhGoNexusERP.Infrastructure.Persistence;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class HrOrgService(AppDbContext db) : IHrOrgService
{
    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken ct = default) =>
        await db.Departments.OrderBy(x => x.Code)
            .Select(x => new DepartmentDto(x.Id, x.Code, x.Name, x.IsActive)).ToListAsync(ct);

    public async Task<DepartmentDto> CreateDepartmentAsync(DepartmentUpsertDto dto, CancellationToken ct = default)
    {
        var e = new Department { Code = dto.Code.Trim().ToUpperInvariant(), Name = dto.Name.Trim(), IsActive = dto.IsActive };
        db.Departments.Add(e);
        await db.SaveChangesAsync(ct);
        return new DepartmentDto(e.Id, e.Code, e.Name, e.IsActive);
    }

    public async Task<IReadOnlyList<DesignationDto>> GetDesignationsAsync(CancellationToken ct = default) =>
        await db.Designations.OrderBy(x => x.Code)
            .Select(x => new DesignationDto(x.Id, x.Code, x.Name, x.IsActive)).ToListAsync(ct);

    public async Task<DesignationDto> CreateDesignationAsync(DesignationUpsertDto dto, CancellationToken ct = default)
    {
        var e = new Designation { Code = dto.Code.Trim().ToUpperInvariant(), Name = dto.Name.Trim(), IsActive = dto.IsActive };
        db.Designations.Add(e);
        await db.SaveChangesAsync(ct);
        return new DesignationDto(e.Id, e.Code, e.Name, e.IsActive);
    }

    public async Task<IReadOnlyList<PayrollComponentDto>> GetComponentsAsync(CancellationToken ct = default) =>
        await db.PayrollComponents.OrderBy(x => x.Code)
            .Select(x => new PayrollComponentDto(x.Id, x.Code, x.Name, x.ComponentType.ToString(), x.IsSystem, x.GlMappingKey))
            .ToListAsync(ct);
}

public class GlMappingService(AppDbContext db) : IGlMappingService
{
    public async Task<IReadOnlyList<GlAccountMappingDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.GlAccountMappings.Include(x => x.Account).OrderBy(x => x.MappingKey)
            .Select(x => new GlAccountMappingDto(x.Id, x.MappingKey, x.AccountId, x.Account!.Code, x.Account.Name, x.IsActive))
            .ToListAsync(ct);

    public async Task<GlAccountMappingDto> UpsertAsync(GlAccountMappingUpsertDto dto, CancellationToken ct = default)
    {
        var key = dto.MappingKey.Trim();
        var row = await db.GlAccountMappings.Include(x => x.Account).FirstOrDefaultAsync(x => x.MappingKey == key, ct);
        var acct = await db.Accounts.FirstOrDefaultAsync(x => x.Id == dto.AccountId && x.IsPostable, ct)
            ?? throw new InvalidOperationException("Account not found or not postable.");
        if (row is null)
        {
            row = new GlAccountMapping { MappingKey = key, AccountId = acct.Id, IsActive = dto.IsActive };
            db.GlAccountMappings.Add(row);
        }
        else
        {
            row.AccountId = acct.Id;
            row.IsActive = dto.IsActive;
        }
        await db.SaveChangesAsync(ct);
        return new GlAccountMappingDto(row.Id, row.MappingKey, acct.Id, acct.Code, acct.Name, row.IsActive);
    }
}

public class EmployeeService(AppDbContext db, IAuditService audit) : IEmployeeService
{
    public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await db.Employees
            .Include(x => x.Department).Include(x => x.Designation).Include(x => x.Branch)
            .Include(x => x.PayrollProfile!).ThenInclude(p => p.Lines).ThenInclude(l => l.PayrollComponent)
            .OrderBy(x => x.EmployeeCode).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var e = await LoadAsync(id, ct);
        return e is null ? null : Map(e);
    }

    public async Task<EmployeeDto> CreateAsync(EmployeeUpsertDto dto, CancellationToken ct = default)
    {
        var branch = await db.Branches.FirstOrDefaultAsync(x => x.Id == dto.BranchId, ct)
            ?? throw new InvalidOperationException("Branch not found.");
        var emp = new Employee
        {
            EmployeeCode = dto.Code.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            DepartmentId = dto.DepartmentId,
            DesignationId = dto.DesignationId,
            BranchId = branch.Id,
            JoiningDate = (dto.JoinDate ?? DateTime.UtcNow).Date,
            EmploymentStatus = ParseStatus(dto.EmploymentStatus),
            EmploymentType = ParseType(dto.EmploymentType),
            BasicSalary = dto.BasicSalary,
            Phone = dto.Phone,
            Email = dto.Email,
            IsActive = dto.IsActive
        };
        db.Employees.Add(emp);
        await db.SaveChangesAsync(ct);
        await UpsertProfileAsync(emp.Id, dto.ProfileLines, ct);
        await audit.WriteAsync(nameof(Employee), emp.Id, "Created", null, emp.EmployeeCode, ct);
        return (await GetByIdAsync(emp.Id, ct))!;
    }

    public async Task<EmployeeDto?> UpdateAsync(Guid id, EmployeeUpsertDto dto, CancellationToken ct = default)
    {
        var emp = await db.Employees.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (emp is null) return null;
        emp.Name = dto.Name.Trim();
        emp.DepartmentId = dto.DepartmentId;
        emp.DesignationId = dto.DesignationId;
        emp.BranchId = dto.BranchId;
        if (dto.JoinDate is DateTime jd) emp.JoiningDate = jd.Date;
        emp.EmploymentStatus = ParseStatus(dto.EmploymentStatus);
        emp.EmploymentType = ParseType(dto.EmploymentType);
        emp.BasicSalary = dto.BasicSalary;
        emp.Phone = dto.Phone;
        emp.Email = dto.Email;
        emp.IsActive = dto.IsActive;
        await db.SaveChangesAsync(ct);
        if (dto.ProfileLines is not null)
            await UpsertProfileAsync(emp.Id, dto.ProfileLines, ct);
        return await GetByIdAsync(id, ct);
    }

    private async Task UpsertProfileAsync(Guid employeeId, IReadOnlyList<PayrollProfileLineUpsertDto>? lines, CancellationToken ct)
    {
        var profile = await db.PayrollProfiles.Include(x => x.Lines).FirstOrDefaultAsync(x => x.EmployeeId == employeeId, ct);
        if (profile is null)
        {
            profile = new PayrollProfile { EmployeeId = employeeId };
            db.PayrollProfiles.Add(profile);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            db.PayrollProfileLines.RemoveRange(profile.Lines);
            await db.SaveChangesAsync(ct);
        }

        if (lines is null || lines.Count == 0) return;
        foreach (var l in lines)
        {
            db.PayrollProfileLines.Add(new PayrollProfileLine
            {
                PayrollProfileId = profile.Id,
                PayrollComponentId = l.ComponentId,
                Amount = l.Amount
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<Employee?> LoadAsync(Guid id, CancellationToken ct) =>
        await db.Employees
            .Include(x => x.Department).Include(x => x.Designation).Include(x => x.Branch)
            .Include(x => x.PayrollProfile!).ThenInclude(p => p.Lines).ThenInclude(l => l.PayrollComponent)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    private static EmployeeDto Map(Employee e) => new(
        e.Id, e.EmployeeCode, e.Name, e.DepartmentId, e.Department?.Name, e.DesignationId, e.Designation?.Name,
        e.BranchId, e.Branch?.Name, e.JoiningDate, e.EmploymentStatus.ToString(), e.EmploymentType.ToString(),
        e.BasicSalary, e.Phone, e.Email, e.IsActive,
        (e.PayrollProfile?.Lines ?? []).Select(l => new PayrollProfileLineDto(
            l.Id, l.PayrollComponentId, l.PayrollComponent?.Code, l.PayrollComponent?.Name,
            l.PayrollComponent?.ComponentType.ToString() ?? "", l.Amount)).ToList());

    private static EmploymentStatus ParseStatus(string? s) =>
        Enum.TryParse<EmploymentStatus>(s, true, out var v) ? v : EmploymentStatus.Active;

    private static EmploymentType ParseType(string? s) =>
        Enum.TryParse<EmploymentType>(s, true, out var v) ? v : EmploymentType.Permanent;
}

public class AttendanceService(AppDbContext db) : IAttendanceService
{
    public async Task<IReadOnlyList<AttendanceDto>> GetAttendanceAsync(CancellationToken ct = default)
    {
        var rows = await db.Attendances.Include(x => x.Employee).OrderByDescending(x => x.Date).Take(500).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<AttendanceDto> CreateAttendanceAsync(AttendanceCreateDto dto, CancellationToken ct = default)
    {
        _ = await db.Employees.FirstOrDefaultAsync(x => x.Id == dto.EmployeeId, ct)
            ?? throw new InvalidOperationException("Employee not found.");
        var status = Enum.TryParse<AttendanceStatus>(dto.Status, true, out var s) ? s : AttendanceStatus.Present;
        var date = dto.Date.Date;
        var existing = await db.Attendances.FirstOrDefaultAsync(x => x.EmployeeId == dto.EmployeeId && x.Date == date, ct);
        if (existing is not null)
        {
            existing.Status = status;
            existing.OvertimeHours = dto.OvertimeHours;
            existing.Remarks = dto.Remarks;
            await db.SaveChangesAsync(ct);
            return Map(await db.Attendances.Include(x => x.Employee).FirstAsync(x => x.Id == existing.Id, ct));
        }

        var row = new Attendance
        {
            EmployeeId = dto.EmployeeId,
            Date = date,
            Status = status,
            OvertimeHours = dto.OvertimeHours,
            Remarks = dto.Remarks
        };
        db.Attendances.Add(row);
        await db.SaveChangesAsync(ct);
        return Map(await db.Attendances.Include(x => x.Employee).FirstAsync(x => x.Id == row.Id, ct));
    }

    public async Task<IReadOnlyList<AttendanceSummaryDto>> GetSummaryAsync(int year, int month, CancellationToken ct = default)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var rows = await db.Attendances.Include(x => x.Employee)
            .Where(x => x.Date >= start && x.Date < end).ToListAsync(ct);
        return rows.GroupBy(x => x.EmployeeId).Select(g =>
        {
            var emp = g.First().Employee;
            return new AttendanceSummaryDto(
                g.Key, emp?.Name, year, month,
                g.Count(x => x.Status == AttendanceStatus.Present),
                g.Count(x => x.Status == AttendanceStatus.Absent),
                g.Count(x => x.Status == AttendanceStatus.Late),
                g.Count(x => x.Status == AttendanceStatus.HalfDay),
                g.Count(x => x.Status == AttendanceStatus.Overtime),
                g.Sum(x => x.OvertimeHours ?? 0));
        }).OrderBy(x => x.EmployeeName).ToList();
    }

    public Task<IReadOnlyList<OvertimeDto>> GetOvertimeAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<OvertimeDto>>(Array.Empty<OvertimeDto>());

    public async Task<OvertimeDto> CreateOvertimeAsync(OvertimeCreateDto dto, CancellationToken ct = default)
    {
        var att = await CreateAttendanceAsync(new AttendanceCreateDto(dto.Date, dto.EmployeeId, "Overtime", dto.Hours, dto.Remarks), ct);
        return new OvertimeDto(att.Id, $"OT-{att.Id:N}"[..12], att.Date, att.EmployeeId, att.EmployeeName,
            dto.Hours, dto.PerHourRate, dto.Hours * dto.PerHourRate, dto.Remarks);
    }

    private static AttendanceDto Map(Attendance x) =>
        new(x.Id, x.Date, x.EmployeeId, x.Employee?.Name, x.Status.ToString(), x.OvertimeHours, x.Remarks);
}

public class LeaveService(AppDbContext db, IAuditService audit) : ILeaveService
{
    public async Task<IReadOnlyList<LeaveTypeDto>> GetTypesAsync(CancellationToken ct = default) =>
        await db.LeaveTypes.OrderBy(x => x.Code)
            .Select(x => new LeaveTypeDto(x.Id, x.Code, x.Name, x.Paid, x.AnnualAllowanceDays, x.IsActive))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LeaveBalanceDto>> GetBalancesAsync(Guid? employeeId = null, CancellationToken ct = default)
    {
        var q = db.LeaveBalances.Include(x => x.Employee).Include(x => x.LeaveType).AsQueryable();
        if (employeeId is Guid id) q = q.Where(x => x.EmployeeId == id);
        var rows = await q.OrderBy(x => x.Year).ToListAsync(ct);
        return rows.Select(x => new LeaveBalanceDto(x.Id, x.EmployeeId, x.Employee?.Name, x.LeaveTypeId, x.LeaveType?.Name,
            x.Year, x.Entitled, x.Used, x.Remaining)).ToList();
    }

    public async Task<IReadOnlyList<LeaveDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await db.LeaveRequests.Include(x => x.Employee).Include(x => x.LeaveType)
            .OrderByDescending(x => x.FromDate).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<LeaveDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var x = await db.LeaveRequests.Include(e => e.Employee).Include(e => e.LeaveType)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        return x is null ? null : Map(x);
    }

    public async Task<LeaveDto> CreateAsync(LeaveCreateDto dto, CancellationToken ct = default)
    {
        _ = await db.Employees.FirstOrDefaultAsync(x => x.Id == dto.EmployeeId, ct)
            ?? throw new InvalidOperationException("Employee not found.");
        _ = await db.LeaveTypes.FirstOrDefaultAsync(x => x.Id == dto.LeaveTypeId, ct)
            ?? throw new InvalidOperationException("Leave type not found.");

        var days = (decimal)(dto.ToDate.Date - dto.FromDate.Date).TotalDays + 1;
        if (days <= 0) throw new InvalidOperationException("Invalid leave date range.");

        var company = await db.Companies.FirstAsync(ct);
        var branch = await db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        var fy = await db.FinancialYears.FirstAsync(x => x.IsCurrent, ct);
        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "LV", ct);
        var no = await DocumentNumbering.NextAsync(db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "LV", fy.StartDate.Year, ct);

        var doc = new Document
        {
            DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id,
            DocumentNo = no, DocumentDate = DateTime.UtcNow.Date, Narration = dto.Remarks
        };
        var req = new LeaveRequest
        {
            Document = doc,
            RequestNo = no,
            EmployeeId = dto.EmployeeId,
            LeaveTypeId = dto.LeaveTypeId,
            FromDate = dto.FromDate.Date,
            ToDate = dto.ToDate.Date,
            Days = days,
            Status = DocumentStatus.Draft,
            Remarks = dto.Remarks
        };
        db.LeaveRequests.Add(req);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(LeaveRequest), req.Id, "Created", null, no, ct);
        return (await GetByIdAsync(req.Id, ct))!;
    }

    public async Task<LeaveDto?> TransitionAsync(Guid id, string action, CancellationToken ct = default)
    {
        var req = await db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (req is null) return null;

        var actionKey = action.Trim().ToLowerInvariant();
        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "LV", ct);
        var old = req.Status;
        var next = DocumentWorkflowService.ResolveNextStatus(old, actionKey, dt.RequiresVerification, dt.RequiresApproval, req.RequestNo);
        req.Status = next;
        if (req.DocumentId is Guid did)
        {
            var doc = await db.Documents.FirstAsync(x => x.Id == did, ct);
            doc.Status = next;
        }
        await db.SaveChangesAsync(ct);

        if (actionKey == "approve" && next == DocumentStatus.Approved)
            await ApplyBalanceAsync(req, ct);

        await audit.WriteAsync(nameof(LeaveRequest), req.Id, actionKey, old.ToString(), next.ToString(), ct);
        return await GetByIdAsync(id, ct);
    }

    private async Task ApplyBalanceAsync(LeaveRequest req, CancellationToken ct)
    {
        var year = req.FromDate.Year;
        var bal = await db.LeaveBalances.FirstOrDefaultAsync(x =>
            x.EmployeeId == req.EmployeeId && x.LeaveTypeId == req.LeaveTypeId && x.Year == year, ct);
        if (bal is null)
        {
            var lt = await db.LeaveTypes.FirstAsync(x => x.Id == req.LeaveTypeId, ct);
            bal = new LeaveBalance
            {
                EmployeeId = req.EmployeeId,
                LeaveTypeId = req.LeaveTypeId,
                Year = year,
                Entitled = lt.AnnualAllowanceDays,
                Used = 0,
                Remaining = lt.AnnualAllowanceDays
            };
            db.LeaveBalances.Add(bal);
        }
        if (bal.Remaining < req.Days)
            throw new InvalidOperationException($"Insufficient leave balance ({bal.Remaining}) for {req.Days} days.");
        bal.Used += req.Days;
        bal.Remaining = bal.Entitled - bal.Used;
        await db.SaveChangesAsync(ct);
    }

    private static LeaveDto Map(LeaveRequest x) => new(
        x.Id, x.RequestNo, x.EmployeeId, x.Employee?.Name, x.LeaveTypeId, x.LeaveType?.Name,
        x.FromDate, x.ToDate, x.Days, x.Status, x.Remarks);
}

public class PayrollService(
    AppDbContext db,
    IAuditService audit,
    IAccountingPostingService accounting,
    IPermissionService permissions) : IPayrollService
{
    public async Task<IReadOnlyList<PayrollPeriodDto>> GetPeriodsAsync(CancellationToken ct = default) =>
        await db.PayrollPeriods.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            .Select(x => new PayrollPeriodDto(x.Id, x.Year, x.Month, x.StartDate, x.EndDate, x.Status.ToString()))
            .ToListAsync(ct);

    public async Task<PayrollPeriodDto> CreatePeriodAsync(PayrollPeriodCreateDto dto, CancellationToken ct = default)
    {
        if (await db.PayrollPeriods.AnyAsync(x => x.Year == dto.Year && x.Month == dto.Month, ct))
            throw new InvalidOperationException("Period already exists.");
        var start = new DateTime(dto.Year, dto.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var p = new PayrollPeriod
        {
            Year = dto.Year, Month = dto.Month, StartDate = start, EndDate = end,
            Status = PayrollPeriodStatus.Open
        };
        db.PayrollPeriods.Add(p);
        await db.SaveChangesAsync(ct);
        return new PayrollPeriodDto(p.Id, p.Year, p.Month, p.StartDate, p.EndDate, p.Status.ToString());
    }

    public async Task<PayrollPeriodDto?> ClosePeriodAsync(Guid id, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("HR.Payroll.Approve", ct);
        var p = await db.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;
        if (p.Status == PayrollPeriodStatus.Closed)
            return new PayrollPeriodDto(p.Id, p.Year, p.Month, p.StartDate, p.EndDate, p.Status.ToString());
        p.Status = PayrollPeriodStatus.Closed;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(PayrollPeriod), p.Id, "Close", "Open", "Closed", ct);
        return new PayrollPeriodDto(p.Id, p.Year, p.Month, p.StartDate, p.EndDate, p.Status.ToString());
    }

    public async Task<IReadOnlyList<PayrollRunDto>> GetRunsAsync(CancellationToken ct = default)
    {
        var rows = await db.PayrollRuns.Include(x => x.PayrollPeriod)
            .Include(x => x.Employees).ThenInclude(e => e.Employee)
            .Include(x => x.Employees).ThenInclude(e => e.Lines).ThenInclude(l => l.PayrollComponent)
            .OrderByDescending(x => x.RunDate).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<PayrollRunDto?> GetRunByIdAsync(Guid id, CancellationToken ct = default)
    {
        var x = await LoadRunAsync(id, ct);
        return x is null ? null : Map(x);
    }

    public async Task<PayrollRunDto> CreateRunAsync(PayrollRunCreateDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("HR.Payroll.Create", ct);
        var period = await db.PayrollPeriods.FirstOrDefaultAsync(x => x.Id == dto.PeriodId, ct)
            ?? throw new InvalidOperationException("Payroll period not found.");
        if (period.Status == PayrollPeriodStatus.Closed)
            throw new InvalidOperationException("Payroll period is closed.");

        var company = await db.Companies.FirstAsync(ct);
        var branch = await db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        var fy = await db.FinancialYears.FirstAsync(x => x.IsCurrent, ct);
        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "PR", ct);
        var no = await DocumentNumbering.NextAsync(db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "PR", fy.StartDate.Year, ct);

        var doc = new Document
        {
            DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id,
            DocumentNo = no, DocumentDate = (dto.RunDate ?? DateTime.UtcNow).Date, Narration = dto.Remarks
        };
        var run = new PayrollRun
        {
            PayrollPeriodId = period.Id,
            Document = doc,
            RunNo = no,
            RunDate = (dto.RunDate ?? DateTime.UtcNow).Date,
            Status = DocumentStatus.Draft,
            Remarks = dto.Remarks
        };
        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(PayrollRun), run.Id, "Created", null, no, ct);
        return (await GetRunByIdAsync(run.Id, ct))!;
    }

    public async Task<PayrollRunDto?> CalculateAsync(Guid id, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("HR.Payroll.Calculate", ct);
        var run = await db.PayrollRuns
            .Include(x => x.PayrollPeriod)
            .Include(x => x.Employees).ThenInclude(e => e.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (run is null) return null;
        EnsurePayrollPeriodOpen(run.PayrollPeriod);
        if (run.Status is not DocumentStatus.Draft and not DocumentStatus.Calculated)
            throw new InvalidOperationException($"Cannot calculate payroll in status {run.Status}.");

        foreach (var empRow in run.Employees.ToList())
        {
            db.PayrollLines.RemoveRange(empRow.Lines);
            db.PayrollEmployees.Remove(empRow);
        }
        await db.SaveChangesAsync(ct);

        var basicComp = await db.PayrollComponents.FirstAsync(x => x.Code == "BASIC", ct);
        var employees = await db.Employees
            .Include(x => x.PayrollProfile!).ThenInclude(p => p.Lines).ThenInclude(l => l.PayrollComponent)
            .Where(x => x.IsActive && x.EmploymentStatus == EmploymentStatus.Active)
            .ToListAsync(ct);

        decimal grossTotal = 0, dedTotal = 0, netTotal = 0;
        foreach (var emp in employees)
        {
            var pe = new PayrollEmployee
            {
                PayrollRunId = run.Id,
                EmployeeId = emp.Id,
                Basic = emp.BasicSalary
            };
            var lines = new List<PayrollLine>
            {
                new()
                {
                    PayrollComponentId = basicComp.Id,
                    ComponentType = PayrollComponentType.Earning,
                    Amount = emp.BasicSalary
                }
            };
            foreach (var pl in emp.PayrollProfile?.Lines ?? [])
            {
                lines.Add(new PayrollLine
                {
                    PayrollComponentId = pl.PayrollComponentId,
                    ComponentType = pl.PayrollComponent?.ComponentType ?? PayrollComponentType.Earning,
                    Amount = pl.Amount
                });
            }

            pe.Gross = lines.Where(l => l.ComponentType == PayrollComponentType.Earning).Sum(l => l.Amount);
            pe.Deductions = lines.Where(l => l.ComponentType == PayrollComponentType.Deduction).Sum(l => l.Amount);
            pe.Net = pe.Gross - pe.Deductions;
            pe.Lines = lines;
            db.PayrollEmployees.Add(pe);

            grossTotal += pe.Gross;
            dedTotal += pe.Deductions;
            netTotal += pe.Net;
        }

        run.GrossTotal = grossTotal;
        run.DeductionTotal = dedTotal;
        run.NetTotal = netTotal;
        run.Status = DocumentStatus.Calculated;
        if (run.DocumentId is Guid did)
        {
            var doc = await db.Documents.FirstAsync(x => x.Id == did, ct);
            doc.Status = DocumentStatus.Calculated;
        }
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(nameof(PayrollRun), run.Id, "calculate", "Draft", "Calculated", ct);
        return await GetRunByIdAsync(id, ct);
    }

    public async Task<PayrollRunDto?> TransitionAsync(Guid id, string action, CancellationToken ct = default)
    {
        var run = await db.PayrollRuns
            .Include(x => x.PayrollPeriod)
            .Include(x => x.Employees)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (run is null) return null;

        var actionKey = action.Trim().ToLowerInvariant();
        await EnsurePermAsync(actionKey, ct);
        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "PR", ct);
        var old = run.Status;

        DocumentStatus next;
        if (actionKey == "calculate")
            return await CalculateAsync(id, ct);

        // Mutating workflow (submit/verify/approve/post) blocked when payroll period closed
        if (actionKey is "submit" or "verify" or "approve" or "post")
            EnsurePayrollPeriodOpen(run.PayrollPeriod);

        if (actionKey == "submit" && old == DocumentStatus.Calculated)
            next = DocumentStatus.Submitted;
        else
            next = DocumentWorkflowService.ResolveNextStatus(old, actionKey, dt.RequiresVerification, dt.RequiresApproval, run.RunNo);

        run.Status = next;
        if (run.DocumentId is Guid did)
        {
            var doc = await db.Documents.FirstAsync(x => x.Id == did, ct);
            doc.Status = next;
        }
        await db.SaveChangesAsync(ct);

        if (actionKey == "post" && next == DocumentStatus.Posted)
            await accounting.PostPayrollRunAsync(run, ct);

        await audit.WriteAsync(nameof(PayrollRun), run.Id, actionKey, old.ToString(), next.ToString(), ct);
        return await GetRunByIdAsync(id, ct);
    }

    public async Task<SalaryPaymentDto> CreatePaymentAsync(SalaryPaymentCreateDto dto, CancellationToken ct = default)
    {
        await permissions.EnsureAsync("HR.Payroll.Post", ct);
        var run = await db.PayrollRuns
            .Include(x => x.PayrollPeriod)
            .FirstOrDefaultAsync(x => x.Id == dto.PayrollRunId, ct)
            ?? throw new InvalidOperationException("Payroll run not found.");
        EnsurePayrollPeriodOpen(run.PayrollPeriod);
        if (run.Status != DocumentStatus.Posted)
            throw new InvalidOperationException("Payroll must be posted before payment.");
        if (await db.SalaryPayments.AnyAsync(x => x.PayrollRunId == run.Id && x.Status == DocumentStatus.Posted, ct))
            throw new InvalidOperationException("Salary already paid for this run.");

        _ = await db.CashBankAccounts.FirstOrDefaultAsync(x => x.Id == dto.CashBankAccountId, ct)
            ?? throw new InvalidOperationException("Cash/bank account not found.");

        var company = await db.Companies.FirstAsync(ct);
        var branch = await db.Branches.FirstAsync(x => x.IsDefault || true, ct);
        var fy = await db.FinancialYears.FirstAsync(x => x.IsCurrent, ct);
        var dt = await db.DocumentTypes.FirstAsync(x => x.Code == "HPAY", ct);
        var no = await DocumentNumbering.NextAsync(db, company.Id, branch.Id, dt.Id, fy.Id, dt.Prefix ?? "HPAY", fy.StartDate.Year, ct);

        var doc = new Document
        {
            DocumentTypeId = dt.Id, CompanyId = company.Id, BranchId = branch.Id, FinancialYearId = fy.Id,
            DocumentNo = no, DocumentDate = (dto.PaymentDate ?? DateTime.UtcNow).Date, Narration = dto.Remarks,
            Status = dto.Post ? DocumentStatus.Posted : DocumentStatus.Draft
        };
        var pay = new SalaryPayment
        {
            PayrollRunId = run.Id,
            Document = doc,
            PaymentNo = no,
            PaymentDate = (dto.PaymentDate ?? DateTime.UtcNow).Date,
            CashBankAccountId = dto.CashBankAccountId,
            Amount = run.NetTotal,
            Status = dto.Post ? DocumentStatus.Posted : DocumentStatus.Draft,
            Remarks = dto.Remarks
        };
        db.SalaryPayments.Add(pay);
        await db.SaveChangesAsync(ct);

        if (dto.Post)
            await accounting.PostSalaryPaymentAsync(pay, ct);

        return (await GetPaymentsAsync(run.Id, ct)).First(x => x.Id == pay.Id);
    }

    public async Task<IReadOnlyList<SalaryPaymentDto>> GetPaymentsAsync(Guid? runId = null, CancellationToken ct = default)
    {
        var q = db.SalaryPayments.Include(x => x.CashBankAccount).AsQueryable();
        if (runId is Guid id) q = q.Where(x => x.PayrollRunId == id);
        var rows = await q.OrderByDescending(x => x.PaymentDate).ToListAsync(ct);
        return rows.Select(x => new SalaryPaymentDto(
            x.Id, x.PaymentNo, x.PayrollRunId, x.PaymentDate, x.CashBankAccountId,
            x.CashBankAccount?.Name, x.Amount, x.Status, x.Remarks)).ToList();
    }

    private async Task EnsurePermAsync(string action, CancellationToken ct)
    {
        var code = action switch
        {
            "approve" or "verify" => "HR.Payroll.Approve",
            "post" => "HR.Payroll.Post",
            "calculate" => "HR.Payroll.Calculate",
            _ => "HR.Payroll.Create"
        };
        await permissions.EnsureAsync(code, ct);
    }

    private static void EnsurePayrollPeriodOpen(PayrollPeriod? period)
    {
        if (period is null)
            throw new InvalidOperationException("Payroll period not found.");
        if (period.Status == PayrollPeriodStatus.Closed)
            throw new InvalidOperationException("Payroll period is closed.");
    }

    private async Task<PayrollRun?> LoadRunAsync(Guid id, CancellationToken ct) =>
        await db.PayrollRuns.Include(x => x.PayrollPeriod)
            .Include(x => x.Employees).ThenInclude(e => e.Employee)
            .Include(x => x.Employees).ThenInclude(e => e.Lines).ThenInclude(l => l.PayrollComponent)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    private static PayrollRunDto Map(PayrollRun x) => new(
        x.Id, x.RunNo, x.PayrollPeriodId, x.PayrollPeriod?.Year ?? 0, x.PayrollPeriod?.Month ?? 0,
        x.RunDate, x.Status, x.GrossTotal, x.DeductionTotal, x.NetTotal, x.Remarks,
        x.Employees.Select(e => new PayrollEmployeeDto(
            e.Id, e.EmployeeId, e.Employee?.EmployeeCode, e.Employee?.Name,
            e.Basic, e.Gross, e.Deductions, e.Net,
            e.Lines.Select(l => new PayrollLineDto(
                l.Id, l.PayrollComponentId, l.PayrollComponent?.Code, l.PayrollComponent?.Name,
                l.ComponentType.ToString(), l.Amount)).ToList())).ToList());
}

/// <summary>Compatibility shim: maps old salary-sheet API onto payroll runs.</summary>
public class SalarySheetService(IPayrollService payroll) : ISalarySheetService
{
    public async Task<SalarySheetDto> GetAsync(int year, int month, CancellationToken ct = default)
    {
        var runs = await payroll.GetRunsAsync(ct);
        var run = runs.FirstOrDefault(r => r.Year == year && r.Month == month);
        if (run is null)
            return new SalarySheetDto(year, month, 0, 0, Array.Empty<SalarySheetLineDto>());
        var lines = run.Employees.Select(e => new SalarySheetLineDto(
            e.Id, e.EmployeeId, e.EmployeeName, null, year, month, e.Basic,
            e.Gross - e.Basic, e.Deductions, e.Net, run.Status.ToString(), null)).ToList();
        return new SalarySheetDto(year, month, lines.Count, run.NetTotal, lines);
    }

    public async Task<SalarySheetDto> GenerateAsync(SalaryGenerateDto dto, CancellationToken ct = default)
    {
        var periods = await payroll.GetPeriodsAsync(ct);
        var period = periods.FirstOrDefault(p => p.Year == dto.Year && p.Month == dto.Month)
            ?? await payroll.CreatePeriodAsync(new PayrollPeriodCreateDto(dto.Year, dto.Month), ct);
        var run = await payroll.CreateRunAsync(new PayrollRunCreateDto(period.Id), ct);
        run = await payroll.CalculateAsync(run.Id, ct) ?? run;
        return await GetAsync(dto.Year, dto.Month, ct);
    }

    public Task<SalarySheetLineDto?> MarkPaidAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult<SalarySheetLineDto?>(null);
}
