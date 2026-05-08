# Eatopia Deployment and Domain Guide

This project is now prepared for a future external domain such as `example.com`.

Target URLs:

| Area | URL |
| --- | --- |
| Frontend | `https://example.com` |
| Frontend www alias | `https://www.example.com` |
| Backend API | `https://api.example.com` |
| AI | Embedded inside the Backend API by default. Use `https://ai.example.com` only if you later split AI into a standalone HTTP service. |

## Project Structure

| Part | Folder | Runtime |
| --- | --- | --- |
| Frontend | `frontend-src` | React Create React App |
| Backend API | `Eatopia/src/Eatopia.Api` | ASP.NET Core 8 Web API |
| AI engine | `ai` | Python CLI called by the backend through `PythonAiClient` |
| Database | configured in `Eatopia/src/Eatopia.Api/appsettings.json` | SQL Server through Entity Framework Core |

Current local URLs:

- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:3001`
- Backend Swagger in development: `http://localhost:3001/swagger`
- SignalR chat hub: `http://localhost:3001/hubs/chat`

## One-Command Domain Update

After buying your domain, run this from the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\set-domain.ps1 -Domain example.com
```

If you later deploy AI as a separate HTTP service:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\set-domain.ps1 -Domain example.com -UseSeparateAiService
```

The script updates:

- `frontend-src/.env.production`
- `Eatopia/src/Eatopia.Api/appsettings.Production.json`
- `Eatopia/src/Eatopia.Api/appsettings.Production.example.json`
- `.env.deploy`

## Frontend Deployment

Folder: `frontend-src`

Build command:

```bash
npm ci
npm run build
```

Output folder:

```text
frontend-src/build
```

Production environment variables:

```env
REACT_APP_API_URL=https://api.example.com
REACT_APP_API_BASE_URL=https://api.example.com/api
REACT_APP_CHAT_HUB_URL=https://api.example.com/hubs/chat
REACT_APP_GOOGLE_CLIENT_ID=YOUR_GOOGLE_CLIENT_ID
```

For static hosts such as Netlify, Vercel, Firebase Hosting, Cloudflare Pages, or similar:

- Set the root/build directory to `frontend-src`.
- Set the build command to `npm run build`.
- Set the publish/output directory to `build`.
- Add the production environment variables above.
- Configure SPA fallback so every route serves `index.html`.

Docker option:

```bash
docker build -t eatopia-frontend ./frontend-src
docker run -p 3000:80 eatopia-frontend
```

## Backend Deployment

Folder: `Eatopia/src/Eatopia.Api`

Required runtime:

- .NET 8
- SQL Server database
- Python 3 if you deploy without Docker, because the backend calls the Python AI CLI

Production environment variables:

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
API_BASE_URL=https://api.example.com
FRONTEND_URL=https://example.com
WWW_FRONTEND_URL=https://www.example.com
CORS_ALLOWED_ORIGINS=https://example.com,https://www.example.com
ConnectionStrings__DefaultConnection=Server=YOUR_SQL_SERVER;Database=Eatopia;User Id=YOUR_SQL_USER;Password=YOUR_SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true
Jwt__Key=CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS
Authentication__Google__ClientId=YOUR_GOOGLE_CLIENT_ID
Email__Username=YOUR_EMAIL
Email__Password=YOUR_APP_PASSWORD
Email__FromEmail=YOUR_EMAIL
MediaStorage__UploadsRoot=/app/uploads
AI__RootPath=/app/ai
AI__PythonExecutable=/app/ai/.venv/bin/python
AI_SERVICE_URL=
```

Important production CORS rule:

- Allowed origins must be exactly `https://example.com` and `https://www.example.com`.
- Do not use `*`.
- Do not use localhost in production.

Docker build from the project root:

```bash
docker build -f Eatopia/Dockerfile -t eatopia-api .
docker run -p 3001:8080 --env-file .env.deploy eatopia-api
```

Docker Compose:

```bash
copy .env.deploy.example .env.deploy
powershell -ExecutionPolicy Bypass -File .\scripts\set-domain.ps1 -Domain example.com
# Fill JWT_KEY, SQLSERVER_CONNECTION_STRING, SMTP, and Google values in .env.deploy.
docker compose up --build -d
```

## AI Deployment

The current AI is not a separate HTTP service. The backend exposes AI features through:

- `POST https://api.example.com/api/ai/diet-plan`
- `POST https://api.example.com/api/ai/scan`

Because the backend runs `ai/eatopia_ai_cli.py` internally, no `ai.example.com` DNS record is required right now.

Only create `ai.example.com` if you later build a standalone FastAPI/Flask service. In that future case:

- Deploy the AI service separately.
- Set `AI_SERVICE_URL=https://ai.example.com`.
- Add DNS for `ai.example.com`.
- Update the backend AI client to call the HTTP service instead of the local Python CLI.

Optional CLI image:

```bash
docker build -t eatopia-ai-cli ./ai
```

## DNS Records

Create these records at your domain provider:

| Host | Type | Target |
| --- | --- | --- |
| `example.com` | A / CNAME / ALIAS | Frontend hosting target |
| `www.example.com` | CNAME | Frontend hosting target |
| `api.example.com` | A / CNAME | Backend hosting target |
| `ai.example.com` | A / CNAME | AI hosting target, only if AI becomes a standalone service |

Use the exact DNS type and target your hosting provider gives you. Static frontend hosts often provide a CNAME target. VPS providers usually provide an IP address for an A record.

## HTTPS and SSL

Enable HTTPS for every public host:

- `https://example.com`
- `https://www.example.com`
- `https://api.example.com`
- `https://ai.example.com` only if used

Most managed hosts create certificates automatically after DNS is correct. For a VPS, use a reverse proxy such as Nginx/Caddy/Traefik and issue certificates with Let's Encrypt.

Do not point the frontend to `http://api.example.com` in production. Use HTTPS only.

## Test Checklist

After DNS and SSL are active:

```bash
curl -I https://example.com
curl -I https://www.example.com
curl https://api.example.com/swagger
```

Then test in the browser:

- Open `https://example.com`.
- Sign up or log in.
- Check activation email links open `https://example.com/activate-account`.
- Open pages that call API data.
- Upload a profile/community image and verify it loads from `https://api.example.com/uploads/...`.
- Test AI diet plan and food scan from the frontend.
- Confirm the browser console has no CORS errors.

## Localhost Audit

Localhost remains only in development/test files, examples, and historical docs:

- `frontend-src/.env`, `frontend-src/.env.development`, `frontend-src/.env.example`
- `Eatopia/src/Eatopia.Api/appsettings.json`
- `Eatopia/src/Eatopia.Api/appsettings.Development.json`
- `Eatopia/src/Eatopia.Api/Properties/launchSettings.json`
- frontend/backend tests
- old report markdown files and downloaded image source metadata

Production files use `https://example.com`, `https://www.example.com`, and `https://api.example.com` placeholders and can be updated with `scripts/set-domain.ps1`.

Remaining non-localhost `http://` entries are third-party source metadata in recipe image JSON or historical AI dataset documentation. They are not service endpoints required for production deployment.
