namespace WalletService.Domain.Entities;

using WalletService.Domain.Enums;

public class Wallet : AggregateRoot<WalletId>
{
    public string Name { get; private set; } = default!;
    public string LastName { get; private set; } = default!;  
    public Email Email { get; private set; } = default!;
    public PhoneNumber Phone { get; private set; } = default!;
    public WalletLimit WalletLimit { get; private set; } = default!;
    
    public WalletBalance WalletBalance { get; private set; } = default!;

    public DocumentId Document { get; private set; } = default!;

    public WalletStatus WalletStatus  { get; private set; }
    

    private Wallet()
    {
    }
    
    public static Wallet Create(Guid walletId, string name, string lastName, DocumentType documentType, string documentNumber,
        string email, string phone, CurrencyType currency, decimal dailyLimit, decimal? balanceAmount=null)
    {
        var errors = ValidateFieldsRequired(name, lastName, documentType, documentNumber, email, phone, currency, dailyLimit);

        if (errors.Any())
            throw new DomainValidationException("wallet.invalid", "Validation failed", errors);

        var wallet = new Wallet();

        try
        {
            var wid = new WalletId(walletId);

            var walletLimit = WalletLimit.Create(wid, currency, dailyLimit);

            wallet = new Wallet
            {
                Id = wid,
                Name = name.Trim(),
                LastName = lastName.Trim(),
                Email = Email.Create(email),
                Phone = PhoneNumber.Create(phone),
                WalletLimit = walletLimit,
                Document = DocumentId.Create(documentType, documentNumber),
                WalletStatus = WalletStatus.OPERATIVE,
                WalletBalance = WalletBalance.Create(wid, currency, balanceAmount ?? 0m)
            };
        }
        catch (InvalidValueObjectException iv)
        {
            var prefix = iv.Code switch
            {
                "document.invalid" => "documentNumber",
                "currency.invalid" => "currency",
                _ => ""
            };

            var errorsVo = iv.Errors.ToDictionary(k => prefix, v => v.Value);

            foreach (var kv in errorsVo)
                errors[kv.Key] = kv.Value;

        }

        if (errors.Count > 0)
            throw new DomainValidationException("wallet.invalid", "Domain validation failed", errors);

        return wallet;
    }

    private static Dictionary<string, string[]> ValidateFieldsRequired(string name, string lastName, DocumentType documentType, 
        string documentNumber, string email, string phone, CurrencyType currency, decimal dailyLimit)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
            errors["name"] = ["El nombre es requerido."];

        if (string.IsNullOrWhiteSpace(lastName))
            errors["lastName"] = ["El apellido es requerido."];

        // Validar documentType (enum) y documentNumber
        if (!Enum.IsDefined(typeof(DocumentType), documentType) || documentType.Equals(default(DocumentType)))
            errors["documentType"] = ["El tipo de documento es requerido y debe ser válido."];

        if (string.IsNullOrWhiteSpace(documentNumber))
            errors["documentNumber"] = ["El número de documento es requerido."];

        // Validación de email
        if (string.IsNullOrWhiteSpace(email))
            errors["email"] = ["El email es requerido."];

        // Validación de phone
        if (string.IsNullOrWhiteSpace(phone))
            errors["phone"] = ["El teléfono es requerido."];

        // Currency and dailyLimit validation
        if (!Enum.IsDefined(typeof(CurrencyType), currency) || currency.Equals(default(CurrencyType)))
            errors["currency"] = ["El tipo de moneda es requerido y debe ser válido."];

        if (dailyLimit <= 0m)
            errors["dailyLimit"] = ["El límite diario debe ser mayor a 0."];

        return errors;
    }
    
    public void ChangeName(string name)
    {
        EnsureOperative();
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleViolationException("wallet.name.required", "El nombre es obligatorio");
        Name = name.Trim();
        SetModified();
    }

    public void ChangeLastName(string lastName)
    {
        EnsureOperative();
        if (string.IsNullOrWhiteSpace(lastName)) throw new BusinessRuleViolationException("wallet.lastName.required", "El apellido es obligatorio");
        LastName = lastName.Trim();
        SetModified();
    }
    
    public void ChangeDocument(DocumentType type, string number)
    {
        EnsureOperative();
        Document = DocumentId.Create(type, number);
        SetModified();
    }
    
    public void ChangeEmail(string email)
    {
        EnsureOperative();
        var emailNew = Email.Create(email);
        if (emailNew.Equals(Email)) return; 
        Email = emailNew;
        SetModified();
    }

    public void ChangePhone(string phone)
    {
        EnsureOperative();
        Phone = PhoneNumber.Create(phone);
        SetModified();
    }
    
    private void EnsureOperative()
    {
        if (WalletStatus != WalletStatus.OPERATIVE)
            throw new InvalidDomainStateException("wallet.walletStatus.suspended", "La billetera no está activo para esta operación.");
    }

    public void Suspend()
    {
        if (WalletStatus != WalletStatus.OPERATIVE)
            throw new InvalidDomainStateException("wallet.walletStatus.suspended", "Solo un cliente activo puede desactivarse.");
        WalletStatus = WalletStatus.SUSPENDED;
        SetModified();
    }

    public void SoftDelete()
    {
        SetDeleted();
    }
}