# Eatopia

Eatopia contains a React frontend, an ASP.NET Core backend API, and a Python AI engine used by the backend.

## Structure

| Part | Folder | Notes |
| --- | --- | --- |
| Frontend | `frontend-src` | React Create React App |
| Backend API | `Eatopia/src/Eatopia.Api` | ASP.NET Core 8 Web API |
| AI | `ai` | Python CLI embedded in the backend, not a separate public service by default |
| Database | SQL Server | EF Core connection string in backend configuration |

## Run Locally

Backend:

```bash
cd Eatopia
dotnet restore Eatopia.sln
dotnet run --project src/Eatopia.Api
```

Frontend:

```bash
cd frontend-src
npm ci
npm start
```

Local URLs:

- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:3001`
- Swagger: `http://localhost:3001/swagger`

Local frontend env files:

- `frontend-src/.env`
- `frontend-src/.env.development`

## Build

Frontend:

```bash
cd frontend-src
npm ci
npm run build
```

Output folder: `frontend-src/build`

Backend:

```bash
cd Eatopia
dotnet publish src/Eatopia.Api/Eatopia.Api.csproj -c Release -o ../artifacts/api
```

Docker:

```bash
docker build -f Eatopia/Dockerfile -t eatopia-api .
docker build -t eatopia-frontend ./frontend-src
```

## Prepare Domain

When you buy a domain, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\set-domain.ps1 -Domain example.com
```

This updates production settings for:

- `https://example.com`
- `https://www.example.com`
- `https://api.example.com`

The current AI runs inside the backend, so `ai.example.com` is not required unless you later split AI into a standalone HTTP service.

## Deploy

Frontend:

- Build command: `npm run build`
- Publish folder: `frontend-src/build`
- Required env: `REACT_APP_API_URL`, `REACT_APP_API_BASE_URL`, `REACT_APP_CHAT_HUB_URL`, `REACT_APP_GOOGLE_CLIENT_ID`

Backend:

- Deploy `Eatopia/src/Eatopia.Api` or use `Eatopia/Dockerfile`.
- Set `ASPNETCORE_ENVIRONMENT=Production`.
- Set SQL Server, JWT, Google, email, frontend URL, and CORS environment variables.

See `DEPLOYMENT_DOMAIN_GUIDE.md` for exact DNS records, HTTPS setup, and test steps.

## Values You Must Provide

- Domain name, for example `example.com`
- SQL Server production connection string
- Strong `Jwt__Key`
- Google OAuth Client ID
- SMTP username/password or app password
- Hosting targets for frontend and backend DNS records

Domain values go in:

- `frontend-src/.env.production`
- `Eatopia/src/Eatopia.Api/appsettings.Production.json`
- `.env.deploy` if using Docker Compose

Use `scripts/set-domain.ps1` to update them automatically.
