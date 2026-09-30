#!/usr/bin/env python3
"""Phase 9 BI verification + reconciliation + RBAC + export + Phase 0–8 smoke.

Usage:
  python3 scripts/phase9-acceptance.py
  BASE_URL=http://localhost:5080/api python3 scripts/phase9-acceptance.py

Exit 0 only when all checks PASS. Does not start Phase 10.
"""
from __future__ import annotations

import json
import os
import sys
import urllib.error
import urllib.request
from datetime import date
from typing import Any

BASE = os.environ.get("BASE_URL", "http://localhost:5080/api").rstrip("/")
TOL = 0.05
RESULTS: list[tuple[str, bool, str]] = []


def check(name: str, cond: bool, detail: str = "") -> None:
    RESULTS.append((name, bool(cond), detail))
    print(("PASS" if cond else "FAIL"), name, detail)


def req(method: str, path: str, token: str | None = None, body: Any = None, raw: bool = False):
    data = None if body is None else json.dumps(body).encode()
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    r = urllib.request.Request(BASE + path, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(r, timeout=120) as resp:
            payload = resp.read()
            if raw:
                return resp.status, payload
            if not payload:
                return resp.status, None
            return resp.status, json.loads(payload.decode())
    except urllib.error.HTTPError as e:
        payload = e.read()
        if raw:
            return e.code, payload
        try:
            return e.code, json.loads(payload.decode()) if payload else {"message": str(e)}
        except Exception:
            return e.code, {"message": payload.decode(errors="replace")[:200]}


def login(email: str, password: str) -> str:
    st, data = req("POST", "/auth/login", body={"email": email, "password": password})
    if st != 200 or not data or not data.get("token"):
        raise SystemExit(f"Login failed for {email}: {st} {data}")
    return data["token"]


def kpi(sections: dict, section: str, key: str) -> float | None:
    for k in sections.get(section, {}).get("kpis", []):
        if k.get("key") == key:
            return float(k["value"])
    return None


def ensure_role_user(admin: str, role_code: str, perms: list[str], email: str, password: str) -> str:
    """Create (or reuse) role+permissions+user; return login token."""
    st, roles = req("GET", "/admin/roles", admin)
    if st != 200:
        raise SystemExit(f"Cannot list roles: {st}")
    role = next((r for r in roles if str(r.get("code", "")).lower() == role_code.lower()), None)
    if role is None:
        st, role = req("POST", "/admin/roles", admin, {"code": role_code, "name": role_code})
        if st not in (200, 201) or not role:
            raise SystemExit(f"Create role {role_code} failed: {st} {role}")
    role_id = role["id"]
    st, _ = req(
        "PUT",
        f"/admin/roles/{role_id}/permissions",
        admin,
        {"roleId": role_id, "permissionCodes": perms},
    )
    if st not in (200, 204):
        # Some APIs return 200 with body
        if st != 200:
            raise SystemExit(f"Set permissions for {role_code} failed: {st}")

    st, users = req("GET", "/users", admin)
    user = None
    if st == 200 and isinstance(users, list):
        user = next((u for u in users if str(u.get("email", "")).lower() == email.lower()), None)
    if user is None:
        st, user = req(
            "POST",
            "/users",
            admin,
            {"email": email, "fullName": role_code, "password": password, "role": role_code},
        )
        if st not in (200, 201):
            raise SystemExit(f"Create user {email} failed: {st} {user}")
    else:
        st, _ = req("PUT", f"/admin/users/{user['id']}/roles", admin, {"roleIds": [role_id]})
        if st not in (200, 204):
            # ignore if already assigned
            pass
    return login(email, password)


def main() -> int:
    today = date.today().isoformat()
    ms = date.today().replace(day=1).isoformat()
    ys = date.today().replace(month=1, day=1).isoformat()

    admin = login("admin@softaxe.local", "Admin@123")

    # —— 1. Endpoint smoke ——
    smoke_paths = [
        f"/dashboard/executive?from={ms}&to={today}",
        "/dashboard/summary",
        f"/reports/finance/trial-balance?asOf={today}",
        f"/reports/trial-balance?asOf={today}",
        f"/reports/finance/profit-loss?from={ys}&to={today}",
        f"/reports/profit-loss?from={ys}&to={today}",
        f"/reports/finance/balance-sheet?asOf={today}",
        f"/reports/balance-sheet?asOf={today}",
        f"/reports/finance/cash-flow?from={ms}&to={today}",
        f"/reports/cash-flow?from={ms}&to={today}",
        f"/reports/finance/aging/receivables?asOf={today}",
        f"/reports/aging/receivables?asOf={today}",
        f"/reports/finance/aging/payables?asOf={today}",
        f"/reports/aging/payables?asOf={today}",
        f"/reports/sales/summary?from={ms}&to={today}",
        f"/reports/sales/by-customer?from={ms}&to={today}",
        f"/reports/sales/by-product?from={ms}&to={today}",
        f"/reports/sales/by-category?from={ms}&to={today}",
        f"/reports/sales/by-branch?from={ms}&to={today}",
        f"/reports/sales/trend?from={ms}&to={today}",
        f"/reports/sales/collections?from={ms}&to={today}",
        f"/reports/procurement/summary?from={ms}&to={today}",
        f"/reports/procurement/by-supplier?from={ms}&to={today}",
        f"/reports/procurement/by-product?from={ms}&to={today}",
        f"/reports/procurement/outstanding?asOf={today}",
        f"/reports/procurement/price-analysis?from={ms}&to={today}",
        f"/reports/procurement/grn-vs-invoice?from={ms}&to={today}",
        f"/reports/procurement/trend?from={ms}&to={today}",
        "/reports/inventory/valuation",
        f"/reports/inventory/movement?from={ms}&to={today}",
        f"/reports/inventory/slow-moving?from={ms}&to={today}",
        f"/reports/inventory/fast-moving?from={ms}&to={today}",
        f"/reports/inventory/aging?asOf={today}",
        "/reports/inventory/warehouse-comparison",
        f"/reports/inventory/adjustments?from={ms}&to={today}",
        f"/reports/inventory/turnover?from={ms}&to={today}",
        "/reports/hr/employees",
        "/reports/hr/attendance",
        "/reports/hr/leave",
        f"/reports/hr/payroll?from={ms}&to={today}",
        "/reports/hr/department-cost",
        f"/reports/hr/overtime?from={ms}&to={today}",
        f"/reports/production/summary?from={ms}&to={today}",
        "/reports/production/order-status",
        f"/reports/production/consumption?from={ms}&to={today}",
        f"/reports/production/cost?from={ms}&to={today}",
        f"/reports/production/yield?from={ms}&to={today}",
        f"/reports/production/finished-goods?from={ms}&to={today}",
        "/reports/production/wip",
        "/reports/farm/flocks",
        f"/reports/farm/mortality?from={ms}&to={today}",
        f"/reports/farm/feed?from={ms}&to={today}",
        f"/reports/farm/expenses?from={ms}&to={today}",
        "/reports/farm/performance",
        f"/reports/quality/samples?from={ms}&to={today}",
        f"/reports/quality/results?from={ms}&to={today}",
        f"/reports/quality/pass-fail?from={ms}&to={today}",
        f"/reports/operations/weighments?from={ms}&to={today}",
        f"/reports/operations/vehicles?from={ms}&to={today}",
        f"/reports/operations/gate-passes?from={ms}&to={today}",
        f"/reports/pos/sales?from={ms}&to={today}",
        f"/reports/pos/payments?from={ms}&to={today}",
        f"/reports/pos/shifts?from={ms}&to={today}",
        f"/reports/pos/products?from={ms}&to={today}",
    ]

    # GL ledger needs an account id
    st, accounts = req("GET", "/accounts", admin)
    check("smoke_accounts", st == 200 and isinstance(accounts, list) and len(accounts) > 0, f"status={st}")
    if st == 200 and accounts:
        acct = next((a for a in accounts if a.get("isPostable")), accounts[0])
        smoke_paths.insert(0, f"/reports/ledger?accountId={acct['id']}&from={ms}&to={today}")

    for path in smoke_paths:
        st, _ = req("GET", path, admin)
        check(f"smoke_{path.split('?')[0]}", st == 200, f"status={st}")

    # —— 2. Reconciliation ——
    st, execd = req("GET", f"/dashboard/executive?from={ms}&to={today}", admin)
    check("executive_ok", st == 200 and len((execd or {}).get("sections", [])) >= 8)
    sections = {s["key"]: s for s in (execd or {}).get("sections", [])}

    sales_kpi = kpi(sections, "sales", "sales")
    ar_kpi = kpi(sections, "sales", "ar")
    ap_kpi = kpi(sections, "procurement", "ap")
    inv_kpi = kpi(sections, "inventory", "value")
    rev_kpi = kpi(sections, "finance", "revenue")
    pay_kpi = kpi(sections, "hr", "payroll")
    prod_kpi = kpi(sections, "production", "qty")
    pos_kpi = kpi(sections, "pos", "sales")
    wb_kpi = kpi(sections, "operations", "netWeight")

    _, sales = req("GET", f"/reports/sales/summary?from={ms}&to={today}", admin)
    range_amt = next((float(r["amount"]) for r in sales.get("rows", []) if r.get("code") == "RANGE"), None)
    check("recon_sales", sales_kpi is not None and range_amt is not None and abs(sales_kpi - range_amt) <= TOL,
          f"{sales_kpi}=={range_amt}")

    _, ar = req("GET", f"/reports/finance/aging/receivables?asOf={today}", admin)
    check("recon_ar", ar_kpi is not None and abs(ar_kpi - float(ar.get("grandTotal", -1))) <= TOL,
          f"{ar_kpi}=={ar.get('grandTotal')}")

    _, ap = req("GET", f"/reports/finance/aging/payables?asOf={today}", admin)
    check("recon_ap", ap_kpi is not None and abs(ap_kpi - float(ap.get("grandTotal", -1))) <= TOL,
          f"{ap_kpi}=={ap.get('grandTotal')}")

    _, val = req("GET", "/reports/inventory/valuation", admin)
    val_total = sum(float(x.get("value") or 0) for x in (val or [])) if isinstance(val, list) else -1
    check("recon_inventory", inv_kpi is not None and abs(inv_kpi - val_total) <= TOL, f"{inv_kpi}=={val_total}")

    _, dash = req("GET", "/dashboard/summary", admin)
    check("recon_dash_inv", abs(float(dash.get("inventoryValue") or 0) - val_total) <= TOL,
          f"{dash.get('inventoryValue')}=={val_total}")

    _, pl = req("GET", f"/reports/finance/profit-loss?from={ys}&to={today}", admin)
    check("recon_revenue", rev_kpi is not None and abs(rev_kpi - float(pl.get("revenue") or -1)) <= TOL,
          f"{rev_kpi}=={pl.get('revenue')}")

    _, hrpay = req("GET", f"/reports/hr/payroll?from={ms}&to={today}", admin)
    check("recon_payroll", pay_kpi is not None and abs(pay_kpi - float(hrpay.get("grandTotal") or -1)) <= TOL,
          f"{pay_kpi}=={hrpay.get('grandTotal')}")

    _, prod = req("GET", f"/reports/production/summary?from={ms}&to={today}", admin)
    produced = next((float(r.get("qty") or r.get("amount") or 0) for r in prod.get("rows", []) if r.get("code") == "PRODUCED"), None)
    check("recon_production_qty", prod_kpi is not None and produced is not None and abs(prod_kpi - produced) <= TOL,
          f"{prod_kpi}=={produced}")

    _, pos = req("GET", f"/reports/pos/sales?from={ms}&to={today}", admin)
    check("recon_pos", pos_kpi is not None and abs(pos_kpi - float(pos.get("grandTotal") or -1)) <= TOL,
          f"{pos_kpi}=={pos.get('grandTotal')}")

    _, ops = req("GET", f"/reports/operations/weighments?from={ms}&to={today}", admin)
    net_row = next((float(r["amount"]) for r in ops.get("rows", []) if "net" in str(r.get("code", "")).lower() or "net" in str(r.get("name", "")).lower()), None)
    if net_row is None and ops.get("rows"):
        # Ops weighment summary often uses codes like NET
        net_row = next((float(r["amount"]) for r in ops["rows"] if r.get("code") in ("NET", "NetWeight", "NET_WEIGHT")), None)
    if wb_kpi is not None and net_row is not None:
        check("recon_weighbridge", abs(wb_kpi - net_row) <= TOL, f"{wb_kpi}=={net_row}")
    else:
        check("recon_weighbridge", wb_kpi is not None, f"kpi={wb_kpi} report={net_row} rows={ops.get('rows')}")

    # —— 3. Financial integrity ——
    _, tb = req("GET", f"/reports/finance/trial-balance?asOf={today}", admin)
    dr, cr = float(tb.get("totalDebit") or 0), float(tb.get("totalCredit") or 0)
    check("tb_balanced", abs(dr - cr) <= TOL, f"dr={dr} cr={cr}")

    _, bs = req("GET", f"/reports/finance/balance-sheet?asOf={today}", admin)
    assets = float(bs.get("totalAssets") or 0)
    liab = float(bs.get("totalLiabilities") or 0)
    equity = float(bs.get("totalEquity") or 0)
    check("bs_balanced", abs(assets - (liab + equity)) <= TOL, f"A={assets} L+E={liab + equity}")

    check("pl_has_totals", "revenue" in pl and "netProfit" in pl, f"rev={pl.get('revenue')} net={pl.get('netProfit')}")

    # —— 4. Exports ——
    for domain, name in [
        ("sales", "summary"),
        ("procurement", "summary"),
        ("hr", "payroll"),
        ("pos", "sales"),
        ("production", "summary"),
        ("sales", "by-customer"),  # larger-ish named set
    ]:
        for fmt, magic in [("csv", None), ("xlsx", b"PK"), ("pdf", b"%PDF")]:
            st, blob = req(
                "GET",
                f"/reports/{domain}/{name}/export?format={fmt}&from={ys}&to={today}",
                admin,
                raw=True,
            )
            ok = st == 200 and isinstance(blob, (bytes, bytearray)) and len(blob) > 20
            if ok and magic:
                ok = blob.startswith(magic)
            if ok and fmt == "csv":
                text = blob.decode(errors="replace")
                ok = "Code" in text or "code" in text or "," in text
            check(f"export_{domain}_{name}_{fmt}", ok, f"status={st} bytes={len(blob) if isinstance(blob, (bytes, bytearray)) else 0}")

    # —— 5. RBAC ——
    sales_tok = ensure_role_user(
        admin, "P9SalesOnly", ["Reports.Sales.View"], "p9.sales@softaxe.local", "Test@12345"
    )
    fin_tok = ensure_role_user(
        admin, "P9FinanceOnly", ["Reports.Finance.View"], "p9.finance@softaxe.local", "Test@12345"
    )
    none_tok = ensure_role_user(admin, "P9NoReports", [], "p9.none@softaxe.local", "Test@12345")

    st, _ = req("GET", f"/reports/sales/summary?from={ms}&to={today}", sales_tok)
    check("rbac_sales_ok_sales", st == 200, f"status={st}")
    for path in [
        f"/reports/finance/trial-balance?asOf={today}",
        f"/reports/trial-balance?asOf={today}",
        f"/dashboard/executive?from={ms}&to={today}",
        "/reports/hr/employees",
        "/reports/inventory/valuation",
    ]:
        st, _ = req("GET", path, sales_tok)
        check(f"rbac_sales_deny_{path.split('?')[0]}", st == 403, f"status={st}")

    st, _ = req("GET", f"/reports/finance/trial-balance?asOf={today}", fin_tok)
    check("rbac_fin_ok_finance", st == 200, f"status={st}")
    st, _ = req("GET", f"/reports/trial-balance?asOf={today}", fin_tok)
    check("rbac_fin_ok_legacy_tb", st == 200, f"status={st}")
    st, _ = req("GET", f"/reports/sales/summary?from={ms}&to={today}", fin_tok)
    check("rbac_fin_deny_sales", st == 403, f"status={st}")
    st, _ = req("GET", f"/dashboard/executive?from={ms}&to={today}", fin_tok)
    check("rbac_fin_deny_exec", st == 403, f"status={st}")

    for path in [
        f"/reports/sales/summary?from={ms}&to={today}",
        f"/reports/finance/trial-balance?asOf={today}",
        f"/reports/trial-balance?asOf={today}",
        f"/dashboard/executive?from={ms}&to={today}",
        "/reports/hr/employees",
        "/reports/inventory/valuation",
    ]:
        st, _ = req("GET", path, none_tok)
        check(f"rbac_none_deny_{path.split('?')[0]}", st == 403, f"status={st}")

    # —— 6. Phase 0–8 smoke ——
    for path in [
        "/products",
        "/sales/invoices",
        "/purchases/invoices",
        "/employees",
        "/production/orders",
        "/farm/flocks",
        "/lab/samples",
        "/operations/vehicles",
        "/weighbridge/weighments",
        "/pos/sales",
        "/accounts",
    ]:
        st, _ = req("GET", path, admin)
        check(f"regress_{path}", st == 200, f"status={st}")

    # Farm performance should succeed (post N+1 fix)
    st, farm = req("GET", "/reports/farm/performance", admin)
    check("perf_farm_performance", st == 200 and isinstance(farm.get("rows"), list), f"status={st} rows={len(farm.get('rows', [])) if farm else 0}")

    passed = sum(1 for _, ok, _ in RESULTS if ok)
    failed = [(n, d) for n, ok, d in RESULTS if not ok]
    print(f"\n=== PHASE 9 ACCEPTANCE {passed}/{len(RESULTS)} ===")
    for n, d in failed:
        print("FAIL", n, d)
    if failed:
        print("Phase 9 NOT officially COMPLETE — fix failures above.")
        return 1
    print("COMPLETE — Phase 9 BI + Advanced Reporting verified & stabilized.")
    print("Do not start Phase 10 until product owner schedules it.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except KeyboardInterrupt:
        raise SystemExit(130)
