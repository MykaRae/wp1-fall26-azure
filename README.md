# wp1-fall26-azure

## Service status demo

This repository contains two separate .NET 10 applications:

- `frontend` serves a small status page on port 8080. Its `/api/health` route forwards the request to the API.
- `api` returns `{"status":"healthy"}` from `/health` on port 8080.

The frontend uses the `ApiBaseUrl` configuration value. Docker Compose sets it to `http://api:8080`, where `api` is the Compose service name. When running the frontend outside Compose, it defaults to `http://localhost:8081`.

The GitHub Actions deployment builds and deploys the API first, waits for its revision, then gives the API's HTTPS URL to the frontend. The browser talks to the frontend only; the frontend server forwards the health check to the API.

## Run with Docker Compose

Start Docker Desktop, then run these commands from the repository root:

```powershell
docker compose up --build
```

Open <http://localhost:8080> for the status page. The API is also available directly at <http://localhost:8081/health>.

Stop the containers with `Ctrl+C`, then run:

```powershell
docker compose down
```

Both containers listen on port 8080 internally, matching the Azure Container Apps deployment configuration.