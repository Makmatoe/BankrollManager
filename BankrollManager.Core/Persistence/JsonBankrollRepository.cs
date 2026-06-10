using System.Text.Json;
using System.Text.Json.Serialization;
using BankrollManager.Core.Models;
using BankrollManager.Core.Services;

namespace BankrollManager.Core.Persistence;

public sealed class JsonBankrollRepository
{
    public const string DataDirectoryEnvironmentVariable = "BANKROLL_MANAGER_DATA_DIR";

    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly string? _legacyDataDirectory;

    public JsonBankrollRepository(string? dataDirectory = null, string? legacyDataDirectory = null)
    {
        DataDirectory = dataDirectory ?? ResolveDefaultDataDirectory();
        _legacyDataDirectory = dataDirectory is null
            ? legacyDataDirectory ?? ResolveLegacyDataDirectory()
            : legacyDataDirectory;
        FilePath = Path.Combine(DataDirectory, "bankroll-data.json");
        LegacyFilePath = _legacyDataDirectory is null
            ? null
            : Path.Combine(_legacyDataDirectory, Path.GetFileName(FilePath));
    }

    public string DataDirectory { get; }
    public string FilePath { get; }
    public string? LegacyFilePath { get; }

    public BankrollData LoadOrCreate()
    {
        Directory.CreateDirectory(DataDirectory);
        MigrateLegacyDataIfNeeded();

        if (!File.Exists(FilePath))
        {
            var data = new BankrollData();
            Save(data);
            return data;
        }

        return LoadFromFile(FilePath);
    }

    public void Save(BankrollData data)
    {
        Directory.CreateDirectory(DataDirectory);
        PrepareForPersistence(data);
        WriteJsonAtomically(FilePath, data);
    }

    public void ExportJson(BankrollData data, string destinationPath)
    {
        PrepareForPersistence(data, updateLastSaved: false);
        WriteJsonAtomically(destinationPath, data);
    }

    public BankrollData ImportJson(string sourcePath)
    {
        var data = LoadFromFile(sourcePath);
        return data;
    }

    public string CreateTimestampedBackup(BankrollData data, string? backupDirectory = null)
    {
        var directory = backupDirectory ?? Path.Combine(DataDirectory, "Backups");
        Directory.CreateDirectory(directory);
        var backupPath = Path.Combine(directory, $"bankroll-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        ExportJson(data, backupPath);
        return backupPath;
    }

    private BankrollData LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<BankrollData>(json, _options)
            ?? throw new InvalidOperationException("The selected JSON file did not contain bankroll data.");
        if (data.DataSchemaVersion > BankrollData.CurrentDataSchemaVersion)
        {
            throw new InvalidOperationException(
                $"This bankroll file uses schema version {data.DataSchemaVersion}, but this app supports up to version {BankrollData.CurrentDataSchemaVersion}. Update the app before opening it.");
        }

        data.EnsureDefaults();
        BankrollCalculator.RecalculateTrackingFields(data);
        return data;
    }

    private void PrepareForPersistence(BankrollData data, bool updateLastSaved = true)
    {
        if (data.DataSchemaVersion > BankrollData.CurrentDataSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Cannot save schema version {data.DataSchemaVersion}; this app supports up to version {BankrollData.CurrentDataSchemaVersion}.");
        }

        data.EnsureDefaults();
        BankrollCalculator.RecalculateTrackingFields(data);
        if (updateLastSaved)
        {
            data.LastSavedUtc = DateTime.UtcNow;
        }
    }

    private void MigrateLegacyDataIfNeeded()
    {
        if (LegacyFilePath is null
            || File.Exists(FilePath)
            || !File.Exists(LegacyFilePath)
            || PathsEqual(FilePath, LegacyFilePath))
        {
            return;
        }

        Directory.CreateDirectory(DataDirectory);
        File.Copy(LegacyFilePath, FilePath);
    }

    private void WriteJsonAtomically(string destinationPath, BankrollData data)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var targetDirectory = string.IsNullOrWhiteSpace(directory) ? "." : directory;
        var tempPath = Path.Combine(
            targetDirectory,
            $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, data, _options);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(destinationPath))
            {
                File.Replace(tempPath, destinationPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, destinationPath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static string ResolveDefaultDataDirectory()
    {
        var configured = Environment.GetEnvironmentVariable(DataDirectoryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return string.IsNullOrWhiteSpace(appData)
            ? Path.Combine(AppContext.BaseDirectory, "Data")
            : Path.Combine(appData, "BankrollManager", "Data");
    }

    private static string ResolveLegacyDataDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "Data");
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }
}
