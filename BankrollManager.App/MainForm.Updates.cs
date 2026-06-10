using System.Reflection;
using Velopack;
using Velopack.Exceptions;
using Velopack.Sources;

namespace BankrollManager.App;

public sealed partial class MainForm
{
    private const string UpdateRepositoryUrl = "https://github.com/Makmatoe/BankrollManager";

    private async void CheckForUpdates()
    {
        if (_updateCheckInProgress)
        {
            return;
        }

        _updateCheckInProgress = true;
        _statusLabel.Text = "Checking for updates...";

        try
        {
            var manager = CreateUpdateManager();
            if (!manager.IsInstalled)
            {
                MessageBox.Show(
                    "In-app updates are available after installing Bankroll Manager with the setup installer. Debug builds and old portable ZIP copies cannot update themselves.",
                    "Updates",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                _statusLabel.Text = "Updates require the installer build.";
                return;
            }

            if (manager.UpdatePendingRestart is not null)
            {
                ApplyPendingUpdate(manager);
                return;
            }

            var update = await manager.CheckForUpdatesAsync();
            if (update is null)
            {
                _statusLabel.Text = "Bankroll Manager is up to date.";
                MessageBox.Show(
                    "Bankroll Manager is already up to date.",
                    "Updates",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (!ConfirmUpdate(update))
            {
                _statusLabel.Text = "Update skipped.";
                return;
            }

            _statusLabel.Text = $"Downloading {UpdateVersion(update)}...";
            await manager.DownloadUpdatesAsync(update);
            _statusLabel.Text = "Update downloaded. Restarting...";
            manager.ApplyUpdatesAndRestart(update);
        }
        catch (NotInstalledException)
        {
            _statusLabel.Text = "Updates require the installer build.";
            MessageBox.Show(
                "In-app updates are available after installing Bankroll Manager with the setup installer.",
                "Updates",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception error)
        {
            _statusLabel.Text = "Update check failed.";
            MessageBox.Show(
                $"The update check failed.{Environment.NewLine}{Environment.NewLine}{error.Message}",
                "Updates",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        finally
        {
            _updateCheckInProgress = false;
        }
    }

    private static UpdateManager CreateUpdateManager()
    {
        return new UpdateManager(new GithubSource(UpdateRepositoryUrl, accessToken: null, prerelease: false));
    }

    private void ShowAbout()
    {
        var manager = CreateUpdateManager();
        var installedMode = manager.IsInstalled
            ? "Installer build - in-app updates enabled"
            : "Portable/debug build - install the setup exe to enable updates";
        var pendingUpdate = manager.UpdatePendingRestart is null
            ? "No"
            : manager.UpdatePendingRestart.Version.ToString();
        var message =
            $"Bankroll Manager {CurrentVersion()}{Environment.NewLine}{Environment.NewLine}" +
            $"Update mode: {installedMode}{Environment.NewLine}" +
            $"Update source: {UpdateRepositoryUrl}{Environment.NewLine}" +
            $"Pending restart update: {pendingUpdate}{Environment.NewLine}{Environment.NewLine}" +
            $"Data file:{Environment.NewLine}{_repository.FilePath}";

        MessageBox.Show(
            message,
            "About Bankroll Manager",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private bool ConfirmUpdate(UpdateInfo update)
    {
        var notes = string.IsNullOrWhiteSpace(update.TargetFullRelease.NotesMarkdown)
            ? "No release notes were included with this update."
            : update.TargetFullRelease.NotesMarkdown.Trim();
        var message =
            $"Bankroll Manager {UpdateVersion(update)} is available.{Environment.NewLine}{Environment.NewLine}" +
            $"{notes}{Environment.NewLine}{Environment.NewLine}" +
            "Download and install it now? The app will restart after the update is applied.";

        return MessageBox.Show(
            message,
            "Update available",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information) == DialogResult.Yes;
    }

    private void ApplyPendingUpdate(UpdateManager manager)
    {
        var result = MessageBox.Show(
            "An update has already been downloaded. Restart now to apply it?",
            "Update ready",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);
        if (result != DialogResult.Yes)
        {
            _statusLabel.Text = "Update waiting for restart.";
            return;
        }

        _statusLabel.Text = "Restarting to apply update...";
        manager.ApplyUpdatesAndRestart(manager.UpdatePendingRestart);
    }

    private static string UpdateVersion(UpdateInfo update)
    {
        return update.TargetFullRelease.Version.ToString();
    }

    private static string CurrentVersion()
    {
        var assembly = typeof(MainForm).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion;
        }

        return assembly.GetName().Version?.ToString()
            ?? "unknown";
    }
}
