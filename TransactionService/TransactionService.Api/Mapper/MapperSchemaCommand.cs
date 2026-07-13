using TransactionService.Application.Transactions.Commands.CreateRecharge;
using TransactionService.Application.Transactions.Commands.CreateTransaction;
using TransactionService.Application.Transactions.Commands.DeleteRecharge;
using TransactionService.Application.Transactions.Commands.DeleteTransaction;
using TransactionService.Application.Transactions.Dtos;
using TransactionService.Api.Schema;

namespace TransactionService.Api.Mapper;

public static class MapperSchemaCommand
{
    // ── Recharge ──────────────────────────────────────────────
    public static CreateRechargeCommand ToCommand(this RechargeSchemaRequest schema)
        => new(
            WalletId:   schema.WalletId,
            Amount:     (decimal)schema.Amount,
            Currency:   schema.Currency,
            MethodType: schema.MethodType
        );

    public static DeleteRechargeCommand ToDeleteRechargeCommand(this Guid rechargeId)
        => new(RechargeId: rechargeId);

    public static RechargeSchemaResponse ToResponse(this RechargeDto dto)
        => new()
        {
            RechargeId = dto.Id,
            WalletId   = dto.WalletId,
            Amount     = (double)dto.Amount,
            Currency   = dto.Currency,
            MethodType = dto.MethodType
        };

    public static RechargeSchemaIdResponse ToRechargeIdResponse(this Guid rechargeId)
        => new() { RechargeId = rechargeId };

    // ── Transaction ───────────────────────────────────────────
    public static CreateTransactionCommand ToCommand(this TransactionSchemaRequest schema)
        => new(
            TransactionId: schema.TransactionId == Guid.Empty ? Guid.NewGuid() : schema.TransactionId,
            FromWalletId:  schema.FromWalletId,
            ToWalletId:    schema.ToWalletId,
            Amount:        (decimal)schema.Amount,
            Currency:      schema.Currency,
            SourceType:    schema.SourceType
        );

    public static DeleteTransactionCommand ToDeleteTransactionCommand(this Guid transactionId)
        => new(TransactionId: transactionId);

    public static TransactionSchemaResponse ToResponse(this TransactionDto dto)
        => new()
        {
            PaymentId    = dto.Id,
            FromWalletId = dto.FromWalletId,
            ToWalletId   = dto.ToWalletId,
            Amount       = (double)dto.Amount,
            Currency     = dto.Currency,
            SourceType   = dto.SourceType
        };

    public static TransactionSchemaIdResponse ToTransactionIdResponse(this Guid transactionId)
        => new() { TransactionId = transactionId };
}
