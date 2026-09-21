# Implementation Roadmap: wib ("Who is Better")

This roadmap defines the step-by-step implementation plan for **wib**. Each phase is split into small, independently verifiable tasks designed for high agentic autonomy and test-driven validation.

---

## Phase 1: Project Scaffolding & Auth Foundation

### Task 1.1: Solution Scaffolding & Multi-Stage Dockerfile
* **Backend**: Initialize .NET 10 solution:
  * `backend/Wib.Api` (Web API)
  * `backend/Wib.UnitTests` (xUnit + FluentAssertions)
* **Frontend**: Initialize React 19 + TypeScript + Vite SPA in `wib-frontend/` styled with Tailwind CSS and Radix UI.
* **Database**: EF Core with `Pomelo.EntityFrameworkCore.MySql` and automatic startup migration runner (`context.Database.Migrate()`).
* **Container (Issue 3.C Focus)**: Multi-stage Dockerfile:
  * Build frontend $\rightarrow$ Publish API with assets in `wwwroot` $\rightarrow$ Runtime `aspnet:10.0-noble-chiseled-extra`.
  * **Explicit IANA Timezone Support**: Ensure `tzdata` and globalization libraries are configured so `TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw")` functions properly without exceptions in Linux containers.
* **Verification**: `dotnet build` succeeds, `npm run build` succeeds, Docker image builds successfully.

### Task 1.2: Playwright E2E Harness & Test Auth Bypass
* Setup Playwright in `e2e/` (or within `wib-frontend/`).
* **Backend Test Auth (Issue 3.D Focus)**:
  * Implement `TestAuthHandler` enabled when `Testing:BypassAuth=true`, accepting synthetic user headers (e.g. `X-Test-User: maciej`).
* **Frontend Mock Auth Toggle (Issue 3.D Focus)**:
  * Backend serves `GET /api/config` containing `{ auth0: { domain, clientId, audience }, isTestMode: boolean }`.
  * In React, if `isTestMode` is true, bypass `<Auth0Provider>` redirects and inject the synthetic test user identity for seamless headless E2E testing without external Auth0 calls.
* **Verification**: Playwright boots the frontend and verifies a test ping endpoint.

### Task 1.3: Auth0 JWT Bearer Auth & JIT Member Provisioning
* Configure JWT Bearer middleware in `Wib.Api` validating Auth0 tokens.
* Implement Just-In-Time (JIT) `Member` provisioning:
  * When an authenticated request arrives, auto-provision or update `Member` (`Id`, `Auth0Sub`, `DisplayName`, `Email`, `AvatarUrl`, `WalletBalance`).
  * Ensure concurrency-safe insertion (handling simultaneous initial requests gracefully).
* **Verification**: Unit tests for Member provisioning logic + authenticated API endpoint test.

---

## Phase 2: Core Chore Engine & Freshness Math (TDD)

### Task 2.1: Chore & Tag Domain Models + Freshness Calculator
* **Domain Entities**: `Chore`, `Tag`, `ChoreTag` with EF Core migrations.
  * `Chore`: `Title`, `Description`, `Points` (default 1), `CadenceDays` (nullable int), `LastCompletedAt` (nullable DateTime UTC), `DefaultAssigneeMemberId`, `IsArchived`.
* **TDD FreshnessCalculator**:
  * Unit tests covering ratio: $\text{Ratio} = \frac{\text{Days Since Last Done}}{\text{CadenceDays}}$:
    * `0% - 79%`: Fresh (Green)
    * `80% - 99%`: Due Soon (Yellow)
    * `100% - 129%`: Overdue (Orange)
    * `≥ 130%`: Neglected (Red)
  * Handling unscheduled chores (`CadenceDays = null`).
* **Chore CRUD API**: Endpoints to list chores (sorted by urgency ratio descending), create, update, and archive chores.
* **Verification**: `Wib.UnitTests` passes 100%.

### Task 2.2: 1-Tap Chore Completion & Point Minting
* **Domain Entity**: `ChoreCompletion` (`Id`, `ChoreId`, `ChoreTitle`, `CompletedByMemberId`, `CompletedAt`, `PointsAwarded`).
* **Endpoint `POST /api/chores/{id}/complete`**:
  * In a single atomic EF Core transaction:
    1. Set `Chore.LastCompletedAt = DateTime.UtcNow`.
    2. Insert `ChoreCompletion`.
    3. Increment `Member.WalletBalance += Chore.Points`.
* **Verification**: Unit tests covering atomic completion and balance updates.

---

## Phase 3: Gamification & Monthly Scoreboard

### Task 3.1: Monthly Race & Scoreboard API
* **Scoreboard Calculation Engine**:
  * Aggregates `ChoreCompletion` records for the active calendar month.
  * Calculates monthly cutoff times strictly at 00:00:00 on the 1st of each month in `Europe/Warsaw` timezone, querying against corresponding UTC range.
  * Computes total monthly points earned per member and the **Work-Share Percentage** (% share of total household chores completed).
  * Lifetime statistics: Total chores done, total lifetime points earned.
* **Endpoint `GET /api/scoreboard/current`**: Returns current month leaderboard and work-share split.
* **Verification**: Unit tests verifying leap years, month rollovers, and timezone boundaries in `Europe/Warsaw`.

---

## Phase 4: Reward Store & Voucher Wallet

### Task 4.1: Store Catalog Management
* **Domain Entity**: `RewardItem` (`Id`, `Title`, `Description`, `PointCost`, `Icon`, `IsActive`, `CreatedByMemberId`).
* **Endpoints**: List active rewards, create new reward item, archive reward item.
* **Verification**: Unit tests for store item validation.

### Task 4.2: Voucher Wallet Lifecycle & Atomic Purchase
* **Domain Entity**: `Voucher` (`Id`, `RewardItemId`, `TitleSnapshot`, `PointCostSnapshot`, `OwnedByMemberId`, `PurchasedAt`, `Status` [Available | Redeemed], `RedeemedAt`).
* **Endpoint `POST /api/store/{id}/buy`**:
  * Verifies `Member.WalletBalance >= RewardItem.PointCost`.
  * Atomically deducts `WalletBalance` and inserts `Voucher` in `Available` status.
* **Endpoint `POST /api/vouchers/{id}/redeem`**:
  * Transitions voucher status from `Available` $\rightarrow$ `Redeemed` with `RedeemedAt = DateTime.UtcNow`.
* **Verification**: Unit tests for balance deduction, insufficient funds rejection, and status transitions.

---

## Phase 5: Polish Frontend UI (Mobile-First Web Clip)

### Task 5.1: Navigation Shell & iOS Web Clip
* Responsive mobile-first layout with Polish labels (`Zadania`, `Kto jest lepszy?`, `Sklep`, `Portfel`).
* Header with active member avatar, current spendable points badge, and Auth0 login/logout.
* iOS Web Clip meta tags (`apple-mobile-web-app-capable`, `apple-touch-icon`, `viewport-fit=cover`).

### Task 5.2: Chores Backlog View
* Backlog view with cards color-coded by freshness ratio (Green / Yellow / Orange / Red).
* Unscheduled tasks grouped in "Do zrobienia (bez terminu)".
* 1-Tap "Zrobione!" completion button with optimistic update and celebratory micro-animation.
* Multi-tag filtering and search.
* "Dodaj zadanie" modal (Title, Points, Cadence in days/weeks/months or unscheduled, Tags).

### Task 5.3: "Kto jest lepszy?" Scoreboard View
* Segmented progress bar showing the proportional % split of work this month between partners.
* Leaderboard podium showing monthly points earned.
* Recent activity feed: *"Maciej ukończył: Zmywarka (1 pkt) 15 min temu"*.

### Task 5.4: Store Catalog & Voucher Wallet View
* "Sklep z nagrodami": Grid of available rewards with point costs and 1-tap "Kup nagrodę".
* "Mój portfel": List of owned vouchers with "Zrealizuj kupon" action and "Historia zrealizowanych" tab.

---

## Phase 6: E2E Golden Journeys & Mikr.us Deployment

### Task 6.1: Playwright Golden Journeys
* Automated headless end-to-end tests exercising full stack via `TestAuthHandler`:
  1. *Chore Journey*: Create chore $\rightarrow$ Complete chore $\rightarrow$ Verify card resets to green & wallet increments.
  2. *Scoreboard Journey*: Complete chores as two different users $\rightarrow$ Verify segmented progress bar updates.
  3. *Store Journey*: Buy reward $\rightarrow$ Verify wallet deduction $\rightarrow$ Redeem voucher in wallet.

### Task 6.2: Mikr.us Deployment & Docker Compose
* Production `docker-compose.yml` matching `mikrus-iaac` conventions.
* Secrets loading from `/run/secrets/appsettings.secret.json`.
* Nginx reverse proxy `VIRTUAL_HOST` setup.
