# Inventory valuation — Weighted Average (perpetual)

SheikhGo-Nexus-ERP Phase 3 locks **Weighted Average** costing with **perpetual** inventory GL. Softaxe reference: `trg_CalculateWeightedAverage`.

## Event matrix

| Event | Quantity | Monetary GL |
|-------|----------|-------------|
| GRN Post | Stock ↑ | **None** (qty only) |
| Purchase Invoice Post | — | Dr Inventory (1200) / Cr AP (2100); update `Product.AverageCost` |
| Delivery Order Post | Stock ↓ | Dr COGS (5100) / Cr Inventory (1200) at **pre-issue** average cost |
| Sales Invoice Post | — | Dr AR (1100) / Cr Sales (4100) — no stock |
| Customer / Supplier Payment | — | Cash/Bank ↔ AR/AP |

## Average cost on PI post (per product)

```text
NewAvg = (OldQty × OldAvg + InvoiceQty × InvoiceRate) / (OldQty + InvoiceQty)
```

- `OldQty` = on-hand quantity across warehouses **before** this invoice’s cost update (GRN already raised qty).
- If `OldQty + InvoiceQty` is zero, average cost is left unchanged (or seeded from invoice rate when previously zero).
- Demo seed: opening qty 100, `AverageCost` aligned with `PurchasePrice` (e.g. 100).

## COGS on delivery post

Uses the product’s **current** `AverageCost` × delivered qty **before** any cost change on that posting. Stock quantity decreases; inventory GL credit = COGS debit.

When inventory is written off via Stock Adjustment (negative qty), Phase 4 posts:

```text
Dr Inventory Adjustment (5600)
    Cr Inventory (1200)
```

Positive adjustments reverse that pair. Delivery COGS remains unchanged (Dr 5100 / Cr 1200).

## Out of scope

FIFO, specific identification, landed-cost layers, multi-currency costing, mandatory bin-level quantity.
