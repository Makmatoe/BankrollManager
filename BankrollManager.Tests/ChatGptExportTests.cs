using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;

namespace BankrollManager.Tests;

[TestClass]
public sealed class ChatGptExportTests
{
    [TestMethod]
    public void ChatGptExportBuildsReasoningFriendlyMarkdown()
    {
        var date = new DateOnly(2026, 6, 9);
        var data = new BankrollData
        {
            Settings = new BankrollSettings
            {
                StartingBankroll = 20m,
                CurrencySymbol = "$",
                ProtectModeBelowBankroll = 0m,
                DailyStopLossAmount = 0m,
                MonthlyPokerStopLossPercent = 0m
            },
            LedgerEntries =
            [
                new LedgerEntry
                {
                    Date = date,
                    Type = LedgerType.Deposit,
                    Platform = Platform.Unibet,
                    Description = "Opening deposit",
                    Amount = 20m
                }
            ],
            TournamentEntries =
            [
                new TournamentEntry
                {
                    Date = date,
                    RegistrationTime = new TimeOnly(12, 0),
                    FinishedDate = date,
                    FinishedTime = new TimeOnly(14, 30),
                    Status = TournamentStatus.Finished,
                    Platform = Platform.GGPoker,
                    EventName = "Pipe | Event",
                    BuyIn = 5m,
                    ActualBullets = 1,
                    CashPrize = 9m,
                    Notes = "Good spot"
                }
            ],
            CashSessions =
            [
                new CashSession
                {
                    Date = date,
                    SessionTime = new TimeOnly(15, 45),
                    ClosedDate = date,
                    ClosedTime = new TimeOnly(16, 15),
                    Platform = Platform.HollandCasino,
                    Game = "Cash",
                    Stakes = "0.01/0.02",
                    StartStackBuyIn = 4m,
                    Cashout = 6m,
                    Notes = "First line\nSecond line"
                }
            ]
        };

        var markdown = ChatGptBankrollExporter.BuildMarkdown(data, new DateTime(2026, 6, 9, 21, 0, 0));

        StringAssert.Contains(markdown, "# Bankroll Manager ChatGPT Export");
        StringAssert.Contains(markdown, "Currency symbol for all money columns: $");
        StringAssert.Contains(markdown, "Money values are plain decimal numbers");
        StringAssert.Contains(markdown, "## Current Snapshot");
        StringAssert.Contains(markdown, "current_cash_bankroll");
        StringAssert.Contains(markdown, "## Event Timeline");
        StringAssert.Contains(markdown, "## Tournament Entries");
        StringAssert.Contains(markdown, "## Cash Sessions");
        StringAssert.Contains(markdown, "## Ledger Entries");
        StringAssert.Contains(markdown, "12:00");
        StringAssert.Contains(markdown, "14:30");
        StringAssert.Contains(markdown, "15:45");
        StringAssert.Contains(markdown, "Pipe \\| Event");
        StringAssert.Contains(markdown, "First line Second line");
    }
}
