using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AffordStack.Application.Common.Exceptions;
using AffordStack.Application.DTOs.Payment;
using AffordStack.Application.Interfaces;
using AffordStack.Infrastructure.Configuration;

namespace AffordStack.Infrastructure.Services;

public class CashfreeService : ICashfreeService
{
    private readonly HttpClient _httpClient;
    private readonly CashfreeSettings _settings;
    private readonly ILogger<CashfreeService> _logger;

    public string Environment => _settings.Environment;

    public CashfreeService(HttpClient httpClient, IConfiguration configuration, ILogger<CashfreeService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _settings = new CashfreeSettings();
        configuration.GetSection(CashfreeSettings.SectionName).Bind(_settings);

        // Fallbacks if not set in config or set to placeholder
        if (string.IsNullOrWhiteSpace(_settings.AppId) || _settings.AppId.Contains("PLACEHOLDER") || _settings.AppId.Contains("1029384756"))
        {
            _settings.AppId = configuration["CASHFREE_APP_ID"]
                ?? configuration["Cashfree:AppId"]
                ?? configuration["Cashfree__AppId"]
                ?? System.Environment.GetEnvironmentVariable("CASHFREE_APP_ID")
                ?? System.Environment.GetEnvironmentVariable("Cashfree__AppId")
                ?? "TEST1029384756";
        }
        if (string.IsNullOrWhiteSpace(_settings.SecretKey) || _settings.SecretKey.Contains("PLACEHOLDER"))
        {
            _settings.SecretKey = configuration["CASHFREE_SECRET_KEY"]
                ?? configuration["Cashfree:SecretKey"]
                ?? configuration["Cashfree__SecretKey"]
                ?? System.Environment.GetEnvironmentVariable("CASHFREE_SECRET_KEY")
                ?? System.Environment.GetEnvironmentVariable("Cashfree__SecretKey")
                ?? "TEST_SECRET_KEY_PLACEHOLDER";
        }

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    private void SetCashfreeHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("x-client-id", _settings.AppId);
        request.Headers.Add("x-client-secret", _settings.SecretKey);
        request.Headers.Add("x-api-version", _settings.ApiVersion);
    }

    public async Task<CashfreeOrderResponse> CreateOrderAsync(
        string orderId,
        decimal amount,
        string customerId,
        string customerName,
        string customerEmail,
        string customerPhone,
        string orderNote,
        string? returnUrl = null)
    {
        try
        {
            // Sanitize phone: Cashfree expects 10 digits for Indian numbers
            var cleanPhone = SanitizePhoneNumber(customerPhone);
            var cleanCustomerId = SanitizeIdentifier(customerId);

            var payload = new
            {
                order_id = orderId,
                order_amount = Math.Round(amount, 2),
                order_currency = "INR",
                customer_details = new
                {
                    customer_id = string.IsNullOrWhiteSpace(cleanCustomerId) ? "cust_guest" : cleanCustomerId,
                    customer_name = string.IsNullOrWhiteSpace(customerName) ? "Valued Client" : customerName.Trim(),
                    customer_email = string.IsNullOrWhiteSpace(customerEmail) ? "client@affordstack.com" : customerEmail.Trim(),
                    customer_phone = cleanPhone
                },
                order_meta = new
                {
                    return_url = returnUrl ?? string.Empty
                },
                order_note = orderNote
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/orders");
            SetCashfreeHeaders(request);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            _logger.LogInformation("Creating Cashfree PG order {OrderId} for amount ₹{Amount} ({Environment})", orderId, amount, _settings.Environment);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Cashfree PG order creation returned HTTP {StatusCode}: {ResponseBody}", response.StatusCode, responseBody);

                // Attempt to parse Cashfree structured error
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        var cfMsg = msgProp.GetString();
                        throw new BadRequestException($"Cashfree payment initiation error: {cfMsg}");
                    }
                }
                catch (JsonException) { }

                throw new BadRequestException($"Unable to initiate Cashfree payment order (HTTP {response.StatusCode}). Please check Cashfree gateway configuration.");
            }

            using var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;

            var returnedOrderId = root.GetProperty("order_id").GetString() ?? orderId;
            var paymentSessionId = root.TryGetProperty("payment_session_id", out var psProp) ? psProp.GetString() : null;
            var orderStatus = root.TryGetProperty("order_status", out var stProp) ? stProp.GetString() ?? "ACTIVE" : "ACTIVE";
            var orderAmount = root.TryGetProperty("order_amount", out var amProp) ? amProp.GetDecimal() : amount;
            var orderCurrency = root.TryGetProperty("order_currency", out var curProp) ? curProp.GetString() ?? "INR" : "INR";

            return new CashfreeOrderResponse
            {
                OrderId = returnedOrderId,
                PaymentSessionId = paymentSessionId,
                OrderAmount = orderAmount,
                OrderCurrency = orderCurrency,
                OrderStatus = orderStatus,
                Environment = _settings.Environment
            };
        }
        catch (BadRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception while calling Cashfree CreateOrder API for {OrderId}", orderId);
            throw new BadRequestException($"Payment gateway communication failure: {ex.Message}");
        }
    }

    public async Task<PaymentVerificationResponse> GetOrderDetailsAsync(string orderId)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.BaseUrl}/orders/{orderId}");
            SetCashfreeHeaders(request);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Cashfree GetOrderDetails returned HTTP {StatusCode}: {ResponseBody}", response.StatusCode, responseBody);
                return new PaymentVerificationResponse
                {
                    Success = false,
                    OrderId = orderId,
                    Status = "FAILED",
                    Message = "Could not fetch order status from Cashfree."
                };
            }

            using var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;

            var orderStatus = root.TryGetProperty("order_status", out var stProp) ? stProp.GetString() ?? "" : "";
            var orderAmount = root.TryGetProperty("order_amount", out var amProp) ? amProp.GetDecimal() : 0m;

            // Now check payment attempts to retrieve the cf_payment_id
            string? paymentId = null;
            DateTime? paidAt = null;

            try
            {
                using var payRequest = new HttpRequestMessage(HttpMethod.Get, $"{_settings.BaseUrl}/orders/{orderId}/payments");
                SetCashfreeHeaders(payRequest);

                var payResponse = await _httpClient.SendAsync(payRequest);
                if (payResponse.IsSuccessStatusCode)
                {
                    var payBody = await payResponse.Content.ReadAsStringAsync();
                    using var payDoc = JsonDocument.Parse(payBody);
                    if (payDoc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elem in payDoc.RootElement.EnumerateArray())
                        {
                            var pStatus = elem.TryGetProperty("payment_status", out var ps) ? ps.GetString() : null;
                            if (pStatus == "SUCCESS")
                            {
                                paymentId = elem.TryGetProperty("cf_payment_id", out var cpid) ? cpid.GetString() : null;
                                if (elem.TryGetProperty("payment_time", out var pt) && DateTime.TryParse(pt.GetString(), out var dt))
                                {
                                    paidAt = dt;
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to fetch payments array for Cashfree order {OrderId}: {Message}", orderId, ex.Message);
            }

            bool isPaid = string.Equals(orderStatus, "PAID", StringComparison.OrdinalIgnoreCase);

            return new PaymentVerificationResponse
            {
                Success = isPaid,
                OrderId = orderId,
                PaymentId = paymentId,
                Status = orderStatus,
                Amount = orderAmount,
                PaidAt = paidAt ?? (isPaid ? DateTime.UtcNow : null),
                Message = isPaid ? "Payment successfully captured by Cashfree." : $"Order status is {orderStatus}."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while checking Cashfree order status for {OrderId}", orderId);
            return new PaymentVerificationResponse
            {
                Success = false,
                OrderId = orderId,
                Status = "ERROR",
                Message = ex.Message
            };
        }
    }

    public bool VerifyWebhookSignature(string rawBody, string signature, string timestamp)
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey) || string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        try
        {
            var dataToSign = $"{timestamp}{rawBody}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
            var computedSignature = Convert.ToBase64String(hash);

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature.Trim()),
                Encoding.UTF8.GetBytes(computedSignature.Trim()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook signature verification error");
            return false;
        }
    }

    private static string SanitizePhoneNumber(string? rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone)) return "9999999999";
        var digitsOnly = new string(rawPhone.Where(char.IsDigit).ToArray());
        if (digitsOnly.Length > 10)
        {
            // If prefixed with 91, take last 10 digits
            return digitsOnly.Substring(digitsOnly.Length - 10);
        }
        if (digitsOnly.Length < 10)
        {
            return digitsOnly.PadLeft(10, '9');
        }
        return digitsOnly;
    }

    private static string SanitizeIdentifier(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "cust_affordstack";
        // Allow alphanumeric, underscores, hyphens
        var clean = new string(id.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
        return clean.Length > 50 ? clean.Substring(0, 50) : (clean.Length == 0 ? "cust_affordstack" : clean);
    }
}
