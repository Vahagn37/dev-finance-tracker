# Finance Tracker API

A production-minded ASP.NET Core API built step by step as a learning project.

## Run locally

```bash
docker compose up -d postgres
dotnet run --project src/FinanceTracker.Api
```

Visit `http://localhost:5178/health` after the API starts.

## Run the complete stack in containers

```bash
colima start
docker compose up --build
```

The containerized API is available at `http://localhost:5178` and applies development migrations automatically. Health endpoints are:

```text
/health       General health status
/health/live  Process liveness (does not require PostgreSQL)
/health/ready  Database readiness
```

Stop the stack with:

```bash
docker compose down
```

To apply the checked-in database migration during local development, set `Database:ApplyMigrations` to `true` in a local configuration override, or run the EF command directly:

```bash
DOTNET_ROOT=/opt/homebrew/Cellar/dotnet/10.0.401/libexec \
  ~/.dotnet/tools/dotnet-ef database update \
  --project src/FinanceTracker.Api \
  --startup-project src/FinanceTracker.Api
```

## Build plan

1. Project skeleton and health check ✅
2. PostgreSQL and EF Core
3. User registration and JWT authentication
4. Categories and transactions
5. Budgets and monthly reports
6. Tests, Docker, logging, and deployment
