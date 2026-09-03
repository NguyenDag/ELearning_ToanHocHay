using System.Globalization;
using System.Text;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>
    /// Sinh file chi hộ CSV (mẫu generic ngân hàng VN) cho một <see cref="RefundBatch"/>.
    /// Finance tải file này lên internet banking để chuyển khoản hàng loạt.
    /// Cột: STT, SoTaiKhoan, TenNguoiHuong, MaNganHang, SoTien, NoiDung.
    /// </summary>
    public static class RefundCsvWriter
    {
        public static byte[] Build(
            IReadOnlyList<RefundRequest> items,
            IRefundFieldProtector protector)
        {
            var sb = new StringBuilder();
            sb.Append("STT,SoTaiKhoan,TenNguoiHuong,MaNganHang,SoTien,NoiDung\r\n");

            var i = 1;
            foreach (var r in items)
            {
                var accountNumber = SafeUnprotect(protector, r.BankAccountNumberProtected);
                var amount = ((long)Math.Round(r.Amount, MidpointRounding.AwayFromZero))
                    .ToString(CultureInfo.InvariantCulture);
                var content = Ascii($"HOAN TIEN {r.ReasonCode} REF {r.PublicId:N}");

                sb.Append(i.ToString(CultureInfo.InvariantCulture)).Append(',');
                sb.Append(Csv(accountNumber)).Append(',');
                sb.Append(Csv(Ascii(r.BankAccountHolderName))).Append(',');
                sb.Append(Csv(r.BankBin)).Append(',');
                sb.Append(amount).Append(',');
                sb.Append(Csv(content)).Append("\r\n");
                i++;
            }

            return new UTF8Encoding(false).GetBytes(sb.ToString());
        }

        private static string SafeUnprotect(IRefundFieldProtector protector, string ciphertext)
        {
            try { return protector.Unprotect(ciphertext); }
            catch { return "DECRYPT_ERROR"; }
        }

        /// <summary>
        /// RFC 4180 quoting + CSV formula-injection defense (CWE-1236): a field that a
        /// spreadsheet would treat as a formula gets a leading apostrophe so Excel / Sheets
        /// render it as text. <see cref="RefundRequest.BankAccountHolderName"/> is free text
        /// supplied by the requester and this file is opened by a Finance operator.
        /// </summary>
        private static string Csv(string? value)
        {
            value ??= "";

            if (value.Length > 0 && "=+-@\t\r".IndexOf(value[0]) >= 0)
                value = "'" + value;

            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>Bỏ dấu tiếng Việt — nội dung chuyển khoản ngân hàng chỉ nhận ASCII.</summary>
        private static string Ascii(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var normalized = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark) continue;
                if (c == 'đ') { sb.Append('d'); continue; }
                if (c == 'Đ') { sb.Append('D'); continue; }
                sb.Append(c <= 127 ? c : ' ');
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).Trim();
        }
    }
}
