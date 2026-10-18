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
  * *Cadence Presets*: Standard floating intervals supported in the UI: Daily (1 day), Weekly (7 days), Monthly (30 days), Quarterly (90 days / "Co 3 mies."), Semi-annually (180 days), and Annually (365 days). Custom day values are also supported.
  * *Cadence Filter*: A backlog filter with one chip per preset, plus "Bez terminu" for unscheduled chores; only one can be active at a time. Each preset covers a range of days, from above the previous preset up to and including its own value: 1, 2–7, 8–30, 31–90, 91–180, 181+. So a 14-day chore matches "Co miesiąc".
* **Freshness / Urgency Ratio**: The quotient $\text{Ratio} = \frac{\text{Days Since Last Done}}{\text{CadenceDays}}$:
  * Calculated in whole calendar days elapsed in the `Europe/Warsaw` timezone: $\text{Days Since Last Done} = \max(0, \text{Today}_{\text{Warsaw}} - \text{ReferenceDate}_{\text{Warsaw}})$.
  * When a chore has never been completed (`LastCompletedAt == null`), `ReferenceDate` defaults to `CreatedAt`.
  * **Fresh** ($0\% \le \text{Ratio} < 80\%$): Recently done, no attention needed (Green).
  * **Due Soon** ($80\% \le \text{Ratio} \le 100\%$): Approaching or on the due date (Yellow). The due day itself (Ratio = 100%) is not late.
  * **Overdue** ($100\% < \text{Ratio} < 130\%$): Past expected cadence (Orange). The first day past due is always Overdue, even when Ratio ≥ 130% (short cadences, e.g. a daily chore on day 2).
  * **Neglected** ($\text{Ratio} \ge 130\%$ and at least 2 days past due): Significantly overdue; highest priority (Red).
  * *Backlog order*: by urgency band (Neglected → Overdue → Due Soon → Fresh → Unscheduled), then by Ratio within a band.
  * *Unscheduled Chore*: Has no cadence (`CadenceDays = null`); urgency is `Unscheduled` with no numerical ratio.
* **Chore Completion**: An immutable record created when a member marks a chore done. Captures `CompletedAt` (UTC), `PointsAwarded`, `CompletedByMemberId`, and references the completed `ChoreId`. Also snapshots its **Completion Outcome**: `DaysSincePrevious` and `UrgencyAtCompletion`.
  * *Daily lock*: once a chore is marked done, its 1-tap "Zrobione!" button stays locked for the rest of that `Europe/Warsaw` calendar day. This is enforced in the UI only; the API accepts repeats by design — abuse is visible to the household in the scoreboard activity stream, and social accountability is the guard.
  * *Same-day repeat*: the deliberate escape hatch from the Daily lock. While a chore is locked, its card menu offers "Zrobione ponownie", which records another completion after a confirmation dialog. Any member can do it, and it earns full points like any other completion. Its Completion Outcome is judged normally, so with `DaysSincePrevious = 0` it lands as "Przed czasem" and counts toward the on-time share.
* **Completion Outcome**: How timely a completion was, judged at the moment it happened with the same Freshness math as the backlog (see ADR 0005). The reference date is the chore's previous completion, or `CreatedAt` for the first one, and the cadence is the one in force at that moment. The outcome is stored and never recomputed.
  * Outcome labels: Fresh → "Przed czasem", Due Soon → "W terminie", Overdue → "Z opóźnieniem", Neglected → "Mocno spóźnione". A chore without a cadence gets `Unscheduled` and no label.
  * *On time* ("na czas"): Przed czasem + W terminie. The on-time share is taken over judged completions only; completions made while the chore had no cadence (`Unscheduled`) count neither for nor against it, though they still count toward the total.
* **Completion History**: A read-only, per-chore view of its Chore Completions, opened from "Historia" in the chore card menu. It lists completions newest first, grouped by `Europe/Warsaw` month, showing date, member, points, gap since the previous completion, and outcome. Its summary covers all time: total, outcome counts, on-time share, and how completions split between members (counts only — no per-member performance).
* **Tag**: A multi-label category attached to chores for filtering (e.g. `kuchnia`, `sprzątanie`, `ogród`, `konserwacja`).

### 3. Gamification & Economy
* **Earned Points (XP)**: Cumulative points awarded upon chore completion. Drives scoreboard rankings, monthly competitions, and streaks. **Never decreases** when purchasing rewards.
* **Wallet Balance**: Spendable currency held by a member. Minted upon chore completion and burned when purchasing vouchers.
* **Monthly Race ("Kto jest lepszy?")**: The primary competition cycle. Tracks points earned during the current calendar month, resetting on the 1st of each month at 00:00 `Europe/Warsaw` time.
* **Work Share Ratio**: The proportional percentage of monthly points earned by each member in the current period, visualized as a segmented progress bar.
* **Reward Item**: A catalog item in the in-app store representing a favor, treat, or privilege (e.g. "Masaż", "Wyjście solo", "Kupno książki") with a designated point cost and optional available quantity (supports single-claim or batch perks; automatically deactivates when inventory is exhausted).
  * *Catalog order*: most expensive first (point cost descending), then by title.
* **Voucher**: An instance of a purchased reward owned by a member in their wallet, preserving title and point cost snapshots from the time of purchase. Marked redeemed with a completion timestamp (`RedeemedAt`) once fulfilled.

### 4. Integrations
* **TRMNL Display**: A read-only, household-wide view of wib rendered on the home's TRMNL e-ink device (quadrant layout). Shows the Monthly Race standings and up to 10 scheduled chores in backlog order (Neglected, Overdue and Due Soon first, Fresh filling the rest; never Unscheduled); when nothing is due, its footer says "wszystko ogarnięte". It has no member identity of its own: TRMNL polls the regular API as an Auth0 OAuth client (see [ADR 0004](docs/adr/0004-trmnl-display-via-oauth.md)).

---

## Boundaries & Non-Goals

* **No Financial Settlements**: Points cannot be converted to real money within the app; fulfillment is interpersonal.
* **No Inter-Member Transfers**: Points cannot be traded or gifted between members' spendable balances.
* **No Complex Calendar Scheduling**: No recurring calendar recurrence rules (e.g., RRULE / every 2nd Tuesday of the month). Everything operates on floating cadence since last completion.
* **Code in English, UI in Polish**: All entities, database tables, APIs, tests, and documentation are written in English. All client-facing UI labels and error messages are presented in Polish.
