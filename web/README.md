# Softaxe ERP — Web

Angular frontend for Softaxe ERP (feed mills & poultry). API base: `http://localhost:5080/api`.

## Prerequisites

- Node.js 20+ (LTS recommended)
- Backend API running on port **5080**

## Install & run

```bash
cd /Users/alirazatahir/Projects/SoftaxeERP/web
npm install
npm start
```

App: [http://localhost:4200](http://localhost:4200)

Demo login: `admin@softaxe.local` / `Admin@123`

## Useful scripts

| Command        | Description              |
|----------------|--------------------------|
| `npm start`    | `ng serve` on port 4200  |
| `npm run build`| Production build         |

## Routes

- `/login`
- `/dashboard`
- `/accounts`
- `/parties`
- `/products`
- `/vouchers`
- `/sales`
- `/purchase`
- `/inventory`
- `/payroll`
- `/settings/company`

JWT is stored in `localStorage` under `softaxe_token` and attached via HTTP interceptor. HTTP 401 redirects to login.
