namespace AffordStack.Infrastructure.Configuration;

public class CashfreeSettings
{
    public const string SectionName = "Cashfree";

    public string AppId { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Environment { get; set; } = "SANDBOX"; // "SANDBOX" or "PRODUCTION"
    public string ApiVersion { get; set; } = "2023-08-01";

    public string BaseUrl => string.Equals(Environment, "PRODUCTION", StringComparison.OrdinalIgnoreCase)
        ? "https://api.cashfree.com/pg"
        : "https://sandbox.cashfree.com/pg";
}
