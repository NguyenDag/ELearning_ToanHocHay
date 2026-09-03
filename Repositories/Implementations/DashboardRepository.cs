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
            // P4 — streak from DailyActivitySnapshot instead of scanning every attempt.
            var days = await _context.DailyActivitySnapshots
                .AsNoTracking()
                .Where(s => s.StudentId == studentId
                            && (s.MinutesStudied > 0 || s.ExercisesDone > 0 || s.LessonsDone > 0))
                .Select(s => s.Date.ToDateTime(TimeOnly.MinValue))
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
            var rows = await _context.NodeProgresses
                .AsNoTracking()
                .Where(np => np.StudentId == studentId && np.Node!.NodeType == NodeType.Lesson)
                .OrderByDescending(np => np.LastAccessedAt)
                .Take(limit)
                .Select(np => new
                {
                    np.NodeId,
                    LessonName = np.Node!.Title,
                    TopicName = np.Node.Parent != null ? np.Node.Parent.Title : "",
                    np.Node.MaterializedPath,
                    np.Status,
                    np.LastAccessedAt,
                    np.CompletionPercent
                })
                .ToListAsync();

            var result = new List<RecentLessonModel>();
            var chapterCache = new Dictionary<string, string>();
            foreach (var r in rows)
            {
                if (!chapterCache.TryGetValue(r.MaterializedPath, out var chapterName))
                {
                    chapterName = await ResolveChapterNameAsync(r.MaterializedPath);
                    chapterCache[r.MaterializedPath] = chapterName;
                }
                result.Add(new RecentLessonModel
                {
                    LessonId = r.NodeId,
                    LessonName = r.LessonName,
                    TopicName = r.TopicName,
                    ChapterName = chapterName,
                    CompletedAt = r.Status == ProgressStatus.Completed ? r.LastAccessedAt : null,
                    IsCompleted = r.Status == ProgressStatus.Completed,
                    ProgressPercentage = (int)r.CompletionPercent
                });
            }
            return result;
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
                var prefix = ch.MaterializedPath; // already ends with "/{chapterId}/"
                var lessons = await _context.ContentNodes
                    .AsNoTracking()
                    .Where(n => n.NodeType == NodeType.Lesson && n.NodeId != ch.NodeId
                                && n.MaterializedPath.StartsWith(prefix))
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
            var raw = await _context.ExerciseAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == studentId && a.Status != AttemptStatus.InProgress
                            && a.MaxScore > 0 && a.Exercise!.NodeId != null)
                .Select(a => new
                {
                    a.Exercise!.NodeId,
                    a.Exercise.Node!.MaterializedPath,
                    Ratio = a.TotalScore / a.MaxScore
                })
                .ToListAsync();

            if (raw.Count == 0) return new List<ChapterScoreComparisonDto>();

            // Roll every attempt up to its Chapter ancestor.
            var chapterIds = raw
                .SelectMany(r => r.MaterializedPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id != 0)
                .Concat(raw.Select(r => r.NodeId!.Value))
                .Distinct()
                .ToList();

            var chapters = await _context.ContentNodes
                .AsNoTracking()
                .Where(n => chapterIds.Contains(n.NodeId) && n.NodeType == NodeType.Chapter)
                .Select(n => new { n.NodeId, n.Title })
                .ToListAsync();
            var chapterById = chapters.ToDictionary(c => c.NodeId, c => c.Title);

            return raw
                .Select(r =>
                {
                    var chapterId = r.MaterializedPath
                        .Split('/', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s, out var id) ? id : 0)
                        .FirstOrDefault(id => chapterById.ContainsKey(id));
                    if (chapterId == 0 && chapterById.ContainsKey(r.NodeId!.Value)) chapterId = r.NodeId.Value;
                    return new { chapterId, r.Ratio };
                })
                .Where(x => x.chapterId != 0)
                .GroupBy(x => x.chapterId)
                .Select(g => new ChapterScoreComparisonDto
                {
                    ChapterId = g.Key,
                    ChapterName = chapterById.TryGetValue(g.Key, out var t) ? t : "",
                    AverageScore = Math.Round((decimal)g.Average(x => x.Ratio) * 10m, 1)
                })
                .ToList();
        }

        public async Task<List<WeakTopicDto>> GetWeakTopicsAsync(int studentId, int limit)
        {
            // P4 — real weak areas from NodeProgress written by ProgressProjectionService.
            var weak = await _context.NodeProgresses
                .AsNoTracking()
                .Where(p => p.StudentId == studentId
                            && p.Node!.NodeType != NodeType.Lesson
                            && (p.WrongCount > p.CorrectCount || p.CompletionPercent < 50m)
                            && p.Status != ProgressStatus.Completed)
                .OrderByDescending(p => p.WrongCount)
                .ThenBy(p => p.CompletionPercent)
                .Take(limit)
                .Select(p => new
                {
                    p.NodeId,
                    TopicName = p.Node!.Title,
                    p.Node.MaterializedPath,
                    p.WrongCount
                })
                .ToListAsync();

            if (weak.Count == 0) return new List<WeakTopicDto>();

            var result = new List<WeakTopicDto>();
            foreach (var w in weak)
            {
                var prefix = w.MaterializedPath; // already ends with "/{nodeId}/"
                var lessons = await _context.ContentNodes
                    .AsNoTracking()
                    .Where(n => n.NodeType == NodeType.Lesson && !n.IsHidden && n.NodeId != w.NodeId
                                && n.MaterializedPath.StartsWith(prefix))
                    .OrderBy(n => n.OrderIndex)
                    .Select(n => new { n.NodeId, n.Title })
                    .ToListAsync();

                var chapterName = await ResolveChapterNameAsync(w.MaterializedPath);

                result.Add(new WeakTopicDto
                {
                    TopicId = w.NodeId,
                    TopicName = w.TopicName,
                    ChapterName = chapterName,
                    ErrorCount = w.WrongCount,
                    FirstLessonId = lessons.FirstOrDefault()?.NodeId,
                    LessonNames = lessons.Select(l => l.Title).ToList()
                });
            }
            return result;
        }

        public async Task<List<TopicPerformanceDto>> GetFullPerformanceAsync(int studentId)
        {
            // P4 — average score per node, from real submitted attempts.
            var rows = await _context.ExerciseAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == studentId
                            && a.Status != AttemptStatus.InProgress
                            && a.MaxScore > 0
                            && a.Exercise!.NodeId != null)
                .Select(a => new { NodeId = a.Exercise!.NodeId!.Value, Ratio = a.TotalScore / a.MaxScore })
                .ToListAsync();

            if (rows.Count == 0) return new List<TopicPerformanceDto>();

            var nodeIds = rows.Select(r => r.NodeId).Distinct().ToList();
            var nodes = await _context.ContentNodes
                .AsNoTracking()
                .Where(n => nodeIds.Contains(n.NodeId))
                .Select(n => new { n.NodeId, n.Title, n.MaterializedPath })
                .ToListAsync();
            var nodeById = nodes.ToDictionary(n => n.NodeId);

            var chapterNameCache = new Dictionary<string, string>();

            var result = new List<TopicPerformanceDto>();
            foreach (var g in rows.GroupBy(r => r.NodeId))
            {
                if (!nodeById.TryGetValue(g.Key, out var node)) continue;

                if (!chapterNameCache.TryGetValue(node.MaterializedPath, out var chapterName))
                {
                    chapterName = await ResolveChapterNameAsync(node.MaterializedPath);
                    chapterNameCache[node.MaterializedPath] = chapterName;
                }

                result.Add(new TopicPerformanceDto
                {
                    TopicName = node.Title,
                    ChapterName = chapterName,
                    AverageScore = Math.Round((decimal)g.Average(x => x.Ratio) * 10m, 1),
                    TotalAttempts = g.Count()
                });
            }
            return result.OrderBy(r => r.AverageScore).ToList();
        }

        /// <summary>Title of the Chapter-type ancestor named in a MaterializedPath ("" if none).</summary>
        private async Task<string> ResolveChapterNameAsync(string materializedPath)
        {
            var ids = materializedPath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id != 0)
                .ToList();
            if (ids.Count == 0) return "";

            return await _context.ContentNodes
                .AsNoTracking()
                .Where(n => ids.Contains(n.NodeId) && n.NodeType == NodeType.Chapter)
                .OrderBy(n => n.Depth)
                .Select(n => n.Title)
                .FirstOrDefaultAsync() ?? "";
        }
    }
}
