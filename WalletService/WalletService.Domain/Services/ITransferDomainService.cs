using WalletService.Domain.Entities;
using WalletService.Domain.Enums;

namespace WalletService.Domain.Services;

public interface ITransferDomainService
{
    /// <summary>
    /// Valida y ejecuta la transferencia entre dos wallets.
    /// Las wallets deben estar cargadas con sus WalletBalance y WalletLimit incluidos.
    /// </summary>
    /// <param name="fromWallet">Wallet origen.</param>
    /// <param name="toWallet">Wallet destino.</param>
    /// <param name="amount">Monto de la operación en la moneda indicada.</param>
    /// <param name="currency">Moneda del monto a transferir.</param>
    /// <param name="usdToPenRate">Tipo de cambio USD → PEN configurado.</param>
    TransferResult Execute(
        Wallet fromWallet,
        Wallet toWallet,
        decimal amount,
        CurrencyType currency,
        decimal usdToPenRate);
}

