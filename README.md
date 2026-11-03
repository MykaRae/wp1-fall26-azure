# wp1-fall26-azure

## Service status demo

This repository contains two separate .NET 10 applications:

- `frontend` serves a small status page on port 8080. Its `/api/health` route forwards the request to the API.
- `api` returns `{"status":"healthy"}` from `/health` and supports creating database-backed service records at `POST /api/services`.

The frontend uses the `ApiBaseUrl` configuration value. When running the frontend outside Compose, it defaults to `http://localhost:8081`.

The GitHub Actions deployment builds and deploys the API first, waits for its revision, then gives the API's HTTPS URL to the frontend. The browser talks to the frontend only; the frontend server forwards the health check to the API.

## Run the API and PostgreSQL with Docker Compose

Start Docker Desktop, then run these commands from the repository root:

```powershell
docker compose up --build
```

The API is available at <http://localhost:8080/health>. PostgreSQL 17 is available on port 5432, and its data is stored in the `pgdata` named volume. Compose waits for PostgreSQL's healthcheck to pass before starting the API.

The Compose setup includes only the API and PostgreSQL. `POST /api/services` creates records in the local PostgreSQL database and returns `201 Created` with a URL for retrieving the record. Set `POSTGRES_PASSWORD` in the environment to override the local development default.

In Azure, the API uses the SQL Server connection string provided as `ConnectionStrings__DefaultConnection`; configure the GitHub `SQL_CONNECTION_STRING` secret with a SQL Server/Azure SQL connection string. The API creates `dbo.service_records` at startup, so the SQL login must be permitted to create tables. Local PostgreSQL records are not automatically copied to Azure SQL.

Stop the containers with `Ctrl+C`, then run:

```powershell
docker compose down
```

The named database volume is retained when containers are stopped or removed. To discard the stored database data, run `docker compose down --volumes`.