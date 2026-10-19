# ADR 0005: Completion Outcomes Are Snapshotted at Write Time

## Status
Accepted

## Context
The **Completion History** shows, for each Chore Completion, how timely it was: the gap since the previous completion and the urgency band at that moment (the **Completion Outcome**). It also summarizes outcomes across all of a chore's completions.

An outcome depends on the previous completion, the chore's cadence, and the band thresholds. The cadence can be edited later, and the thresholds have been amended before (ADR 0002, 2026-10-12). We considered:
1. **Recompute on read**: load all of a chore's completions and judge them in memory. Simple, and threshold changes apply retroactively. But it is O(all completions) per request, and judging old completions by today's cadence rewrites history: after weekly → monthly, last week's "late" becomes "early".
2. **Snapshot on write**: judge each completion once, when it is recorded, and store the result. Paging and the summary become plain SQL (`ORDER BY … LIMIT`, `GROUP BY`).
3. **SQL window functions** (`LAG`): duplicates the band math in SQL. Rejected.

## Decision
1. `ChoreCompletion` stores `DaysSincePrevious` and `UrgencyAtCompletion`, both `NOT NULL`. `Chore.Complete` computes them as a `CompletionOutcome` with `FreshnessCalculator.JudgeCompletion`, which shares its band math with `FreshnessCalculator.Evaluate` behind live Freshness, so the two cannot drift.
2. **Verdicts are judged by the rules of the day.** Later cadence edits or threshold amendments do not rewrite stored outcomes.
3. **The cadence itself is not stored.** Re-judging history under new rules is therefore not supported; if ever wanted, it needs its own decision.
4. Completions recorded before this change were backfilled once using each chore's cadence at migration time — the best available guess. The backfill runs at startup between the `AddCompletionOutcome` and `MakeCompletionOutcomeRequired` migrations. It is **temporary** (`CompletionOutcomeBackfill` and its call in `DatabaseMigrator`), to be removed once every environment has migrated past `MakeCompletionOutcomeRequired`; it is a no-op on fresh databases.
   - *Amended 2026-10-20*: removed. Every environment is past `MakeCompletionOutcomeRequired`. A database still before `AddCompletionOutcome` can no longer migrate straight to the current schema, because the `NOT NULL` step would meet unfilled rows. It has to be upgraded through a build that still contains the backfill (any commit before its removal).

## Consequences
- History paging and summaries are cheap and scale with an index on `(ChoreId, CompletedAt)`.
- A completion's outcome never changes after it is recorded, matching how members experienced it at the time.
- Changing the band thresholds later affects only new completions; old and new verdicts may follow different rules.
- Backfilled outcomes for chores whose cadence changed before this ADR may not match what members saw back then.
