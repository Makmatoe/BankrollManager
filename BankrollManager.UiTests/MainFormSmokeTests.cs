using System.Reflection;
using BankrollManager.App;
using BankrollManager.App.Forms;
using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;

namespace BankrollManager.UiTests;

[TestClass]
public sealed class MainFormSmokeTests
{
    private static int _winFormsConfigured;

    private static readonly string[] MainPages =
    [
        "Overview",
        "Wallets",
        "Audit",
        "Timeline",
        "MTTs",
        "Cash",
        "Ledger",
        "Day",
        "Month",
        "Monthly Review",
        "Year",
        "Decide",
        "EV Check",
        "Settings"
    ];

    private static readonly Size[] SmokeSizes =
    [
        new(1280, 720),
        new(1600, 900)
    ];

    [TestMethod]
    [TestCategory("UI")]
    public void MainViewsRenderInDarkAndLightAtCommonDesktopSizes()
    {
        foreach (var mode in new[] { AppearanceMode.Dark, AppearanceMode.Light })
        {
            foreach (var size in SmokeSizes)
            {
                RunOnStaThread(() => SmokeMainForm(mode, size));
            }
        }
    }

    [TestMethod]
    [TestCategory("UI")]
    public void TournamentQuickAddDialogRendersTwentyRows()
    {
        RunOnStaThread(() =>
        {
            Theme.Configure(AppearanceMode.Dark);
            using var dialog = new TournamentQuickAddDialog(
                new TournamentPreset
                {
                    Name = "Daily",
                    Platform = Platform.Unibet,
                    Category = TournamentCategory.MainGrind,
                    Format = TournamentFormat.MTT,
                    BuyIn = 1.10m,
                    ActualBullets = 1
                },
                new BankrollSettings(),
                20)
            {
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Size = new Size(900, 760),
                Location = new Point(60, 60)
            };

            dialog.Show();
            Application.DoEvents();
            dialog.PerformLayout();

            AssertVisibleControlsHaveSaneBounds(dialog, "Quick add dialog");
            AssertRenderedBitmapHasContent(dialog, "Quick add dialog");

            var rowCount = EnumerateVisibleControls(dialog)
                .OfType<Label>()
                .Count(label => label.Text.StartsWith("Tournament ", StringComparison.Ordinal)
                    && !label.Text.Contains(" of ", StringComparison.Ordinal));
            Assert.AreEqual(20, rowCount);
            dialog.Close();
        });
    }

    [TestMethod]
    [TestCategory("UI")]
    public void TournamentPresetManagerDialogRenders()
    {
        RunOnStaThread(() =>
        {
            Theme.Configure(AppearanceMode.Dark);
            using var dialog = new TournamentPresetManagerDialog(
                [
                    new TournamentPreset
                    {
                        Name = "Daily",
                        Platform = Platform.Unibet,
                        Category = TournamentCategory.MainGrind,
                        Format = TournamentFormat.MTT,
                        BuyIn = 1.10m,
                        ActualBullets = 1,
                        IsFavorite = true
                    }
                ],
                new BankrollSettings())
            {
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Size = new Size(820, 560),
                Location = new Point(60, 60)
            };

            dialog.Show();
            Application.DoEvents();
            dialog.PerformLayout();

            AssertVisibleControlsHaveSaneBounds(dialog, "Preset manager dialog");
            AssertRenderedBitmapHasContent(dialog, "Preset manager dialog");
            dialog.Close();
        });
    }

    [TestMethod]
    [TestCategory("UI")]
    public void TournamentBulkFinishDialogRenders()
    {
        RunOnStaThread(() =>
        {
            Theme.Configure(AppearanceMode.Dark);
            using var dialog = new TournamentBulkFinishDialog(
                [
                    new TournamentEntry
                    {
                        Date = new DateOnly(2026, 6, 10),
                        RegistrationTime = new TimeOnly(20, 0),
                        Status = TournamentStatus.Registered,
                        EventName = "Target stack",
                        Platform = Platform.GGPoker,
                        Category = TournamentCategory.FlipSatellite,
                        Format = TournamentFormat.TargetStackSatellite,
                        BuyIn = 1m,
                        ActualBullets = 1
                    }
                ],
                new BankrollSettings())
            {
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Size = new Size(720, 560),
                Location = new Point(60, 60)
            };

            dialog.Show();
            Application.DoEvents();
            dialog.PerformLayout();

            AssertVisibleControlsHaveSaneBounds(dialog, "Bulk finish dialog");
            AssertRenderedBitmapHasContent(dialog, "Bulk finish dialog");
            dialog.Close();
        });
    }

    private static void SmokeMainForm(AppearanceMode appearanceMode, Size size)
    {
        var dataDirectory = CreateTempDataDirectory();
        try
        {
            var repository = new JsonBankrollRepository(dataDirectory, legacyDataDirectory: null);
            var data = SeedDataFactory.Create();
            data.Settings.AppearanceMode = appearanceMode;
            data.Settings.TutorialCompleted = true;
            repository.Save(data);

            using var form = new MainForm(repository)
            {
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                Size = size,
                Location = new Point(60, 60)
            };

            form.Show();
            Application.DoEvents();

            foreach (var page in MainPages)
            {
                NavigateToPage(form, page);
                form.PerformLayout();
                Application.DoEvents();

                var context = $"{appearanceMode} {size.Width}x{size.Height} {page}";
                AssertVisibleControlsHaveSaneBounds(form, context);
                AssertRenderedBitmapHasContent(form, context);
            }

            form.Close();
        }
        finally
        {
            TryDeleteDirectory(dataDirectory);
        }
    }

    private static void NavigateToPage(MainForm form, string page)
    {
        var method = typeof(MainForm).GetMethod(
            "NavigateToPage",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method, "MainForm navigation method was not found.");
        method.Invoke(form, [page]);
    }

    private static void AssertVisibleControlsHaveSaneBounds(Control root, string context)
    {
        foreach (var control in EnumerateVisibleControls(root))
        {
            if (control is Form)
            {
                continue;
            }

            Assert.IsTrue(
                control.Width > 0 && control.Height > 0,
                $"{context}: {Describe(control)} has an invalid size {control.Width}x{control.Height}.");

            if (control.Parent is null || ParentAllowsOverflow(control.Parent))
            {
                continue;
            }

            const int tolerance = 48;
            var parentClient = control.Parent.ClientRectangle;
            var allowed = new Rectangle(
                parentClient.Left - tolerance,
                parentClient.Top - tolerance,
                parentClient.Width + tolerance * 2,
                parentClient.Height + tolerance * 2);

            Assert.IsTrue(
                allowed.Contains(control.Bounds),
                $"{context}: {Describe(control)} is outside {Describe(control.Parent)}. Bounds={control.Bounds}, parent={control.Parent.ClientRectangle}.");
        }
    }

    private static void AssertRenderedBitmapHasContent(Form form, string context)
    {
        using var bitmap = new Bitmap(
            Math.Max(1, form.Width),
            Math.Max(1, form.Height));

        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));

        var sampleStep = Math.Max(1, Math.Min(bitmap.Width, bitmap.Height) / 48);
        var colors = new HashSet<int>();
        for (var y = 0; y < bitmap.Height; y += sampleStep)
        {
            for (var x = 0; x < bitmap.Width; x += sampleStep)
            {
                colors.Add(bitmap.GetPixel(x, y).ToArgb());
                if (colors.Count >= 12)
                {
                    return;
                }
            }
        }

        Assert.Fail($"{context}: rendered form appears blank or visually collapsed.");
    }

    private static IEnumerable<Control> EnumerateVisibleControls(Control root)
    {
        if (!root.Visible)
        {
            yield break;
        }

        yield return root;
        foreach (Control child in root.Controls)
        {
            foreach (var descendant in EnumerateVisibleControls(child))
            {
                yield return descendant;
            }
        }
    }

    private static bool ParentAllowsOverflow(Control parent)
    {
        return parent is ScrollableControl { AutoScroll: true }
            || parent is TabControl
            || parent is Form;
    }

    private static string Describe(Control control)
    {
        var name = string.IsNullOrWhiteSpace(control.Name) ? "(unnamed)" : control.Name;
        var text = control.Text;
        if (text.Length > 40)
        {
            text = string.Concat(text.AsSpan(0, 37), "...");
        }

        return $"{control.GetType().Name} {name} \"{text}\"";
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                ConfigureWinFormsOnce();
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                Application.ExitThread();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static void ConfigureWinFormsOnce()
    {
        if (Interlocked.Exchange(ref _winFormsConfigured, 1) != 0)
        {
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
    }

    private static string CreateTempDataDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "BankrollManager.UiTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
