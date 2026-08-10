# NurseryCam — Nursery Camera Monitoring System

NurseryCam lets a parent check on their child's attendance and, only while the
child is checked in, view a short-lived, authorized session on their
nursery's camera. Nursery staff and admins manage rooms, cameras, attendance,
and can review an audit trail of every sensitive action.

The system is split into four pieces:

| Component | Path | Description |
|---|---|---|
| API | `src/NurseryCamera.Api` (+ `Application`/`Infrastructure`/`Domain`) | ASP.NET Core 9 REST API, JWT auth, SignalR hub, EF Core + SQL Server, Redis |
| Parent app | `frontend/parent` | Angular 19 standalone app for parents |
| Admin app | `frontend/admin` | Angular 19 standalone app for nursery staff/admins |
| Reverse proxy (optional) | `docker/nginx/reverse-proxy.conf` | Single entry point fronting both SPAs + API/hub |

## Demo credentials

The API seeds a demo nursery on first run (Development environment only). Use
these to sign in once everything is running:

| Role | Email | Password |
|---|---|---|
| Parent | `parent@demo-nursery.local` | `Passw0rd!123` |
| Admin | `admin@demo-nursery.local` | `Passw0rd!123` |

## Running with Docker (recommended)

Requires Docker Desktop (or another Docker Engine + Compose v2).

```bash
cp .env.example .env
# Edit .env if you want different values - the defaults work for local dev.

docker compose up --build
```

This starts:

- `sqlserver` — SQL Server 2022, port `1433`
- `redis` — Redis 7, internal only
- `api` — the REST API + SignalR hub, port `8080` (applies EF Core migrations
  and seeds the demo data automatically on startup)
- `frontend-parent` — the parent SPA served by nginx, port `4200`
- `frontend-admin` — the admin SPA served by nginx, port `4300`

Once the containers are healthy:

- Parent app: <http://localhost:4200>
- Admin app: <http://localhost:4300>
- API/Swagger: <http://localhost:8080/swagger>
- Health checks: <http://localhost:8080/health>

To also start the optional single-port reverse proxy:

```bash
docker compose --profile proxy up --build
```

- Parent app + API + hub: <http://localhost>
- Admin app: <http://localhost:81> (kept on its own port since the Angular
  build uses an absolute base href of `/`)

To stop everything: `docker compose down` (add `-v` to also drop the SQL
Server/Redis volumes and start fresh next time).

## Running locally (without Docker)

### 1. API

Requires the .NET 9 SDK, and a reachable SQL Server instance (a quick option
is `docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=Your_strong_Password123! -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest`).

```bash
cd src/NurseryCamera.Api
dotnet restore
dotnet run
```

The API listens on the URLs from `Properties/launchSettings.json` (HTTPS by
default in `Development`). It applies pending EF Core migrations and seeds
the demo data automatically the first time it starts against an empty
database. Update `appsettings.Development.json` / environment variables if
your SQL Server, Redis, or JWT signing key differ from the defaults.

> `CameraSecurity:EncryptionKeyReference` must be a base64-encoded 32-byte
> key (`openssl rand -base64 32`) or camera secret encryption will fail to
> start.

### 2. Parent app

Requires Node.js 20+.

```bash
cd frontend/parent
npm install
npm start
```

Serves on <http://localhost:4200> by default and calls the API at the
`apiUrl`/`hubUrl` configured in `src/environments/environment.ts`
(`http://localhost:8080/api` / `http://localhost:8080/hubs/nursery`). Update
those if your API runs elsewhere.

### 3. Admin app

```bash
cd frontend/admin
npm install
npm start
```

Serves on <http://localhost:4300> (see `angular.json` — the dev server port
is set to avoid colliding with the parent app) and points at the same API by
default.

## Architecture summary

- **Parent app** (`frontend/parent`) — Angular 19 standalone components,
  functional route guards/interceptors, signals for local state. Shows the
  parent's children, each child's attendance state, and — only while a child
  is checked in and camera viewing is enabled for that parent — the cameras
  available in that child's room. Starting a viewing session calls the API
  for a short-lived session + stream token, then the parent app opens a
  **WebRTC** connection to the media gateway (never to RTSP). A "Stop
  viewing" button and an automatic stop on the `ChildCheckedOut` SignalR
  event both end the session.
- **Admin app** (`frontend/admin`) — Angular 19 standalone app for staff/
  admins: manage rooms and cameras, look up a child by ID to check them in
  or out, and browse the audit log with basic filtering/pagination.
- **API** (`src/NurseryCamera.Api` + `Application`/`Infrastructure`/`Domain`)
  — ASP.NET Core 9, Clean Architecture (Domain → Application → Infrastructure
  → Api), MediatR for CQRS-style handlers, EF Core + SQL Server for
  persistence, Redis for caching/session-adjacent state, SignalR
  (`/hubs/nursery`) for real-time push events (`ChildCheckedIn`,
  `ChildCheckedOut`, `ViewingSessionRevoked`, `CameraStatusChanged`,
  `NotificationCreated`), JWT bearer auth with short-lived access tokens +
  refresh tokens, and IP-based rate limiting on sensitive endpoints (login,
  viewing-session start).
- **Media** — `NurseryCamera.MediaGateway` + `go2rtc`. The browser posts an
  SDP offer + stream token to the media gateway. The gateway validates the
  token with the API (`/api/internal/stream/resolve`), registers a private
  source on go2rtc, and returns an SDP answer. Placeholder/demo cameras use
  an ffmpeg test pattern; real RTSP cameras stay on the private network.
- **Data** — SQL Server holds nurseries, rooms, cameras, children, parents,
  attendance, viewing sessions, and audit logs. Redis backs caching and
  short-lived tokens. Camera RTSP URLs/credentials are encrypted at rest
  (AES-256-GCM) and are never returned by any parent-facing API response.

### Security note: RTSP is never exposed

No parent/admin SPA response, SignalR event, or browser WebRTC path ever
contains an RTSP URL or camera credentials. Only the media gateway (with
shared API key) can resolve a private source after stream-token validation.
go2rtc stays on `internal-network` and is never published to the host for
browser access.

## Local media stack (WebRTC)

In addition to the API + frontends, start:

```bash
# Terminal A — go2rtc (private RTSP→WebRTC engine)
docker run --rm -p 1984:1984 -v "$PWD/docker/go2rtc/go2rtc.yaml:/config/go2rtc.yaml:ro" alexxit/go2rtc:latest

# Terminal B — media gateway (token auth + WebRTC signaling)
cd src/NurseryCamera.MediaGateway
dotnet run

# Terminal C — API (Provider=Go2Rtc in appsettings.Development.json)
cd src/NurseryCamera.Api
dotnet run --urls http://localhost:8080
```

Or with Compose (includes go2rtc + media-gateway):

```bash
docker compose up --build
```

## Repository layout

```
src/                          .NET solution (Domain, Application, Infrastructure, Api, MediaGateway)
tests/                        Unit + integration tests
frontend/parent/              Angular 19 parent app (EN/AR + WebRTC viewer)
frontend/admin/               Angular 19 admin app (EN/AR)
docker/go2rtc/                Private go2rtc config
docker/nginx/                 Optional reverse-proxy nginx config
docker-compose.yml            Full local stack
.env.example                  Placeholder env vars for docker compose (no real secrets)
```

## Tests

```bash
dotnet test
```

runs the `.NET` unit and integration test projects under `tests/`.
