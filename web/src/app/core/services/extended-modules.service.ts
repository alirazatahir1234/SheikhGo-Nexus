import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

@Injectable({ providedIn: 'root' })
export class ExtendedModulesService {
  constructor(private readonly http: HttpClient) {}

  // Contracts
  getContracts() { return this.http.get<any[]>(`${API_BASE_URL}/contracts`); }
  createContract(payload: any) { return this.http.post<any>(`${API_BASE_URL}/contracts`, payload); }
  closeContract(id: string) { return this.http.post(`${API_BASE_URL}/contracts/${id}/close`, {}); }

  // Lab
  getLabTests() { return this.http.get<any[]>(`${API_BASE_URL}/lab/tests`); }
  upsertLabTest(payload: any) { return this.http.post<any>(`${API_BASE_URL}/lab/tests`, payload); }
  getLabResults() { return this.http.get<any[]>(`${API_BASE_URL}/lab/results`); }
  createLabResult(payload: any) { return this.http.post<any>(`${API_BASE_URL}/lab/results`, payload); }

  // Cost centres
  getCostCentres() { return this.http.get<any[]>(`${API_BASE_URL}/cost-centres`); }
  createCostCentre(payload: any) { return this.http.post<any>(`${API_BASE_URL}/cost-centres`, payload); }

  // Cheques
  getChequeBooks() { return this.http.get<any[]>(`${API_BASE_URL}/cheques/books`); }
  createChequeBook(payload: any) { return this.http.post<any>(`${API_BASE_URL}/cheques/books`, payload); }
  getPdcs() { return this.http.get<any[]>(`${API_BASE_URL}/cheques/pdc`); }
  createPdc(payload: any) { return this.http.post<any>(`${API_BASE_URL}/cheques/pdc`, payload); }
  updatePdcStatus(id: string, status: string) {
    return this.http.post<any>(`${API_BASE_URL}/cheques/pdc/${id}/status`, { status });
  }

  // Shareholders
  getShareholders(flockId?: string) {
    const q = flockId ? `?flockId=${flockId}` : '';
    return this.http.get<any[]>(`${API_BASE_URL}/farm/shareholders${q}`);
  }
  createShareholder(payload: any) { return this.http.post<any>(`${API_BASE_URL}/farm/shareholders`, payload); }

  // Salesforce
  getAreas() { return this.http.get<any[]>(`${API_BASE_URL}/salesforce/areas`); }
  createArea(payload: any) { return this.http.post<any>(`${API_BASE_URL}/salesforce/areas`, payload); }
  getSalesmen() { return this.http.get<any[]>(`${API_BASE_URL}/salesforce/salesmen`); }
  createSalesman(payload: any) { return this.http.post<any>(`${API_BASE_URL}/salesforce/salesmen`, payload); }
  getTargets() { return this.http.get<any[]>(`${API_BASE_URL}/salesforce/targets`); }
  createTarget(payload: any) { return this.http.post<any>(`${API_BASE_URL}/salesforce/targets`, payload); }

  // Demands
  getDemands() { return this.http.get<any[]>(`${API_BASE_URL}/demands`); }
  createDemand(payload: any) { return this.http.post<any>(`${API_BASE_URL}/demands`, payload); }

  // HR finance
  getAdvances() { return this.http.get<any[]>(`${API_BASE_URL}/hr-finance/advances`); }
  createAdvance(payload: any) { return this.http.post<any>(`${API_BASE_URL}/hr-finance/advances`, payload); }
  getLoans() { return this.http.get<any[]>(`${API_BASE_URL}/hr-finance/loans`); }
  createLoan(payload: any) { return this.http.post<any>(`${API_BASE_URL}/hr-finance/loans`, payload); }
  getEobi() { return this.http.get<any[]>(`${API_BASE_URL}/hr-finance/eobi`); }
  createEobi(payload: any) { return this.http.post<any>(`${API_BASE_URL}/hr-finance/eobi`, payload); }

  getWeighbridge() { return this.http.get<any[]>(`${API_BASE_URL}/weighbridge`); }
  createWeighbridge(payload: any) { return this.http.post<any>(`${API_BASE_URL}/weighbridge`, payload); }
  completeWeighbridge(id: string, secondWeight: number) {
    return this.http.post<any>(`${API_BASE_URL}/weighbridge/${id}/complete`, { secondWeight });
  }

  getPos() { return this.http.get<any[]>(`${API_BASE_URL}/pos`); }
  createPos(payload: any) { return this.http.post<any>(`${API_BASE_URL}/pos`, payload); }

  // Brands / conversions / budgets / attendance / day-close
  getBrands() { return this.http.get<any[]>(`${API_BASE_URL}/brands`); }
  createBrand(payload: any) { return this.http.post<any>(`${API_BASE_URL}/brands`, payload); }
  getConversions() { return this.http.get<any[]>(`${API_BASE_URL}/conversions`); }
  createConversion(payload: any) { return this.http.post<any>(`${API_BASE_URL}/conversions`, payload); }
  getBudgets() { return this.http.get<any[]>(`${API_BASE_URL}/budgets`); }
  createBudget(payload: any) { return this.http.post<any>(`${API_BASE_URL}/budgets`, payload); }
  approveBudget(id: string) { return this.http.post(`${API_BASE_URL}/budgets/${id}/approve`, {}); }
  getAttendance() { return this.http.get<any[]>(`${API_BASE_URL}/attendance`); }
  createAttendance(payload: any) { return this.http.post<any>(`${API_BASE_URL}/attendance`, payload); }
  getOvertime() { return this.http.get<any[]>(`${API_BASE_URL}/attendance/overtime`); }
  createOvertime(payload: any) { return this.http.post<any>(`${API_BASE_URL}/attendance/overtime`, payload); }
  getDayCloses() { return this.http.get<any[]>(`${API_BASE_URL}/day-close`); }
  closeDay(payload: any) { return this.http.post<any>(`${API_BASE_URL}/day-close`, payload); }

  getStockTransfers() { return this.http.get<any[]>(`${API_BASE_URL}/stock-transfers`); }
  createStockTransfer(payload: any) { return this.http.post<any>(`${API_BASE_URL}/stock-transfers`, payload); }

  getSaleReturns() { return this.http.get<any[]>(`${API_BASE_URL}/returns/sale`); }
  createSaleReturn(payload: any) { return this.http.post<any>(`${API_BASE_URL}/returns/sale`, payload); }
  getPurchaseReturns() { return this.http.get<any[]>(`${API_BASE_URL}/returns/purchase`); }
  createPurchaseReturn(payload: any) { return this.http.post<any>(`${API_BASE_URL}/returns/purchase`, payload); }

  getLeaves() { return this.http.get<any[]>(`${API_BASE_URL}/leaves`); }
  createLeave(payload: any) { return this.http.post<any>(`${API_BASE_URL}/leaves`, payload); }

  getSalarySheet(year: number, month: number) {
    return this.http.get<any>(`${API_BASE_URL}/salary?year=${year}&month=${month}`);
  }
  generateSalary(payload: any) { return this.http.post<any>(`${API_BASE_URL}/salary/generate`, payload); }
  paySalary(id: string) { return this.http.post<any>(`${API_BASE_URL}/salary/${id}/pay`, {}); }
}
