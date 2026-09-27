# CONTEXT: wib ("Who is Better")

## Domain Overview

**wib** is a gamified household chore and domestic habit tracker designed to fairly distribute chores, maintain home freshness, and foster domestic harmony through friendly competition and a custom reward economy.

---

## Ubiquitous Language & Glossary

### 1. Identity & Household
* **Household**: The single shared domestic context representing the home.
* **Member**: A person residing in the household who completes chores, earns points, and redeems rewards. Dynamically provisioned upon first authentication.
* **Auth0 Identity**: The external authentication provider supplying the JWT Bearer token with claims (`sub`, `name`, `email`, `picture`).

### 2. Chores & Cadence
* **Chore**: A domestic task or maintenance activity.
  * *Recurring Chore*: Has a defined `CadenceDays` interval.
  * *Unscheduled Chore*: Has no schedule (`CadenceDays = null`); lives in the backlog for ad-hoc execution.
* **Cadence**: A floating interval (in days) representing how often a chore should be performed relative to its last completion date ("days since last done"), rather than fixed calendar slots.
* **Freshness / Urgency Ratio**: The quotient $\text{Ratio} = \frac{\text{Days Since Last Done}}{\text{CadenceDays}}$:
  * Calculated in whole calendar days elapsed in the `Europe/Warsaw` timezone: $\text{Days Since Last Done} = \max(0, \text{Today}_{\text{Warsaw}} - \text{ReferenceDate}_{\text{Warsaw}})$.
  * When a chore has never been completed (`LastCompletedAt == null`), `ReferenceDate` defaults to `CreatedAt`.
  * **Fresh** ($0\% \le \text{Ratio} < 80\%$): Recently done, no attention needed (Green).
  * **Due Soon** ($80\% \le \text{Ratio} < 100\%$): Approaching due date (Yellow).
  * **Overdue** ($100\% \le \text{Ratio} < 130\%$): Past expected cadence (Orange).
  * **Neglected** ($\text{Ratio} \ge 130\%$): Significantly overdue; highest priority (Red).
  * *Unscheduled Chore*: Has no cadence (`CadenceDays = null`); urgency is `Unscheduled` with no numerical ratio.
* **Chore Completion**: An immutable record created when a member marks a chore done. Captures `CompletedAt` (UTC), `PointsAwarded`, `MemberId`, and chore title snapshot.
* **Tag**: A multi-label category attached to chores for filtering (e.g. `kuchnia`, `sprzątanie`, `ogród`, `konserwacja`).

### 3. Gamification & Economy
* **Earned Points (XP)**: Cumulative points awarded upon chore completion. Drives scoreboard rankings, monthly competitions, and streaks. **Never decreases** when purchasing rewards.
* **Wallet Balance**: Spendable currency held by a member. Minted upon chore completion and burned when purchasing vouchers.
* **Monthly Race ("Kto jest lepszy?")**: The primary competition cycle. Tracks points earned during the current calendar month, resetting on the 1st of each month at 00:00 `Europe/Warsaw` time.
* **Work Share Ratio**: The proportional percentage of household chores completed by each member in the current period, visualized as a segmented progress bar.
* **Reward Item**: A catalog item in the in-app store representing a favor, treat, or privilege (e.g. "Masaż", "Wyjście solo", "Kupno książki") with a designated point cost.
* **Voucher**: An instance of a purchased reward owned by a member in their wallet.
  * Status: `Available` (purchased, unspent) $\rightarrow$ `Redeemed` (claimed/fulfilled with timestamp).

---

## Boundaries & Non-Goals

* **No Financial Settlements**: Points cannot be converted to real money within the app; fulfillment is interpersonal.
* **No Inter-Member Transfers**: Points cannot be traded or gifted between members' spendable balances.
* **No Complex Calendar Scheduling**: No recurring calendar recurrence rules (e.g., RRULE / every 2nd Tuesday of the month). Everything operates on floating cadence since last completion.
* **Code in English, UI in Polish**: All entities, database tables, APIs, tests, and documentation are written in English. All client-facing UI labels and error messages are presented in Polish.
