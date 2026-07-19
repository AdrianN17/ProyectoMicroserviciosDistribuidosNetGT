using WalletService.Api.Schema;
using WalletService.Application.Wallets.Commands.CreateWallet;
using WalletService.Application.Wallets.Commands.DeleteWallet;
using WalletService.Application.Wallets.Commands.UpdateWallet;
using WalletService.Application.Wallets.Dtos;
using WalletService.Domain.ValueObjects;

namespace WalletService.Api.Mapper;

public static class MapperSchemaCommand
{
    // ── WalletSchemaRequest → CreateWalletCommand ─────────────────
    public static CreateWalletCommand ToCreateWalletCommand(this WalletSchemaRequest request, Guid walletId) =>
        new(
            WalletId:       walletId,
            Name:           request.Name,
            LastName:       request.LastName,
            DocumentType:   request.DocumentType,
            DocumentNumber: request.DocumentNumber,
            Email:          request.Email,
            Phone:          request.Phone,
            Currency:       request.Currency,
            DailyLimit:     (decimal)request.DailyLimit
        );

    // ── WalletPatchSchemaRequest → UpdateWalletCommand ────────────
    public static UpdateWalletCommand ToUpdateWalletCommand(this WalletPatchSchemaRequest request, Guid walletId) =>
        new(
            WalletId:       walletId,
            Name:           request.Name,
            LastName:       request.LastName,
            DocumentType:   request.DocumentType,
            DocumentNumber: request.DocumentNumber,
            Email:          request.Email,
            Phone:          request.Phone,
            DailyLimit:     request.DailyLimit is {} d ? (decimal)d : null
        );

    // ── Guid → DeleteWalletCommand ────────────────────────────────
    public static DeleteWalletCommand ToDeleteWalletCommand(this Guid walletId) =>
        new(WalletId: walletId);

    // ── Guid → WalletSchemaIdResponse ─────────────────────────────
    public static WalletSchemaIdResponse ToSchemaIdResponse(this Guid walletId) =>
        new() { WalletId = walletId };

    // ── WalletId (ValueObject) → WalletSchemaIdResponse ───────────
    public static WalletSchemaIdResponse ToSchemaIdResponse(this WalletId walletId) =>
        new() { WalletId = walletId.Value };

    // ── WalletDto → WalletSchemaResponse ──────────────────────────
    public static WalletSchemaResponse ToSchemaResponse(this WalletDto dto) =>
        new()
        {
            WalletId       = dto.WalletId.ToString(),
            Name           = dto.Name,
            LastName       = dto.LastName,
            DocumentType   = dto.DocumentType,
            DocumentNumber = dto.DocumentNumber,
            Email          = dto.Email,
            Phone          = dto.Phone,
            Currency       = dto.Currency,
            DailyLimit     = (double)dto.DailyLimit,
            BalanceAmount  = (double)dto.balanceAmount
        };
}