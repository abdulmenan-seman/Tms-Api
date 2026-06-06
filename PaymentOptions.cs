using System.ComponentModel.DataAnnotations;

public class PaymentOptions
{
    [Required(AllowEmptyStrings = false)]
    public required string GatewayUrl { get; init; } // [cite: 156, 159]

    [Range(100, 100000, ErrorMessage = "Max deposit must be between 100 and 100,000 Ethiopian Birr.")]
    public decimal MaxDepositBirr { get; init; } // [cite: 158]
}