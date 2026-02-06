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

        public DashboardRepository(
            AppDbContext context,
            ILogger<DashboardRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<WeeklyStatsModel> GetWeeklyStatsAsync(
            int studentId,
            DateTime startDate,
            DateTime endDate)
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
            {
                return new WeeklyStatsModel
                {
                    TotalMinutes = 0,
                    ExerciseCount = 0,
                    AverageScore = 0
                };
            }

            var totalMinutes = attempts.Sum(a =>
                (int)(a.SubmittedAt.Value - a.StartTime).TotalMinutes);

            var averageScore = (decimal)attempts.Average(a =>
                (a.TotalScore / a.MaxScore) * 100);

            return new WeeklyStatsModel
            {
                TotalMinutes = totalMinutes,
                ExerciseCount = attempts.Count,
                AverageScore = Math.Round(averageScore, 1)
            };
        }

        public async Task<OverallStatsModel> GetOverallStatsAsync(int studentId)
        {
            // Thống kê tất cả bài tập đã làm
            var exerciseStats = await _context.ExerciseAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == studentId &&
                           a.Status != AttemptStatus.InProgress)
                .GroupBy(a => a.StudentId)
                .Select(g => new
                {
                    TotalExercises = g.Count(),
                    AverageScore = g.Average(a => (a.TotalScore / a.MaxScore) * 100)
                })
                .FirstOrDefaultAsync();

            // Đếm số bài học đã hoàn thành
            // (Giả sử có bảng LessonProgress hoặc tracking qua ExerciseAttempt)
            var completedLessons = await _context.StudentProgresses
                .AsNoTracking()
                .Where(sp => sp.StudentId == studentId &&
                            sp.MasteryLevel >= MasteryLevel.Intermediate)
                .Select(sp => sp.TopicId)
                .Distinct()
                .CountAsync();

            return new OverallStatsModel
            {
                AverageScore = exerciseStats != null
                    ? Math.Round((decimal)exerciseStats.AverageScore, 1)
                    : 0,
                TotalExercises = exerciseStats?.TotalExercises ?? 0,
                TotalLessons = completedLessons
            };
        }

        public async Task<StreakDataModel> GetStreakDataAsync(int studentId)
        {
            // Lấy tất cả ngày đã học (distinct dates)
            var studyDates = await _context.ExerciseAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == studentId &&
                           a.Status != AttemptStatus.InProgress &&
                           a.SubmittedAt.HasValue)
                .Select(a => a.SubmittedAt.Value.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToListAsync();

            if (!studyDates.Any())
            {
                return new StreakDataModel
                {
                    CurrentStreak = 0,
                    LongestStreak = 0,
                    StudiedToday = false
                };
            }

            var today = DateTime.UtcNow.Date;
            var studiedToday = studyDates.Contains(today);

            // Tính current streak
            var currentStreak = CalculateCurrentStreak(studyDates, today);

            // Tính longest streak
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
            // Option 1: Nếu có bảng LessonProgress riêng
            // var recentLessons = await _context.LessonProgresses...

            // Option 2: Track qua ExerciseAttempt (mỗi bài tập thuộc 1 topic/lesson)
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
                    DurationMinutes = (int)(a.SubmittedAt.Value - a.StartTime).TotalMinutes,
                    IsCompleted = true,
                    ProgressPercentage = 100
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

                // Đếm số topics đã hoàn thành (Mastery >= Intermediate)
                var completedTopics = chapter.Topics
                    .Count(t => t.studentProgresses
                        .Any(sp => sp.StudentId == studentId &&
                                  sp.MasteryLevel >= MasteryLevel.Intermediate));

                var completionPercentage = totalTopics > 0
                    ? Math.Round((decimal)completedTopics / totalTopics * 100, 1)
                    : 0;

                // Tính mastery trung bình của chapter
                var topicProgresses = chapter.Topics
                    .SelectMany(t => t.studentProgresses)
                    .Where(sp => sp.StudentId == studentId)
                    .ToList();

                MasteryLevel? avgMastery = null;
                if (topicProgresses.Any())
                {
                    var avgMasteryValue = (int)topicProgresses
                        .Average(sp => (int)sp.MasteryLevel);
                    avgMastery = (MasteryLevel)avgMasteryValue;
                }

                // Kiểm tra có bị khóa không (cần Premium)
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

            // Nếu hôm nay chưa học, bắt đầu từ hôm qua
            if (!studyDates.Contains(today))
            {
                checkDate = today.AddDays(-1);
                // Nếu hôm qua cũng chưa học → streak = 0
                if (!studyDates.Contains(checkDate))
                    return 0;
            }

            // Đếm ngược từ hôm nay/hôm qua
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
