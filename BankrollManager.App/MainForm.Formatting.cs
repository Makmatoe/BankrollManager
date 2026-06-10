using System.ComponentModel;
using System.Globalization;
using System.Drawing.Drawing2D;
using BankrollManager.App.Controls;
using BankrollManager.App.Forms;
using BankrollManager.Core.Formatting;
using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;
using Microsoft.Win32;

namespace BankrollManager.App;

public sealed partial class MainForm
{

    private static Color LabelColor(DecisionLabel label)
    {
        return label switch
        {
            DecisionLabel.PlayOk => Theme.Positive,
            DecisionLabel.Review => Theme.Warning,
            DecisionLabel.ShotOk => Theme.Accent,
            DecisionLabel.ShotOnly => Theme.Warning,
            DecisionLabel.Pass => Theme.Negative,
            DecisionLabel.TakeBreak => Theme.Negative,
            DecisionLabel.FundFirst => Theme.Warning,
            _ => Theme.Text
        };
    }

    private static Color RuleColor(string ruleText)
    {
        return ruleText switch
        {
            "PLAY / OK" => Theme.Positive,
            "REVIEW" => Theme.Warning,
            "SHOT OK" => Theme.Accent,
            "SHOT ONLY" => Theme.Warning,
            "PASS" => Theme.Negative,
            "TAKE BREAK" => Theme.Negative,
            "FUND FIRST" => Theme.Warning,
            _ => Theme.Text
        };
    }

    private static Color RuleBackColor(string ruleText)
    {
        return ruleText switch
        {
            "PLAY / OK" => Theme.PositiveSurface,
            "REVIEW" => Theme.WarningSurface,
            "SHOT OK" => Theme.AccentSurface,
            "SHOT ONLY" => Theme.WarningSurface,
            "PASS" => Theme.NegativeSurface,
            "TAKE BREAK" => Theme.DangerSurface,
            "FUND FIRST" => Theme.WarningSurface,
            _ => Theme.Panel
        };
    }


    private string Money(decimal value)
    {
        return MoneyFormatter.Format(value, _data.Settings.CurrencySymbol, CultureInfo.CurrentCulture);
    }

    private string CleanNumber(string value)
    {
        return MoneyParser.Clean(value, _data.Settings.CurrencySymbol);
    }

    private bool TryParseDecimal(string value, out decimal result)
    {
        return MoneyParser.TryParse(value, out result, _data.Settings.CurrencySymbol, CultureInfo.CurrentCulture);
    }
}
