using BankrollManager.Core.Models;

namespace BankrollManager.Core.Services;

public static class RuleEngine
{
    public static DecisionResult Evaluate(BankrollData data, DecisionRequest request, DateOnly today)
    {
        data.EnsureDefaults();
        var context = BuildContext(data, request, today);
        var settings = context.Settings;
        var plannedTournamentCost = request.BuyIn * request.PlannedBullets + request.AddOnsRebuys;
        var ticketBalance = BankrollCalculator.TicketBalance(data);

        if (!request.IsCashSession && request.TicketBuyInValue > 0m && request.TicketBuyInValue > plannedTournamentCost)
        {
            return Result(
                DecisionLabel.Pass,
                context,
                "The ticket buy-in value is larger than the planned tournament cost.",
                "Reduce the ticket amount to the buy-in plus add-ons/rebuys.");
        }

        if (!request.IsCashSession && request.TicketBuyInValue > 0m && request.TicketBuyInValue > ticketBalance)
        {
            return Result(
                DecisionLabel.Pass,
                context,
                $"Ticket buy-in value exceeds the available ticket balance ({Money(ticketBalance, settings)}).",
                "Use a smaller ticket amount or pay the remaining buy-in from bankroll.");
        }

        if (context.RiskAmount > 0m && context.CurrentBankroll <= 0m)
        {
            return Result(
                DecisionLabel.FundFirst,
                context,
                $"The bankroll is {Money(0m, settings)} and this entry costs money.",
                "Fund first or choose freeroll/centroll play.");
        }

        if (context.RiskAmount > 0m && context.StopLossStatus.BreakRequired)
        {
            return Result(
                DecisionLabel.TakeBreak,
                context,
                $"Responsible-play protection is active: {context.StopLossStatus.Explanation}.",
                "Stop for today, record notes, or use cooldown until tomorrow.");
        }

        if (context.RiskAmount > context.CurrentBankroll && context.RiskAmount > 0m)
        {
            return Result(
                DecisionLabel.Pass,
                context,
                "The planned risk is larger than the available bankroll.",
                "Reduce buy-in, skip cash today, or play a freeroll.");
        }

        if (context.Category == TournamentCategory.Reserve && context.RiskAmount > 0m)
        {
            return Result(
                DecisionLabel.Pass,
                context,
                "Reserve category is protected bankroll and cannot be spent.",
                "Move the play to a real category or skip it.");
        }

        if (context.CategoryRule.MinBankroll > 0m
            && context.RiskAmount > 0m
            && context.CurrentBankroll < context.CategoryRule.MinBankroll)
        {
            return Result(
                DecisionLabel.Pass,
                context,
                $"{context.Category} requires at least {Money(context.CategoryRule.MinBankroll, settings)} bankroll.",
                "Stay in lower-risk categories until the bankroll reaches the threshold.");
        }

        if (request.PlannedBullets > context.CategoryRule.BulletCap)
        {
            return Result(
                DecisionLabel.Pass,
                context,
                $"Planned bullets ({request.PlannedBullets}) exceed the {context.Category} cap ({context.CategoryRule.BulletCap}).",
                "One bullet only or skip the event.");
        }

        if (context.CategoryRule.DailyEntryCap > 0 && context.EntriesToday >= context.CategoryRule.DailyEntryCap)
        {
            return Result(
                DecisionLabel.Pass,
                context,
                $"{context.Category} daily entry cap is already reached ({context.EntriesToday}/{context.CategoryRule.DailyEntryCap}).",
                "Switch category, mark the session locked, or stop for today.");
        }

        if (context.LastCooldownEntryDate is { } cooldownDate)
        {
            return Result(
                DecisionLabel.Pass,
                context,
                $"{context.Category} is still cooling down after {cooldownDate:yyyy-MM-dd}.",
                $"Wait {context.CategoryRule.CooldownDays} day(s) between these entries.");
        }

        if (context.DailyRiskCap > 0m && context.DailyRiskAfter > context.DailyRiskCap)
        {
            return Result(
                DecisionLabel.Pass,
                context,
                $"Daily risk would become {Money(context.DailyRiskAfter, settings)}, above the {Money(context.DailyRiskCap, settings)} cap.",
                "Stop adding new risk today or reduce the planned buy-in.");
        }

        if (context.ActiveExposureCap > 0m && context.ActiveExposureAfter > context.ActiveExposureCap)
        {
            return Result(
                DecisionLabel.Pass,
                context,
                $"Active exposure would become {Money(context.ActiveExposureAfter, settings)}, above the {Money(context.ActiveExposureCap, settings)} cap.",
                "Close active cash/open entries first or choose a smaller spot.");
        }

        if (context.BudgetRemaining < context.RiskAmount)
        {
            return Result(
                DecisionLabel.Pass,
                context,
                $"{context.Category} monthly budget is used up for the active month.",
                "Move to freerolls/centrolls or stop for today.");
        }

        return request.IsCashSession
            ? EvaluateCash(settings, context, request)
            : EvaluateTournament(settings, context, request);
    }

    public static string EvaluateHistoricalRisk(
        BankrollSettings settings,
        bool isCashSession,
        TournamentCategory category,
        TournamentFormat format,
        decimal riskAmount,
        int bullets,
        decimal bankrollBefore,
        CashFormat cashFormat = CashFormat.HoldemCash)
    {
        settings.EnsureDefaults();
        var effectiveCategory = isCashSession ? TournamentCategory.CashPractice : category;
        var rule = settings.GetRule(effectiveCategory);
        var riskPercent = BankrollCalculator.RiskPercentage(riskAmount, bankrollBefore);

        if (riskAmount > 0m && bankrollBefore <= 0m)
        {
            return DisplayLabel(DecisionLabel.FundFirst);
        }

        if (riskAmount > 0m && rule.MinBankroll > 0m && bankrollBefore < rule.MinBankroll)
        {
            return DisplayLabel(DecisionLabel.Pass);
        }

        if (bullets > rule.BulletCap)
        {
            return DisplayLabel(DecisionLabel.Pass);
        }

        if (isCashSession)
        {
            var cashCap = EffectiveNormalRiskCap(settings, rule, effectiveCategory, format, cashFormat);
            if (riskPercent > cashCap)
            {
                return DisplayLabel(DecisionLabel.Pass);
            }

            return UsesHighCapShare(riskPercent, cashCap, settings)
                ? DisplayLabel(DecisionLabel.Review)
                : DisplayLabel(DecisionLabel.PlayOk);
        }

        var normalCap = EffectiveNormalRiskCap(settings, rule, category, format, cashFormat);
        if (riskPercent <= normalCap)
        {
            return UsesHighCapShare(riskPercent, normalCap, settings)
                ? DisplayLabel(DecisionLabel.Review)
                : DisplayLabel(DecisionLabel.PlayOk);
        }

        if (bullets == 1
            && riskPercent <= settings.ShotTowerMaxRiskPercent
            && bankrollBefore >= settings.GreenLightShotBankroll)
        {
            return category == TournamentCategory.TowerShot || format == TournamentFormat.Tower
                ? DisplayLabel(DecisionLabel.ShotOk)
                : DisplayLabel(DecisionLabel.ShotOnly);
        }

        return DisplayLabel(DecisionLabel.Pass);
    }

    public static string DisplayLabel(DecisionLabel label)
    {
        return label switch
        {
            DecisionLabel.PlayOk => "PLAY / OK",
            DecisionLabel.Review => "REVIEW",
            DecisionLabel.ShotOk => "SHOT OK",
            DecisionLabel.ShotOnly => "SHOT ONLY",
            DecisionLabel.Pass => "PASS",
            DecisionLabel.TakeBreak => "TAKE BREAK",
            DecisionLabel.FundFirst => "FUND FIRST",
            _ => label.ToString()
        };
    }

    private static DecisionResult EvaluateCash(BankrollSettings settings, RuleContext context, DecisionRequest request)
    {
        if (context.RiskPercent > context.NormalRiskCap)
        {
            return Result(
                DecisionLabel.Pass,
                context,
                $"Cash session risk is {context.RiskPercent:0.0}% and the configured cap is {context.NormalRiskCap:0.0}%.",
                "Use a smaller capped session or skip cash today.");
        }

        if (NeedsReview(context))
        {
            return Result(
                DecisionLabel.Review,
                context,
                "Cash session fits the hard rules, but one or more review thresholds are close.",
                request.CashReloads > 0m
                    ? "Start smaller, lower the reload cap, or wait for a cleaner spot."
                    : "Keep the session capped and quit quickly if the table does not look good.");
        }

        return Result(
            DecisionLabel.PlayOk,
            context,
            "Cash session is inside the configured risk, exposure and category budget limits.",
            request.CashFormat is CashFormat.AllInOrFoldHoldem or CashFormat.AllInOrFoldOmaha
                ? "Use a shorter stop-loss than normal cash and do not chase all-in variance."
                : request.CashFormat is CashFormat.RushAndCashHoldem or CashFormat.RushAndCashOmaha
                    ? "Keep the session capped and review faster because hand volume is higher."
                    : "Keep the session capped and stop at the profit-lock threshold.");
    }

    private static DecisionResult EvaluateTournament(BankrollSettings settings, RuleContext context, DecisionRequest request)
    {
        var normalRiskCap = context.NormalRiskCap;
        var categoryRule = context.CategoryRule;
        var overBuyInCap = request.BuyIn > categoryRule.DefaultBuyInCap && categoryRule.DefaultBuyInCap > 0m;
        var shotEligible = request.PlannedBullets == 1
            && context.RiskPercent <= settings.ShotTowerMaxRiskPercent
            && context.CurrentBankroll >= settings.GreenLightShotBankroll;

        if (overBuyInCap)
        {
            if (context.RiskAmount == 0m)
            {
                return Result(
                    DecisionLabel.Review,
                    context,
                    $"Buy-in is above the normal {context.Category} cap, but the ticket/cash risk is covered.",
                    "Play only if the field/format still matches the plan.");
            }

            if (shotEligible)
            {
                return Result(
                    DecisionLabel.ShotOnly,
                    context,
                    $"Buy-in is above the normal {context.Category} cap, but still below the absolute shot cap.",
                    "Treat it as a planned one-bullet shot only.");
            }

            return Result(
                DecisionLabel.Pass,
                context,
                $"Buy-in exceeds the configured {context.Category} cap.",
                "Use a lower buy-in satellite or skip this one.");
        }

        if (context.RiskPercent <= normalRiskCap)
        {
            if (NeedsReview(context))
            {
                return Result(
                    DecisionLabel.Review,
                    context,
                    "Tournament fits the hard rules, but at least one threshold is near the warning zone.",
                    "Play only with the planned bullet cap and avoid adding late re-entries.");
            }

            return Result(
                DecisionLabel.PlayOk,
                context,
                "Tournament is inside normal bankroll-management limits.",
                "Play as planned and keep bullets capped.");
        }

        if (context.CurrentBankroll < settings.GreenLightShotBankroll)
        {
            return Result(
                DecisionLabel.Pass,
                context,
                $"This is shot-level risk, but bankroll is below the {Money(settings.GreenLightShotBankroll, settings)} shot threshold.",
                "Stay with main-grind stakes until the shot threshold is reached.");
        }

        if (shotEligible)
        {
            var label = context.Category == TournamentCategory.TowerShot || request.Format == TournamentFormat.Tower
                ? DecisionLabel.ShotOk
                : DecisionLabel.ShotOnly;
            return Result(
                label,
                context,
                "This is above normal grind risk but below the absolute shot cap.",
                "One bullet only. No automatic re-entry.");
        }

        return Result(
            DecisionLabel.Pass,
            context,
            $"Risk is {context.RiskPercent:0.0}% and the configured normal cap is {normalRiskCap:0.0}%.",
            "Use a smaller event or play freeroll/centroll volume.");
    }

    private static RuleContext BuildContext(BankrollData data, DecisionRequest request, DateOnly today)
    {
        var settings = data.Settings;
        var category = request.IsCashSession ? TournamentCategory.CashPractice : request.Category;
        var categoryRule = settings.GetRule(category);
        var currentBankroll = BankrollCalculator.CurrentBankroll(data);
        var riskAmount = request.TotalPlannedRisk;
        var riskPercent = BankrollCalculator.RiskPercentage(riskAmount, currentBankroll);
        var monthStart = settings.ActiveMonthStart;
        var monthFunding = BankrollCalculator.MonthFunding(data, monthStart, today);
        var categoryBudget = monthFunding * categoryRule.MonthlyBudgetPercent / 100m;
        var budgetRemaining = BankrollCalculator.CategoryBudgetRemaining(data, category, monthStart, today);
        var dailyCommittedRisk = DailyCommittedRisk(data, today);
        var dailyRiskAfter = dailyCommittedRisk + riskAmount;
        var dailyRiskCap = currentBankroll > 0m ? currentBankroll * settings.DailyRiskCapPercent / 100m : 0m;
        var activeExposure = ActiveExposure(data);
        var activeExposureAfter = activeExposure + riskAmount;
        var activeExposureCap = currentBankroll > 0m ? currentBankroll * settings.ActiveExposureCapPercent / 100m : 0m;
        var normalRiskCap = EffectiveNormalRiskCap(settings, categoryRule, category, request.Format, request.CashFormat);
        var stopLossStatus = StopLossService.GetStatus(data, today);

        var context = new RuleContext(
            settings,
            category,
            categoryRule,
            normalRiskCap,
            currentBankroll,
            riskAmount,
            riskPercent,
            categoryBudget,
            budgetRemaining,
            budgetRemaining - riskAmount,
            dailyCommittedRisk,
            dailyRiskAfter,
            dailyRiskCap,
            activeExposure,
            activeExposureAfter,
            activeExposureCap,
            EntryCountToday(data, request, category, today),
            LastCooldownEntryDate(data, request, category, today),
            stopLossStatus,
            [],
            []);

        return context with
        {
            Thresholds = BuildThresholds(context, request),
            Warnings = BuildWarnings(context, request, data)
        };
    }

    private static List<string> BuildThresholds(RuleContext context, DecisionRequest request)
    {
        var settings = context.Settings;
        var thresholds = new List<string>
        {
            $"Risk: {Money(context.RiskAmount, settings)} ({context.RiskPercent:0.0}%) / normal cap {PercentCap(context.NormalRiskCap)}",
            context.CategoryBudget > 0m
                ? $"Category budget: {Money(context.BudgetRemaining, settings)} available; {Money(context.BudgetAfter, settings)} after this"
                : "Category budget: no spend budget configured",
            $"Daily risk: {Money(context.DailyRiskAfter, settings)} / {MoneyCap(context.DailyRiskCap, settings)}",
            $"Active exposure: {Money(context.ActiveExposureAfter, settings)} / {MoneyCap(context.ActiveExposureCap, settings)}"
        };

        if (context.CategoryRule.MinBankroll > 0m)
        {
            thresholds.Add($"Min bankroll for {context.Category}: {Money(context.CategoryRule.MinBankroll, settings)}");
        }

        if (context.CategoryRule.DailyEntryCap > 0)
        {
            thresholds.Add($"Daily {context.Category} entries: {context.EntriesToday}/{context.CategoryRule.DailyEntryCap} before this");
        }

        if (context.CategoryRule.CooldownDays > 0)
        {
            thresholds.Add($"{context.Category} cooldown: {context.CategoryRule.CooldownDays} day(s)");
        }

        if (request.IsCashSession && request.CashBuyIn > 0m && settings.CashReloadWarningPercent > 0m)
        {
            thresholds.Add($"Reloads: {Money(request.CashReloads, settings)} / {settings.CashReloadWarningPercent:0.0}% of buy-in warning");
        }

        if (settings.DailyStopLossAmount > 0m && settings.StopLossWarningPercent > 0m)
        {
            thresholds.Add($"Daily stop-loss warning starts at {settings.StopLossWarningPercent:0.0}% of {Money(settings.DailyStopLossAmount, settings)}");
        }

        return thresholds;
    }

    private static List<string> BuildWarnings(RuleContext context, DecisionRequest request, BankrollData data)
    {
        var warnings = new List<string>();
        var settings = context.Settings;

        if (UsesHighCapShare(context.RiskPercent, context.NormalRiskCap, settings))
        {
            warnings.Add($"Risk uses at least {settings.ReviewRiskCapUsagePercent:0.0}% of the normal cap.");
        }

        if (settings.BudgetWarningPercent > 0m
            && context.CategoryBudget > 0m
            && context.BudgetAfter <= context.CategoryBudget * settings.BudgetWarningPercent / 100m)
        {
            warnings.Add($"{context.Category} budget would be below {settings.BudgetWarningPercent:0.0}% after this.");
        }

        if (UsesHighCapShare(context.DailyRiskAfter, context.DailyRiskCap, settings))
        {
            warnings.Add("Daily committed risk is close to the daily risk cap.");
        }

        if (UsesHighCapShare(context.ActiveExposureAfter, context.ActiveExposureCap, settings))
        {
            warnings.Add("Open tournaments plus table cash are close to the active exposure cap.");
        }

        if (settings.DailyStopLossAmount > 0m
            && settings.StopLossWarningPercent > 0m
            && context.StopLossStatus.TodayProfitLoss <= -settings.DailyStopLossAmount * settings.StopLossWarningPercent / 100m)
        {
            warnings.Add("Today is close to the daily stop-loss.");
        }

        if (context.StopLossStatus.MonthlyStopLossLimit > 0m
            && settings.StopLossWarningPercent > 0m
            && context.StopLossStatus.ThisMonthProfitLoss <= -context.StopLossStatus.MonthlyStopLossLimit * settings.StopLossWarningPercent / 100m)
        {
            warnings.Add("This month is close to the monthly stop-loss.");
        }

        if (request.IsCashSession && request.CashBuyIn > 0m && settings.CashReloadWarningPercent > 0m)
        {
            var reloadWarningAmount = request.CashBuyIn * settings.CashReloadWarningPercent / 100m;
            if (request.CashReloads > reloadWarningAmount)
            {
                warnings.Add("Reload plan is larger than the configured cash reload warning threshold.");
            }

            if (data.CashSessions.Any(session => session.IsActive))
            {
                warnings.Add("There is already an active cash session; closing it first keeps wallet tracking cleaner.");
            }
        }

        if (request.IsCashSession)
        {
            if (IsRushAndCash(request.CashFormat))
            {
                warnings.Add("Rush & Cash can hit stop-loss faster because of higher hand volume.");
            }

            if (IsAllInOrFold(request.CashFormat))
            {
                warnings.Add("All-In or Fold is not normal cash. Use stricter stop-loss rules.");
            }
        }
        else
        {
            if (IsExtraVarianceTournamentFormat(request.Format))
            {
                warnings.Add("This format has extra variance. Do not use jackpot/multiplier potential to justify oversized buy-ins.");
            }

            if (request.Format is TournamentFormat.ReEntry or TournamentFormat.RebuyAddon)
            {
                warnings.Add("Re-entry/rebuy formats must use planned maximum cost.");
            }

            if (request.Format == TournamentFormat.FlipAndGo)
            {
                warnings.Add("Flip & Go stacks multiply your real buy-in.");
            }

            if (request.Format == TournamentFormat.SpinAndGold)
            {
                warnings.Add("Spin & Gold insurance increases total cost.");
            }

            if (IsSatellitePath(request.Format))
            {
                warnings.Add("Satellite ticket value is not the same as withdrawable cash.");
            }
        }

        var isShotRelevant = !request.IsCashSession
            && (context.Category == TournamentCategory.TowerShot
                || request.Format == TournamentFormat.Tower
                || context.RiskPercent > context.NormalRiskCap
                || (request.BuyIn > context.CategoryRule.DefaultBuyInCap && context.CategoryRule.DefaultBuyInCap > 0m));
        if (isShotRelevant && context.CurrentBankroll > 0m && context.CurrentBankroll < settings.GreenLightShotBankroll)
        {
            warnings.Add("Bankroll is still below the green-light shot threshold.");
        }

        return warnings;
    }

    private static bool NeedsReview(RuleContext context)
    {
        return context.Warnings.Count > 0;
    }

    private static bool UsesHighCapShare(decimal value, decimal cap, BankrollSettings settings)
    {
        return cap > 0m && value >= cap * settings.ReviewRiskCapUsagePercent / 100m;
    }

    private static decimal DailyCommittedRisk(BankrollData data, DateOnly today)
    {
        return data.TournamentEntries
            .Where(entry => entry.Date == today)
            .Sum(entry => entry.CashCost)
            + data.CashSessions
                .Where(entry => entry.Date == today)
                .Sum(entry => entry.SessionCost);
    }

    private static decimal ActiveExposure(BankrollData data)
    {
        return data.TournamentEntries
            .Where(entry => entry.Status != TournamentStatus.Finished)
            .Sum(entry => entry.CashCost)
            + data.CashSessions.Sum(entry => entry.ActiveTableCash);
    }

    private static int EntryCountToday(BankrollData data, DecisionRequest request, TournamentCategory category, DateOnly today)
    {
        return request.IsCashSession
            ? data.CashSessions.Count(entry => entry.Date == today)
            : data.TournamentEntries.Count(entry => entry.Date == today && entry.Category == category);
    }

    private static DateOnly? LastCooldownEntryDate(
        BankrollData data,
        DecisionRequest request,
        TournamentCategory category,
        DateOnly today)
    {
        var cooldownDays = data.Settings.GetRule(category).CooldownDays;
        if (cooldownDays <= 0)
        {
            return null;
        }

        var startDate = today.AddDays(-cooldownDays);
        var dates = request.IsCashSession
            ? data.CashSessions
                .Where(entry => entry.Date >= startDate && entry.Date < today)
                .Select(entry => entry.Date)
            : data.TournamentEntries
                .Where(entry => entry.Category == category && entry.Date >= startDate && entry.Date < today)
                .Select(entry => entry.Date);

        return dates
            .OrderDescending()
            .Select(date => (DateOnly?)date)
            .FirstOrDefault();
    }

    private static decimal NormalRiskCap(BankrollSettings settings, TournamentCategory category, TournamentFormat format)
    {
        if (category == TournamentCategory.FlipSatellite
            || format is TournamentFormat.Flip
                or TournamentFormat.SpinAndGold
                or TournamentFormat.FlipAndGo
                or TournamentFormat.AoFSitAndGo)
        {
            return settings.FlipMaxRiskPercent;
        }

        if (category == TournamentCategory.HexaProSng
            || format is TournamentFormat.SNG
                or TournamentFormat.HexaPro
                or TournamentFormat.MysteryBattleRoyale)
        {
            return settings.SngHexaProMaxRiskPercent;
        }

        if (category == TournamentCategory.TowerShot || format == TournamentFormat.Tower)
        {
            return settings.ShotTowerMaxRiskPercent;
        }

        if (category == TournamentCategory.CashPractice)
        {
            return settings.CashSessionMaxRiskPercent;
        }

        return settings.NormalMttMaxRiskPercent;
    }

    private static decimal EffectiveNormalRiskCap(
        BankrollSettings settings,
        CategoryRuleSettings rule,
        TournamentCategory category,
        TournamentFormat format,
        CashFormat cashFormat = CashFormat.HoldemCash)
    {
        var formatCap = NormalRiskCap(settings, category, format);
        var cap = rule.MaxRiskPercent > 0m && formatCap > 0m
            ? Math.Min(rule.MaxRiskPercent, formatCap)
            : Math.Max(rule.MaxRiskPercent, formatCap);

        if (category != TournamentCategory.CashPractice)
        {
            return cap;
        }

        if (IsAllInOrFold(cashFormat))
        {
            return ScaleCap(cap, 0.5m);
        }

        if (IsRushAndCash(cashFormat))
        {
            return ScaleCap(cap, 0.75m);
        }

        return cap;
    }

    private static decimal ScaleCap(decimal cap, decimal factor)
    {
        return cap > 0m ? cap * factor : cap;
    }

    private static bool IsRushAndCash(CashFormat format)
    {
        return format is CashFormat.RushAndCashHoldem or CashFormat.RushAndCashOmaha;
    }

    private static bool IsAllInOrFold(CashFormat format)
    {
        return format is CashFormat.AllInOrFoldHoldem or CashFormat.AllInOrFoldOmaha;
    }

    private static bool IsSatellitePath(TournamentFormat format)
    {
        return format is TournamentFormat.Satellite
            or TournamentFormat.TurboSatellite
            or TournamentFormat.TargetStackSatellite
            or TournamentFormat.FlashSatellite
            or TournamentFormat.WSOPExpress;
    }

    private static bool IsExtraVarianceTournamentFormat(TournamentFormat format)
    {
        return format is TournamentFormat.MysteryBounty
            or TournamentFormat.SpinAndGold
            or TournamentFormat.FlipAndGo
            or TournamentFormat.MysteryBattleRoyale
            or TournamentFormat.AoFSitAndGo;
    }

    private static DecisionResult Result(
        DecisionLabel label,
        RuleContext context,
        string explanation,
        string saferAlternative)
    {
        return new DecisionResult(
            label,
            DisplayLabel(label),
            context.CurrentBankroll,
            context.RiskAmount,
            context.RiskPercent,
            context.BudgetRemaining,
            explanation,
            saferAlternative,
            context.Warnings,
            context.Thresholds);
    }

    private static string Money(decimal value, BankrollSettings settings)
    {
        var sign = value < 0m ? "-" : string.Empty;
        return $"{sign}{settings.CurrencySymbol}{Math.Abs(value):0.00}";
    }

    private static string MoneyCap(decimal value, BankrollSettings settings)
    {
        return value > 0m ? Money(value, settings) : "disabled";
    }

    private static string PercentCap(decimal value)
    {
        return value > 0m ? $"{value:0.0}%" : "disabled";
    }

    private sealed record RuleContext(
        BankrollSettings Settings,
        TournamentCategory Category,
        CategoryRuleSettings CategoryRule,
        decimal NormalRiskCap,
        decimal CurrentBankroll,
        decimal RiskAmount,
        decimal RiskPercent,
        decimal CategoryBudget,
        decimal BudgetRemaining,
        decimal BudgetAfter,
        decimal DailyCommittedRisk,
        decimal DailyRiskAfter,
        decimal DailyRiskCap,
        decimal ActiveExposure,
        decimal ActiveExposureAfter,
        decimal ActiveExposureCap,
        int EntriesToday,
        DateOnly? LastCooldownEntryDate,
        StopLossStatus StopLossStatus,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Thresholds);
}
