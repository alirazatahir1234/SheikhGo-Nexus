#!/usr/bin/env python3
"""Sweets Shop POS — API acceptance + physical counter hardware pilot.

Usage:
  python3 scripts/sweets-pos-pilot-acceptance.py
  API_BASE=http://localhost:5080/api python3 scripts/sweets-pos-pilot-acceptance.py

Full step-by-step counter runbook (sign-off table):
  scripts/pos-hardware-pilot-checklist.md

Physical scanner / printer / shift checklist (manual on Angular /pos/terminal):
  [ ] USB HID scanner wedges into focused barcode field (F2 / autofocus recovery)
  [ ] Scanner sends Enter suffix; line adds to cart
  [ ] Unknown barcode shows error and refocuses scan box
  [ ] After clicking cart qty, next scan still lands in barcode (document key redirect)
  [ ] Window/tab refocus restores barcode focus
  [ ] Sell-by-weight item defaults to 0.25; cashier edits scale weight
  [ ] Cash tender: cash received > total shows change due
  [ ] Complete sale posts SO→DO→SI→CP; 80mm browser receipt opens
  [ ] Allow pop-ups if receipt blocked; reprint from Sales works
  [ ] Open shift → sell → return → close shift → daily report
  NOTE: Physical USB/thermal gear must be verified by Ali on the counter.
"""

from __future__ import annotations

import json
import os
import sys
import uuid
import urllib.error
import urllib.parse
import urllib.request

API = os.environ.get("API_BASE", "http://localhost:5080/api").rstrip("/")
EMAIL = os.environ.get("POS_USER", "admin@softaxe.local")
PASSWORD = os.environ.get("POS_PASS", "Admin@123")
SEED_BARCODE = "8901001000001"


def req(method: str, path: str, token: str | None = None, body: dict | None = None):
    data = None if body is None else json.dumps(body).encode()
    headers = {"Content-Type": "application/json", "Accept": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    r = urllib.request.Request(f"{API}{path}", data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(r, timeout=30) as resp:
            raw = resp.read().decode() or "null"
            return resp.status, json.loads(raw)
    except urllib.error.HTTPError as e:
        raw = e.read().decode()
        try:
            payload = json.loads(raw) if raw else None
        except json.JSONDecodeError:
            payload = raw
        return e.code, payload


def main() -> int:
    print(f"API {API}")
    code, login = req("POST", "/auth/login", body={"email": EMAIL, "password": PASSWORD})
    assert code == 200 and login and login.get("token"), f"login failed: {code} {login}"
    token = login["token"]
    print("OK login")

    code, terms = req("GET", "/pos/terminals", token)
    assert code == 200 and terms, f"terminals: {code} {terms}"
    term = terms[0]
    wh = term["warehouseId"]
    print(f"OK terminal {term.get('code')} warehouse={wh}")

    code, prod = req("GET", f"/products/barcode/{SEED_BARCODE}?warehouseId={urllib.parse.quote(wh)}", token)
    assert code == 200 and prod and prod.get("barcode") == SEED_BARCODE, f"barcode: {code} {prod}"
    print(f"OK barcode lookup {prod['name']} stock={prod['stockQty']}")

    code, bad = req("GET", "/products/barcode/9999999999999", token)
    assert code == 404, f"unknown barcode expected 404 got {code} {bad}"
    print("OK unknown barcode 404")

    code, shifts = req("GET", "/pos/shifts", token)
    assert code == 200
    open_shift = next((s for s in (shifts or []) if s.get("status") == "Open"), None)
    if not open_shift:
        code, open_shift = req(
            "POST",
            "/pos/shifts/open",
            token,
            {"terminalId": term["id"], "openingCash": 5000},
        )
        assert code == 200, f"open shift: {code} {open_shift}"
    print(f"OK shift {open_shift['shiftNo']}")

    idem = f"pilot-{uuid.uuid4()}"
    qty = 0.25
    amount = round(qty * float(prod["salePrice"]), 2)
    payload = {
        "shiftId": open_shift["id"],
        "discount": 0,
        "idempotencyKey": idem,
        "lines": [{"productId": prod["id"], "qty": qty, "rate": prod["salePrice"], "amount": amount}],
        "payments": [{"tender": "Cash", "amount": amount}],
    }
    code, sale1 = req("POST", "/pos/sales", token, payload)
    assert code == 200 and sale1.get("salesOrderId") and sale1.get("salesInvoiceId"), f"sale: {code} {sale1}"
    print(f"OK sale {sale1['saleNo']} SO→DO→SI posted")

    code, sale2 = req("POST", "/pos/sales", token, payload)
    assert code == 200 and sale2["id"] == sale1["id"], f"idempotency: {sale1['id']} vs {sale2.get('id')}"
    print("OK idempotency replay")

    from datetime import date

    today = date.today().isoformat()
    code, daily = req("GET", f"/pos/reports/daily-sales?date={today}", token)
    assert code == 200 and daily.get("saleCount", 0) >= 1, f"daily: {code} {daily}"
    print(f"OK daily sales net={daily.get('netSales')} tickets={daily.get('saleCount')}")

    print("\nAll API checks passed. Complete the physical scanner checklist in the docstring.")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except AssertionError as e:
        print(f"FAIL: {e}", file=sys.stderr)
        sys.exit(1)
