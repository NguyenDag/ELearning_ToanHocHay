using Microsoft.AspNetCore.DataProtection;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>
    /// Bảo vệ số tài khoản ngân hàng người nhận (PII) bằng ASP.NET Data Protection.
    /// Key ring persist vào DB (bảng DataProtectionKeys) nên sống sót qua redeploy.
    /// Số TK chỉ được giải mã khi build file chi hộ CSV; nơi khác chỉ dùng 4 số cuối.
    /// </summary>
    public interface IRefundFieldProtector
    {
        string Protect(string plaintext);
        string Unprotect(string ciphertext);
        string Last4(string accountNumber);
    }

    public class RefundFieldProtector : IRefundFieldProtector
    {
        private readonly IDataProtector _protector;

        public RefundFieldProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("Refund.BankAccount.v1");
        }

        public string Protect(string plaintext) => _protector.Protect(plaintext ?? "");

        public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);

        public string Last4(string accountNumber)
        {
            var digits = new string((accountNumber ?? "").Where(char.IsDigit).ToArray());
            return digits.Length <= 4 ? digits : digits[^4..];
        }
    }
}
