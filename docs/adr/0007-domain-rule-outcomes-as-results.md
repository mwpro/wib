# ADR 0007: Domain Rule Outcomes Are Returned as Results

## Status
Accepted (Amends [ADR 0001](0001-architecture-and-stack.md))

## Context
Store and Voucher rules (inactive reward, not enough points, voucher already redeemed) were written twice: endpoints pre-checked them and answered `400` with Polish ProblemDetails, then the domain re-checked the same rules and threw `InvalidOperationException` with the same Polish text. A missing pre-check meant a member got a `500` that also reached Sentry. Chore and Reward Item input rules lived only in the HTTP request DTOs, so the domain accepted anything.

We considered:
1. **One domain exception type** caught by a single handler at the HTTP seam. Signatures stay unchanged, but a method's error modes are visible only in its tests.
2. **Results returned by domain methods.** Error modes are part of the method signature, and endpoints translate them.
3. **The ErrorOr library** for option 2. Rejected: ADR 0003 keeps `Wib.Domain` free of external packages, and the needed surface is small.

## Decision
1. Domain methods whose rules a member's request can break return a hand-rolled `Result` or `Result<T>` (`Wib.Domain.Common`) carrying either the value or a single `DomainError`. Implicit conversions let the domain write `return voucher;` and `return WalletErrors.NotEnoughPoints;`.
2. `DomainError(Code, Kind)`: `Code` is a namespaced English identifier (`Wallet.NotEnoughPoints`, `Voucher.AlreadyRedeemed`), declared as a static instance next to the aggregate that owns the rule. `Kind` is `Invalid` or `NotFound`, chosen by what the member is told, not by where the rule lives: redeeming someone else's voucher is a domain rule that answers `Voucher.NotFound`.
3. The domain carries no user-facing text. `Wib.Api` maps a `DomainError` once (`ToProblem`) to ProblemDetails: `Invalid` → `400`, `NotFound` → `404`, `title` and a `code` extension both set to the code. The SPA translates codes to Polish in `frontend/src/api/errors.ts` and falls back to a generic message for unknown codes.
4. Exceptions remain for programmer errors and infrastructure failures: null arguments, impossible states such as a non-positive wallet amount, and database errors. These are bugs and belong in Sentry as `500`s.
5. A failed `Result` leaves the domain unchanged. Rules are checked before any state changes.

## Consequences
- Each rule is written once, in the domain, and tested there by asserting the returned error. Endpoint tests cover the mapping once per error kind, plus rules that need persisted ids (voucher ownership).
- Endpoints branch with `if (result.IsFailure) return result.Error.ToProblem();` before saving.
- A new rule needs a code in the domain and, for a friendly message, an entry in the SPA's code map. Without the entry, members see the generic fallback.
- Adding another HTTP status means adding an `ErrorKind`, which is deliberately not done until a client needs it.
