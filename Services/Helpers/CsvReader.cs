using System.Text;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>
    /// Bộ đọc CSV theo RFC 4180 — hỗ trợ trường có dấu phẩy, xuống dòng, dấu nháy kép
    /// (escape bằng <c>""</c>) và bỏ BOM UTF-8. Dùng cho luồng import nội dung (A3/P2).
    /// </summary>
    public static class CsvReader
    {
        /// <summary>
        /// Đọc toàn bộ nội dung CSV thành danh sách bản ghi, khoá = tên cột lấy từ dòng đầu.
        /// Bỏ qua dòng trống hoàn toàn. Cột thiếu ở một dòng → chuỗi rỗng.
        /// </summary>
        public static List<CsvRecord> Read(string content)
        {
            var rows = Parse(content);
            var result = new List<CsvRecord>();
            if (rows.Count == 0) return result;

            var headers = rows[0].Select(h => h.Trim()).ToList();
            for (var i = 1; i < rows.Count; i++)
            {
                var raw = rows[i];
                if (raw.Count == 0 || (raw.Count == 1 && string.IsNullOrWhiteSpace(raw[0])))
                    continue;

                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var c = 0; c < headers.Count; c++)
                    map[headers[c]] = c < raw.Count ? raw[c] : "";

                // dòng dữ liệu bắt đầu từ 1 (không tính header)
                result.Add(new CsvRecord(i, map));
            }
            return result;
        }

        /// <summary>Chỉ lấy danh sách tên cột (dòng đầu).</summary>
        public static List<string> Headers(string content)
        {
            var rows = Parse(content);
            return rows.Count == 0 ? new List<string>() : rows[0].Select(h => h.Trim()).ToList();
        }

        private static List<List<string>> Parse(string content)
        {
            var records = new List<List<string>>();
            if (string.IsNullOrEmpty(content)) return records;
            if (content[0] == '﻿') content = content[1..];

            var field = new StringBuilder();
            var record = new List<string>();
            var inQuotes = false;
            var sawAny = false;
            var i = 0;

            while (i < content.Length)
            {
                var ch = content[i];

                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < content.Length && content[i + 1] == '"')
                        {
                            field.Append('"');
                            i += 2;
                            continue;
                        }
                        inQuotes = false;
                        i++;
                        continue;
                    }
                    field.Append(ch);
                    i++;
                    continue;
                }

                switch (ch)
                {
                    case '"':
                        inQuotes = true;
                        sawAny = true;
                        i++;
                        break;
                    case ',':
                        record.Add(field.ToString());
                        field.Clear();
                        sawAny = true;
                        i++;
                        break;
                    case '\r':
                        i++;
                        break;
                    case '\n':
                        record.Add(field.ToString());
                        field.Clear();
                        records.Add(record);
                        record = new List<string>();
                        sawAny = false;
                        i++;
                        break;
                    default:
                        field.Append(ch);
                        sawAny = true;
                        i++;
                        break;
                }
            }

            if (sawAny || field.Length > 0 || record.Count > 0)
            {
                record.Add(field.ToString());
                records.Add(record);
            }
            return records;
        }
    }

    /// <summary>Một dòng dữ liệu CSV: số dòng (1-based, không tính header) + map cột→giá trị.</summary>
    public sealed class CsvRecord
    {
        private readonly Dictionary<string, string> _fields;

        public CsvRecord(int rowNumber, Dictionary<string, string> fields)
        {
            RowNumber = rowNumber;
            _fields = fields;
        }

        public int RowNumber { get; }

        /// <summary>Giá trị cột đã <c>Trim()</c>; cột không có → chuỗi rỗng.</summary>
        public string Get(string column)
            => _fields.TryGetValue(column, out var v) ? v.Trim() : "";

        /// <summary>Giá trị cột giữ nguyên khoảng trắng đầu/cuối (dùng cho nội dung block).</summary>
        public string GetRaw(string column)
            => _fields.TryGetValue(column, out var v) ? v : "";

        public bool Has(string column) => _fields.ContainsKey(column);
    }
}
