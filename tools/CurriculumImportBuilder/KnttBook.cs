using static CurriculumImportBuilder.Dsl;

namespace CurriculumImportBuilder;

/// <summary>
/// Toán 6 – Kết nối tri thức với cuộc sống. Biên soạn chi tiết: mỗi bài học là một
/// chuỗi <see cref="Block"/> gồm tiêu đề, dẫn nhập, định nghĩa, công thức (LaTeX),
/// ví dụ có lời giải, chú ý và bộ flashcard từ khoá.
/// </summary>
internal static partial class KnttBook
{
    public static Book Build()
    {
        var chapters = new List<Chapter>
        {
            ChapterI(),
            ChapterII(),
            ChapterIII(),
            ChapterIV(),
            ChapterV(),
            ChapterVI(),
            ChapterVII(),
            ChapterVIII(),
            ChapterIX(),
        };

        return new Book(
            Slug: "toan-6-ket-noi-tri-thuc",
            Title: "Toán 6 – Kết nối tri thức với cuộc sống",
            SubjectCode: "MATH",
            GradeCode: "G6",
            FrameworkCode: "KNTT",
            FrameworkName: "Kết nối tri thức với cuộc sống",
            Publisher: "NXB Giáo dục Việt Nam",
            ListPrice: 299000m,
            VersionLabel: "Năm học 2025–2026",
            Description: "Khung chương trình Toán 6 theo bộ sách Kết nối tri thức với cuộc sống — 9 chương, "
                + "43 bài học biên soạn chi tiết kèm luyện tập chung và bài tập cuối chương.",
            Chapters: chapters);
    }
}
