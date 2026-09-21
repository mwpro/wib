# ADR 0002: Gamification, Scoreboard, and Floating Cadence Lifecycle

## Status
Accepted

## Context
Traditional chore systems with rigid calendar schedules induce "late guilt" and backlog clutter when life interrupts routines. Furthermore, conflating leaderboard standing with spending currency causes hoarding behavior where users avoid buying rewards to avoid losing ranking.

## Decision
1. **Floating Cadence ("Needs-Based Freshness")**:
   - Chores are scheduled by floating interval (`CadenceDays`) elapsed since their last completion, rather than static calendar days.
   - When completed, the next expected interval starts from the completion date. Missed cycles never stack or generate multiple overdue instances.
   - Chores can also be unscheduled (`CadenceDays = null`) to support one-off tasks.
2. **Freshness & Urgency Math**:
   - Status is calculated dynamically: $\text{Ratio} = \frac{\text{Days Since Last Done}}{\text{CadenceDays}}$:
     - `0% - 79%`: Fresh (Green)
     - `80% - 99%`: Due Soon (Yellow)
     - `100% - 129%`: Overdue (Orange)
     - `≥ 130%`: Neglected (Red, prioritized at the top of backlog)
3. **Dual-Currency Separation**:
   - **Earned Points (Monthly XP)**: Cumulative points awarded for completions within the active month. Drives the leaderboard and work-share progress bar. Never decreases upon purchasing rewards.
   - **Wallet Balance**: Spendable balance. Incremented when a chore is completed, decremented when a voucher is purchased.
4. **Monthly Competition Cycle**:
   - Resets at 00:00 on the 1st of each month according to `Europe/Warsaw` local timezone.
   - Displays a segmented progress bar representing the proportional share (%) of completed chores for each participating member.
5. **Reward Store & Voucher Wallet**:
   - Store catalog items can be created by any household member.
   - Purchasing an item burns points and creates an `Available` voucher in the user's wallet.
   - Claiming a perk marks the voucher `Redeemed` with a completion timestamp.

## Consequences
- Eliminates domestic guilt and task accumulation.
- Motivates users to spend earned rewards freely without compromising competition standings.
- Clear mathematical bounds make urgency categorization easily testable via unit tests.
