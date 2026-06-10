using BankrollManager.Core.Models;

namespace BankrollManager.Core.Services;

public static class CashSessionWorkflowService
{
    public static CashSession CreateActiveDraft(DateTime now, Platform platform)
    {
        return new CashSession
        {
            Date = DateOnly.FromDateTime(now),
            SessionTime = TimeOnly.FromDateTime(now),
            Status = CashSessionStatus.Active,
            Platform = platform,
            Game = "Cash"
        };
    }

    public static CashSession CreateClosedDraft(DateTime now, Platform platform)
    {
        return new CashSession
        {
            Date = DateOnly.FromDateTime(now),
            SessionTime = TimeOnly.FromDateTime(now),
            Status = CashSessionStatus.Finished,
            ClosedDate = DateOnly.FromDateTime(now),
            ClosedTime = TimeOnly.FromDateTime(now),
            Platform = platform,
            Game = "Cash"
        };
    }

    public static void MarkActive(CashSession session)
    {
        session.Status = CashSessionStatus.Active;
        session.ClosedDate = null;
        session.ClosedTime = null;
        session.Cashout = 0m;
        session.CashDropWon = 0m;
        session.JackpotFortunePrizeWon = 0m;
        session.Minutes = null;
        session.Hands = null;
    }

    public static void MarkClosed(CashSession session, CashSessionCloseDetails details)
    {
        session.Status = CashSessionStatus.Finished;
        session.ClosedDate = details.ClosedDate;
        session.ClosedTime = details.ClosedTime;
        session.Reloads = details.Reloads;
        session.Cashout = details.Cashout;
        session.CashDropWon = details.CashDropWon;
        session.JackpotFortunePrizeWon = details.JackpotFortunePrizeWon;
        session.Minutes = details.Minutes is > 0
            ? details.Minutes
            : CalculateTrackedMinutes(session.Date, session.SessionTime, details.ClosedDate, details.ClosedTime);
        session.Hands = details.Hands;
        session.Notes = details.Notes;
    }

    public static int? CalculateTrackedMinutes(
        DateOnly startDate,
        TimeOnly? startTime,
        DateOnly? closedDate,
        TimeOnly? closedTime)
    {
        if (startTime is not { } startedAt || closedDate is not { } closedOn || closedTime is not { } closedAt)
        {
            return null;
        }

        var started = startDate.ToDateTime(startedAt);
        var closed = closedOn.ToDateTime(closedAt);
        if (closed < started)
        {
            return null;
        }

        return (int)(closed - started).TotalMinutes;
    }
}
