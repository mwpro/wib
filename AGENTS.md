# Agent Guidelines

## Agent skills

### Issue tracker

Issues and specs live in GitHub Issues. See `docs/agents/issue-tracker.md`.

### Domain docs

Single-context layout (`CONTEXT.md` + `docs/adr/` at repo root). See `docs/agents/domain.md`.

---

## How to Run & Test the Project

### Architecture Summary
- **Backend**: ASP.NET Core (.NET 10) REST API (`backend/Wib.Api/`) with Entity Framework Core (MySQL/MariaDB) and domain unit tests (`backend/Wib.UnitTests/`).
- **Frontend**: React 19 + TypeScript + Vite + Tailwind CSS + TanStack Query (`frontend/`).
- **E2E**: Playwright tests (`e2e/`).

### Backend Guidelines
- **No `InternalsVisibleTo`**: Never use `[InternalsVisibleTo]` or `<InternalsVisibleTo>` to expose internal API symbols to test projects. Tests must interact strictly with public contracts and domain methods, preserving proper encapsulation.

---

### 1. Running the Project

#### Full-Stack (Backend API serving SPA static files)
```powershell
# 1. Build frontend and copy assets to API wwwroot
cd frontend
npm run build
Copy-Item -Path "dist/*" -Destination "../backend/Wib.Api/wwwroot" -Recurse -Force
cd ..

# 2. Run backend
dotnet run --project backend/Wib.Api/Wib.Api.csproj
```

#### Frontend Development Server (with HMR)
```powershell
cd frontend
npm run dev
```
Runs Vite dev server (proxies `/api` calls to backend at `http://localhost:5195`).

#### Test Auth Bypass Mode
In test or headless development mode, Auth0 authentication can be bypassed by setting:
`JwtAuth__BypassAuth=true` (or `JwtAuth:BypassAuth=true`). This enables `TestAuthHandler`, allowing instant synthetic user authentication.

---

### 2. Testing the Project

#### Backend Unit Tests
```powershell
dotnet test
```
Fast xUnit + FluentAssertions suite testing domain math (freshness ratio, Europe/Warsaw timezone boundaries, completion logic).

#### Frontend Lint & Build
```powershell
cd frontend
npm run lint    # Runs oxlint
npm run build   # Runs strict TypeScript check (tsc -b) and Vite build
```

#### Playwright E2E Tests
Playwright automatically starts `Wib.Api` with auth bypass enabled (`webServer` in `e2e/playwright.config.ts`), which serves SPA assets from `backend/Wib.Api/wwwroot`.

> [!IMPORTANT]
> When testing frontend changes in Playwright, always build and copy `frontend/dist` to `backend/Wib.Api/wwwroot` first:
> ```powershell
> cd frontend; npm run build; Copy-Item -Path "dist/*" -Destination "../backend/Wib.Api/wwwroot" -Recurse -Force; cd ../e2e; npm test
> ```
> If `Wib.Api.exe` gets locked by a previous run on Windows, terminate it before rebuilding:
> ```powershell
> Get-Process Wib.Api -ErrorAction SilentlyContinue | Stop-Process -Force
> ```
