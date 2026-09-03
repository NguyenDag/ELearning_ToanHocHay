using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Notification;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public static class NotificationRules
    {
        public const string TabSwitch = "tab-switch";
        public const string LowScore = "low-score";
        public const string Inactivity = "inactivity";

        public static readonly string[] All = { TabSwitch, LowScore, Inactivity };
    }

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedResult<NotificationDto>>> GetMineAsync(int userId, bool unreadOnly, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = _context.Notifications.AsNoTracking().Where(n => n.UserId == userId);
            if (unreadOnly) q = q.Where(n => !n.IsRead);

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    StudentId = n.StudentId,
                    Audience = n.Audience,
                    Title = n.Title,
                    Message = n.Message,
                    NotificationType = n.NotificationType,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return ApiResponse<PagedResult<NotificationDto>>.SuccessResponse(new PagedResult<NotificationDto>
            {
                Items = items, Total = total, Page = page, PageSize = pageSize
            });
        }

        public async Task<ApiResponse<int>> GetUnreadCountAsync(int userId)
            => ApiResponse<int>.SuccessResponse(
                await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead));

        public async Task<ApiResponse<bool>> MarkReadAsync(int userId, int notificationId)
        {
            var n = await _context.Notifications
                .FirstOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == userId);
            if (n == null) return ApiResponse<bool>.ErrorResponse("Notification not found");

            if (!n.IsRead) { n.IsRead = true; n.ReadAt = DateTime.UtcNow; await _context.SaveChangesAsync(); }
            return ApiResponse<bool>.SuccessResponse(true, "Marked read");
        }

        public async Task<ApiResponse<bool>> MarkAllReadAsync(int userId)
        {
            var unread = await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            var now = DateTime.UtcNow;
            foreach (var n in unread) { n.IsRead = true; n.ReadAt = now; }
            await _context.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(true, $"{unread.Count} đã đọc");
        }

        public async Task<ApiResponse<List<NotificationPreferenceDto>>> GetPreferencesAsync(int userId)
        {
            var rows = await _context.NotificationPreferences.AsNoTracking()
                .Where(p => p.UserId == userId).ToDictionaryAsync(p => p.RuleKey, p => p.Enabled);

            var dto = NotificationRules.All
                .Select(k => new NotificationPreferenceDto { RuleKey = k, Enabled = !rows.TryGetValue(k, out var e) || e })
                .ToList();
            return ApiResponse<List<NotificationPreferenceDto>>.SuccessResponse(dto);
        }

        public async Task<ApiResponse<bool>> SetPreferenceAsync(int userId, string ruleKey, bool enabled)
        {
            if (!NotificationRules.All.Contains(ruleKey))
                return ApiResponse<bool>.ErrorResponse($"Unknown rule '{ruleKey}'");

            var row = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.RuleKey == ruleKey);
            if (row == null)
            {
                row = new NotificationPreference { UserId = userId, RuleKey = ruleKey };
                _context.NotificationPreferences.Add(row);
            }
            row.Enabled = enabled;
            await _context.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Đã cập nhật");
        }
    }

    public class NotificationRuleEngine : INotificationRuleEngine
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NotificationRuleEngine> _logger;

        private const decimal LowScoreThreshold = 5m;    // out of 10
        private const int InactivityDays = 3;

        public NotificationRuleEngine(AppDbContext context, ILogger<NotificationRuleEngine> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> OnTabSwitchAsync(int attemptId, int switchCount)
        {
            var a = await _context.ExerciseAttempts
                .Where(x => x.AttemptId == attemptId)
                .Select(x => new { x.StudentId, ExerciseName = x.Exercise!.ExerciseName })
                .FirstOrDefaultAsync();
            if (a?.StudentId == null) return 0;

            return await FanOutAsync(a.StudentId.Value, NotificationRules.TabSwitch, NotifyAudience.Both,
                NotificationType.Warning,
                "Cảnh báo chuyển tab khi làm bài",
                $"Đã chuyển tab {switchCount} lần trong bài \"{a.ExerciseName}\".");
        }

        public async Task<int> OnExerciseCompletedAsync(int attemptId)
        {
            var a = await _context.ExerciseAttempts
                .Where(x => x.AttemptId == attemptId && x.Status != AttemptStatus.InProgress && x.MaxScore > 0)
                .Select(x => new { x.StudentId, x.TotalScore, x.MaxScore, ExerciseName = x.Exercise!.ExerciseName })
                .FirstOrDefaultAsync();
            if (a?.StudentId == null) return 0;

            var scoreOutOf10 = (decimal)(a.TotalScore / a.MaxScore) * 10m;
            if (scoreOutOf10 >= LowScoreThreshold) return 0;

            return await FanOutAsync(a.StudentId.Value, NotificationRules.LowScore, NotifyAudience.Both,
                NotificationType.Warning,
                "Điểm bài làm thấp",
                $"Bài \"{a.ExerciseName}\" chỉ đạt {scoreOutOf10:0.0}/10. Nên xem lại phần này.");
        }

        public async Task<int> RunInactivitySweepAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var since = today.AddDays(-InactivityDays);

            // Students who have studied before but not in the last N days.
            var everActive = await _context.DailyActivitySnapshots
                .Where(s => s.MinutesStudied > 0 || s.ExercisesDone > 0 || s.LessonsDone > 0)
                .Select(s => s.StudentId).Distinct().ToListAsync();

            var recentlyActive = await _context.DailyActivitySnapshots
                .Where(s => s.Date > since && (s.MinutesStudied > 0 || s.ExercisesDone > 0 || s.LessonsDone > 0))
                .Select(s => s.StudentId).Distinct().ToListAsync();

            var idle = everActive.Except(recentlyActive).ToList();
            var created = 0;
            var cutoff = DateTime.UtcNow.AddDays(-InactivityDays);

            foreach (var studentId in idle)
            {
                // Dedup: skip if we already nudged this student recently.
                var studentUserId = await _context.Students.Where(s => s.StudentId == studentId)
                    .Select(s => s.UserId).FirstOrDefaultAsync();
                if (studentUserId == 0) continue;

                var alreadyNudged = await _context.Notifications.AnyAsync(n =>
                    n.UserId == studentUserId && n.StudentId == studentId
                    && n.Title == "Lâu rồi chưa học" && n.CreatedAt > cutoff);
                if (alreadyNudged) continue;

                created += await FanOutAsync(studentId, NotificationRules.Inactivity, NotifyAudience.Both,
                    NotificationType.Reminder,
                    "Lâu rồi chưa học",
                    $"Đã {InactivityDays} ngày chưa có hoạt động học tập. Quay lại luyện tập nhé!");
            }

            if (created > 0) _logger.LogInformation("Inactivity sweep created {Count} notifications", created);
            return created;
        }

        // ---- fan-out to student + active-linked parents, honouring opt-outs ----
        private async Task<int> FanOutAsync(
            int studentId, string ruleKey, NotifyAudience audience,
            NotificationType type, string title, string message)
        {
            var studentUserId = await _context.Students
                .Where(s => s.StudentId == studentId).Select(s => s.UserId).FirstOrDefaultAsync();

            var parentUserIds = await _context.ParentLinks
                .Where(l => l.StudentId == studentId && l.Status == LinkStatus.Active)
                .Select(l => l.Parent!.UserId)
                .ToListAsync();

            var targets = new List<int>();
            if (audience != NotifyAudience.Parent && studentUserId != 0) targets.Add(studentUserId);
            if (audience != NotifyAudience.Student) targets.AddRange(parentUserIds);
            targets = targets.Distinct().ToList();
            if (targets.Count == 0) return 0;

            var optedOut = await _context.NotificationPreferences
                .Where(p => targets.Contains(p.UserId) && p.RuleKey == ruleKey && !p.Enabled)
                .Select(p => p.UserId)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var created = 0;
            foreach (var userId in targets.Except(optedOut))
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    StudentId = studentId,
                    Audience = audience,
                    Title = title,
                    Message = message,
                    NotificationType = type,
                    CreatedAt = now
                });
                created++;
            }
            await _context.SaveChangesAsync();
            return created;
        }
    }
}
