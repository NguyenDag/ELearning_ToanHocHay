using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Content;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class ProgressProjectionService : IProgressProjectionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProgressProjectionService> _logger;

        // A lesson-linked exercise attempt at >= this % marks the lesson complete.
        private const decimal LessonCompleteScorePct = 70m;
        // Minimum viewing time before a lesson can be marked read.
        private const int MinViewSeconds = 20;

        public ProgressProjectionService(AppDbContext context, ILogger<ProgressProjectionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================================================================
        // Attempt -> progress
        // ==================================================================
        public async Task ProjectAttemptAsync(int attemptId)
        {
            try
            {
                var a = await _context.ExerciseAttempts
                    .AsNoTracking()
                    .Where(x => x.AttemptId == attemptId)
                    .Select(x => new
                    {
                        x.StudentId,
                        x.Status,
                        x.CorrectAnswers,
                        x.WrongAnswers,
                        x.StartTime,
                        x.SubmittedAt,
                        x.CompletionPercentage,
                        NodeId = x.Exercise!.NodeId
                    })
                    .FirstOrDefaultAsync();

                if (a?.StudentId == null || a.Status == AttemptStatus.InProgress)
                    return;

                var studentId = a.StudentId.Value;
                var endedAt = a.SubmittedAt ?? DateTime.UtcNow;
                var minutes = Math.Max(0, (int)(endedAt - a.StartTime).TotalMinutes);
                var questions = a.CorrectAnswers + a.WrongAnswers;

                await BumpSnapshotAsync(studentId, minutes, exercises: 1, lessons: 0, questions: questions);

                if (a.NodeId == null)
                {
                    await _context.SaveChangesAsync();
                    return;
                }

                var node = await _context.ContentNodes.AsNoTracking().FirstOrDefaultAsync(n => n.NodeId == a.NodeId.Value);
                if (node == null)
                {
                    await _context.SaveChangesAsync();
                    return;
                }

                var np = await GetOrCreateAsync(studentId, node.NodeId);
                np.TotalAttempts += 1;
                np.CorrectCount += a.CorrectAnswers;
                np.WrongCount += a.WrongAnswers;
                np.TimeSpentSeconds += Math.Max(0, (int)(endedAt - a.StartTime).TotalSeconds);
                np.LastAccessedAt = DateTime.UtcNow;

                var pct = Math.Clamp(a.CompletionPercentage, 0m, 100m);
                np.CompletionPercent = Math.Max(np.CompletionPercent, pct);
                np.MasteryLevel = MasteryFor(np.CompletionPercent);
                if (np.CompletionPercent >= LessonCompleteScorePct)
                    np.Status = ProgressStatus.Completed;
                else if (np.Status != ProgressStatus.Completed)
                    np.Status = ProgressStatus.InProgress;

                await _context.SaveChangesAsync();

                await RollUpAsync(studentId, node);
                await UpdateCourseCacheAsync(studentId, node.CourseVersionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProjectAttemptAsync failed for attempt {AttemptId}", attemptId);
            }
        }

        // ==================================================================
        // Lesson viewed -> progress
        // ==================================================================
        public async Task<ApiResponse<NodeProgressDto>> MarkLessonCompleteAsync(int studentId, int nodeId, int secondsViewed)
        {
            var node = await _context.ContentNodes.FirstOrDefaultAsync(n => n.NodeId == nodeId);
            if (node == null || node.IsHidden)
                return ApiResponse<NodeProgressDto>.ErrorResponse("Lesson not found");
            if (node.NodeType != NodeType.Lesson)
                return ApiResponse<NodeProgressDto>.ErrorResponse("Only a lesson node can be marked complete");

            if (!node.IsFree)
            {
                var enrolled = await _context.StudentCourses.AnyAsync(sc =>
                    sc.StudentId == studentId &&
                    sc.CourseVersionId == node.CourseVersionId &&
                    sc.Status == StudentCourseStatus.Active);
                if (!enrolled)
                    return ApiResponse<NodeProgressDto>.Forbidden("Bạn chưa ghi danh khoá học này");
            }

            if (secondsViewed < MinViewSeconds)
                return ApiResponse<NodeProgressDto>.ErrorResponse(
                    $"Cần xem bài ít nhất {MinViewSeconds} giây trước khi đánh dấu hoàn thành");

            var np = await GetOrCreateAsync(studentId, nodeId);
            np.Status = ProgressStatus.Completed;
            np.CompletionPercent = Math.Max(np.CompletionPercent, 100m);
            if (np.MasteryLevel < MasteryLevel.Intermediate) np.MasteryLevel = MasteryLevel.Intermediate;
            np.TimeSpentSeconds += Math.Max(0, secondsViewed);
            np.LastAccessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await BumpSnapshotAsync(studentId, minutes: Math.Max(0, secondsViewed / 60), exercises: 0, lessons: 1, questions: 0);
            await _context.SaveChangesAsync();

            await RollUpAsync(studentId, node);
            await UpdateCourseCacheAsync(studentId, node.CourseVersionId);

            return ApiResponse<NodeProgressDto>.SuccessResponse(Map(np, node), "Đã đánh dấu hoàn thành bài học");
        }

        public async Task RecomputeCourseVersionAsync(int studentId, int courseVersionId)
        {
            var aggregates = await _context.ContentNodes
                .Where(n => n.CourseVersionId == courseVersionId && n.NodeType != NodeType.Lesson)
                .OrderByDescending(n => n.Depth)
                .ToListAsync();

            foreach (var node in aggregates)
                await RecomputeAggregateAsync(studentId, node);

            await UpdateCourseCacheAsync(studentId, courseVersionId);
        }

        // ==================================================================
        // reads
        // ==================================================================
        public async Task<ApiResponse<List<NodeProgressDto>>> GetVersionProgressAsync(int studentId, int courseVersionId)
        {
            var nodes = await _context.ContentNodes
                .AsNoTracking()
                .Where(n => n.CourseVersionId == courseVersionId)
                .ToDictionaryAsync(n => n.NodeId);

            if (nodes.Count == 0)
                return ApiResponse<List<NodeProgressDto>>.SuccessResponse(new List<NodeProgressDto>());

            var rows = await _context.NodeProgresses
                .AsNoTracking()
                .Where(p => p.StudentId == studentId && nodes.Keys.Contains(p.NodeId))
                .ToListAsync();

            var dtos = rows
                .Where(p => nodes.ContainsKey(p.NodeId))
                .Select(p => Map(p, nodes[p.NodeId]))
                .OrderBy(d => d.NodeId)
                .ToList();

            return ApiResponse<List<NodeProgressDto>>.SuccessResponse(dtos);
        }

        public async Task<List<DailyActivityDto>> GetHeatmapAsync(int studentId, int days)
        {
            days = Math.Clamp(days, 1, 366);
            var from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-days + 1);

            return await _context.DailyActivitySnapshots
                .AsNoTracking()
                .Where(s => s.StudentId == studentId && s.Date >= from)
                .OrderBy(s => s.Date)
                .Select(s => new DailyActivityDto
                {
                    Date = s.Date,
                    MinutesStudied = s.MinutesStudied,
                    ExercisesDone = s.ExercisesDone,
                    LessonsDone = s.LessonsDone,
                    QuestionsAnswered = s.QuestionsAnswered
                })
                .ToListAsync();
        }

        // ==================================================================
        // helpers
        // ==================================================================
        private async Task RollUpAsync(int studentId, ContentNode leaf)
        {
            // MaterializedPath is "/id1/id2/leafId/" — every id but the last is an ancestor.
            var ancestorIds = leaf.MaterializedPath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id != 0 && id != leaf.NodeId)
                .ToList();

            if (ancestorIds.Count == 0) return;

            var ancestors = await _context.ContentNodes
                .Where(n => ancestorIds.Contains(n.NodeId))
                .OrderByDescending(n => n.Depth)
                .ToListAsync();

            foreach (var node in ancestors)
                await RecomputeAggregateAsync(studentId, node);
        }

        private async Task RecomputeAggregateAsync(int studentId, ContentNode node)
        {
            // MaterializedPath already ends with the node's own id ("/…/nodeId/").
            var prefix = node.MaterializedPath;
            var lessonIds = await _context.ContentNodes
                .Where(n => n.NodeType == NodeType.Lesson && !n.IsHidden
                            && n.NodeId != node.NodeId
                            && n.MaterializedPath.StartsWith(prefix))
                .Select(n => n.NodeId)
                .ToListAsync();

            if (lessonIds.Count == 0) return;

            var completed = await _context.NodeProgresses
                .CountAsync(p => p.StudentId == studentId
                                 && lessonIds.Contains(p.NodeId)
                                 && p.Status == ProgressStatus.Completed);

            var pct = Math.Round((decimal)completed / lessonIds.Count * 100m, 2);

            var np = await GetOrCreateAsync(studentId, node.NodeId);
            np.CompletionPercent = pct;
            np.Status = pct >= 100m ? ProgressStatus.Completed
                       : pct > 0m ? ProgressStatus.InProgress
                       : ProgressStatus.NotStarted;
            np.MasteryLevel = MasteryFor(pct);
            np.LastAccessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private async Task UpdateCourseCacheAsync(int studentId, int courseVersionId)
        {
            var enrolments = await _context.StudentCourses
                .Where(sc => sc.StudentId == studentId && sc.CourseVersionId == courseVersionId)
                .ToListAsync();
            if (enrolments.Count == 0) return;

            var lessonIds = await _context.ContentNodes
                .Where(n => n.CourseVersionId == courseVersionId && n.NodeType == NodeType.Lesson && !n.IsHidden)
                .Select(n => n.NodeId)
                .ToListAsync();

            decimal pct = 0;
            if (lessonIds.Count > 0)
            {
                var done = await _context.NodeProgresses.CountAsync(p =>
                    p.StudentId == studentId && lessonIds.Contains(p.NodeId) && p.Status == ProgressStatus.Completed);
                pct = Math.Round((decimal)done / lessonIds.Count * 100m, 2);
            }

            foreach (var e in enrolments)
            {
                e.ProgressPercent = pct;
                if (pct >= 100m && e.Status == StudentCourseStatus.Active)
                    e.Status = StudentCourseStatus.Completed;
            }
            await _context.SaveChangesAsync();
        }

        private async Task<NodeProgress> GetOrCreateAsync(int studentId, int nodeId)
        {
            var np = await _context.NodeProgresses
                .FirstOrDefaultAsync(p => p.StudentId == studentId && p.NodeId == nodeId);

            if (np == null)
            {
                np = new NodeProgress
                {
                    StudentId = studentId,
                    NodeId = nodeId,
                    Status = ProgressStatus.NotStarted,
                    MasteryLevel = MasteryLevel.NotStarted,
                    LastAccessedAt = DateTime.UtcNow
                };
                _context.NodeProgresses.Add(np);
            }
            return np;
        }

        private async Task BumpSnapshotAsync(int studentId, int minutes, int exercises, int lessons, int questions)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var snap = await _context.DailyActivitySnapshots
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.Date == today);

            if (snap == null)
            {
                snap = new DailyActivitySnapshot { StudentId = studentId, Date = today };
                _context.DailyActivitySnapshots.Add(snap);
            }

            snap.MinutesStudied += minutes;
            snap.ExercisesDone += exercises;
            snap.LessonsDone += lessons;
            snap.QuestionsAnswered += questions;
        }

        private static MasteryLevel MasteryFor(decimal pct) => pct switch
        {
            >= 90m => MasteryLevel.Mastered,
            >= 70m => MasteryLevel.Advanced,
            >= 40m => MasteryLevel.Intermediate,
            > 0m => MasteryLevel.Beginner,
            _ => MasteryLevel.NotStarted
        };

        private static NodeProgressDto Map(NodeProgress np, ContentNode node) => new()
        {
            NodeId = np.NodeId,
            NodeType = node.NodeType,
            Title = node.Title,
            Status = np.Status,
            MasteryLevel = np.MasteryLevel,
            CompletionPercent = np.CompletionPercent,
            TimeSpentSeconds = np.TimeSpentSeconds,
            TotalAttempts = np.TotalAttempts,
            CorrectCount = np.CorrectCount,
            WrongCount = np.WrongCount,
            LastAccessedAt = np.LastAccessedAt
        };
    }
}
