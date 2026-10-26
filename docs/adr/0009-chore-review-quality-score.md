# ADR 0009: Chore Review Grades Chores With a Quality Score

## Status
Accepted

## Context
The Monthly Race shows who earned the most points, but not how well the household kept up with its chores. The **Chore Review** answers that per chore for the selected month: which chores need attention and which were kept well.

A chore can go wrong in two ways in a month: it is done late, or it is not done at all. Late completions are stored as Completion Outcomes (ADR 0005). A chore that was never done leaves no completion, so it has to be judged from its live state.

We considered:
1. **Two separate signals.** List chores with late completions and, separately, chores that were missed. This needs two rules and two orderings, and a chore can appear in both lists.
2. **One score per chore** that counts a chore that is currently late as one extra failed execution. Chosen.

## Decision
1. **Quality Score** = `onTime / (judged + currentlyBad)`, over completions in the month:
   - `onTime` counts outcomes Przed czasem and W terminie, and `judged` counts every judged outcome. Unscheduled completions count in neither.
   - `currentlyBad` is 1 when the chore's live Freshness at the cut-off is Overdue or Neglected, and 0 otherwise.
   - The cut-off is the end of the month, or now for the current month.
   - A chore whose denominator is 0 has no score and is listed nowhere: it was not due and was not done.
2. A score of **≥ 75%** lists the chore under "Dobra robota", and anything below lists it under "Do poprawy".
   - 75% lets one slip in four executions still count as well kept. Without that tolerance a single late completion would outweigh a month of good work.
3. **`currentlyBad` is judged with the current cadence**, from the last completion before the cut-off (or `CreatedAt`). This is unlike ADR 0005, where outcomes are snapshotted. Cadences are not historized, and snapshotting a monthly state would need a scheduled job.
4. Archived chores are never `currentlyBad`, since nobody can act on them anymore. Their completions in the month still count.
5. The review is per chore and household-wide. It names no members, in line with Completion History reporting counts only, so that the competition does not turn into blame.
6. Same-day repeats count like any other completion. They always land as Przed czasem and could raise a score, but this is rare and visible in the activity stream.

## Consequences
- A past month's review can change after the fact if a chore's cadence is edited later, because `currentlyBad` uses the current cadence. Completion-based counts never change.
- The calculation is a pure domain function (`ChoreReviewCalculator`) over a single query, so it needs no new tables.
- It is served at `GET /api/analytics/{year}/{month}/chore-review`. The scoreboard moved next to it, to `GET /api/analytics/{year}/{month}/scoreboard`, so that month-scoped reads share one prefix.
- Archived chores can appear in the lists but cannot open Completion History, because the history endpoints exclude archived chores (ADR 0007).
