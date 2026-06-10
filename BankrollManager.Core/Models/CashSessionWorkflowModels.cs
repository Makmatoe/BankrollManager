namespace BankrollManager.Core.Models;

public sealed record CashSessionCloseDetails(
    DateOnly ClosedDate,
    TimeOnly? ClosedTime,
    decimal Reloads,
    decimal Cashout,
    int? Minutes,
    int? Hands,
    string Notes,
    decimal CashDropWon = 0m,
    decimal JackpotFortunePrizeWon = 0m);
