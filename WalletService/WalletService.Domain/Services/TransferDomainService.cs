using WalletService.Domain.Entities;
using WalletService.Domain.Enums;
using WalletService.Domain.ValueObjects;

namespace WalletService.Domain.Services;

/// <summary>
/// Domain Service responsable de ejecutar la lógica de transferencia entre wallets.
/// Contiene todas las validaciones de negocio y las operaciones de saldo.
/// </summary>
public sealed class TransferDomainService : ITransferDomainService
{
    // Límite máximo de transferencia: 500 soles equivalentes (regla de negocio fija)
    private const decimal MaxTransferAmountInPen = 500m;

    public TransferResult Execute(
        Wallet fromWallet,
        Wallet toWallet,
        decimal amount,
        CurrencyType currency,
        decimal usdToPenRate)
    {
        // Paso 3: Validar que ambas wallets puedan operar
        if (fromWallet.WalletStatus != WalletStatus.OPERATIVE ||
            toWallet.WalletStatus != WalletStatus.OPERATIVE)
        {
            return TransferResult.Failure(TransferFailureReason.WalletBlocked);
        }

        // Paso 4: Validar límite de transferencia (500 soles equivalentes)
        var amountInPen = ConvertToPen(amount, currency, usdToPenRate);
        if (amountInPen > MaxTransferAmountInPen)
        {
            return TransferResult.Failure(TransferFailureReason.LimitExceeded);
        }

        // Paso 5: Validar saldo suficiente en la wallet origen
        // Convertir el monto a la moneda del balance origen para comparar
        var amountInFromCurrency = Convert(amount, currency, fromWallet.WalletBalance.Currency, usdToPenRate);
        if (fromWallet.WalletBalance.BalanceAmount < amountInFromCurrency)
        {
            return TransferResult.Failure(TransferFailureReason.InsufficientBalance);
        }

        // Paso 6 y 7: Ejecutar operaciones de saldo con conversión si aplica
        var debitOperation = new Operation
        {
            Type = TypeOperation.Subtract,
            WalletId = fromWallet.Id,
            Amount = amountInFromCurrency,
            Currency = fromWallet.WalletBalance.Currency
        };

        var amountInToCurrency = Convert(amount, currency, toWallet.WalletBalance.Currency, usdToPenRate);
        var creditOperation = new Operation
        {
            Type = TypeOperation.Addition,
            WalletId = toWallet.Id,
            Amount = amountInToCurrency,
            Currency = toWallet.WalletBalance.Currency
        };

        fromWallet.WalletBalance.UpdateBalance(debitOperation);
        toWallet.WalletBalance.UpdateBalance(creditOperation);

        return TransferResult.Success();
    }

    /// <summary>
    /// Convierte un monto desde una moneda origen a PEN.
    /// </summary>
    private static decimal ConvertToPen(decimal amount, CurrencyType from, decimal usdToPenRate)
    {
        return from switch
        {
            CurrencyType.PEN => amount,
            CurrencyType.USD => amount * usdToPenRate,
            _ => throw new InvalidOperationException($"Moneda no soportada: {from}")
        };
    }

    /// <summary>
    /// Convierte un monto entre dos monedas utilizando el tipo de cambio USD/PEN.
    /// </summary>
    private static decimal Convert(decimal amount, CurrencyType from, CurrencyType to, decimal usdToPenRate)
    {
        if (from == to) return amount;

        // Convertir primero a PEN como moneda base
        var amountInPen = ConvertToPen(amount, from, usdToPenRate);

        return to switch
        {
            CurrencyType.PEN => amountInPen,
            CurrencyType.USD => amountInPen / usdToPenRate,
            _ => throw new InvalidOperationException($"Moneda destino no soportada: {to}")
        };
    }
}

