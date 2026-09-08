namespace CurriculumImportBuilder;

/// <summary>
/// Dựng phần đánh giá (ngân hàng câu hỏi + bài tập/đề) cho một <see cref="Book"/>.
/// Bài tập gắn vào node chương (NodeKey = key của chương trong nodes.csv).
/// </summary>
internal static class AssessmentFactory
{
    /// <param name="questionsPerChapter">số câu sinh cho mỗi chương.</param>
    /// <param name="fullExamSet">true: đủ 4 dạng (Practice/Quiz Free, Test Standard, Exam Premium) + đề học kì; false: chỉ Practice Free + Test Standard.</param>
    public static Assessment Build(
        Book book,
        string bankKey,
        int[][] chapterThemes,
        int questionsPerChapter,
        bool fullExamSet,
        int seed)
    {
        var rng = new Random(seed);
        var bank = new QBank(
            Key: bankKey,
            Name: $"Ngân hàng câu hỏi {book.Title}",
            Description: $"Câu hỏi trắc nghiệm / điền đáp số Toán 6 theo {book.Chapters.Count} chương của bộ {book.FrameworkName}.");

        var questions = new List<QItem>();
        var exercises = new List<ExItem>();

        for (var ci = 0; ci < book.Chapters.Count; ci++)
        {
            var chapter = book.Chapters[ci];
            var themes = ci < chapterThemes.Length ? chapterThemes[ci] : new[] { (ci % 9) + 1 };
            var chapterQ = QuestionFactory.ForChapter(bankKey, chapter.Key, ci + 1, themes, questionsPerChapter, rng);
            questions.AddRange(chapterQ);

            var shortTitle = StripPrefix(chapter.Title);

            exercises.Add(Exercise($"{bankKey}-ex-c{ci + 1}-lt", chapter.Key,
                $"Luyện tập — {shortTitle}", "Practice", "Free",
                duration: null, maxAttempts: null, passing: 50, chapterQ));

            if (fullExamSet)
            {
                exercises.Add(Exercise($"{bankKey}-ex-c{ci + 1}-bt", chapter.Key,
                    $"Bài tập trắc nghiệm — {shortTitle}", "Quiz", "Free",
                    duration: null, maxAttempts: null, passing: 50, Take(chapterQ, 8)));

                exercises.Add(Exercise($"{bankKey}-ex-c{ci + 1}-kt15", chapter.Key,
                    $"Đề kiểm tra 15 phút — {shortTitle}", "Test", "Standard",
                    duration: 15, maxAttempts: 2, passing: 50, Take(chapterQ, 10)));

                exercises.Add(Exercise($"{bankKey}-ex-c{ci + 1}-kt45", chapter.Key,
                    $"Đề kiểm tra 45 phút — {shortTitle}", "Exam", "Premium",
                    duration: 45, maxAttempts: 1, passing: 50, chapterQ));
            }
            else
            {
                exercises.Add(Exercise($"{bankKey}-ex-c{ci + 1}-kt", chapter.Key,
                    $"Đề kiểm tra — {shortTitle}", "Test", "Standard",
                    duration: 30, maxAttempts: 2, passing: 50, Take(chapterQ, 10)));
            }
        }

        if (fullExamSet)
        {
            var half = (book.Chapters.Count + 1) / 2;
            var hk1 = questions.Where(q => ChapterIndexOf(q.Key) <= 3).ToList();
            var hk1Full = questions.Where(q => ChapterIndexOf(q.Key) <= half).ToList();
            var hk2 = questions.Where(q => ChapterIndexOf(q.Key) > half && ChapterIndexOf(q.Key) <= half + 2).ToList();
            var hk2Full = questions.Where(q => ChapterIndexOf(q.Key) > half).ToList();

            var rootKey = book.Chapters[0].Key; // treo các đề tổng hợp vào chương đầu

            exercises.Add(Exercise($"{bankKey}-ex-gk1", rootKey, "Đề thi giữa học kì I", "Test", "Standard",
                duration: 45, maxAttempts: 2, passing: 50, Sample(hk1, 15, seed + 1)));
            exercises.Add(Exercise($"{bankKey}-ex-ck1", rootKey, "Đề thi cuối học kì I", "Exam", "Premium",
                duration: 60, maxAttempts: 1, passing: 50, Sample(hk1Full, 25, seed + 2)));
            exercises.Add(Exercise($"{bankKey}-ex-gk2", rootKey, "Đề thi giữa học kì II", "Test", "Standard",
                duration: 45, maxAttempts: 2, passing: 50, Sample(hk2, 15, seed + 3)));
            exercises.Add(Exercise($"{bankKey}-ex-ck2", rootKey, "Đề thi cuối học kì II", "Exam", "Premium",
                duration: 60, maxAttempts: 1, passing: 50, Sample(hk2Full, 25, seed + 4)));
        }

        return new Assessment(bank, questions, exercises);
    }

    private static ExItem Exercise(string key, string nodeKey, string name, string type, string tier,
        int? duration, int? maxAttempts, int passing, IEnumerable<QItem> qs)
    {
        var links = qs.Select(q => new ExLink(q.Key, 1.0)).ToList();
        return new ExItem(key, nodeKey, name, type, tier, duration, maxAttempts, passing, links);
    }

    private static List<QItem> Take(List<QItem> src, int n) => src.Take(Math.Min(n, src.Count)).ToList();

    private static List<QItem> Sample(List<QItem> src, int n, int seed)
    {
        var rng = new Random(seed);
        return src.OrderBy(_ => rng.Next()).Take(Math.Min(n, src.Count)).OrderBy(q => q.Key).ToList();
    }

    private static int ChapterIndexOf(string questionKey)
    {
        // key dạng "<bank>-c<idx>-NN"
        var m = System.Text.RegularExpressions.Regex.Match(questionKey, @"-c(\d+)-");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }

    private static string StripPrefix(string title)
    {
        var idx = title.IndexOf(". ", StringComparison.Ordinal);
        return idx > 0 && idx < 14 ? title[(idx + 2)..] : title;
    }
}
