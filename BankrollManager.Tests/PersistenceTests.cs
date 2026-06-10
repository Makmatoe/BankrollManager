using BankrollManager.Core.Models;
using BankrollManager.Core.Persistence;
using BankrollManager.Core.Services;

namespace BankrollManager.Tests;

[TestClass]
public sealed class PersistenceTests
{
    [TestMethod]
    public void RepositorySavesCurrentSchemaVersionAndCleansTemporaryFiles()
    {
        var folder = CreateTempFolder();
        try
        {
            var repository = new JsonBankrollRepository(folder);
            var data = new BankrollData
            {
                DataSchemaVersion = 0,
                LedgerEntries =
                [
                    new LedgerEntry
                    {
                        Type = LedgerType.Deposit,
                        Platform = Platform.Unibet,
                        Amount = 12.50m
                    }
                ]
            };

            repository.Save(data);

            Assert.AreEqual(BankrollData.CurrentDataSchemaVersion, data.DataSchemaVersion);
            Assert.IsTrue(File.Exists(repository.FilePath));
            Assert.AreEqual(0, Directory.EnumerateFiles(folder, "*.tmp").Count());
            StringAssert.Contains(
                File.ReadAllText(repository.FilePath),
                $"\"DataSchemaVersion\": {BankrollData.CurrentDataSchemaVersion}");

            data.LedgerEntries[0].Amount = 13.75m;
            repository.Save(data);
            Assert.AreEqual(0, Directory.EnumerateFiles(folder, "*.tmp").Count());

            var loaded = repository.LoadOrCreate();
            Assert.AreEqual(BankrollData.CurrentDataSchemaVersion, loaded.DataSchemaVersion);
            Assert.AreEqual(13.75m, BankrollCalculator.TotalDeposits(loaded));
        }
        finally
        {
            DeleteTempFolder(folder);
        }
    }

    [TestMethod]
    public void RepositoryMigratesLegacyDataWhenNewDataDirectoryIsEmpty()
    {
        var currentFolder = CreateTempFolder();
        var legacyFolder = CreateTempFolder();
        try
        {
            var legacyRepository = new JsonBankrollRepository(legacyFolder);
            legacyRepository.Save(new BankrollData
            {
                LedgerEntries =
                [
                    new LedgerEntry
                    {
                        Type = LedgerType.Deposit,
                        Platform = Platform.HollandCasino,
                        Amount = 15m
                    }
                ]
            });

            var repository = new JsonBankrollRepository(currentFolder, legacyFolder);
            var loaded = repository.LoadOrCreate();

            Assert.IsTrue(File.Exists(repository.FilePath));
            Assert.IsTrue(File.Exists(legacyRepository.FilePath));
            Assert.AreEqual(15m, BankrollCalculator.TotalDeposits(loaded));
        }
        finally
        {
            DeleteTempFolder(currentFolder);
            DeleteTempFolder(legacyFolder);
        }
    }

    [TestMethod]
    public void RepositoryDoesNotOverwriteExistingDataDuringLegacyMigration()
    {
        var currentFolder = CreateTempFolder();
        var legacyFolder = CreateTempFolder();
        try
        {
            new JsonBankrollRepository(legacyFolder).Save(new BankrollData
            {
                LedgerEntries =
                [
                    new LedgerEntry { Type = LedgerType.Deposit, Amount = 99m }
                ]
            });

            var repository = new JsonBankrollRepository(currentFolder, legacyFolder);
            repository.Save(new BankrollData
            {
                LedgerEntries =
                [
                    new LedgerEntry { Type = LedgerType.Deposit, Amount = 7m }
                ]
            });

            var loaded = repository.LoadOrCreate();

            Assert.AreEqual(7m, BankrollCalculator.TotalDeposits(loaded));
        }
        finally
        {
            DeleteTempFolder(currentFolder);
            DeleteTempFolder(legacyFolder);
        }
    }

    [TestMethod]
    public void RepositoryRejectsFutureSchemaFiles()
    {
        var folder = CreateTempFolder();
        try
        {
            var repository = new JsonBankrollRepository(folder);
            Directory.CreateDirectory(folder);
            File.WriteAllText(repository.FilePath, "{\"DataSchemaVersion\":999}");

            try
            {
                repository.LoadOrCreate();
                Assert.Fail("Future schema files should not load.");
            }
            catch (InvalidOperationException error)
            {
                StringAssert.Contains(error.Message, "schema version 999");
            }
        }
        finally
        {
            DeleteTempFolder(folder);
        }
    }

    private static string CreateTempFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), "BankrollManagerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static void DeleteTempFolder(string folder)
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
