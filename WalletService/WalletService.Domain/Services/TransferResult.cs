namespace WalletService.Domain.Services;

public sealed class TransferResult
{
    public bool IsSuccess { get; }
    public TransferFailureReason? FailureReason { get; }

    private TransferResult(bool isSuccess, TransferFailureReason? reason = null)
    {
        IsSuccess = isSuccess;
        FailureReason = reason;
    }

    public static TransferResult Success() => new(true);
    public static TransferResult Failure(TransferFailureReason reason) => new(false, reason);
}

