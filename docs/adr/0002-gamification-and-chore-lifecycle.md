# ADR 0002: Gamification, Scoreboard, and Floating Cadence Lifecycle

## Status
Accepted

## Context
Traditional chore systems with rigid calendar schedules induce "late guilt" and backlog clutter when life interrupts routines. Furthermore, conflating leaderboard standing with spending currency causes hoarding behavior where users avoid buying rewards to avoid losing ranking.

## Decision
1. **Floating Cadence ("Needs-Based Freshness")**:
   - Chores are scheduled by floating interval (`CadenceDays`) elapsed since their last completion, rather than static calendar days.
   - When completed, the next expected interval starts from the completion date. Missed cycles never stack or generate multiple overdue instances.
   - Chores can also be unscheduled (`CadenceDays = null`) to support ad-hoc tasks, repeatable at most once per calendar day.
2. **Freshness & Urgency Math**:
   - Status is calculated dynamically: $\text{Ratio} = \frac{\text{Days Since Last Done}}{\text{CadenceDays}}$:
     - `0% - 79%`: Fresh (Green)
     - `80% - 100%`: Due Soon (Yellow) — the due day itself is not late
     - `> 100%` and `< 130%`: Overdue (Orange); the first day past due is always Overdue, even when ≥ 130%
     - `≥ 130%` and at least 2 days past due: Neglected (Red, prioritized at the top of backlog)
   - The backlog sorts by urgency band first, then by ratio within a band.
   - *Amended 2026-10-12*: originally `100%` (the due day) counted as Overdue, so a daily chore done yesterday showed "Zaległe" while today was still available, and short cadences jumped from Fresh/Due Soon straight to Neglected. Ratio-only sorting was replaced by band-first sorting so a 1-day-late daily chore (200%, Overdue) cannot outrank a Neglected one.
3. **Dual-Currency Separation**:
   - **Earned Points (Monthly XP)**: Cumulative points awarded for completions within the active month. Drives the leaderboard and work-share progress bar. Never decreases upon purchasing rewards.
   - **Wallet Balance**: Spendable balance. Incremented when a chore is completed, decremented when a voucher is purchased.
4. **Monthly Competition Cycle**:
   - Resets at 00:00 on the 1st of each month according to `Europe/Warsaw` local timezone.
   - Displays a segmented progress bar representing the proportional share (%) of monthly points earned for each participating member.
5. **Reward Store & Voucher Wallet**:
   - Store catalog items can be created by any household member.
   - Purchasing an item burns points and creates an `Available` voucher in the user's wallet.
   - Claiming a perk marks the voucher `Redeemed` with a completion timestamp.

## Consequences
- Eliminates domestic guilt and task accumulation.
- Motivates users to spend earned rewards freely without compromising competition standings.
- Clear mathematical bounds make urgency categorization easily testable via unit tests.
