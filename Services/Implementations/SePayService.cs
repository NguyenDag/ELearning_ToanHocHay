using ELearning_ToanHocHay_Control.Models.DTOs.Sepay;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class SePayService : ISePayService
    {
        private readonly SePayOptions _options;
        private readonly ILogger<SePayService> _logger;

        public SePayService(IOptions<SePayOptions> options, ILogger<SePayService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Tạo QR thanh toán SePay
        /// </summary>
        public string GenerateQrUrl(int subscriptionId, decimal amount)
        {
            var description = $"TKPTTS SUBSCRIPTION_{subscriptionId}";

            var qrUrl =
                $"{_options.BaseUrl}/img" +
                $"?acc={_options.VA}" +
                $"&bank={_options.BankName}" +
                $"&amount={(long)amount}" +
                $"&des={Uri.EscapeDataString(description)}";

            _logger.LogInformation(
                    "Generated QR URL for subscription {SubscriptionId} with amount {Amount}",
                    subscriptionId,
                    amount
                );

            return qrUrl;
        }

        /// <summary>
        /// Verify ApiKey từ header IPN
        /// </summary>
        public bool ValidateApiKey(string? apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("API Key is null or empty");
                return false;
            }

            var expectedApiKey = _options.ApiKeyValidator;

            if (string.IsNullOrEmpty(expectedApiKey))
            {
                _logger.LogError("SePay API Key is not configured in SePayOptions");
                return false;
            }

            var isValid = apiKey == expectedApiKey;

            if (!isValid)
            {
                _logger.LogWarning("Invalid API Key provided");
            }

            return isValid;
        }

        /// <summary>
        /// Parse SUBSCRIPTION_{id} từ nội dung chuyển khoản
        /// </summary>
        public int? ExtractSubscriptionId(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Content is null or empty");
                return null;
            }

            // Pattern: SUBSCRIPTION_123 / SUBSCRIPTION-123 / SUBSCRIPTION123 (xem SePayContentParser).
            var subscriptionId = Helpers.SePayContentParser.TryParseSubscriptionId(content);

            if (subscriptionId == null)
            {
                _logger.LogWarning("Could not extract subscription ID from content: {Content}", content);
                return null;
            }

            _logger.LogInformation(
                "Extracted subscription ID {SubscriptionId} from content: {Content}", subscriptionId, content);
            return subscriptionId;
        }

        /// <summary>
        /// Check số tiền có khớp không
        /// </summary>
        public bool IsValidAmount(long transferAmount, decimal expectedAmount)
        {
            var isValid = transferAmount == (long)expectedAmount;

            if (!isValid)
            {
                _logger.LogWarning(
                    "Amount mismatch. Transfer: {TransferAmount}, Expected: {ExpectedAmount}",
                    transferAmount,
                    (long)expectedAmount
                );
            }

            return isValid;
        }
    }
}
