using System.Text.RegularExpressions;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>
    /// Rút <c>SUBSCRIPTION_{id}</c> từ nội dung chuyển khoản SePay. Tách khỏi
    /// <see cref="Implementations.SePayIpnService"/> để test bảng nội dung CK không cần DB.
    /// Mẫu: <c>SUBSCRIPTION_123</c> / <c>SUBSCRIPTION-123</c> / <c>SUBSCRIPTION123</c> (không phân biệt hoa/thường).
    /// </summary>
    public static class SePayContentParser
    {
        private static readonly Regex Pattern =
            new(@"SUBSCRIPTION[\-_]?(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static int? TryParseSubscriptionId(string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;

            var match = Pattern.Match(content);
            if (!match.Success) return null;

            // Số quá lớn (tràn int) -> null, không ném.
            return int.TryParse(match.Groups[1].Value, out var id) ? id : null;
        }
    }
}
