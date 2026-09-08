namespace CurriculumImportBuilder;

/// <summary>Ngân hàng câu hỏi của một bộ sách (Subject + Grade, gắn Course).</summary>
public sealed record QBank(string Key, string Name, string Description);

/// <summary>Một câu hỏi. <paramref name="Type"/> khớp enum <c>QuestionType</c>, <paramref name="Difficulty"/> khớp <c>DifficultyLevel</c>.</summary>
public sealed record QItem(
    string Key,
    string BankKey,
    string? NodeKey,
    string Type,
    string Difficulty,
    string Text,
    string CorrectAnswer,
    string Explanation,
    List<QOption> Options);

/// <summary>Một phương án của câu trắc nghiệm.</summary>
public sealed record QOption(int Order, string Text, bool IsCorrect);

/// <summary>Một bài tập / đề. <paramref name="Type"/> khớp <c>ExerciseType</c>, <paramref name="Tier"/> khớp <c>AccessTier</c>.</summary>
public sealed record ExItem(
    string Key,
    string? NodeKey,
    string Name,
    string Type,
    string Tier,
    int? DurationMinutes,
    int? MaxAttempts,
    int PassingPercent,
    List<ExLink> Questions);

/// <summary>Gán một câu hỏi vào một bài tập kèm điểm.</summary>
public sealed record ExLink(string QuestionKey, double Score);

/// <summary>Toàn bộ phần đánh giá (ngân hàng + câu hỏi + bài tập) đi kèm một <see cref="Book"/>.</summary>
public sealed record Assessment(QBank Bank, List<QItem> Questions, List<ExItem> Exercises);
