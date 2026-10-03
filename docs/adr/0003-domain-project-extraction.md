# ADR 0003: Domain Project Extraction

## Status
Accepted (Amends ADR 0001)

## Context
The backend was initially established as a single monolithic API project (`Wib.Api`) hosting HTTP routing, Entity Framework Core mappings, and core business rules together. As the domain grew to incorporate complex rules—such as needs-based floating cadence calculations, Europe/Warsaw calendar boundary evaluations, dual-currency wallet economics, and Hamilton-Hare proportional work-share math—coupling domain entities directly to ASP.NET Core and EF Core constructs introduced risks of architectural erosion.

To preserve domain purity, enforce compile-time dependency inversion, and keep domain logic completely independent of framework plumbing, the domain needed to be isolated into a dedicated project.

## Decision
1. **Extracted `Wib.Domain` Project**:
   - `Wib.Domain` is established as a pure .NET 10 class library with **zero external package dependencies** (no EF Core, no ASP.NET Core).
   - Contains all domain entities and aggregates ([`Chore`](file:///C:/Users/macie/RiderProjects/wib/backend/Wib.Domain/Chores/Chore.cs), [`Member`](file:///C:/Users/macie/RiderProjects/wib/backend/Wib.Domain/Members/Member.cs), [`RewardItem`](file:///C:/Users/macie/RiderProjects/wib/backend/Wib.Domain/Store/RewardItem.cs), [`Voucher`](file:///C:/Users/macie/RiderProjects/wib/backend/Wib.Domain/Vouchers/Voucher.cs), [`ChoreCompletion`](file:///C:/Users/macie/RiderProjects/wib/backend/Wib.Domain/Chores/ChoreCompletion.cs), [`Tag`](file:///C:/Users/macie/RiderProjects/wib/backend/Wib.Domain/Chores/Tag.cs)).
   - Contains domain services and gamification calculators ([`ScoreboardCalculator`](file:///C:/Users/macie/RiderProjects/wib/backend/Wib.Domain/Scoreboard/ScoreboardCalculator.cs), [`IScoreboardCalculator`](file:///C:/Users/macie/RiderProjects/wib/backend/Wib.Domain/Scoreboard/IScoreboardCalculator.cs)).
   - Contains domain date/calendar arithmetic ([`WarsawTimeZone`](file:///C:/Users/macie/RiderProjects/wib/backend/Wib.Domain/Common/WarsawTimeZone.cs), `MonthlyPeriod`) and calculation value objects ([`FreshnessResult`](file:///C:/Users/macie/RiderProjects/wib/backend/Wib.Domain/Chores/FreshnessResult.cs), [`FreshnessUrgency`](file:///C:/Users/macie/RiderProjects/wib/backend/Wib.Domain/Chores/FreshnessUrgency.cs), `ScoreboardResponse`, `MemberScoreboardItem`, `MemberStatsInput`).
2. **Pure POCO Encapsulation & Fluent Persistence Mapping**:
   - Domain entities use private setters and backing collections with encapsulation (`IReadOnlyCollection<T>`).
   - The many-to-many relationship between chores and tags is modeled natively in the domain as `Chore.Tags` (`IReadOnlyCollection<Tag>`), hiding database join table concepts from the domain API.
   - EF Core mappings live exclusively in `Wib.Api` within [`WibDbContext`](file:///C:/Users/macie/RiderProjects/wib/backend/Wib.Api/Data/WibDbContext.cs) using Fluent API, mapping `Chore.Tags` directly to the existing `chore_tags` database table without requiring schema migrations.
3. **Endpoint Ergonomics**:
   - Minimal API endpoints in `Wib.Api` continue orchestrating operations directly with `WibDbContext`: loading domain entities, invoking domain methods (`Complete`, `Purchase`, `Archive`), and persisting changes via `SaveChangesAsync()`.
4. **Deterministic Time Rules**:
   - Domain methods explicitly accept UTC timestamps (`DateTime nowUtc`) or offsets supplied by the outer application layer, keeping domain logic deterministic and trivially testable without clock mocking.

## Consequences
- **Domain Purity**: Domain business rules cannot accidentally couple to database concerns, HTTP request contexts, or external identity providers.
- **Fast, Independent Testing**: Pure domain tests execute in sub-second time without needing in-memory database fixtures or WebApplicationFactory boots.
- **Lean Architecture**: Maintains a clean 2-project structure (`Wib.Domain` + `Wib.Api`) appropriate for lightweight VPS hosting, avoiding unnecessary layer sprawl.
- **Zero Database Downtime**: Fully backwards-compatible with existing MariaDB schema and EF Core migrations.
