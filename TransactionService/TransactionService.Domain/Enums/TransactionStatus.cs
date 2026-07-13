namespace TransactionService.Domain.Enums;

public enum TransactionStatus
{
    PENDING   = 0,
    COMPLETED = 1,
    CANCELLED = 2,
    FAILED    = 3,
}