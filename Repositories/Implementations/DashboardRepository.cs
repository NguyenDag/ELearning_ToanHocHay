using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardRepository> _logger;

        // Múi giờ Việt Nam UTC+7
        private static readonly TimeZoneInfo VnTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        private static DateTime VnNow =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VnTimeZone);

        public DashboardRepository(AppDbContext context, ILogger<DashboardRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<WeeklyStatsModel> GetWeeklyStatsAsync(
            int studentId, DateTime startDate, DateTime endDate)
        {
            var attempts = await _context.ExerciseAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == studentId &&
                           a.Status != AttemptStatus.InProgress &&
                           a.SubmittedAt.HasValue &&
                           a.SubmittedAt.Value >= startDate &&
                           a.SubmittedAt.Value < endDate)
                .ToListAsync();

            if (!attempts.Any())
                return new WeeklyStatsModel { TotalMinutes = 0, ExerciseCount = 0, AverageScore = 0 };

            var totalMinutes = attempts.Sum(a =>
                (int)(a.SubmittedAt!.Value - a.StartTime).TotalMinutes);

            // FIX: cast sang decimal trước khi chia để tránh integer division
            var averageScore = attempts.Average(a =>
                a.MaxScore > 0 ? ((decimal)a.TotalScore / (decimal)a.MaxScore) * 100m : 0m);

            return new WeeklyStatsModel
            {
                TotalMinutes = totalMinutes,
                ExerciseCount = attempts.Count,
                AverageScore = Math.Round(averageScore, 1)
            };
        }

        public async Task<OverallStatsModel> GetOverallStatsAsync(int studentId)
        {
            var attempts = await _context.ExerciseAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == studentId &&
                           a.Status != AttemptStatus.InProgress &&
                           a.MaxScore > 0)
                .ToListAsync();

            double averageScore = 0;
            int totalExercises = 0;

            if (attempts.Any())
            {
                totalExercises = attempts.Count;
                // FIX: cast sang double trước khi chia
                averageScore = attempts.Average(a =>
                    ((double)a.TotalScore / (double)a.MaxScore) * 100.0);
                averageScore = Math.Round(averageScore, 1);
            }

            var completedLessons = await _context.StudentProgresses
                .AsNoTracking()
                .Where(sp => sp.StudentId == studentId &&
                            sp.MasteryLevel >= MasteryLevel.Intermediate)
                .Select(sp => sp.TopicId)
                .Distinct()
                .CountAsync();

            return new OverallStatsModel
            {
                AverageScore = (decimal)averageScore,
                TotalExercises = totalExercises,
                TotalLessons = completedLessons
            };
        }

        public async Task<StreakDataModel> GetStreakDataAsync(int studentId)
        {
            var attempts = await _context.ExerciseAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == studentId &&
                           a.Status != AttemptStatus.InProgress &&
                           a.SubmittedAt.HasValue)
                .Select(a => a.SubmittedAt!.Value)
                .ToListAsync();

            if (!attempts.Any())
                return new StreakDataModel { CurrentStreak = 0, LongestStreak = 0, StudiedToday = false };

            // FIX: Chuyển tất cả timestamp sang giờ VN trước khi lấy .Date
            var studyDates = attempts
                .Select(dt => TimeZoneInfo.ConvertTimeFromUtc(dt, VnTimeZone).Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            // FIX: So sánh với ngày hôm nay theo giờ VN
            var today = VnNow.Date;
            var studiedToday = studyDates.Contains(today);
            var currentStreak = CalculateCurrentStreak(studyDates, today);
            var longestStreak = CalculateLongestStreak(studyDates);

            return new StreakDataModel
            {
                CurrentStreak = currentStreak,
                LongestStreak = longestStreak,
                StudiedToday = studiedToday
            };
        }

        public async Task<List<RecentLessonModel>> GetRecentLessonsAsync(int studentId, int limit)
        {
            var recentActivities = await _context.ExerciseAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == studentId && a.Status != AttemptStatus.InProgress)
                .Include(a => a.Exercise)
                    .ThenInclude(e => e.Topic)
                        .ThenInclude(t => t.Chapter)
                .OrderByDescending(a => a.SubmittedAt)
                .Take(limit)
                .Select(a => new RecentLessonModel
                {
                    LessonId = a.Exercise.Topic.Lessons.FirstOrDefault().LessonId,
                    LessonName = a.Exercise.Topic.Lessons.FirstOrDefault().LessonName ?? "N/A",
                    TopicName = a.Exercise.Topic.TopicName,
                    ChapterName = a.Exercise.Topic.Chapter.ChapterName,
                    CompletedAt = a.SubmittedAt,
                    DurationMinutes = (int)(a.SubmittedAt!.Value - a.StartTime).TotalMinutes,
                    IsCompleted = true,
                    ProgressPercentage = 100,
                    // FIX: cast sang double trước khi chia
                    Score = a.MaxScore > 0
                        ? Math.Round((double)a.TotalScore / (double)a.MaxScore * 100.0, 1)
                        : (double?)null
                })
                .ToListAsync();

            return recentActivities;
        }

        public async Task<List<ChapterProgressModel>> GetChapterProgressAsync(int studentId)
        {
            var chapters = await _context.Chapters
                .AsNoTracking()
                .Include(c => c.Topics)
                    .ThenInclude(t => t.studentProgresses)
                .Where(c => c.IsActive)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();

            var progressList = new List<ChapterProgressModel>();

            foreach (var chapter in chapters)
            {
                var totalTopics = chapter.Topics.Count;

                var completedTopics = chapter.Topics
                    .Count(t => t.studentProgresses
                        .Any(sp => sp.StudentId == studentId &&
                                  sp.MasteryLevel >= MasteryLevel.Intermediate));

                var completionPercentage = totalTopics > 0
                    ? Math.Round((decimal)completedTopics / totalTopics * 100, 1)
                    : 0;

                var topicProgresses = chapter.Topics
                    .SelectMany(t => t.studentProgresses)
                    .Where(sp => sp.StudentId == studentId)
                    .ToList();

                MasteryLevel? avgMastery = null;
                if (topicProgresses.Any())
                {
                    var avgMasteryValue = (int)topicProgresses.Average(sp => (int)sp.MasteryLevel);
                    avgMastery = (MasteryLevel)avgMasteryValue;
                }

                var isLocked = !chapter.Topics.Any(t => t.IsFree);

                progressList.Add(new ChapterProgressModel
                {
                    ChapterId = chapter.ChapterId,
                    ChapterName = chapter.ChapterName,
                    OrderIndex = chapter.OrderIndex,
                    CompletionPercentage = completionPercentage,
                    CompletedTopics = completedTopics,
                    TotalTopics = totalTopics,
                    IsLocked = isLocked,
                    AverageMastery = avgMastery
                });
            }

            return progressList;
        }

        private int CalculateCurrentStreak(List<DateTime> studyDates, DateTime today)
        {
            if (!studyDates.Any()) return 0;

            int streak = 0;
            var checkDate = today;

            if (!studyDates.Contains(today))
            {
                checkDate = today.AddDays(-1);
                if (!studyDates.Contains(checkDate)) return 0;
            }

            while (studyDates.Contains(checkDate))
            {
                streak++;
                checkDate = checkDate.AddDays(-1);
            }

            return streak;
        }

        private int CalculateLongestStreak(List<DateTime> studyDates)
        {
            if (!studyDates.Any()) return 0;

            studyDates = studyDates.OrderBy(d => d).ToList();

            int longestStreak = 1;
            int currentStreak = 1;

            for (int i = 1; i < studyDates.Count; i++)
            {
                var daysDiff = (studyDates[i] - studyDates[i - 1]).Days;
                if (daysDiff == 1)
                {
                    currentStreak++;
                    longestStreak = Math.Max(longestStreak, currentStreak);
                }
                else
                {
                    currentStreak = 1;
                }
            }

            return longestStreak;
        }
    }
}