namespace BankrollManager.Core.Models;

public enum AttentionSeverity
{
    High,
    Check,
    Info,
    Clear
}

public enum AttentionTargetType
{
    None,
    Cash,
    Tournament,
    Wallet,
    Settings
}

public sealed record AttentionOptions(
    decimal WalletDifferenceThreshold = 0.01m,
    decimal HighWalletDifferenceThreshold = 1m,
    int MaxItems = 12);

public sealed record AttentionItem(
    int Priority,
    AttentionSeverity Severity,
    string Area,
    string Summary,
    string Action,
    AttentionTargetType TargetType,
    Guid? TargetId,
    string TargetKey);
