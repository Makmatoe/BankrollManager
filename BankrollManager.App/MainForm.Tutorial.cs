using System.ComponentModel;
using System.Globalization;
using System.Drawing.Drawing2D;
using BankrollManager.App.Controls;
using BankrollManager.App.Forms;
using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;
using Microsoft.Win32;

namespace BankrollManager.App;

public sealed partial class MainForm
{

    private Panel BuildTutorialPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Right,
            Width = TutorialPanelWidth(),
            Visible = false,
            AutoScroll = true,
            BackColor = Theme.Panel,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(14)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = Theme.Panel,
            Margin = new Padding(0)
        };
        panel.Controls.Add(layout);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = Theme.Panel,
            Margin = new Padding(0, 0, 0, 12)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        var title = Theme.Label("Interactive Tutorial", Theme.HeaderFont, Theme.Text);
        title.AutoSize = false;
        title.Dock = DockStyle.Fill;
        title.TextAlign = ContentAlignment.MiddleLeft;
        title.Margin = new Padding(0);
        header.Controls.Add(title, 0, 0);
        var hide = Theme.Button("Hide");
        hide.AutoSize = false;
        hide.Dock = DockStyle.Top;
        hide.Height = Theme.ButtonHeight;
        hide.Margin = new Padding(8, 0, 0, 0);
        hide.Click += (_, _) =>
        {
            panel.Visible = false;
            _statusLabel.Text = "Tutorial hidden. Use the Tutorial button to resume.";
        };
        header.Controls.Add(hide, 1, 0);
        layout.Controls.Add(header);

        _tutorialProgressLabel = TutorialLabel(Theme.SubHeaderFont, Theme.Muted);
        layout.Controls.Add(_tutorialProgressLabel);

        _tutorialProgressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 12,
            Minimum = 0,
            Maximum = 100,
            Margin = new Padding(0, 0, 0, 12)
        };
        layout.Controls.Add(_tutorialProgressBar);

        _tutorialStepList = new ListBox
        {
            BackColor = Theme.PanelAlt,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = Theme.BodyFont,
            IntegralHeight = false,
            Height = 166,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 14)
        };
        _tutorialStepList.Items.AddRange(_tutorialSteps
            .OrderBy(step => step.Title, NaturalSortComparer.Instance)
            .Select(step => step.Title)
            .Cast<object>()
            .ToArray());
        _tutorialStepList.SelectedIndexChanged += (_, _) =>
        {
            if (_syncingTutorialList || _tutorialStepList.SelectedIndex < 0)
            {
                return;
            }

            ShowTutorialStep(_tutorialStepList.SelectedIndex);
        };
        layout.Controls.Add(_tutorialStepList);

        _tutorialTitle = TutorialLabel(Theme.SubHeaderFont, Theme.Text);
        _tutorialTitle.Margin = new Padding(0, 0, 0, 8);
        layout.Controls.Add(_tutorialTitle);

        _tutorialBody = TutorialLabel(Theme.BodyFont, Theme.Muted);
        _tutorialBody.Margin = new Padding(0, 0, 0, 12);
        layout.Controls.Add(_tutorialBody);

        _tutorialChecklist = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Theme.Panel,
            Margin = new Padding(0, 0, 0, 14)
        };
        layout.Controls.Add(_tutorialChecklist);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Theme.Panel,
            Margin = new Padding(0)
        };
        _tutorialTryButton = Theme.Button("Try this");
        _tutorialTryButton.Click += (_, _) => RunTutorialAction();
        _tutorialPreviousButton = Theme.Button("Previous");
        _tutorialPreviousButton.Click += (_, _) => MoveTutorial(-1);
        _tutorialNextButton = Theme.Button("Next");
        _tutorialNextButton.Click += (_, _) => MoveTutorial(1);
        var restart = Theme.Button("Restart");
        restart.Click += (_, _) => RestartTutorial();
        var complete = Theme.Button("Done");
        complete.Click += (_, _) => CompleteTutorial();
        actions.Controls.Add(_tutorialTryButton);
        actions.Controls.Add(_tutorialPreviousButton);
        actions.Controls.Add(_tutorialNextButton);
        actions.Controls.Add(restart);
        actions.Controls.Add(complete);
        layout.Controls.Add(actions);

        return panel;
    }

    private int TutorialPanelWidth()
    {
        return Math.Clamp(ClientSize.Width / 3, 360, 430);
    }

    private void UpdateTutorialPanelWidth()
    {
        if (_tutorialPanel is null || _tutorialPanel.IsDisposed)
        {
            return;
        }

        _tutorialPanel.Width = TutorialPanelWidth();
    }

    private static Label TutorialLabel(Font font, Color color)
    {
        return new Label
        {
            AutoSize = true,
            MaximumSize = new Size(382, 0),
            Font = font,
            ForeColor = color,
            BackColor = Theme.Panel,
            Margin = new Padding(0, 0, 0, 6),
            UseMnemonic = false
        };
    }

    private IReadOnlyList<TutorialStep> BuildTutorialSteps()
    {
        return
        [
            new TutorialStep(
                "1. Read the cockpit",
                "Overview",
                "Use Overview as the health check before you play. It keeps the important state visible without forcing you through every table.",
                [
                    "Check the stop-loss banner first.",
                    "Open Needs attention when it shows a warning.",
                    "Use Recent activity to sanity-check bankroll movement."
                ],
                "Open Overview",
                () => NavigateToPage("Overview")),
            new TutorialStep(
                "2. Set your defaults",
                "Settings",
                "Set the defaults once so every new decision starts from your real bankroll rules instead of from memory.",
                [
                    "Choose appearance and default platform.",
                    "Set active month start and starting bankroll.",
                    "Review risk caps, stop-loss, protect mode, and cooldown settings."
                ],
                "Open Settings",
                () => NavigateToPage("Settings")),
            new TutorialStep(
                "3. Reconcile wallets",
                "Wallets",
                "Platform wallets compare expected cash against the amount you see on each poker site. This is where small tracking mistakes become obvious.",
                [
                    "Use Reconcile to enter the actual platform balance.",
                    "Use Transfer when money moves between platforms.",
                    "Expected wallet plus On Tables should explain platform exposure."
                ],
                "Open Wallets",
                () => NavigateToPage("Wallets")),
            new TutorialStep(
                "4. Decide before buying in",
                "Decide",
                "Decide turns the bankroll rules into a clear play, review, shot, pass, or break signal before you register.",
                [
                    "Pick platform, category, format, buy-in, and planned bullets.",
                    "Use presets for tournaments you play often.",
                    "Read warnings for GGPoker formats like Spin & Gold, Flip & Go, and All-In or Fold."
                ],
                "Load Example",
                LoadDecisionTutorialExample),
            new TutorialStep(
                "5. Log tournaments",
                "MTTs",
                "The MTT table is the audit trail for registrations, finishes, tickets, bounties, presets, and format-specific GGPoker fields.",
                [
                    "Use Add for a fresh tournament.",
                    "Use Finish to enter the final result after play.",
                    "Use Save Preset for recurring tournaments so the next entry is faster."
                ],
                "Open Add MTT",
                OpenTournamentTutorialDraft),
            new TutorialStep(
                "6. Track tickets correctly",
                "MTTs",
                "Ticket value is tracked separately from cash. That keeps a satellite win useful without pretending it is withdrawable money.",
                [
                    "Use Ticket Won when a tournament pays a ticket.",
                    "Use Ticket buy-in when a ticket pays for a future entry.",
                    "Mark ticket realized only when it becomes cash value."
                ],
                "Open MTTs",
                () => NavigateToPage("MTTs")),
            new TutorialStep(
                "7. Run cash sessions",
                "Cash",
                "Cash is intentionally a start-close workflow: start moves money to the table, close returns the cashout and result.",
                [
                    "Use Start Cash before sitting down.",
                    "Use Close Cash when leaving the table.",
                    "For GGPoker, select Rush & Cash or All-In or Fold so warnings and extra prizes are tracked."
                ],
                "Open Start Cash",
                OpenCashTutorialDraft),
            new TutorialStep(
                "8. Review patterns",
                "Timeline",
                "Use review pages after the session, not during the heat of play. They show whether the bankroll story adds up over time.",
                [
                    "Timeline explains the event-by-event bankroll path.",
                    "Day and Month show stop-loss pressure.",
                    "Year and comparison tabs show where profit/loss is coming from."
                ],
                "Open Timeline",
                () => NavigateToPage("Timeline")),
            new TutorialStep(
                "9. Use safety controls",
                "Settings",
                "Lock Today and Cooldown are there for the moment when the best bankroll decision is to stop adding risk.",
                [
                    "Lock Today blocks new play for the current date.",
                    "Cooldown blocks play until the configured date.",
                    "Clear removes the lock when you intentionally reset it."
                ],
                "Open Settings",
                () => NavigateToPage("Settings")),
            new TutorialStep(
                "10. Back up and export",
                "Overview",
                "Use backups and exports before larger edits or after a clean reconciliation. Boring, but it saves future-you from detective work.",
                [
                    "Use Backup before big cleanup sessions.",
                    "Use JSON for a full app backup.",
                    "Use CSV when you want spreadsheet review."
                ],
                "Show Options",
                ShowBackupExportGuide),
            new TutorialStep(
                "11. GGPoker quick guide",
                "Decide",
                "The quick guide is the compact reminder for special GGPoker formats and what to record for each one.",
                [
                    "Spin & Gold: multiplier, insurance, prize, placement.",
                    "Flip & Go: buy-in per stack and stack count.",
                    "Satellites and WSOP Express: ticket value stays separate from cash."
                ],
                "Open Guide",
                ShowGgPokerGuide)
        ];
    }

    private void StartTutorial(bool restart = false)
    {
        if (_tutorialSteps.Count == 0)
        {
            return;
        }

        var wasCompleted = _data.Settings.TutorialCompleted;
        _tutorialPanel.Visible = true;
        _tutorialPanel.BringToFront();
        if (restart || wasCompleted)
        {
            _data.Settings.TutorialCompleted = false;
            _data.Settings.TutorialStepIndex = 0;
            _data.Settings.TutorialCompletedTasks.Clear();
        }

        var index = _data.Settings.TutorialCompleted
            ? 0
            : Math.Clamp(_data.Settings.TutorialStepIndex, 0, _tutorialSteps.Count - 1);
        ShowTutorialStep(index);
        _statusLabel.Text = restart || wasCompleted
            ? "Tutorial restarted."
            : "Tutorial resumed.";
    }

    private void RestartTutorial()
    {
        StartTutorial(restart: true);
    }

    private void ShowTutorialStep(int index, bool navigate = true)
    {
        if (_tutorialSteps.Count == 0)
        {
            return;
        }

        _tutorialStepIndex = Math.Clamp(index, 0, _tutorialSteps.Count - 1);
        var step = _tutorialSteps[_tutorialStepIndex];
        if (navigate)
        {
            NavigateToPage(step.TargetTab);
        }

        _tutorialTitle.Text = step.Title;
        _tutorialBody.Text = step.Body;
        _tutorialChecklist.SuspendLayout();
        _tutorialChecklist.Controls.Clear();
        for (var taskIndex = 0; taskIndex < step.Tasks.Length; taskIndex++)
        {
            _tutorialChecklist.Controls.Add(BuildTutorialTask(step.Tasks[taskIndex], taskIndex));
        }
        _tutorialChecklist.ResumeLayout();
        UpdateTutorialProgress(step);

        _tutorialTryButton.Text = step.ActionLabel;
        _tutorialTryButton.Visible = step.TryAction is not null;
        _tutorialPreviousButton.Enabled = _tutorialStepIndex > 0;
        _tutorialNextButton.Text = _tutorialStepIndex == _tutorialSteps.Count - 1 ? "Finish" : "Next";
        _syncingTutorialList = true;
        _tutorialStepList.SelectedIndex = _tutorialStepIndex;
        _syncingTutorialList = false;

        _data.Settings.TutorialCompleted = false;
        _data.Settings.TutorialStepIndex = _tutorialStepIndex;
        _repository.Save(_data);
        _tutorialPanel.BringToFront();
    }

    private Control BuildTutorialTask(string text, int taskIndex)
    {
        var key = TutorialTaskKey(_tutorialStepIndex, taskIndex);
        var task = new CheckBox
        {
            Text = text,
            Checked = _data.Settings.TutorialCompletedTasks.Contains(key, StringComparer.Ordinal),
            AutoSize = true,
            MaximumSize = new Size(376, 0),
            ForeColor = Theme.Text,
            BackColor = Theme.Panel,
            Font = Theme.BodyFont,
            Margin = new Padding(0, 0, 0, 8),
            UseMnemonic = false
        };
        task.CheckedChanged += (_, _) =>
        {
            if (task.Checked)
            {
                if (!_data.Settings.TutorialCompletedTasks.Contains(key, StringComparer.Ordinal))
                {
                    _data.Settings.TutorialCompletedTasks.Add(key);
                }
            }
            else
            {
                _data.Settings.TutorialCompletedTasks.RemoveAll(value => string.Equals(value, key, StringComparison.Ordinal));
            }

            _repository.Save(_data);
            UpdateTutorialProgress(_tutorialSteps[_tutorialStepIndex]);
        };
        return task;
    }

    private void UpdateTutorialProgress(TutorialStep step)
    {
        var completedTasks = CompletedTaskCount(_tutorialStepIndex);
        var totalTasks = Math.Max(1, step.Tasks.Length);
        var stepFraction = completedTasks / (decimal)totalTasks;
        _tutorialProgressLabel.Text = $"Step {_tutorialStepIndex + 1} of {_tutorialSteps.Count}  |  {completedTasks}/{step.Tasks.Length} tasks checked";
        _tutorialProgressBar.Value = Math.Clamp(
            (int)Math.Round((_tutorialStepIndex + stepFraction) * 100m / _tutorialSteps.Count),
            0,
            100);
    }

    private int CompletedTaskCount(int stepIndex)
    {
        return _tutorialSteps[stepIndex].Tasks
            .Select((_, taskIndex) => TutorialTaskKey(stepIndex, taskIndex))
            .Count(key => _data.Settings.TutorialCompletedTasks.Contains(key, StringComparer.Ordinal));
    }

    private static string TutorialTaskKey(int stepIndex, int taskIndex)
    {
        return $"{stepIndex}:{taskIndex}";
    }

    private void MoveTutorial(int direction)
    {
        if (direction > 0 && _tutorialStepIndex >= _tutorialSteps.Count - 1)
        {
            CompleteTutorial();
            return;
        }

        ShowTutorialStep(_tutorialStepIndex + direction);
    }

    private void RunTutorialAction()
    {
        var action = _tutorialSteps[_tutorialStepIndex].TryAction;
        action?.Invoke();
        if (!_tutorialPanel.IsDisposed)
        {
            _tutorialPanel.Visible = true;
            _tutorialPanel.BringToFront();
        }
    }

    private void CompleteTutorial()
    {
        _data.Settings.TutorialCompleted = true;
        _data.Settings.TutorialStepIndex = Math.Max(0, _tutorialSteps.Count - 1);
        _repository.Save(_data);
        _tutorialPanel.Visible = false;
        _statusLabel.Text = "Tutorial completed. Use Tutorial to restart it anytime.";
    }

    private void NavigateToPage(string title)
    {
        if (_navigationPages.Count == 0 || _navigationButtons.Count != _navigationPages.Count)
        {
            return;
        }

        var index = _navigationPages
            .Select((page, pageIndex) => new { page.Title, Index = pageIndex })
            .FirstOrDefault(page => string.Equals(page.Title, title, StringComparison.OrdinalIgnoreCase))
            ?.Index;
        if (index is null)
        {
            return;
        }

        SelectNavigationPage(_contentHost, _navigationPages, _navigationButtons, index.Value);
    }

    private void LoadDecisionTutorialExample()
    {
        NavigateToPage("Decide");
        _decisionIsCash.Checked = false;
        _decisionPlatform.SelectedItem = Platform.GGPoker;
        _decisionCategory.SelectedItem = TournamentCategory.MainGrind;
        _decisionFormat.SelectedItem = TournamentFormat.MysteryBounty;
        _decisionEventName.Text = "Tutorial Mystery Bounty";
        _decisionBuyIn.Value = ClampToBox(_decisionBuyIn, 1m);
        _decisionBullets.Value = ClampToBox(_decisionBullets, 1m);
        _decisionAddOns.Value = ClampToBox(_decisionAddOns, 0m);
        _decisionTicketBuyIn.Value = ClampToBox(_decisionTicketBuyIn, 0m);
        _decisionCashBuyIn.Value = ClampToBox(_decisionCashBuyIn, 0m);
        _decisionCashReloads.Value = ClampToBox(_decisionCashReloads, 0m);
        _decisionNotes.Text = "Tutorial example. Adjust before registering.";
        RefreshDecision();
        _statusLabel.Text = "Tutorial example loaded in Decide.";
    }

    private void OpenTournamentTutorialDraft()
    {
        NavigateToPage("MTTs");
        AddTournament();
    }

    private void OpenCashTutorialDraft()
    {
        NavigateToPage("Cash");
        StartCash();
    }

    private void ShowBackupExportGuide()
    {
        MessageBox.Show(
            string.Join(Environment.NewLine,
            [
                "Use Backup before bigger cleanup sessions or before importing data.",
                "Use Export > JSON when you want a full portable copy of the app data.",
                "Use Export > CSV when you want to inspect the bankroll in a spreadsheet.",
                "Use Import only when you intentionally want to replace the current in-app data."
            ]),
            "Backup and export",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }


    private sealed record TutorialStep(
        string Title,
        string TargetTab,
        string Body,
        string[] Tasks,
        string ActionLabel,
        Action? TryAction);
}
