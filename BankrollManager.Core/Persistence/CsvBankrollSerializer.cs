using System.Globalization;
using System.Text;
using BankrollManager.Core.Formatting;
using BankrollManager.Core.Models;
using BankrollManager.Core.Services;

namespace BankrollManager.Core.Persistence;

public static class CsvBankrollSerializer
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static void ExportToFolder(BankrollData data, string folderPath)
    {
        data.EnsureDefaults();
        BankrollCalculator.RecalculateTrackingFields(data);
        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "ledger.csv"), BuildLedgerCsv(data), Encoding.UTF8);
        File.WriteAllText(Path.Combine(folderPath, "tournaments.csv"), BuildTournamentCsv(data), Encoding.UTF8);
        File.WriteAllText(Path.Combine(folderPath, "cash.csv"), BuildCashCsv(data), Encoding.UTF8);
        File.WriteAllText(Path.Combine(folderPath, "wallets.csv"), BuildWalletCsv(data), Encoding.UTF8);
        File.WriteAllText(Path.Combine(folderPath, "daily.csv"), BuildDailyCsv(data), Encoding.UTF8);
        File.WriteAllText(Path.Combine(folderPath, "monthly.csv"), BuildMonthlyCsv(data), Encoding.UTF8);
    }

    public static BankrollData ImportFromFolder(string folderPath, BankrollSettings? settings = null)
    {
        var data = new BankrollData { Settings = settings ?? new BankrollSettings() };
        var ledgerPath = Path.Combine(folderPath, "ledger.csv");
        var tournamentsPath = Path.Combine(folderPath, "tournaments.csv");
        var cashPath = Path.Combine(folderPath, "cash.csv");
        var walletsPath = Path.Combine(folderPath, "wallets.csv");

        if (File.Exists(ledgerPath))
        {
            data.LedgerEntries = ParseLedger(File.ReadAllText(ledgerPath));
        }

        if (File.Exists(tournamentsPath))
        {
            data.TournamentEntries = ParseTournaments(File.ReadAllText(tournamentsPath));
        }

        if (File.Exists(cashPath))
        {
            data.CashSessions = ParseCash(File.ReadAllText(cashPath));
        }

        if (File.Exists(walletsPath))
        {
            data.PlatformWallets = ParseWallets(File.ReadAllText(walletsPath));
        }

        data.EnsureDefaults();
        BankrollCalculator.RecalculateTrackingFields(data);
        return data;
    }

    private static string BuildLedgerCsv(BankrollData data)
    {
        var rows = new List<string[]>
        {
            new[] { "Id", "Date", "Type", "Platform", "Description", "Amount", "Category", "Notes", "CashBankrollBefore", "CashBankrollAfter" }
        };
        rows.AddRange(data.LedgerEntries.Select(entry => new[]
        {
            entry.Id.ToString(),
            entry.Date.ToString("yyyy-MM-dd", Culture),
            entry.Type.ToString(),
            entry.Platform.ToString(),
            entry.Description,
            Money(entry.Amount),
            entry.Category.ToString(),
            entry.Notes,
            Money(entry.BankrollBefore),
            Money(entry.BankrollAfter)
        }));
        return WriteRows(rows);
    }

    private static string BuildTournamentCsv(BankrollData data)
    {
        var rows = new List<string[]>
        {
            new[]
            {
                "Id", "Date", "Time", "Platform", "Category", "Format", "EventName", "BuyIn",
                "PlannedBullets", "ActualBullets", "AddOnsRebuys", "BountyTicketValue",
                "CashPrize", "TotalCost", "NetProfit", "ROI", "Placement", "FieldSize",
                "ITM", "FinalTable", "RiskPercent", "RuleCheckResult", "CashBankrollAfter",
                "PreGameFocus", "Tags", "MistakeLesson", "Notes", "CashBankrollBefore",
                "Status", "FinishedDate", "FinishedTime", "TicketBuyInValue", "TicketBuyInPlatform", "TicketValueWon",
                "CashCost", "TicketBalanceImpact", "TotalValueProfitLoss", "ValueROI",
                "Currency", "FeeRake", "EventTag", "IsPromoFreebieTicketEvent",
                "TournamentDollarsWon", "CashDollarsWon", "RegularCashPrize", "MysteryBountyPrize",
                "BountyPhaseReached", "KnockoutsAfterBountyPhase", "MysteryBountyNotes",
                "BountyPrize", "Knockouts", "SpinPlayerCount", "InsuranceUsed", "InsuranceCost",
                "MultiplierHit", "PrizeWon", "FlipBuyInPerStack", "FlipStacksBought",
                "FlipPhaseWon", "GoPhaseReached", "RushStageSurvived", "BattleRoyaleFinalTableReached",
                "TargetEventName", "TargetEventBuyIn", "TicketWon", "Qualified", "TicketConvertedRealized",
                "WsopExpressStepNumber", "TicketUsedValue", "TargetPackageEvent"
            }
        };
        rows.AddRange(data.TournamentEntries.Select(entry => new[]
        {
            entry.Id.ToString(),
            entry.Date.ToString("yyyy-MM-dd", Culture),
            FormatTime(entry.RegistrationTime),
            entry.Platform.ToString(),
            entry.Category.ToString(),
            entry.Format.ToString(),
            entry.EventName,
            Money(entry.BuyIn),
            entry.PlannedBullets.ToString(Culture),
            entry.ActualBullets.ToString(Culture),
            Money(entry.AddOnsRebuys),
            Money(entry.BountyTicketValue),
            Money(entry.CashPrize),
            Money(entry.TotalCost),
            Money(entry.NetProfit),
            entry.ROI.ToString("0.####", Culture),
            entry.Placement?.ToString(Culture) ?? string.Empty,
            entry.FieldSize?.ToString(Culture) ?? string.Empty,
            entry.ITM.ToString(Culture),
            entry.FinalTable.ToString(Culture),
            entry.RiskPercentageOfBankrollAtRegistration.ToString("0.####", Culture),
            entry.RuleCheckResult,
            Money(entry.BankrollAfter),
            entry.PreGameFocus,
            entry.Tags,
            entry.MistakeLesson,
            entry.Notes,
            Money(entry.BankrollBefore),
            entry.Status.ToString(),
            FormatDate(entry.FinishedDate),
            FormatTime(entry.FinishedTime),
            Money(entry.TicketBuyInValue),
            entry.TicketBuyInPlatform?.ToString() ?? string.Empty,
            Money(entry.TicketValueWon),
            Money(entry.CashCost),
            Money(entry.TicketBalanceImpact),
            Money(entry.TotalValueProfitLoss),
            entry.ValueROI.ToString("0.####", Culture),
            entry.Currency,
            Money(entry.FeeRake),
            entry.EventTag.ToString(),
            entry.IsPromoFreebieTicketEvent.ToString(Culture),
            Money(entry.TournamentDollarsWon),
            Money(entry.CashDollarsWon),
            Money(entry.RegularCashPrize),
            Money(entry.MysteryBountyPrize),
            entry.BountyPhaseReached.ToString(Culture),
            entry.KnockoutsAfterBountyPhase?.ToString(Culture) ?? string.Empty,
            entry.MysteryBountyNotes,
            Money(entry.BountyPrize),
            entry.Knockouts?.ToString(Culture) ?? string.Empty,
            entry.SpinPlayerCount?.ToString(Culture) ?? string.Empty,
            entry.InsuranceUsed.ToString(Culture),
            Money(entry.InsuranceCost),
            entry.MultiplierHit.ToString("0.####", Culture),
            Money(entry.PrizeWon),
            Money(entry.FlipBuyInPerStack),
            entry.FlipStacksBought.ToString(Culture),
            entry.FlipPhaseWon.ToString(Culture),
            entry.GoPhaseReached.ToString(Culture),
            entry.RushStageSurvived.ToString(Culture),
            entry.BattleRoyaleFinalTableReached.ToString(Culture),
            entry.TargetEventName,
            Money(entry.TargetEventBuyIn),
            entry.TicketWon.ToString(Culture),
            entry.Qualified.ToString(Culture),
            entry.TicketConvertedRealized.ToString(Culture),
            entry.WsopExpressStepNumber?.ToString(Culture) ?? string.Empty,
            Money(entry.TicketUsedValue),
            entry.TargetPackageEvent
        }));
        return WriteRows(rows);
    }

    private static string BuildCashCsv(BankrollData data)
    {
        var rows = new List<string[]>
        {
            new[]
            {
                "Id", "Date", "Time", "Platform", "Game", "Stakes", "BigBlindAmount",
                "StartStackBuyIn", "Reloads", "Cashout", "Minutes", "Hands",
                "SessionCost", "NetProfit", "BBWon", "BBPer100", "RiskPercent",
                "RuleCheckResult", "CashBankrollAfter", "Notes", "CashBankrollBefore",
                "Status", "ClosedDate", "ClosedTime", "ReloadCap", "ActiveTableCash",
                "WalletCashImpact", "Format", "SmallBlindAmount", "CashDropWon", "JackpotFortunePrizeWon"
            }
        };
        rows.AddRange(data.CashSessions.Select(entry => new[]
        {
            entry.Id.ToString(),
            entry.Date.ToString("yyyy-MM-dd", Culture),
            FormatTime(entry.SessionTime),
            entry.Platform.ToString(),
            entry.Game,
            entry.Stakes,
            Money(entry.BigBlindAmount),
            Money(entry.StartStackBuyIn),
            Money(entry.Reloads),
            Money(entry.Cashout),
            entry.Minutes?.ToString(Culture) ?? string.Empty,
            entry.Hands?.ToString(Culture) ?? string.Empty,
            Money(entry.SessionCost),
            Money(entry.NetProfit),
            entry.BBWon.ToString("0.####", Culture),
            entry.BBPer100.ToString("0.####", Culture),
            entry.RiskPercentageOfBankrollAtSessionStart.ToString("0.####", Culture),
            entry.RuleCheckResult,
            Money(entry.BankrollAfter),
            entry.Notes,
            Money(entry.BankrollBefore),
            entry.Status.ToString(),
            FormatDate(entry.ClosedDate),
            FormatTime(entry.ClosedTime),
            Money(entry.ReloadCap),
            Money(entry.ActiveTableCash),
            Money(entry.WalletCashImpact),
            entry.Format.ToString(),
            Money(entry.SmallBlindAmount),
            Money(entry.CashDropWon),
            Money(entry.JackpotFortunePrizeWon)
        }));
        return WriteRows(rows);
    }

    private static string BuildWalletCsv(BankrollData data)
    {
        var rows = new List<string[]>
        {
            new[] { "Platform", "ActualCashBalance", "AcceptedCashDifference", "LastUpdatedDate", "Notes" }
        };
        rows.AddRange(data.PlatformWallets.Select(wallet => new[]
        {
            wallet.Platform.ToString(),
            wallet.ActualCashBalance.HasValue ? Money(wallet.ActualCashBalance.Value) : string.Empty,
            wallet.AcceptedCashDifference.HasValue ? Money(wallet.AcceptedCashDifference.Value) : string.Empty,
            FormatDate(wallet.LastUpdatedDate),
            wallet.Notes
        }));
        return WriteRows(rows);
    }

    private static string BuildDailyCsv(BankrollData data)
    {
        var rows = new List<string[]>
        {
            new[] { "Date", "TournamentCashPL", "CashSessionPL", "TicketPL", "TotalCashPL", "TotalValuePL", "Sessions", "Hours", "CashPerHour", "ValuePerHour", "RunningMonthCashPL", "RunningLifetimeCashBankroll", "RunningLifetimeBankrollValue" }
        };
        rows.AddRange(BankrollCalculator.GetDailySummaries(data).Select(summary => new[]
        {
            summary.Date.ToString("yyyy-MM-dd", Culture),
            Money(summary.TournamentProfitLoss),
            Money(summary.CashProfitLoss),
            Money(summary.TicketProfitLoss),
            Money(summary.TotalProfitLoss),
            Money(summary.TotalValueProfitLoss),
            summary.NumberOfSessions.ToString(Culture),
            summary.HoursPlayed.ToString("0.####", Culture),
            Money(summary.CashPerHour),
            Money(summary.ValuePerHour),
            Money(summary.RunningMonthProfitLoss),
            Money(summary.RunningLifetimeBankroll),
            Money(summary.RunningLifetimeBankrollValue)
        }));
        return WriteRows(rows);
    }

    private static string BuildMonthlyCsv(BankrollData data)
    {
        var rows = new List<string[]>
        {
            new[]
            {
                "Month", "Deposits", "Withdrawals", "TournamentCashPL", "CashSessionPL",
                "TicketPL", "TotalCashPL", "TotalValuePL", "Tournaments", "CashSessions", "AverageTournamentBuyIn",
                "Hours", "CashPerHour", "ValuePerHour", "BiggestWin", "BiggestLoss", "StopLossBreaches", "Notes"
            }
        };
        rows.AddRange(BankrollCalculator.GetMonthlySummaries(data).Select(summary => new[]
        {
            BankrollDateFormatter.FormatMonth(summary.Month, Culture),
            Money(summary.Deposits),
            Money(summary.Withdrawals),
            Money(summary.TournamentProfitLoss),
            Money(summary.CashProfitLoss),
            Money(summary.TicketProfitLoss),
            Money(summary.TotalPokerProfitLoss),
            Money(summary.TotalValueProfitLoss),
            summary.NumberOfTournaments.ToString(Culture),
            summary.NumberOfCashSessions.ToString(Culture),
            Money(summary.AverageTournamentBuyIn),
            summary.HoursPlayed.ToString("0.####", Culture),
            Money(summary.CashPerHour),
            Money(summary.ValuePerHour),
            Money(summary.BiggestWin),
            Money(summary.BiggestLoss),
            summary.StopLossBreaches.ToString(Culture),
            summary.Notes
        }));
        return WriteRows(rows);
    }

    private static List<LedgerEntry> ParseLedger(string csv)
    {
        var rows = ParseRows(csv);
        if (rows.Count == 0)
        {
            return [];
        }

        var header = new CsvHeader(rows[0]);
        return rows.Skip(1).Select(row => new LedgerEntry
        {
            Id = ParseGuid(header.Get(row, "Id", 0)),
            Date = ParseDate(header.Get(row, "Date", 1)),
            Type = ParseEnum(header.Get(row, "Type", 2), LedgerType.Other),
            Platform = ParseEnum(header.Get(row, "Platform", 3), Platform.Other),
            Description = header.Get(row, "Description", 4),
            Amount = ParseDecimal(header.Get(row, "Amount", 5)),
            Category = ParseEnum(header.Get(row, "Category", 6), TournamentCategory.Other),
            Notes = header.Get(row, "Notes", 7)
        }).ToList();
    }

    private static List<TournamentEntry> ParseTournaments(string csv)
    {
        var rows = ParseRows(csv);
        if (rows.Count == 0)
        {
            return [];
        }

        var header = new CsvHeader(rows[0]);
        var hasTimeColumn = header.Has("Time");
        var offset = hasTimeColumn ? 1 : 0;

        return rows.Skip(1).Select(row => new TournamentEntry
        {
            Id = ParseGuid(header.Get(row, "Id", 0)),
            Date = ParseDate(header.Get(row, "Date", 1)),
            RegistrationTime = hasTimeColumn ? ParseTime(header.Get(row, "Time", 2)) : null,
            Status = ParseEnum(header.Get(row, "Status"), TournamentStatus.Finished),
            FinishedDate = ParseNullableDate(header.Get(row, "FinishedDate")),
            FinishedTime = ParseTime(header.Get(row, "FinishedTime")),
            Platform = ParseEnum(header.Get(row, "Platform", 2 + offset), Platform.Other),
            Category = ParseEnum(header.Get(row, "Category", 3 + offset), TournamentCategory.Other),
            Format = ParseEnum(header.Get(row, "Format", 4 + offset), TournamentFormat.Other),
            EventName = header.Get(row, "EventName", 5 + offset),
            Currency = header.Get(row, "Currency"),
            EventTag = ParseEnum(header.Get(row, "EventTag"), EventTag.None),
            IsPromoFreebieTicketEvent = ParseBool(header.Get(row, "IsPromoFreebieTicketEvent")),
            BuyIn = ParseDecimal(header.Get(row, "BuyIn", 6 + offset)),
            FeeRake = ParseDecimal(header.Get(row, "FeeRake")),
            PlannedBullets = ParseInt(header.Get(row, "PlannedBullets", 7 + offset)) ?? 1,
            ActualBullets = ParseInt(header.Get(row, "ActualBullets", 8 + offset)) ?? 1,
            AddOnsRebuys = ParseDecimal(header.Get(row, "AddOnsRebuys", 9 + offset)),
            BountyTicketValue = ParseDecimal(header.Get(row, "BountyTicketValue", 10 + offset)),
            TicketBuyInValue = ParseDecimal(header.Get(row, "TicketBuyInValue")),
            TicketBuyInPlatform = ParseNullableEnum<Platform>(header.Get(row, "TicketBuyInPlatform")),
            TicketValueWon = ParseDecimal(header.Get(row, "TicketValueWon")),
            CashPrize = ParseDecimal(header.Get(row, "CashPrize", 11 + offset)),
            TournamentDollarsWon = ParseDecimal(header.Get(row, "TournamentDollarsWon")),
            CashDollarsWon = ParseDecimal(header.Get(row, "CashDollarsWon")),
            RegularCashPrize = ParseDecimal(header.Get(row, "RegularCashPrize")),
            MysteryBountyPrize = ParseDecimal(header.Get(row, "MysteryBountyPrize")),
            BountyPhaseReached = ParseBool(header.Get(row, "BountyPhaseReached")),
            KnockoutsAfterBountyPhase = ParseInt(header.Get(row, "KnockoutsAfterBountyPhase")),
            MysteryBountyNotes = header.Get(row, "MysteryBountyNotes"),
            BountyPrize = ParseDecimal(header.Get(row, "BountyPrize")),
            Knockouts = ParseInt(header.Get(row, "Knockouts")),
            SpinPlayerCount = ParseInt(header.Get(row, "SpinPlayerCount")),
            InsuranceUsed = ParseBool(header.Get(row, "InsuranceUsed")),
            InsuranceCost = ParseDecimal(header.Get(row, "InsuranceCost")),
            MultiplierHit = ParseDecimal(header.Get(row, "MultiplierHit")),
            PrizeWon = ParseDecimal(header.Get(row, "PrizeWon")),
            FlipBuyInPerStack = ParseDecimal(header.Get(row, "FlipBuyInPerStack")),
            FlipStacksBought = ParseInt(header.Get(row, "FlipStacksBought")) ?? 0,
            FlipPhaseWon = ParseBool(header.Get(row, "FlipPhaseWon")),
            GoPhaseReached = ParseBool(header.Get(row, "GoPhaseReached")),
            RushStageSurvived = ParseBool(header.Get(row, "RushStageSurvived")),
            BattleRoyaleFinalTableReached = ParseBool(header.Get(row, "BattleRoyaleFinalTableReached")),
            TargetEventName = header.Get(row, "TargetEventName"),
            TargetEventBuyIn = ParseDecimal(header.Get(row, "TargetEventBuyIn")),
            TicketWon = ParseBool(header.Get(row, "TicketWon")),
            Qualified = ParseBool(header.Get(row, "Qualified")),
            TicketConvertedRealized = ParseBool(header.Get(row, "TicketConvertedRealized")),
            WsopExpressStepNumber = ParseInt(header.Get(row, "WsopExpressStepNumber")),
            TicketUsedValue = ParseDecimal(header.Get(row, "TicketUsedValue")),
            TargetPackageEvent = header.Get(row, "TargetPackageEvent"),
            Placement = ParseInt(header.Get(row, "Placement", 15 + offset)),
            FieldSize = ParseInt(header.Get(row, "FieldSize", 16 + offset)),
            ITM = ParseBool(header.Get(row, "ITM", 17 + offset)),
            FinalTable = ParseBool(header.Get(row, "FinalTable", 18 + offset)),
            PreGameFocus = header.Get(row, "PreGameFocus", 22 + offset),
            Tags = header.Get(row, "Tags", 23 + offset),
            MistakeLesson = header.Get(row, "MistakeLesson", 24 + offset),
            Notes = header.Get(row, "Notes", 25 + offset)
        }).ToList();
    }

    private static List<CashSession> ParseCash(string csv)
    {
        var rows = ParseRows(csv);
        if (rows.Count == 0)
        {
            return [];
        }

        var header = new CsvHeader(rows[0]);
        var hasTimeColumn = header.Has("Time");
        var offset = hasTimeColumn ? 1 : 0;

        return rows.Skip(1).Select(row => new CashSession
        {
            Id = ParseGuid(header.Get(row, "Id", 0)),
            Date = ParseDate(header.Get(row, "Date", 1)),
            SessionTime = hasTimeColumn ? ParseTime(header.Get(row, "Time", 2)) : null,
            Status = ParseEnum(header.Get(row, "Status"), CashSessionStatus.Finished),
            ClosedDate = ParseNullableDate(header.Get(row, "ClosedDate")),
            ClosedTime = ParseTime(header.Get(row, "ClosedTime")),
            Platform = ParseEnum(header.Get(row, "Platform", 2 + offset), Platform.Other),
            Format = ParseEnum(header.Get(row, "Format"), CashFormat.HoldemCash),
            Game = header.Get(row, "Game", 3 + offset),
            Stakes = header.Get(row, "Stakes", 4 + offset),
            SmallBlindAmount = ParseDecimal(header.Get(row, "SmallBlindAmount")),
            BigBlindAmount = ParseDecimal(header.Get(row, "BigBlindAmount", 5 + offset)),
            StartStackBuyIn = ParseDecimal(header.Get(row, "StartStackBuyIn", 6 + offset)),
            Reloads = ParseDecimal(header.Get(row, "Reloads", 7 + offset)),
            ReloadCap = ParseDecimal(header.Get(row, "ReloadCap")),
            Cashout = ParseDecimal(header.Get(row, "Cashout", 8 + offset)),
            CashDropWon = ParseDecimal(header.Get(row, "CashDropWon")),
            JackpotFortunePrizeWon = ParseDecimal(header.Get(row, "JackpotFortunePrizeWon")),
            Minutes = ParseInt(header.Get(row, "Minutes", 9 + offset)),
            Hands = ParseInt(header.Get(row, "Hands", 10 + offset)),
            Notes = header.Get(row, "Notes", 18 + offset)
        }).ToList();
    }

    private static List<PlatformWallet> ParseWallets(string csv)
    {
        var rows = ParseRows(csv);
        if (rows.Count == 0)
        {
            return [];
        }

        var header = new CsvHeader(rows[0]);
        return rows.Skip(1).Select(row =>
        {
            var acceptedDifference = header.Has("AcceptedCashDifference")
                ? header.Get(row, "AcceptedCashDifference")
                : string.Empty;
            return new PlatformWallet
            {
                Platform = ParseEnum(header.Get(row, "Platform", 0), Platform.Other),
                ActualCashBalance = string.IsNullOrWhiteSpace(header.Get(row, "ActualCashBalance", 1))
                    ? null
                    : ParseDecimal(header.Get(row, "ActualCashBalance", 1)),
                AcceptedCashDifference = string.IsNullOrWhiteSpace(acceptedDifference)
                    ? null
                    : ParseDecimal(acceptedDifference),
                LastUpdatedDate = ParseNullableDate(header.Get(row, "LastUpdatedDate", 3)),
                Notes = header.Get(row, "Notes", 4)
            };
        }).ToList();
    }

    private static string WriteRows(IEnumerable<string[]> rows)
    {
        return string.Join(Environment.NewLine, rows.Select(row => string.Join(",", row.Select(Escape))));
    }

    private static string Escape(string value)
    {
        value = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
        return value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    private static List<string[]> ParseRows(string csv)
    {
        var rows = new List<string[]>();
        foreach (var line in csv.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            rows.Add(ParseLine(line).ToArray());
        }

        return rows;
    }

    private static List<string> ParseLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (character == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        values.Add(current.ToString());
        return values;
    }

    private static string Get(string[] row, int index)
    {
        return index >= 0 && index < row.Length ? row[index] : string.Empty;
    }

    private static string Money(decimal value)
    {
        return MoneyFormatter.Format(value, culture: Culture);
    }

    private static string FormatTime(TimeOnly? value)
    {
        return BankrollDateFormatter.FormatTime(value, Culture);
    }

    private static string FormatDate(DateOnly? value)
    {
        return BankrollDateFormatter.FormatDate(value, Culture);
    }

    private static decimal ParseDecimal(string value)
    {
        return MoneyParser.ParseOrDefault(value, culture: Culture);
    }

    private static int? ParseInt(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, Culture, out var result) ? result : null;
    }

    private static bool ParseBool(string value)
    {
        return bool.TryParse(value, out var result) && result;
    }

    private static DateOnly ParseDate(string value)
    {
        return BankrollDateParser.ParseDateOrDefault(value, DateOnly.FromDateTime(DateTime.Today), Culture);
    }

    private static DateOnly? ParseNullableDate(string value)
    {
        return BankrollDateParser.ParseNullableDate(value, Culture);
    }

    private static TimeOnly? ParseTime(string value)
    {
        return BankrollDateParser.ParseNullableTime(value, Culture);
    }

    private static Guid ParseGuid(string value)
    {
        return Guid.TryParse(value, out var result) ? result : Guid.NewGuid();
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) ? result : fallback;
    }

    private static TEnum? ParseNullableEnum<TEnum>(string value) where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) ? result : null;
    }

    private sealed class CsvHeader
    {
        private readonly Dictionary<string, int> _columns;

        public CsvHeader(IEnumerable<string> columns)
        {
            _columns = columns
                .Select((name, index) => new { Name = name.Trim(), Index = index })
                .Where(column => !string.IsNullOrWhiteSpace(column.Name))
                .GroupBy(column => column.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);
        }

        public bool Has(string columnName)
        {
            return _columns.ContainsKey(columnName);
        }

        public string Get(string[] row, string columnName, int fallbackIndex = -1)
        {
            return CsvBankrollSerializer.Get(
                row,
                _columns.TryGetValue(columnName, out var index) ? index : fallbackIndex);
        }
    }
}
