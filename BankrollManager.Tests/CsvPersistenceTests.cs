using System.Globalization;
using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;
using static BankrollManager.Tests.TestAssertions;

namespace BankrollManager.Tests;

[TestClass]
public sealed class CsvPersistenceTests
{
    [TestMethod]
    public void CsvImportUsesHeadersForCashAndWalletColumns()
    {
        var folder = Path.Combine(Path.GetTempPath(), "BankrollManagerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(
                Path.Combine(folder, "cash.csv"),
                string.Join(Environment.NewLine,
                    "Notes,Status,ReloadCap,Date,Id,Platform,Game,Stakes,BigBlindAmount,StartStackBuyIn,Reloads,Cashout,Minutes,Hands,Time,ClosedDate,ClosedTime",
                    $"Imported active,Active,\"\u20ac1,50\",09-06-2026,{Guid.NewGuid()},HollandCasino,Cash,0.01/0.02,0.02,2.00,0.50,0,10,20,12:34,,"));
            File.WriteAllText(
                Path.Combine(folder, "wallets.csv"),
                string.Join(Environment.NewLine,
                    "Notes,LastUpdatedDate,ActualCashBalance,Platform",
                    "Checked,09-06-2026,\"\u20ac7,00\",HollandCasino"));

            var data = CsvBankrollSerializer.ImportFromFolder(
                folder,
                new BankrollSettings { ProtectModeBelowBankroll = 0m });

            var cash = data.CashSessions.Single();
            var wallet = data.PlatformWallets.Single(wallet => wallet.Platform == Platform.HollandCasino);

            Assert.AreEqual(CashSessionStatus.Active, cash.Status);
            Assert.AreEqual(Platform.HollandCasino, cash.Platform);
            Assert.AreEqual(new DateOnly(2026, 6, 9), cash.Date);
            AssertMoney(1.50m, cash.ReloadCap);
            Assert.AreEqual(new TimeOnly(12, 34), cash.SessionTime);
            AssertMoney(7m, wallet.ActualCashBalance ?? 0m);
            Assert.AreEqual(new DateOnly(2026, 6, 9), wallet.LastUpdatedDate);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    public void CsvRoundTripsTicketBuyInPlatform()
    {
        var folder = Path.Combine(Path.GetTempPath(), "BankrollManagerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            CsvBankrollSerializer.ExportToFolder(
                new BankrollData
                {
                    TournamentEntries =
                    [
                        new TournamentEntry
                        {
                            Platform = Platform.Unibet,
                            EventName = "Ticket target",
                            BuyIn = 5m,
                            ActualBullets = 1,
                            TicketBuyInValue = 5m,
                            TicketBuyInPlatform = Platform.GGPoker
                        }
                    ]
                },
                folder);

            var exported = File.ReadAllText(Path.Combine(folder, "tournaments.csv"));
            StringAssert.Contains(exported, "TicketBuyInPlatform");

            var imported = CsvBankrollSerializer.ImportFromFolder(folder);
            var tournament = imported.TournamentEntries.Single();

            Assert.AreEqual(Platform.GGPoker, tournament.TicketBuyInPlatform);
            Assert.AreEqual(Platform.GGPoker, tournament.EffectiveTicketBuyInPlatform);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
