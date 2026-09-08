using static CurriculumImportBuilder.Dsl;

namespace CurriculumImportBuilder;

/// <summary>
/// Toán 6 – Cánh Diều. Khung chương trình: đầy đủ chương + bài,
/// mỗi bài một khung nội dung chuẩn (tiêu đề + mục tiêu). Chưa biên soạn sâu.
/// </summary>
internal static class CanhDieuBook
{
    public static Book Build()
    {
        var chapters = new List<Chapter>
        {
            Ch("c1", "Chương 1. Số tự nhiên", free: true,
                ("Tập hợp", "khái niệm tập hợp, phần tử, cách cho một tập hợp"),
                ("Tập hợp các số tự nhiên", "tập ℕ, ℕ*, thứ tự, ghi số tự nhiên, số La Mã"),
                ("Phép cộng, phép trừ các số tự nhiên", "phép cộng, phép trừ và tính chất"),
                ("Phép nhân, phép chia các số tự nhiên", "phép nhân, phép chia hết, phép chia có dư"),
                ("Phép tính luỹ thừa với số mũ tự nhiên", "luỹ thừa, nhân và chia hai luỹ thừa cùng cơ số"),
                ("Thứ tự thực hiện các phép tính", "quy tắc tính giá trị biểu thức"),
                ("Quan hệ chia hết. Tính chất chia hết", "quan hệ chia hết, tính chất chia hết của một tổng"),
                ("Dấu hiệu chia hết cho 2, cho 5", "nhận biết số chia hết cho 2, cho 5"),
                ("Dấu hiệu chia hết cho 3, cho 9", "nhận biết số chia hết cho 3, cho 9"),
                ("Số nguyên tố. Hợp số", "phân biệt số nguyên tố và hợp số"),
                ("Phân tích một số ra thừa số nguyên tố", "sơ đồ cây, sơ đồ cột"),
                ("Ước chung và ước chung lớn nhất", "ƯC, ƯCLN và rút gọn phân số"),
                ("Bội chung và bội chung nhỏ nhất", "BC, BCNN và quy đồng mẫu số")),

            Ch("c2", "Chương 2. Số nguyên", free: false,
                ("Số nguyên âm", "số nguyên âm và ý nghĩa thực tiễn"),
                ("Tập hợp các số nguyên", "tập ℤ, trục số, số đối, so sánh"),
                ("Phép cộng các số nguyên", "cộng hai số nguyên cùng dấu, khác dấu, tính chất"),
                ("Phép trừ số nguyên. Quy tắc dấu ngoặc", "phép trừ, quy tắc dấu ngoặc"),
                ("Phép nhân các số nguyên", "nhân hai số nguyên và tính chất"),
                ("Phép chia hết hai số nguyên. Quan hệ chia hết trong tập hợp số nguyên", "phép chia hết, ước và bội của số nguyên")),

            Ch("c3", "Chương 3. Hình học trực quan", free: false,
                ("Tam giác đều. Hình vuông. Lục giác đều", "nhận biết và các yếu tố cơ bản"),
                ("Hình chữ nhật. Hình thoi", "nhận biết, yếu tố cơ bản và cách vẽ"),
                ("Hình bình hành", "nhận biết, yếu tố cơ bản và cách vẽ"),
                ("Hình thang cân", "nhận biết và các yếu tố cơ bản"),
                ("Hình có trục đối xứng", "trục đối xứng của một hình"),
                ("Hình có tâm đối xứng", "tâm đối xứng của một hình"),
                ("Đối xứng trong thực tiễn", "tính đối xứng trong tự nhiên và đời sống"),
                ("Chu vi và diện tích của một số hình trong thực tiễn", "công thức chu vi, diện tích hình chữ nhật, hình thoi, hình bình hành, hình thang")),

            Ch("c4", "Chương 4. Một số yếu tố thống kê và xác suất", free: false,
                ("Thu thập, tổ chức, biểu diễn, phân tích và xử lí dữ liệu", "thu thập và tổ chức dữ liệu bằng bảng"),
                ("Biểu đồ cột", "đọc, phân tích và vẽ biểu đồ cột"),
                ("Biểu đồ cột kép", "đọc, phân tích và vẽ biểu đồ cột kép"),
                ("Mô hình xác suất trong một số trò chơi và thí nghiệm đơn giản", "mô hình xác suất, kết quả có thể"),
                ("Xác suất thực nghiệm trong một số trò chơi và thí nghiệm đơn giản", "tính xác suất thực nghiệm")),

            Ch("c5", "Chương 5. Phân số và số thập phân", free: false,
                ("Phân số với tử và mẫu là số nguyên", "mở rộng phân số, phân số bằng nhau, tính chất cơ bản"),
                ("So sánh các phân số. Hỗn số dương", "so sánh phân số, hỗn số dương"),
                ("Phép cộng, phép trừ phân số", "cộng, trừ phân số và tính chất"),
                ("Phép nhân, phép chia phân số", "nhân, chia phân số, số nghịch đảo"),
                ("Số thập phân", "số thập phân, số đối, so sánh"),
                ("Phép cộng, phép trừ số thập phân", "cộng, trừ số thập phân và tính chất"),
                ("Phép nhân, phép chia số thập phân", "nhân, chia số thập phân"),
                ("Ước lượng và làm tròn số", "quy tắc làm tròn và ước lượng kết quả"),
                ("Tỉ số. Tỉ số phần trăm", "tỉ số, tỉ số phần trăm và ứng dụng"),
                ("Hai bài toán về phân số", "tìm giá trị phân số của một số và bài toán ngược")),

            Ch("c6", "Chương 6. Hình học phẳng", free: false,
                ("Điểm. Đường thẳng", "điểm, đường thẳng, quan hệ thuộc, ba điểm thẳng hàng"),
                ("Hai đường thẳng cắt nhau. Hai đường thẳng song song", "vị trí tương đối của hai đường thẳng"),
                ("Đoạn thẳng", "đoạn thẳng, độ dài, trung điểm"),
                ("Tia", "khái niệm tia, hai tia đối nhau, hai tia trùng nhau"),
                ("Góc", "khái niệm góc, điểm trong của góc"),
                ("Số đo góc", "đo góc, các loại góc, góc nhọn, vuông, tù, bẹt")),
        };

        return new Book(
            Slug: "toan-6-canh-dieu",
            Title: "Toán 6 – Cánh Diều",
            SubjectCode: "MATH",
            GradeCode: "G6",
            FrameworkCode: "CD",
            FrameworkName: "Cánh Diều",
            Publisher: "NXB Đại học Sư phạm",
            ListPrice: 299000m,
            VersionLabel: "Năm học 2025–2026",
            Description: "Khung chương trình Toán 6 theo bộ sách Cánh Diều — 6 chương, đầy đủ danh mục bài học. Nội dung chi tiết sẽ được biên soạn ở giai đoạn sau.",
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
