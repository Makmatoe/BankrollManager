using BankrollManager.Core.Models;
using BankrollManager.Core.Formatting;

namespace BankrollManager.Core.Services;

public static class TournamentEvCalculator
{
    public static TournamentEvResult Evaluate(TournamentEvRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var buyIn = Math.Max(0m, request.BuyIn);
        var currentEntries = Math.Max(1, request.CurrentEntries);
        var totalPrizeValue = TotalPrizeValue(request);
        var maxSinglePrizeValue = MaxSinglePrizeValue(request, totalPrizeValue);
        var uncappedGrossEv = totalPrizeValue / currentEntries;
        var grossEv = Math.Min(uncappedGrossEv, maxSinglePrizeValue);
        var netEv = grossEv - buyIn;
        var roi = buyIn > 0m
            ? netEv / buyIn
            : 0m;
        var thresholds = CalculateThresholds(totalPrizeValue, maxSinglePrizeValue, buyIn);
        var variance = EvaluateVariance(request, buyIn, totalPrizeValue, maxSinglePrizeValue);

        return new TournamentEvResult(
            totalPrizeValue,
            maxSinglePrizeValue,
            uncappedGrossEv,
            grossEv,
            netEv,
            roi,
            thresholds.ExactBreakEvenEntries,
            thresholds.CanBreakEven,
            thresholds.NegativeEvStartsAt,
            thresholds.MaxPositiveEntries,
            StatusFor(netEv),
            variance);
    }

    public static decimal TotalPrizeValue(TournamentEvRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.PrizeType switch
        {
            TournamentEvPrizeType.CashPrizePool => Math.Max(0m, request.ManualPrizeValue),
            _ => Math.Max(0, request.NumberOfTickets) * DiscountedTicketValue(request)
        };
    }

    public static decimal MaxSinglePrizeValue(TournamentEvRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.PrizeType switch
        {
            TournamentEvPrizeType.CashPrizePool => Math.Max(0m, request.ManualPrizeValue),
            _ => request.NumberOfTickets > 0 ? DiscountedTicketValue(request) : 0m
        };
    }

    private static decimal MaxSinglePrizeValue(TournamentEvRequest request, decimal totalPrizeValue)
    {
        return request.PrizeType switch
        {
            TournamentEvPrizeType.CashPrizePool => totalPrizeValue,
            _ => MaxSinglePrizeValue(request)
        };
    }

    private static decimal DiscountedTicketValue(TournamentEvRequest request)
    {
        return Math.Max(0m, request.TicketValue)
            * Math.Clamp(request.TicketValueDiscountPercent, 0m, 100m)
            / 100m;
    }

    private static TournamentEvVarianceResult EvaluateVariance(
        TournamentEvRequest request,
        decimal buyIn,
        decimal totalPrizeValue,
        decimal maxSinglePrizeValue)
    {
        var sampleSize = Math.Max(1, request.SampleSize);
        var totalEntries = Math.Max(1, request.TotalEntries > 0 ? request.TotalEntries : request.CurrentEntries);
        var paidPlaces = Math.Clamp(
            request.PaidPlaces > 0 ? request.PaidPlaces : request.NumberOfTickets,
            0,
            totalEntries);

        if (request.TournamentType == TournamentEvTournamentType.FlatTicketSatellite)
        {
            return EvaluateFlatTicketVariance(
                request,
                buyIn,
                totalPrizeValue,
                maxSinglePrizeValue,
                totalEntries,
                paidPlaces,
                sampleSize);
        }

        return EvaluatePayoutVariance(
            request,
            buyIn,
            totalPrizeValue,
            totalEntries,
            paidPlaces,
            sampleSize);
    }

    private static TournamentEvVarianceResult EvaluateFlatTicketVariance(
        TournamentEvRequest request,
        decimal buyIn,
        decimal totalPrizeValue,
        decimal maxSinglePrizeValue,
        int totalEntries,
        int paidPlaces,
        int sampleSize)
    {
        var winChance = totalEntries > 0 ? (decimal)paidPlaces / totalEntries : 0m;
        var adjustedTicketValue = FlatTicketPrizeValue(request, totalPrizeValue, maxSinglePrizeValue, paidPlaces);
        var ev = winChance * adjustedTicketValue - buyIn;
        var roi = buyIn > 0m ? ev / buyIn : 0m;
        var standardDeviation = adjustedTicketValue * Sqrt(winChance * (1m - winChance));
        var standardDeviationInBuyIns = buyIn > 0m ? standardDeviation / buyIn : 0m;
        var expectedAfterSample = sampleSize * ev;
        var standardDeviationAfterSample = Sqrt(sampleSize) * standardDeviation;
        var chanceNotAhead = ChanceNotAheadForFlatTicket(sampleSize, winChance, adjustedTicketValue, buyIn);

        return BuildVarianceResult(
            ev,
            roi,
            winChance,
            standardDeviation,
            standardDeviationInBuyIns,
            sampleSize,
            expectedAfterSample,
            standardDeviationAfterSample,
            chanceNotAhead,
            chanceNotAheadIsExact: true,
            request.BankrollSize);
    }

    private static TournamentEvVarianceResult EvaluatePayoutVariance(
        TournamentEvRequest request,
        decimal buyIn,
        decimal totalPrizeValue,
        int totalEntries,
        int paidPlaces,
        int sampleSize)
    {
        var payouts = BuildFinishingPositionPayouts(request, totalPrizeValue, totalEntries, paidPlaces);
        var profits = payouts.Select(payout => payout - buyIn).ToList();
        var ev = profits.Count == 0 ? -buyIn : profits.Average();
        var roi = buyIn > 0m ? ev / buyIn : 0m;
        var variance = profits.Count == 0
            ? 0m
            : profits.Sum(profit => Square(profit - ev)) / profits.Count;
        var standardDeviation = Sqrt(variance);
        var standardDeviationInBuyIns = buyIn > 0m ? standardDeviation / buyIn : 0m;
        var winOrCashProbability = totalEntries > 0
            ? (decimal)payouts.Count(payout => payout > 0m) / totalEntries
            : 0m;
        var expectedAfterSample = sampleSize * ev;
        var standardDeviationAfterSample = Sqrt(sampleSize) * standardDeviation;
        var chanceNotAhead = ChanceNotAheadByNormalApproximation(expectedAfterSample, standardDeviationAfterSample);

        return BuildVarianceResult(
            ev,
            roi,
            winOrCashProbability,
            standardDeviation,
            standardDeviationInBuyIns,
            sampleSize,
            expectedAfterSample,
            standardDeviationAfterSample,
            chanceNotAhead,
            chanceNotAheadIsExact: false,
            request.BankrollSize);
    }

    private static TournamentEvVarianceResult BuildVarianceResult(
        decimal ev,
        decimal roi,
        decimal winOrCashProbability,
        decimal standardDeviation,
        decimal standardDeviationInBuyIns,
        int sampleSize,
        decimal expectedAfterSample,
        decimal standardDeviationAfterSample,
        decimal chanceNotAhead,
        bool chanceNotAheadIsExact,
        decimal bankrollSize)
    {
        return new TournamentEvVarianceResult(
            ev,
            roi,
            winOrCashProbability,
            standardDeviation,
            standardDeviationInBuyIns,
            sampleSize,
            expectedAfterSample,
            standardDeviationAfterSample,
            expectedAfterSample - standardDeviationAfterSample,
            expectedAfterSample + standardDeviationAfterSample,
            Math.Clamp(chanceNotAhead, 0m, 1m),
            chanceNotAheadIsExact,
            bankrollSize > 0m ? standardDeviationAfterSample / bankrollSize : 0m,
            VarianceRating(standardDeviationInBuyIns));
    }

    private static decimal FlatTicketPrizeValue(
        TournamentEvRequest request,
        decimal totalPrizeValue,
        decimal maxSinglePrizeValue,
        int paidPlaces)
    {
        if (request.PrizeType == TournamentEvPrizeType.CashPrizePool)
        {
            return paidPlaces > 0 ? totalPrizeValue / paidPlaces : totalPrizeValue;
        }

        if (maxSinglePrizeValue > 0m)
        {
            return maxSinglePrizeValue;
        }

        return paidPlaces > 0 ? totalPrizeValue / paidPlaces : 0m;
    }

    private static List<decimal> BuildFinishingPositionPayouts(
        TournamentEvRequest request,
        decimal totalPrizeValue,
        int totalEntries,
        int paidPlaces)
    {
        var payouts = new decimal[totalEntries];
        var customPayouts = request.Payouts.Count > 0
            ? request.Payouts.Where(payout => payout > 0m).ToList()
            : ParsePayoutStructure(request.PayoutStructure);

        if (customPayouts.Count > 0)
        {
            for (var index = 0; index < Math.Min(totalEntries, customPayouts.Count); index++)
            {
                payouts[index] = customPayouts[index];
            }

            return payouts.ToList();
        }

        if (totalPrizeValue <= 0m || paidPlaces <= 0)
        {
            return payouts.ToList();
        }

        if (request.TournamentType == TournamentEvTournamentType.WinnerTakeAll)
        {
            payouts[0] = totalPrizeValue;
            return payouts.ToList();
        }

        var curve = request.TournamentType == TournamentEvTournamentType.TopHeavyMtt
            ? 1.15d
            : 0.55d;
        var weights = Enumerable.Range(1, paidPlaces)
            .Select(rank => 1d / Math.Pow(rank, curve))
            .ToArray();
        var totalWeight = weights.Sum();

        for (var index = 0; index < paidPlaces; index++)
        {
            payouts[index] = totalWeight > 0d
                ? totalPrizeValue * (decimal)(weights[index] / totalWeight)
                : 0m;
        }

        return payouts.ToList();
    }

    private static List<decimal> ParsePayoutStructure(string payoutStructure)
    {
        if (string.IsNullOrWhiteSpace(payoutStructure))
        {
            return [];
        }

        return payoutStructure
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split(['\n', ';', '|', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part =>
            {
                var value = part.Contains(':')
                    ? part[(part.LastIndexOf(':') + 1)..]
                    : part;
                return MoneyParser.ParseOrDefault(value, 0m);
            })
            .Where(value => value > 0m)
            .ToList();
    }

    private static decimal ChanceNotAheadForFlatTicket(
        int sampleSize,
        decimal winChance,
        decimal adjustedTicketValue,
        decimal buyIn)
    {
        if (adjustedTicketValue <= 0m)
        {
            return buyIn > 0m ? 1m : 0m;
        }

        var maxWinsNotAhead = (int)Math.Floor((double)(sampleSize * buyIn / adjustedTicketValue));
        if (maxWinsNotAhead < 0)
        {
            return 0m;
        }

        if (maxWinsNotAhead >= sampleSize)
        {
            return 1m;
        }

        return (decimal)BinomialCdf(sampleSize, (double)winChance, maxWinsNotAhead);
    }

    private static double BinomialCdf(int trials, double probability, int successes)
    {
        if (successes < 0)
        {
            return 0d;
        }

        if (successes >= trials)
        {
            return 1d;
        }

        if (probability <= 0d)
        {
            return 1d;
        }

        if (probability >= 1d)
        {
            return successes >= trials ? 1d : 0d;
        }

        var failureProbability = 1d - probability;
        var term = Math.Pow(failureProbability, trials);
        var sum = term;
        for (var wins = 1; wins <= successes; wins++)
        {
            term *= (trials - wins + 1) / (double)wins * probability / failureProbability;
            sum += term;
        }

        return Math.Clamp(sum, 0d, 1d);
    }

    private static decimal ChanceNotAheadByNormalApproximation(decimal expectedAfterSample, decimal standardDeviationAfterSample)
    {
        if (standardDeviationAfterSample <= 0m)
        {
            return expectedAfterSample <= 0m ? 1m : 0m;
        }

        var z = (double)((0m - expectedAfterSample) / standardDeviationAfterSample);
        return (decimal)NormalCdf(z);
    }

    private static double NormalCdf(double value)
    {
        return 0.5d * (1d + Erf(value / Math.Sqrt(2d)));
    }

    private static double Erf(double value)
    {
        var sign = Math.Sign(value);
        value = Math.Abs(value);
        var t = 1d / (1d + 0.3275911d * value);
        var y = 1d - (((((1.061405429d * t - 1.453152027d) * t) + 1.421413741d) * t - 0.284496736d) * t + 0.254829592d) * t * Math.Exp(-value * value);
        return sign * y;
    }

    private static TournamentEvVarianceRating VarianceRating(decimal standardDeviationInBuyIns)
    {
        return standardDeviationInBuyIns switch
        {
            < 1.5m => TournamentEvVarianceRating.Low,
            < 3m => TournamentEvVarianceRating.Medium,
            < 6m => TournamentEvVarianceRating.High,
            _ => TournamentEvVarianceRating.Extreme
        };
    }

    private static decimal Sqrt(decimal value)
    {
        return value <= 0m ? 0m : (decimal)Math.Sqrt((double)value);
    }

    private static decimal Sqrt(int value)
    {
        return value <= 0 ? 0m : (decimal)Math.Sqrt(value);
    }

    private static decimal Square(decimal value)
    {
        return value * value;
    }

    private static (decimal ExactBreakEvenEntries, bool CanBreakEven, long NegativeEvStartsAt, long MaxPositiveEntries) CalculateThresholds(
        decimal totalPrizeValue,
        decimal maxSinglePrizeValue,
        decimal buyIn)
    {
        if (buyIn <= 0m)
        {
            return totalPrizeValue > 0m
                ? (0m, true, long.MaxValue, long.MaxValue)
                : (0m, true, 0L, 0L);
        }

        if (totalPrizeValue <= 0m || maxSinglePrizeValue < buyIn)
        {
            return (0m, false, 1L, 0L);
        }

        var exactBreakEvenEntries = totalPrizeValue / buyIn;
        var negativeEvStartsAt = Math.Max(1L, (long)Math.Floor(exactBreakEvenEntries) + 1L);
        var maxPositiveEntries = maxSinglePrizeValue > buyIn
            ? Math.Max(0L, (long)Math.Ceiling(exactBreakEvenEntries) - 1L)
            : 0L;
        return (exactBreakEvenEntries, true, negativeEvStartsAt, maxPositiveEntries);
    }

    private static TournamentEvStatus StatusFor(decimal netEv)
    {
        return netEv switch
        {
            > 0m => TournamentEvStatus.Positive,
            < 0m => TournamentEvStatus.Negative,
            _ => TournamentEvStatus.Breakeven
        };
    }
}
