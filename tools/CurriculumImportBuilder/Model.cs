namespace CurriculumImportBuilder;

/// <summary>Một bộ khung chương trình (một bộ sách giáo khoa).</summary>
public sealed record Book(
    string Slug,
    string Title,
    string SubjectCode,
    string GradeCode,
    string FrameworkCode,
    string FrameworkName,
    string Publisher,
    decimal ListPrice,
    string VersionLabel,
    string Description,
    List<Chapter> Chapters,
    Assessment? Assessment = null);

/// <summary>Chương — node <c>Chapter</c> trong cây nội dung.</summary>
public sealed record Chapter(
    string Key,
    string Title,
    bool IsFree,
    List<Lesson> Lessons);

/// <summary>Bài học — node <c>Lesson</c> trong cây nội dung.</summary>
public sealed record Lesson(
    string Key,
    string Title,
    bool IsFree,
    int? DurationMinutes,
    List<Block> Blocks,
    List<Card> Cards,
    List<Res> Resources);

/// <summary>Một khối nội dung (<c>ContentBlock</c>). <paramref name="Type"/> khớp enum <c>LessonBlockType</c>.</summary>
public sealed record Block(string Type, string? Text, string? Url = null, string? Meta = null);

/// <summary>Một thẻ ghi nhớ trong bộ flashcard của bài.</summary>
public sealed record Card(string Front, string Back, string? Hint = null);

/// <summary>Một tài liệu tải về / liên kết ngoài của bài (<c>LessonResource</c>).</summary>
public sealed record Res(string Title, string Type, string Url, bool Downloadable = true);

/// <summary>Bộ helper viết nội dung bài học ngắn gọn.</summary>
public static class Dsl
{
    public static Block H(string text) => new("Heading", text.StartsWith('#') ? text : "# " + text);
    public static Block H2(string text) => new("Heading", "## " + text);
    public static Block T(string text) => new("Text", text);
    public static Block D(string term, string body) => new("Definition", $"**{term}.** {body}");
    public static Block F(string latex) => new("Formula", latex);
    public static Block Ex(string problem, string solution) => new("Example", $"**Ví dụ.** {problem}\n\n**Lời giải.** {solution}");
    public static Block N(string text) => new("Note", $"**Chú ý.** {text}");
    public static Block Img(string url, string alt, string caption) =>
        new("Image", null, url, $"{{\"alt\":\"{alt}\",\"caption\":\"{caption}\"}}");
    public static Block Vid(string url, string title, int seconds) =>
        new("Video", null, url, $"{{\"provider\":\"youtube\",\"title\":\"{title}\",\"durationSeconds\":{seconds}}}");

    public static Card C(string front, string back, string? hint = null) => new(front, back, hint);

    public static Lesson L(string key, string title, bool free, int duration, IEnumerable<Block> blocks,
        IEnumerable<Card>? cards = null, IEnumerable<Res>? resources = null) =>
        new(key, title, free, duration, blocks.ToList(), (cards ?? []).ToList(), (resources ?? []).ToList());

    /// <summary>Bài "Luyện tập chung" / "Bài tập cuối chương" — khung nhẹ, trỏ sang phần bài tập.</summary>
    public static Lesson Review(string key, string title, string intro) =>
        new(key, title, false, 45,
            new List<Block>
            {
                H(title),
                T(intro),
                N("Hãy làm phần bài tập đính kèm của bài này để tự kiểm tra. Ghi lại những câu sai để ôn lại lý thuyết tương ứng."),
            },
            new List<Card>(), new List<Res>());
}
