using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IDashboardRepository
    {
        Task<WeeklyStatsModel> GetWeeklyStatsAsync(int studentId, DateTime startDate, DateTime endDate);
        Task<OverallStatsModel> GetOverallStatsAsync(int studentId);
        Task<StreakDataModel> GetStreakDataAsync(int studentId);
        Task<List<RecentLessonModel>> GetRecentLessonsAsync(int studentId, int limit);
        Task<List<ChapterProgressModel>> GetChapterProgressAsync(int studentId);
    }

    // Domain Models (for repository layer)
    public class WeeklyStatsModel
    {
        public int TotalMinutes { get; set; }
        public int ExerciseCount { get; set; }
        public decimal AverageScore { get; set; }
    }

    public class OverallStatsModel
    {
        public decimal AverageScore { get; set; }
        public int TotalExercises { get; set; }
        public int TotalLessons { get; set; }
    }

    public class StreakDataModel
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public bool StudiedToday { get; set; }
    }

    public class RecentLessonModel
    {
        public int LessonId { get; set; }
        public string LessonName { get; set; }
        public string TopicName { get; set; }
        public string ChapterName { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? DurationMinutes { get; set; }
        public bool IsCompleted { get; set; }
        public int ProgressPercentage { get; set; }
        public double? Score { get; set; } // THÊM DÒNG NÀY
    }

    public class ChapterProgressModel
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; }
        public int OrderIndex { get; set; }
        public decimal CompletionPercentage { get; set; }
        public int CompletedTopics { get; set; }
        public int TotalTopics { get; set; }
        public bool IsLocked { get; set; }
        public MasteryLevel? AverageMastery { get; set; }
    }
}
