using BankrollManager.App.Forms;
using BankrollManager.Core.Services;

namespace BankrollManager.App;

public sealed partial class MainForm
{
    private void ShowFirstRunSetupIfNeeded()
    {
        if (FirstRunSetupService.ShouldPrompt(_data))
        {
            ShowQuickSetup();
        }
    }

    private void ShowQuickSetup()
    {
        using var dialog = new FirstRunSetupDialog(_data);
        var result = dialog.ShowDialog(this);
        if (result == DialogResult.OK)
        {
            FirstRunSetupService.Apply(_data, dialog.Options);
            SaveData("Setup complete.");
            return;
        }

        if (dialog.SetupSkipped)
        {
            FirstRunSetupService.Skip(_data);
            SaveData("Setup skipped.");
        }
    }
}
