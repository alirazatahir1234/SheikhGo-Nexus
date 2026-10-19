# POS hardware pilot checklist (counter)

Software-side pilot for USB HID barcode scanner + browser thermal print (80mm).  
**Physical hardware must be verified by Ali on the counter** — this checklist is the runbook.

API smoke: `python3 scripts/sweets-pos-pilot-acceptance.py`

Prerequisites: Angular at `/pos/terminal`, open shift, pop-ups allowed for the site origin, scanner in HID keyboard wedge mode.

## Scanner

1. Click the barcode field once (or press **F2**). Confirm the caret is in the scan box.
2. Scan a known product barcode. Scanner should send digits + **Enter** suffix → line adds to cart (or weight dialog opens).
3. Scan an **unknown** barcode → red error “Unknown barcode…”, scan box clears and refocuses.
4. Click a cart qty field, then scan again without clicking the scan box → wedge keys should still land in barcode (document key redirect) and add the item.
5. Alt-tab away and back (or click another app, then return) → barcode field refocuses within ~1s.
6. **Sell-by-weight** item: dialog defaults to **0.25**; edit to scale weight (e.g. 0.350) → confirm adds correct qty × rate.
7. Touch / tablet: on-screen keyboard should **not** pop for the scan field (`inputmode=none`).

## Tender & change

8. Add a cash sale; enter cash received above total → **change** shows correctly.
9. Complete sale → SO→DO→SI→CP posts; notice shows sale number.

## Receipt printer (browser → 80mm)

10. After sale, a receipt window should open and trigger print. If blocked: allow pop-ups for this site, then use **Receipt** on the sale row to **reprint**.
11. Confirm print CSS is ~80mm wide (thermal roll). Close the print dialog; POS scan field refocuses.
12. Reprint from Sales tab works for the last ticket.

## Shift close

13. Open shift → sell at least one cash ticket → optional return → **Close shift** with counted drawer cash.
14. Variance = actual − expected; daily report on Reports tab matches tickets.

## Sign-off

| Check | Pass? | Notes |
|-------|-------|-------|
| Scanner Enter suffix | | |
| Unknown barcode | | |
| Weight qty | | |
| Cash change | | |
| Receipt pop-up / reprint | | |
| Shift close | | |

Tester: _____________  Date: _____________
