# Implementation Roadmap: wib ("Who is Better")

This roadmap defines the step-by-step implementation plan for **wib**. We use **full-stack vertical slices** (backend API + React UI + Playwright E2E in each phase) and deploy the walking skeleton to Mikr.us in Phase 1.

---

## Phase 1: Walking Skeleton & Mikr.us Deployment
* **Task 1.1: Solution Scaffolding & Multi-Stage Dockerfile**
  * .NET 10 solution (`Wib.Api`, `Wib.UnitTests`).
  * React 19 + TypeScript + Vite SPA (`frontend/`) with Tailwind CSS and Radix UI.
  * EF Core MySQL with Pomelo and automatic startup migration runner.
  * Multi-stage Dockerfile with IANA `tzdata` and globalization support.
* **Task 1.2: Auth0 JWT Bearer, JIT Provisioning & Test Auth Bypass**
  * Backend JWT Bearer auth with JIT `Member` auto-provisioning.
  * `GET /api/config` serving runtime Auth0 domain/client ID and `isTestMode` toggle.
  * Backend `TestAuthHandler` + Frontend mock auth switch for seamless headless testing.
* **Task 1.3: Mikr.us Deployment Pipeline & Initial E2E Smoke Test**
  * GitHub Actions workflow building and publishing Docker image to GHCR.
  * Production `docker-compose.yml` for Mikr.us with `/run/secrets/appsettings.secret.json`.
  * Initial Playwright E2E test verifying app boot, auth bypass, and health check.

---

## Phase 2: Chores & Freshness Engine (Full-Stack Slice)
* **Task 2.1: Chore & Tag Backend + FreshnessCalculator (TDD)**
  * Domain entities: `Chore`, `Tag`, `ChoreTag`.
  * Unit-tested `FreshnessCalculator` (0–79% Green, 80–99% Yellow, 100–129% Orange, ≥130% Red).
  * CRUD endpoints for chores with urgency-sorted list.
* **Task 2.2: 1-Tap Completion Endpoint & Atomic Point Minting**
  * Entity `ChoreCompletion`.
  * Atomic `POST /api/chores/{id}/complete`: sets `LastCompletedAt = UtcNow`, records completion, increments `Member.WalletBalance`.
* **Task 2.3: Chores Backlog UI (Polish)**
  * Mobile-first cards with color-coded freshness bars (Green/Yellow/Orange/Red).
  * Unscheduled tasks grouped in "Do zrobienia (bez terminu)".
  * 1-Tap "Zrobione!" button with optimistic UI update.
  * Tag filtering and "Dodaj zadanie" modal.
* **Task 2.4: Playwright Golden Journey: Chores Loop**
  * Automated E2E test: Create chore $\rightarrow$ 1-tap "Zrobione!" $\rightarrow$ Verify card resets to green and points award.

---

## Phase 3: "Kto jest lepszy?" Scoreboard (Full-Stack Slice)
* **Task 3.1: Monthly Scoreboard Backend Engine**
  * Aggregates `ChoreCompletion` records for active calendar month.
  * `Europe/Warsaw` midnight cutoff math.
  * Computes total monthly points earned and **Work-Share Percentage** (%).
* **Task 3.2: Scoreboard & Activity Stream UI (Polish)**
  * Segmented progress bar showing % share of work between partners.
  * Monthly podium and lifetime stats.
  * Live recent activity stream (*"Maciej ukończył: Zmywarka (1 pkt) 15 min temu"*).
* **Task 3.3: Playwright Golden Journey: Scoreboard Loop**
  * Automated E2E test: Complete chores as different members $\rightarrow$ Verify segmented progress bar updates.

---

## Phase 4: Reward Store & Voucher Wallet (Full-Stack Slice)
* **Task 4.1: Store Catalog & Voucher Lifecycle Backend**
  * `RewardItem` catalog management API.
  * `POST /api/store/{id}/buy`: verifies wallet balance, atomically deducts points, creates `Available` voucher.
  * `POST /api/vouchers/{id}/redeem`: marks voucher `Redeemed` with timestamp.
* **Task 4.2: Store & Voucher Wallet UI (Polish)**
  * "Sklep z nagrodami": Grid of rewards with point costs and 1-tap "Kup nagrodę".
  * "Mój portfel": List of owned vouchers with 1-tap "Zrealizuj kupon" and redemption history.
* **Task 4.3: Playwright Golden Journey: Store Loop**
  * Automated E2E test: Buy reward $\rightarrow$ Verify balance deducted $\rightarrow$ Redeem voucher from wallet.
