# Clinic POS v1

Thin vertical slice for a multi-tenant, multi-branch Clinic POS using .NET 10 + Next.js + PostgreSQL + Redis + RabbitMQ.

## Architecture overview

- Backend: ASP.NET Core minimal API in `src/backend/ClinicPos.Api`
- Frontend: Next.js app in `src/frontend/clinic-pos-web`
- Data store: PostgreSQL via EF Core migrations
- Cache: Redis for tenant-scoped patient list reads
- Messaging: RabbitMQ event publish on appointment creation

Tenant safety design:
- Tenant is derived from both `X-Tenant-Id` header and authenticated token claim `tenant_id`
- Request is rejected if header, token, and payload/query tenant do not match
- Every patient and appointment read/write is filtered by `TenantId`
- Cache keys are tenant-scoped: `tenant:{tenantId}:patients:list:{branch|all}:v:{version}`

## Implemented scope

Completed mandatory sections:
- Section A: Create/List Patient with validation and consistent errors
- Section B: Token auth + roles + policy enforcement + user management + seeder

Completed optional sections:
- Section C: Create appointment + duplicate prevention + RabbitMQ event
- Section D: Cache list patients + tenant-scoped keys + invalidation via version bump

Also included from Section E:
- E2 tenant isolation strategy documented in this README

## Assumptions and trade-offs

- Simple bearer token auth is used for speed and clarity
- Seeder auto-runs when database is empty
- Viewer has `patients:view` only and cannot create patient
- Branch access is enforced for non-admin users
- RabbitMQ consumer is intentionally omitted

## One-command run

From repository root:

```bash
docker compose up --build
```

or

```bash
./run.sh
```

Detached mode:

```bash
./run.sh --detached
```

Stop stack:

```bash
./run.sh --down
```

Services:
- Frontend: http://localhost:3000
- Backend: http://localhost:8080
- RabbitMQ UI: http://localhost:15672

Migrations are applied automatically on backend startup by hosted initializer.

## Environment variables

See `.env.example` for defaults. Main runtime variables:
- `ConnectionStrings__Postgres`
- `ConnectionStrings__Redis`
- `RabbitMQ__Host`
- `RabbitMQ__Port`
- `RabbitMQ__Username`
- `RabbitMQ__Password`
- `RabbitMQ__Exchange`
- `NEXT_PUBLIC_API_BASE_URL`

## Seeded users and login

Seeded tenant and branches:
- Tenant: `11111111-1111-1111-1111-111111111111`
- Branch A: `22222222-2222-2222-2222-222222222222`
- Branch B: `33333333-3333-3333-3333-333333333333`

Seeded users:
- Admin: email `admin@demo.local`, token `admin-token`
- User: email `user@demo.local`, token `user-token`
- Viewer: email `viewer@demo.local`, token `viewer-token`

Use token as `Authorization: Bearer <token>` and always include `X-Tenant-Id`.

## API examples

Create patient:

```bash
curl -X POST http://localhost:8080/api/patients \
  -H "Authorization: Bearer admin-token" \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName":"Somchai",
    "lastName":"Sukjai",
    "phoneNumber":"0812345678",
    "tenantId":"11111111-1111-1111-1111-111111111111",
    "primaryBranchId":"22222222-2222-2222-2222-222222222222"
  }'
```

List patients:

```bash
curl "http://localhost:8080/api/patients?tenantId=11111111-1111-1111-1111-111111111111" \
  -H "Authorization: Bearer admin-token" \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111"
```

Create appointment:

```bash
curl -X POST http://localhost:8080/api/appointments \
  -H "Authorization: Bearer admin-token" \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId":"11111111-1111-1111-1111-111111111111",
    "branchId":"22222222-2222-2222-2222-222222222222",
    "patientId":"PUT_PATIENT_ID_HERE",
    "startAt":"2026-02-15T10:30:00+07:00"
  }'
```

Create user:

```bash
curl -X POST http://localhost:8080/api/users \
  -H "Authorization: Bearer admin-token" \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId":"11111111-1111-1111-1111-111111111111",
    "email":"newuser@demo.local",
    "role":"User",
    "branchIds":["22222222-2222-2222-2222-222222222222"]
  }'
```

## Tests

Backend tests:

```bash
cd src/backend
dotnet test ClinicPos.slnx
```

Frontend smoke test:

```bash
cd src/frontend/clinic-pos-web
npm test -- --runInBand
```

Included tests:
- Backend: tenant scoping enforcement on patient list
- Backend: duplicate phone prevention within same tenant
- Frontend: smoke render test for core page actions
