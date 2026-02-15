using System.Text.RegularExpressions;
using System.Web;
using ELearning_ToanHocHay_Control.Models.DTOs.Sepay;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class SePayService : ISePayService
    {
        private readonly SePayOptions _options;

        public SePayService(IOptions<SePayOptions> options)
        {
            _options = options.Value;
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
                $"&des={HttpUtility.UrlEncode(description)}";

            return qrUrl;
        }

        /// <summary>
        /// Verify ApiKey từ header IPN
        /// </summary>
        public bool ValidateApiKey(string? apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return false;

            return apiKey == _options.ApiKeyValidator;
        }

        /// <summary>
        /// Parse SUBSCRIPTION_{id} từ nội dung chuyển khoản
        /// </summary>
        public int? ExtractSubscriptionId(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var match = Regex.Match(
                content,
                @"SUBSCRIPTION[\-_]?(\d+)",
                RegexOptions.IgnoreCase
            );

            if (!match.Success)
                return null;

            return int.Parse(match.Groups[1].Value);
        }

        /// <summary>
        /// Check số tiền có khớp không
        /// </summary>
        public bool IsValidAmount(long transferAmount, decimal expectedAmount)
        {
            return transferAmount == (long)expectedAmount;
        }
    }
}
