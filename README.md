<p align="center">
  <img src="frontend/public/icon-192.png" alt="wib" width="96" height="96" />
</p>

<h1 align="center">wib · Who is Better?</h1>

<p align="center">
  A household chore tracker that turns chores into a friendly monthly competition with its own reward shop.
  <br />
  Built for one home and used every day from phones.
</p>

<p align="center">
  <a href="https://github.com/mwpro/wib/actions/workflows/ci.yml"><img src="https://github.com/mwpro/wib/actions/workflows/ci.yml/badge.svg" alt="CI" /></a>
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet" alt=".NET 10" />
  <img src="https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=black" alt="React 19" />
  <img src="https://img.shields.io/badge/UI-Polish-dc2626" alt="UI in Polish" />
</p>

<p align="center">
  <img src="docs/images/tour.webp" alt="Marking a chore done, then checking the monthly race and the reward shop" width="320" />
  <br />
  <sub>The app's interface is in Polish.</sub>
</p>

---

## How it works

**🧹 Chores without a calendar.** Each chore has a cadence ("every 7 days"), and the clock restarts when someone does it. Nothing piles up after a busy week. Chores are just more or less fresh:

| | Band | Meaning |
|---|---|---|
| 🟢 | Fresh | done recently |
| 🟡 | Due Soon | coming up, or due today |
| 🟠 | Overdue | past its cadence |
| 🔴 | Neglected | well past it; goes to the top |

The backlog puts the most urgent chores first. One tap marks a chore done and awards points.

**🏆 Who is better?** Every month starts a new race. The scoreboard shows each member's share of the month's work as one split bar, along with a podium, all-time stats and a live activity feed.

**🎁 A shop for favors.** Points earned also go into a wallet. Members put up their own rewards (a massage, a solo night out, a new book), buy them as vouchers, and redeem them when the favor is done. Spending never lowers your place in the race.

**📟 On the wall.** A [TRMNL](https://usetrmnl.com) e-ink display at home shows the race and the chores that need attention next.

<p align="center">
  <img src="docs/images/trmnl.png" alt="TRMNL e-ink widget showing the monthly race and the most urgent chores" width="400" />
</p>

<table>
  <tr>
    <td align="center"><img src="docs/images/backlog.png" alt="Chore backlog sorted by urgency" width="240" /><br /><sub>Backlog, most urgent first</sub></td>
    <td align="center"><img src="docs/images/scoreboard.png" alt="Monthly race with work share, podium and activity feed" width="240" /><br /><sub>Monthly race</sub></td>
    <td align="center"><img src="docs/images/store.png" alt="Reward shop with wallet and vouchers" width="240" /><br /><sub>Reward shop and wallet</sub></td>
  </tr>
</table>

## Under the hood

| | |
|---|---|
| Backend | ASP.NET Core (.NET 10) minimal API, EF Core on MariaDB, with the domain in a dependency-free `Wib.Domain` library |
| Frontend | React 19 + TypeScript + Vite, Tailwind CSS, TanStack Query; installable as a PWA |
| Auth | Auth0, members created on first login |
| Tests | xUnit domain and endpoint tests, Playwright end-to-end journeys |
| Hosting | One Docker image on a small VPS, built and published by GitHub Actions |

## Find your way around

- **[CONTEXT.md](CONTEXT.md)**: the domain language, with the exact rules for Freshness, Completion Outcomes, the Monthly Race and the shop.
- **[docs/adr/](docs/adr/)**: architecture decisions and why they were made.
- **[AGENTS.md](AGENTS.md)**: how to run and test the project locally, written for both humans and coding agents.
- **[trmnl/](trmnl/)**: the TRMNL Display plugin and its setup.
