namespace BankrollManager.Core.Models;

public sealed class BankrollData
{
    public const int CurrentDataSchemaVersion = 1;

    public int DataSchemaVersion { get; set; } = CurrentDataSchemaVersion;
    public BankrollSettings Settings { get; set; } = new();
    public List<LedgerEntry> LedgerEntries { get; set; } = [];
    public List<TournamentEntry> TournamentEntries { get; set; } = [];
    public List<TournamentPreset> TournamentPresets { get; set; } = [];
    public List<CashSession> CashSessions { get; set; } = [];
    public List<PlatformWallet> PlatformWallets { get; set; } = [];
    public DateTime LastSavedUtc { get; set; } = DateTime.UtcNow;

    public void EnsureDefaults()
    {
        if (DataSchemaVersion <= 0)
        {
            DataSchemaVersion = CurrentDataSchemaVersion;
        }
        else if (DataSchemaVersion < CurrentDataSchemaVersion)
        {
            DataSchemaVersion = CurrentDataSchemaVersion;
        }

        Settings ??= new BankrollSettings();
        Settings.EnsureDefaults();
        LedgerEntries ??= [];
        TournamentEntries ??= [];
        TournamentPresets ??= [];
        CashSessions ??= [];
        PlatformWallets ??= [];

        foreach (var platform in Enum.GetValues<Platform>())
        {
            if (PlatformWallets.All(wallet => wallet.Platform != platform))
            {
                PlatformWallets.Add(new PlatformWallet { Platform = platform });
            }
        }

        PlatformWallets = PlatformWallets
            .OrderBy(wallet => wallet.Platform.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
