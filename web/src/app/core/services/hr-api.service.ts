import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface Department {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
}

export interface Designation {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
}

export interface PayrollComponent {
  id: string;
  code: string;
  name: string;
  componentType: string;
  isSystem: boolean;
  glMappingKey?: string;
}

export interface PayrollProfileLine {
  id?: string;
  componentId: string;
  componentCode?: string;
  componentName?: string;
  componentType: string;
  amount: number;
}

export interface Employee {
  id: string;
  code: string;
  name: string;
  departmentId?: string;
  department?: string;
  designationId?: string;
  designation?: string;
  branchId: string;
  branchName?: string;
  joinDate?: string;
  employmentStatus: string;
  employmentType: string;
  basicSalary: number;
  phone?: string;
  email?: string;
  isActive: boolean;
  profileLines: PayrollProfileLine[];
}

export interface AttendanceRow {
  id: string;
  date: string;
  employeeId: string;
  employeeName?: string;
  status: string;
  overtimeHours?: number;
  remarks?: string;
}

export interface AttendanceSummary {
  employeeId: string;
  employeeName?: string;
  year: number;
  month: number;
  present: number;
  absent: number;
  late: number;
  halfDay: number;
  overtime: number;
  overtimeHours: number;
}

export interface LeaveType {
  id: string;
  code: string;
  name: string;
  paid: boolean;
  annualAllowanceDays: number;
  isActive: boolean;
}

export interface LeaveBalance {
  id: string;
  employeeId: string;
  employeeName?: string;
  leaveTypeId: string;
  leaveTypeName?: string;
  year: number;
  entitled: number;
  used: number;
  remaining: number;
}

export interface LeaveRequest {
  id: string;
  docNo: string;
  employeeId: string;
  employeeName?: string;
  leaveTypeId: string;
  leaveTypeName?: string;
  fromDate: string;
  toDate: string;
  days: number;
  status: string | number;
  remarks?: string;
}

export interface PayrollPeriod {
  id: string;
  year: number;
  month: number;
  startDate: string;
  endDate: string;
  status: string;
}

export interface PayrollLine {
  id?: string;
  componentId: string;
  componentCode?: string;
  componentName?: string;
  componentType: string;
  amount: number;
}

export interface PayrollEmployee {
  id: string;
  employeeId: string;
  employeeCode?: string;
  employeeName?: string;
  basic: number;
  gross: number;
  deductions: number;
  net: number;
  lines: PayrollLine[];
}

export interface PayrollRun {
  id: string;
  runNo: string;
  periodId: string;
  year: number;
  month: number;
  runDate: string;
  status: string | number;
  grossTotal: number;
  deductionTotal: number;
  netTotal: number;
  remarks?: string;
  employees: PayrollEmployee[];
}

export interface SalaryPayment {
  id: string;
  paymentNo: string;
  payrollRunId: string;
  paymentDate: string;
  cashBankAccountId: string;
  cashBankName?: string;
  amount: number;
  status: string | number;
  remarks?: string;
}

@Injectable({ providedIn: 'root' })
export class HrApiService {
  private readonly hr = `${API_BASE_URL}/hr`;
  private readonly employees = `${API_BASE_URL}/employees`;
  private readonly attendance = `${API_BASE_URL}/attendance`;
  private readonly leaves = `${API_BASE_URL}/leaves`;
  private readonly payroll = `${API_BASE_URL}/payroll`;

  constructor(private readonly http: HttpClient) {}

  getDepartments(): Observable<Department[]> {
    return this.http.get<Department[]>(`${this.hr}/departments`);
  }
  createDepartment(body: { code: string; name: string; isActive?: boolean }): Observable<Department> {
    return this.http.post<Department>(`${this.hr}/departments`, body);
  }
  getDesignations(): Observable<Designation[]> {
    return this.http.get<Designation[]>(`${this.hr}/designations`);
  }
  createDesignation(body: { code: string; name: string; isActive?: boolean }): Observable<Designation> {
    return this.http.post<Designation>(`${this.hr}/designations`, body);
  }
  getComponents(): Observable<PayrollComponent[]> {
    return this.http.get<PayrollComponent[]>(`${this.hr}/components`);
  }

  getEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(this.employees);
  }
  createEmployee(body: unknown): Observable<Employee> {
    return this.http.post<Employee>(this.employees, body);
  }

  getAttendance(): Observable<AttendanceRow[]> {
    return this.http.get<AttendanceRow[]>(this.attendance);
  }
  createAttendance(body: {
    date: string;
    employeeId: string;
    status: string;
    overtimeHours?: number;
    remarks?: string;
  }): Observable<AttendanceRow> {
    return this.http.post<AttendanceRow>(this.attendance, body);
  }
  getAttendanceSummary(year: number, month: number): Observable<AttendanceSummary[]> {
    return this.http.get<AttendanceSummary[]>(`${this.attendance}/summary`, { params: { year, month } });
  }

  getLeaveTypes(): Observable<LeaveType[]> {
    return this.http.get<LeaveType[]>(`${this.leaves}/types`);
  }
  getLeaveBalances(): Observable<LeaveBalance[]> {
    return this.http.get<LeaveBalance[]>(`${this.leaves}/balances`);
  }
  getLeaves(): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(this.leaves);
  }
  createLeave(body: {
    employeeId: string;
    leaveTypeId: string;
    fromDate: string;
    toDate: string;
    remarks?: string;
  }): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(this.leaves, body);
  }
  leaveAction(id: string, action: string): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(`${this.leaves}/${id}/${action}`, {});
  }

  getPeriods(): Observable<PayrollPeriod[]> {
    return this.http.get<PayrollPeriod[]>(`${this.payroll}/periods`);
  }
  createPeriod(year: number, month: number): Observable<PayrollPeriod> {
    return this.http.post<PayrollPeriod>(`${this.payroll}/periods`, { year, month });
  }
  getRuns(): Observable<PayrollRun[]> {
    return this.http.get<PayrollRun[]>(`${this.payroll}/runs`);
  }
  getRun(id: string): Observable<PayrollRun> {
    return this.http.get<PayrollRun>(`${this.payroll}/runs/${id}`);
  }
  createRun(periodId: string, remarks?: string): Observable<PayrollRun> {
    return this.http.post<PayrollRun>(`${this.payroll}/runs`, { periodId, remarks });
  }
  calculateRun(id: string): Observable<PayrollRun> {
    return this.http.post<PayrollRun>(`${this.payroll}/runs/${id}/calculate`, {});
  }
  runAction(id: string, action: string): Observable<PayrollRun> {
    return this.http.post<PayrollRun>(`${this.payroll}/runs/${id}/${action}`, {});
  }
  createPayment(body: {
    payrollRunId: string;
    cashBankAccountId: string;
    paymentDate?: string;
    remarks?: string;
    post?: boolean;
  }): Observable<SalaryPayment> {
    return this.http.post<SalaryPayment>(`${this.payroll}/payments`, body);
  }
  getPayments(runId?: string): Observable<SalaryPayment[]> {
    return this.http.get<SalaryPayment[]>(`${this.payroll}/payments`, {
      params: runId ? { runId } : {},
    });
  }
}
