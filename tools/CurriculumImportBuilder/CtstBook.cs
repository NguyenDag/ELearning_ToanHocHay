using static CurriculumImportBuilder.Dsl;

namespace CurriculumImportBuilder;

/// <summary>
/// Toán 6 – Chân trời sáng tạo. Khung chương trình: đầy đủ chương + bài,
/// mỗi bài một khung nội dung chuẩn (tiêu đề + mục tiêu). Chưa biên soạn sâu.
/// </summary>
internal static class CtstBook
{
    public static Book Build()
    {
        var chapters = new List<Chapter>
        {
            Ch("c1", "Chương 1. Số tự nhiên", free: true,
                ("Tập hợp. Phần tử của tập hợp", "khái niệm tập hợp, phần tử, hai cách mô tả tập hợp"),
                ("Tập hợp số tự nhiên. Ghi số tự nhiên", "tập hợp ℕ và ℕ*, hệ thập phân, số La Mã"),
                ("Các phép tính trong tập hợp số tự nhiên", "cộng, trừ, nhân, chia và các tính chất"),
                ("Luỹ thừa với số mũ tự nhiên", "định nghĩa luỹ thừa, nhân và chia hai luỹ thừa cùng cơ số"),
                ("Thứ tự thực hiện các phép tính", "quy tắc với biểu thức có/không có dấu ngoặc"),
                ("Chia hết và chia có dư. Tính chất chia hết của một tổng", "quan hệ chia hết, phép chia có dư, tính chất chia hết của tổng, hiệu"),
                ("Dấu hiệu chia hết cho 2, cho 5", "nhận biết số chia hết cho 2, cho 5"),
                ("Dấu hiệu chia hết cho 3, cho 9", "nhận biết số chia hết cho 3, cho 9"),
                ("Ước và bội", "khái niệm ước, bội và cách tìm"),
                ("Số nguyên tố. Hợp số. Phân tích một số ra thừa số nguyên tố", "số nguyên tố, hợp số, sơ đồ cây"),
                ("Ước chung. Ước chung lớn nhất", "ƯC, ƯCLN và cách tìm bằng phân tích ra thừa số nguyên tố"),
                ("Bội chung. Bội chung nhỏ nhất", "BC, BCNN và ứng dụng quy đồng, bài toán thực tế")),

            Ch("c2", "Chương 2. Số nguyên", free: false,
                ("Số nguyên âm và tập hợp các số nguyên", "số nguyên âm, tập hợp ℤ, biểu diễn trên trục số"),
                ("Thứ tự trong tập hợp số nguyên", "so sánh hai số nguyên, giá trị tuyệt đối"),
                ("Phép cộng các số nguyên", "cộng hai số nguyên cùng dấu, khác dấu và tính chất"),
                ("Phép trừ hai số nguyên. Quy tắc dấu ngoặc", "trừ số nguyên, bỏ và thêm dấu ngoặc"),
                ("Phép nhân hai số nguyên", "nhân hai số nguyên cùng dấu, khác dấu và tính chất"),
                ("Phép chia hết. Quan hệ chia hết trong tập hợp các số nguyên", "phép chia hết, ước và bội của một số nguyên")),

            Ch("c3", "Chương 3. Các hình phẳng trong thực tiễn", free: false,
                ("Hình vuông – Tam giác đều – Lục giác đều", "nhận biết, yếu tố cơ bản và cách vẽ"),
                ("Hình chữ nhật – Hình thoi – Hình bình hành – Hình thang cân", "nhận biết và các yếu tố cơ bản"),
                ("Chu vi và diện tích của một số hình trong thực tiễn", "công thức chu vi, diện tích và bài toán thực tế")),

            Ch("c4", "Chương 4. Một số yếu tố thống kê", free: false,
                ("Thu thập và phân loại dữ liệu", "cách thu thập, dữ liệu số và không phải số, tính hợp lí"),
                ("Biểu diễn dữ liệu trên bảng", "bảng thống kê, bảng kiểm đếm"),
                ("Biểu đồ tranh", "đọc và vẽ biểu đồ tranh"),
                ("Biểu đồ cột – Biểu đồ cột kép", "đọc, phân tích và vẽ biểu đồ cột, cột kép")),

            Ch("c5", "Chương 5. Phân số", free: false,
                ("Phân số với tử số và mẫu số là số nguyên", "mở rộng khái niệm phân số, phân số bằng nhau"),
                ("Tính chất cơ bản của phân số", "rút gọn, quy đồng mẫu nhiều phân số"),
                ("So sánh phân số", "so sánh phân số cùng mẫu, khác mẫu"),
                ("Phép cộng và phép trừ phân số", "cộng, trừ phân số và tính chất"),
                ("Phép nhân và phép chia phân số", "nhân, chia phân số, số nghịch đảo"),
                ("Giá trị phân số của một số", "tìm giá trị phân số của một số cho trước và bài toán ngược")),

            Ch("c6", "Chương 6. Số thập phân", free: false,
                ("Số thập phân", "số thập phân, số đối, so sánh số thập phân"),
                ("Các phép tính với số thập phân", "cộng, trừ, nhân, chia số thập phân"),
                ("Làm tròn số thập phân và ước lượng kết quả", "quy tắc làm tròn, ước lượng"),
                ("Tỉ số và tỉ số phần trăm", "tỉ số, tỉ số phần trăm và bài toán thực tế")),

            Ch("c7", "Chương 7. Hình học trực quan. Tính đối xứng", free: false,
                ("Hình có trục đối xứng", "trục đối xứng, hình có trục đối xứng"),
                ("Hình có tâm đối xứng", "tâm đối xứng, hình có tâm đối xứng"),
                ("Vai trò của tính đối xứng trong thế giới tự nhiên", "đối xứng trong tự nhiên, kiến trúc, nghệ thuật")),

            Ch("c8", "Chương 8. Các hình hình học cơ bản", free: false,
                ("Điểm. Đường thẳng", "điểm, đường thẳng, điểm thuộc/không thuộc đường thẳng"),
                ("Ba điểm thẳng hàng. Ba điểm không thẳng hàng", "quan hệ thẳng hàng, điểm nằm giữa"),
                ("Hai đường thẳng cắt nhau, song song. Tia", "vị trí tương đối hai đường thẳng, khái niệm tia"),
                ("Đoạn thẳng. Độ dài đoạn thẳng", "đoạn thẳng, đo và so sánh độ dài"),
                ("Trung điểm của đoạn thẳng", "định nghĩa và cách xác định trung điểm"),
                ("Góc", "khái niệm góc, điểm trong của góc"),
                ("Số đo góc. Các góc đặc biệt", "đo góc, góc nhọn, vuông, tù, bẹt")),

            Ch("c9", "Chương 9. Một số yếu tố xác suất", free: false,
                ("Phép thử nghiệm – Sự kiện", "phép thử, kết quả có thể, sự kiện"),
                ("Xác suất thực nghiệm", "tính xác suất thực nghiệm của một sự kiện")),
        };

        return new Book(
            Slug: "toan-6-chan-troi-sang-tao",
            Title: "Toán 6 – Chân trời sáng tạo",
            SubjectCode: "MATH",
            GradeCode: "G6",
            FrameworkCode: "CTST",
            FrameworkName: "Chân trời sáng tạo",
            Publisher: "NXB Giáo dục Việt Nam",
            ListPrice: 299000m,
            VersionLabel: "Năm học 2025–2026",
            Description: "Khung chương trình Toán 6 theo bộ sách Chân trời sáng tạo — 9 chương, đầy đủ danh mục bài học. Nội dung chi tiết sẽ được biên soạn ở giai đoạn sau.",
            Chapters: chapters);
    }

    private static Chapter Ch(string key, string title, bool free, params (string Title, string Goal)[] lessons)
    {
        var list = new List<Lesson>();
        for (int i = 0; i < lessons.Length; i++)
        {
            var (lt, goal) = lessons[i];
            var num = $"Bài {i + 1}. {lt}";
            list.Add(new Lesson(
                Key: $"{key}b{i + 1}",
                Title: num,
                IsFree: free,
                DurationMinutes: 45,
                Blocks: new List<Block>
                {
                    H(num),
                    T($"Bài học thuộc **{title}**. Mục tiêu: nắm được {goal}."),
                    N("Nội dung lý thuyết chi tiết, ví dụ và bài tập của bài này sẽ được cập nhật trong bản biên soạn đầy đủ."),
                },
                Cards: new List<Card>(),
                Resources: new List<Res>()));
        }

        return new Chapter(key, title, free, list);
    }
}
