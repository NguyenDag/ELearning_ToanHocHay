using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models;
using ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    /// <summary>
    /// Dashboard chạy trên mô hình v3 (Course / CourseVersion / ContentNode / NodeProgress).
    /// Không còn hard-code curriculumId; tiến độ theo từng khoá học sinh đã ghi danh.
    /// Roll-up chi tiết (chapter/topic/skill) phụ thuộc ProgressProjectionService — Giai đoạn 2.
    /// </summary>
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardRepository> _logger;

        public DashboardRepository(AppDbContext context, ILogger<DashboardRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<WeeklyStatsModel> GetWeeklyStatsAsync(int studentId, DateTime startDate, DateTime endDate)
        {
            var query = _context.ExerciseAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == studentId &&
                            a.Status != AttemptStatus.InProgress &&
                            a.SubmittedAt.HasValue &&
                            a.SubmittedAt.Value >= startDate &&
                            a.SubmittedAt.Value < endDate);

            var totalMinutes = await query.SumAsync(a => (int)(a.SubmittedAt!.Value - a.StartTime).TotalMinutes);
            var exerciseCount = await query.CountAsync();
            var totalScore = await query.SumAsync(a => a.TotalScore);
            var totalMax = await query.SumAsync(a => a.MaxScore);

            var averageScore = totalMax > 0
                ? Math.Round((decimal)totalScore / (decimal)totalMax * 10m, 1)
                : 0m;

            return new WeeklyStatsModel
            {
                TotalMinutes = totalMinutes,
                ExerciseCount = exerciseCount,
                AverageScore = averageScore
            };
        }

        public async Task<OverallStatsModel> GetOverallStatsAsync(int studentId)
        {
            var attempts = await _context.ExerciseAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == studentId &&
                            a.Status != AttemptStatus.InProgress &&
                            a.MaxScore > 0)
                .Select(a => new { a.TotalScore, a.MaxScore })
                .ToListAsync();

            double averageScore = 0;
            if (attempts.Count > 0)
                averageScore = Math.Round(attempts.Average(a => a.TotalScore / a.MaxScore * 10.0), 1);

            var completedLessons = await _context.NodeProgresses
                .AsNoTracking()
                .Where(np => np.StudentId == studentId && np.Status == ProgressStatus.Completed
                             && np.Node!.NodeType == NodeType.Lesson)
                .CountAsync();

            return new OverallStatsModel
            {
                AverageScore = (decimal)averageScore,
                TotalExercises = attempts.Count,
                TotalLessons = completedLessons
            };
        }

        public async Task<StreakDataModel> GetStreakDataAsync(int studentId)
        {
            var days = await _context.ExerciseAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == studentId && a.Status != AttemptStatus.InProgress && a.SubmittedAt.HasValue)
                .Select(a => a.SubmittedAt!.Value.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToListAsync();

            if (days.Count == 0)
                return new StreakDataModel();

            var today = DateTime.UtcNow.Date;
            bool studiedToday = days.Contains(today);

            int current = 0;
            var cursor = studiedToday ? today : today.AddDays(-1);
            foreach (var _ in days)
            {
                if (days.Contains(cursor)) { current++; cursor = cursor.AddDays(-1); }
                else break;
            }

            int longest = 1, run = 1;
            for (int i = 1; i < days.Count; i++)
            {
                if (days[i] == days[i - 1].AddDays(-1)) run++;
                else run = 1;
                longest = Math.Max(longest, run);
            }

            return new StreakDataModel
            {
                CurrentStreak = current,
                LongestStreak = Math.Max(longest, current),
                StudiedToday = studiedToday
            };
        }

        public async Task<List<RecentLessonModel>> GetRecentLessonsAsync(int studentId, int limit)
        {
            return await _context.NodeProgresses
                .AsNoTracking()
                .Where(np => np.StudentId == studentId && np.Node!.NodeType == NodeType.Lesson)
                .OrderByDescending(np => np.LastAccessedAt)
                .Take(limit)
                .Select(np => new RecentLessonModel
                {
                    LessonId = np.NodeId,
                    LessonName = np.Node!.Title,
                    TopicName = np.Node.Parent != null ? np.Node.Parent.Title : "",
                    ChapterName = "",
                    CompletedAt = np.Status == ProgressStatus.Completed ? np.LastAccessedAt : null,
                    IsCompleted = np.Status == ProgressStatus.Completed,
                    ProgressPercentage = (int)np.CompletionPercent
                })
                .ToListAsync();
        }

        public async Task<List<ChapterProgressModel>> GetChapterProgressAsync(int studentId)
        {
            // Chương = ContentNode kiểu Chapter trong các CourseVersion học sinh đã ghi danh.
            var versionIds = await _context.StudentCourses
                .AsNoTracking()
                .Where(sc => sc.StudentId == studentId)
                .Select(sc => sc.CourseVersionId)
                .ToListAsync();

            if (versionIds.Count == 0)
                return new List<ChapterProgressModel>();

            var chapters = await _context.ContentNodes
                .AsNoTracking()
                .Where(n => n.NodeType == NodeType.Chapter && versionIds.Contains(n.CourseVersionId))
                .OrderBy(n => n.OrderIndex)
                .Select(n => new { n.NodeId, n.Title, n.OrderIndex, n.MaterializedPath })
                .ToListAsync();

            var result = new List<ChapterProgressModel>();
            foreach (var ch in chapters)
            {
                var prefix = ch.MaterializedPath + ch.NodeId + "/";
                var lessons = await _context.ContentNodes
                    .AsNoTracking()
                    .Where(n => n.NodeType == NodeType.Lesson && n.MaterializedPath.StartsWith(prefix))
                    .Select(n => n.NodeId)
                    .ToListAsync();

                int total = lessons.Count;
                int done = total == 0 ? 0 : await _context.NodeProgresses
                    .AsNoTracking()
                    .CountAsync(np => np.StudentId == studentId && lessons.Contains(np.NodeId)
                                      && np.Status == ProgressStatus.Completed);

                result.Add(new ChapterProgressModel
                {
                    ChapterId = ch.NodeId,
                    ChapterName = ch.Title,
                    OrderIndex = ch.OrderIndex,
                    TotalTopics = total,
                    CompletedTopics = done,
                    CompletionPercentage = total == 0 ? 0 : Math.Round((decimal)done / total * 100m, 1),
                    IsLocked = false,
                    AverageMastery = null
                });
            }
            return result;
        }

        public async Task<List<ChapterScoreComparisonDto>> GetChapterComparisonAsync(int studentId)
        {
            // TODO(GĐ2): điểm TB theo chương qua Exercise.Node + MaterializedPath.
            var rows = await _context.ExerciseAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == studentId && a.Status != AttemptStatus.InProgress
                            && a.MaxScore > 0 && a.Exercise!.NodeId != null)
                .GroupBy(a => a.Exercise!.NodeId!.Value)
                .Select(g => new ChapterScoreComparisonDto
                {
                    ChapterId = g.Key,
                    ChapterName = "",
                    AverageScore = (decimal)g.Average(a => a.TotalScore / a.MaxScore * 10.0)
                })
                .ToListAsync();
            return rows;
        }

        public Task<List<WeakTopicDto>> GetWeakTopicsAsync(int studentId, int limit)
        {
            // TODO(GĐ2): dựa SkillProgress / NodeProgress khi ProgressProjectionService có dữ liệu.
            return Task.FromResult(new List<WeakTopicDto>());
        }

        public Task<List<TopicPerformanceDto>> GetFullPerformanceAsync(int studentId)
        {
            // TODO(GĐ2): dựa NodeProgress / SkillProgress.
            return Task.FromResult(new List<TopicPerformanceDto>());
        }
    }
}
