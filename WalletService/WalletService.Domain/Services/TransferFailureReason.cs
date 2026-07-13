namespace WalletService.Domain.Services;

public enum TransferFailureReason
{
    WalletBlocked,
    LimitExceeded,
    InsufficientBalance
}

