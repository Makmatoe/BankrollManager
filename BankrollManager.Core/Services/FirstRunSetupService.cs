using BankrollManager.Core.Models;

namespace BankrollManager.Core.Services;

public static class FirstRunSetupService
{
    public static bool ShouldPrompt(BankrollData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        data.EnsureDefaults();
        return !data.Settings.FirstRunSetupCompleted && !HasUserData(data);
    }

    public static bool HasUserData(BankrollData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        data.EnsureDefaults();
        return data.Settings.StartingBankroll != 0m
            || data.LedgerEntries.Count > 0
            || data.TournamentEntries.Count > 0
            || data.CashSessions.Count > 0
            || data.TournamentPresets.Count > 0
            || data.PlatformWallets.Any(wallet =>
                wallet.ActualCashBalance.HasValue
                || wallet.LastUpdatedDate.HasValue
                || !string.IsNullOrWhiteSpace(wallet.Notes));
    }

    public static void Apply(BankrollData data, FirstRunSetupOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(options);

        data.EnsureDefaults();
        var enabledPlatforms = NormalizeEnabledPlatforms(options.EnabledPlatforms);
        var defaultPlatform = enabledPlatforms.Contains(options.DefaultPlatform)
            ? options.DefaultPlatform
            : enabledPlatforms[0];
        var depositPlatform = enabledPlatforms.Contains(options.DepositPlatform)
            ? options.DepositPlatform
            : defaultPlatform;
        var fundingAmount = Math.Max(0m, options.FundingAmount);

        data.Settings.CurrencySymbol = string.IsNullOrWhiteSpace(options.CurrencySymbol)
            ? "\u20ac"
            : options.CurrencySymbol.Trim();
        data.Settings.EnabledPlatforms = enabledPlatforms;
        data.Settings.DefaultPlatform = defaultPlatform;
        data.Settings.ActiveReviewYear = options.SetupDate.Year;
        data.Settings.StartingBankroll = options.FundingMode == FirstRunFundingMode.StartingBankroll
            ? fundingAmount
            : 0m;

        if (options.FundingMode == FirstRunFundingMode.DepositEntry && fundingAmount > 0m)
        {
            data.LedgerEntries.Add(new LedgerEntry
            {
                Date = options.SetupDate,
                Type = LedgerType.Deposit,
                Platform = depositPlatform,
                Description = "Initial bankroll setup",
                Amount = fundingAmount,
                Category = TournamentCategory.MainGrind,
                Notes = "Created by first-run setup."
            });
        }

        ApplyPlatformBalances(data, options, enabledPlatforms);
        MarkCompleted(data);
        BankrollCalculator.RecalculateTrackingFields(data);
    }

    public static void Skip(BankrollData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        data.EnsureDefaults();
        MarkCompleted(data);
    }

    private static List<Platform> NormalizeEnabledPlatforms(IEnumerable<Platform>? platforms)
    {
        var enabledPlatforms = (platforms ?? [])
            .Where(Enum.IsDefined)
            .Distinct()
            .ToList();

        if (enabledPlatforms.Count == 0)
        {
            throw new ArgumentException("At least one platform must be enabled.", nameof(platforms));
        }

        return enabledPlatforms;
    }

    private static void ApplyPlatformBalances(
        BankrollData data,
        FirstRunSetupOptions options,
        IReadOnlyCollection<Platform> enabledPlatforms)
    {
        foreach (var (platform, balance) in options.PlatformBalances)
        {
            if (!enabledPlatforms.Contains(platform) || balance is null)
            {
                continue;
            }

            var wallet = data.PlatformWallets.First(wallet => wallet.Platform == platform);
            wallet.ActualCashBalance = Math.Max(0m, balance.Value);
            wallet.LastUpdatedDate = options.SetupDate;
        }
    }

    private static void MarkCompleted(BankrollData data)
    {
        data.Settings.FirstRunSetupCompleted = true;
        data.Settings.FirstRunSetupCompletedUtc ??= DateTime.UtcNow;
    }
}
