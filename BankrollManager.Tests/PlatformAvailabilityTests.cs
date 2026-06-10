using BankrollManager.Core.Models;
using BankrollManager.Core.Services;

namespace BankrollManager.Tests;

[TestClass]
public sealed class PlatformAvailabilityTests
{
    [TestMethod]
    public void SettingsKeepDefaultPlatformEnabled()
    {
        var settings = new BankrollSettings
        {
            DefaultPlatform = Platform.GGPoker,
            EnabledPlatforms = [Platform.Unibet]
        };

        settings.EnsureDefaults();

        CollectionAssert.AreEqual(new[] { Platform.Unibet }, settings.EnabledPlatforms);
        Assert.AreEqual(Platform.Unibet, settings.DefaultPlatform);
        Assert.IsTrue(settings.IsPlatformEnabled(Platform.Unibet));
        Assert.IsFalse(settings.IsPlatformEnabled(Platform.GGPoker));
    }

    [TestMethod]
    public void SettingsFallbackToAllPlatformsWhenNoneAreEnabled()
    {
        var settings = new BankrollSettings
        {
            EnabledPlatforms = []
        };

        settings.EnsureDefaults();

        CollectionAssert.AreEquivalent(Enum.GetValues<Platform>(), settings.EnabledPlatforms);
        Assert.IsTrue(settings.IsPlatformEnabled(settings.DefaultPlatform));
    }

    [TestMethod]
    public void PlatformCatalogKeepsIncludedPlatformAvailableForExistingRecords()
    {
        var settings = new BankrollSettings
        {
            EnabledPlatforms = [Platform.Unibet]
        };

        var platforms = PlatformCatalog.EnabledPlatforms(settings, Platform.GGPoker);

        CollectionAssert.Contains(platforms.ToList(), Platform.Unibet);
        CollectionAssert.Contains(platforms.ToList(), Platform.GGPoker);
    }

    [TestMethod]
    public void PlatformCatalogFiltersTournamentFormatsByPlatform()
    {
        var unibetFormats = PlatformCatalog.TournamentFormatsFor(Platform.Unibet);
        var ggFormats = PlatformCatalog.TournamentFormatsFor(Platform.GGPoker);

        CollectionAssert.Contains(ggFormats.ToList(), TournamentFormat.SpinAndGold);
        CollectionAssert.DoesNotContain(unibetFormats.ToList(), TournamentFormat.SpinAndGold);
        CollectionAssert.Contains(unibetFormats.ToList(), TournamentFormat.HexaPro);
        CollectionAssert.DoesNotContain(ggFormats.ToList(), TournamentFormat.HexaPro);
    }

    [TestMethod]
    public void PlatformCatalogFiltersCashFormatsByPlatform()
    {
        var hollandFormats = PlatformCatalog.CashFormatsFor(Platform.HollandCasino);
        var ggFormats = PlatformCatalog.CashFormatsFor(Platform.GGPoker);

        CollectionAssert.Contains(ggFormats.ToList(), CashFormat.RushAndCashHoldem);
        CollectionAssert.DoesNotContain(hollandFormats.ToList(), CashFormat.RushAndCashHoldem);
        CollectionAssert.Contains(hollandFormats.ToList(), CashFormat.HoldemCash);
    }
}
