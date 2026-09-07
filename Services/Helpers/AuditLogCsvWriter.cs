using System.Globalization;
using System.Text;
using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>Xuất nhật ký quản trị ra CSV (UTF-8 BOM để Excel đọc đúng tiếng Việt).</summary>
    public static class AuditLogCsvWriter
    {
        public static byte[] Build(IReadOnlyList<AuditLogDto> items)
        {
            var sb = new StringBuilder();
            sb.Append("ThoiGian,NguoiThucHien,Email,HanhDong,DoiTuong,MaDoiTuong,IP,GiaTriCu,GiaTriMoi\r\n");

            foreach (var l in items)
            {
                sb.Append(Csv(l.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))).Append(',');
                sb.Append(Csv(l.ActorName ?? (l.UserId?.ToString() ?? "hệ thống"))).Append(',');
                sb.Append(Csv(l.ActorEmail)).Append(',');
                sb.Append(Csv(l.Action)).Append(',');
                sb.Append(Csv(l.EntityType)).Append(',');
                sb.Append(Csv(l.EntityId?.ToString())).Append(',');
                sb.Append(Csv(l.IpAddress)).Append(',');
                sb.Append(Csv(l.OldValueJson)).Append(',');
                sb.Append(Csv(l.NewValueJson)).Append("\r\n");
            }

            return new UTF8Encoding(true).GetBytes(sb.ToString());
        }

        private static string Csv(string? value)
        {
            value ??= "";
            if (value.Length > 0 && "=+-@\t\r".IndexOf(value[0]) >= 0)
                value = "'" + value;
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
