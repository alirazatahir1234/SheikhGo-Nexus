# Nexus Owner (Flutter)

Owner mobile app for SheikhGo Nexus POS / sweets retail.

- Login via `POST /api/auth/login`
- Daily sales: `GET /api/pos/reports/daily-sales`
- COGS-aware profit: `GET /api/pos/reports/owner-profit`
- Low-stock alerts from `GET /api/products`

SQL Server remains the system of record; this app only caches the JWT.

```bash
flutter run \
  --dart-define=API_BASE=http://10.0.2.2:5080/api
```

Use `http://localhost:5080/api` on desktop/web, or your LAN IP for a physical device.
