# Competitor Analysis & Feature Opportunities: Household Chore Gamification

## 1. Executive Summary

This research investigates the leading household chore tracking and domestic gamification applications—specifically **Tody**, **Nipto**, **Sweepy**, and **OurHome/Habitica**—to benchmark our design for **wib ("Who is Better")**, identify potential product gaps, and uncover high-value feature opportunities.

---

## 2. Competitor Breakdown

### 2.1 Tody (The Pioneer of Floating Cadence)
* **Core Philosophy**: "Needs-based" cleaning rather than rigid calendar schedules. Eliminates "late guilt" and backlog spam.
* **Freshness Indicator ("Dirtiness" bar)**:
  * Instead of calendar deadlines (e.g. "every Tuesday"), chores have an interval (e.g. every 7 days).
  * A color-coded capsule transitions progressively: **Green (Fresh)** $\rightarrow$ **Yellow (Aging/Due Soon)** $\rightarrow$ **Red (Dirty/Overdue)**.
  * Overdue tasks become deeper red the longer they are neglected, sorting them by real urgency.
* **Key Takeaway for wib**: Validates our floating urgency ratio ($\text{Ratio} = \frac{\text{Days Since Last Done}}{\text{Cadence}}$). No piling up 5 missed occurrences when cleaning windows is late.

### 2.2 Nipto (Gamification for Couples & Roommates)
* **Core Philosophy**: Turn domestic labor into a friendly, fair game with weekly contests and agreed rewards.
* **Mechanics**:
  * Chores have point values based on effort/difficulty.
  * Scoreboard tracks weekly contributions; resets every Sunday night to prevent runaway score inflation.
  * **Winner Perks**: Winner of the week picks a reward from a customizable household reward catalog.
  * **"Bonus Points / Thank You"**: Allows one partner to grant bonus points as an appreciation token for unprompted or difficult chores.
* **Key Takeaway for wib**: Weekly/monthly resets are essential to keep both partners motivated. The "Thank you / appreciation bonus" is a proven relationship harmonizer.

### 2.3 Sweepy (Effort Tiers & Room Cleanliness)
* **Core Philosophy**: Visualizing the state of each room and sharing tasks.
* **Mechanics**:
  * Tasks assigned effort levels (1 to 3 stars/points).
  * Generates a "Cleanliness Score" per room.
* **The Critical Flaw / Community Pain Point**:
  * **No built-in Rewards Store**: Users earn points, but cannot spend them in-app. Users frequently complain about having to track rewards in spreadsheets or verbal promises.
* **Key Takeaway for wib**: Our **Voucher Wallet & Store** directly addresses the biggest missing feature in existing popular chore apps.

### 2.4 Habitica / OurHome (Token Store & Economy)
* **Core Philosophy**: RPG-like incentive loop where completed chores mint gold/points, redeemable in a custom reward shop.
* **Mechanics**:
  * Dual-currency separation: XP / Level progression vs Gold / Spending currency.
  * Users can purchase custom rewards created by themselves or family members.
* **Key Takeaway for wib**: Confirms our decision to separate **Earned Monthly XP** (competition ranking) from **Wallet Balance** (spendable currency). Spending points must never hurt competition standing.

---

## 3. High-Value Feature Opportunities for wib

Based on competitor strengths and user pain points, the following features should be prioritized or slated as near-term enhancements:

1. **Neglected Task Bounty / Multiplier (Catch-up Mechanic)**:
   * *Problem*: When a task is 200%+ overdue (e.g. window cleaning neglected for 5 months), 1 point feels insufficient for the effort, discouraging anyone from tackling it.
   * *Opportunity*: When a task reaches **Neglected (≥130%)**, automatically display an extra bounty (e.g., $+1$ or $2\times$ points) to incentivize whoever finally tackles it.
2. **Recent Activity Feed ("Visibility of Invisible Labor")**:
   * A live feed on the dashboard: *"Maciej completed: Zmywarka (1 pkt) 15 min temu"*. Domestic friction often stems from not realizing the other person did chores; visibility creates mutual appreciation.
3. **Appreciation / "Dziękuję!" Tip**:
   * A button in the activity feed or profile to send your spouse a $+1$ point tip with a heart or thumbs up.
4. **Voucher Redemption History**:
   * Showing redeemed vouchers in a "Zrealizowane" history so both partners can celebrate rewards enjoyed.

---

## 4. UI Localization (Polish)

To ensure high daily adoption, all UI strings must be native Polish, using friendly, conversational domestic terms:
* **Dashboard**: `Pulpit` / `Kto jest lepszy?`
* **Chores / Backlog**: `Zadania` / `Do zrobienia`
* **Done action**: `Zrobione!` (+1 pkt)
* **Urgency States**: `Świeże` (Green), `Wkrótce` (Yellow), `Zaległe` (Orange), `Zaniedbane` (Red)
* **Store**: `Sklep z nagrodami`
* **Wallet**: `Mój portfel` / `Kupony`
* **Activity**: `Ostatnia aktywność`
