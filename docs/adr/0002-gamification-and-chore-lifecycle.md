# ADR 0002: Gamification, Scoreboard, and Floating Cadence Lifecycle

## Status
Accepted

## Context
Traditional chore systems with rigid calendar schedules induce "late guilt" and backlog clutter when life interrupts routines. Furthermore, conflating leaderboard standing with spending currency causes hoarding behavior where users avoid buying rewards to avoid losing ranking.

## Decision
1. **Floating Cadence ("Needs-Based Freshness")**:
   - Chores are scheduled by floating interval (`CadenceDays`) elapsed since their last completion, rather than static calendar days.
   - When completed, the next expected interval starts from the completion date. Missed cycles never stack or generate multiple overdue instances.
   - Chores can also be unscheduled (`CadenceDays = null`) to support ad-hoc tasks.
   - After a completion, the 1-tap button of any chore stays locked for the rest of the calendar day (Daily lock).
   - *Amended 2026-10-18*: originally a chore was repeatable at most once per calendar day, with no way past the lock. Real life occasionally needs a second same-day execution, so a locked chore's card menu now offers "Zrobione ponownie" behind a confirmation dialog (Same-day repeat). The lock stays the default; the extra step keeps accidental double taps out. Repeats earn full points, and the API was already accepting them.
2. **Freshness & Urgency Math**:
   - Status is calculated on read from the ratio of days since last done to `CadenceDays`, and falls into one of four urgency bands: Fresh, Due Soon, Overdue, Neglected. The thresholds and their edge cases are defined under **Freshness / Urgency Ratio** in [CONTEXT.md](../../CONTEXT.md).
   - The backlog sorts by urgency band first, then by ratio within a band (*Backlog order*).
   - *Amended 2026-10-12*: originally `100%` (the due day) counted as Overdue, so a daily chore done yesterday showed "Zaległe" while today was still available, and short cadences jumped from Fresh/Due Soon straight to Neglected. Ratio-only sorting was replaced by band-first sorting so a 1-day-late daily chore (200%, Overdue) cannot outrank a Neglected one.
3. **Dual-Currency Separation**: a completion credits both **Earned Points**, which rank members and never decrease, and the **Wallet Balance**, which is spent on rewards. Spending never costs a member their standing.
4. **Monthly Competition Cycle**: the leaderboard is the **Monthly Race**, which resets at the start of each `Europe/Warsaw` calendar month.
5. **Reward Store & Voucher Wallet**: any member can add **Reward Items**. Buying one burns Wallet Balance and creates a **Voucher**, which is marked redeemed once the perk is claimed.

The terms in bold are defined in [CONTEXT.md](../../CONTEXT.md).

## Consequences
- Eliminates domestic guilt and task accumulation.
- Motivates users to spend earned rewards freely without compromising competition standings.
- Clear mathematical bounds make urgency categorization easily testable via unit tests.
