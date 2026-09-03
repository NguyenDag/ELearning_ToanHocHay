using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Notification;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface INotificationService
    {
        Task<ApiResponse<PagedResult<NotificationDto>>> GetMineAsync(int userId, bool unreadOnly, int page, int pageSize);
        Task<ApiResponse<int>> GetUnreadCountAsync(int userId);
        Task<ApiResponse<bool>> MarkReadAsync(int userId, int notificationId);
        Task<ApiResponse<bool>> MarkAllReadAsync(int userId);
        Task<ApiResponse<List<NotificationPreferenceDto>>> GetPreferencesAsync(int userId);
        Task<ApiResponse<bool>> SetPreferenceAsync(int userId, string ruleKey, bool enabled);
    }

    /// <summary>P6 — turns learning events into Notification rows for the right recipients.</summary>
    public interface INotificationRuleEngine
    {
        Task<int> OnTabSwitchAsync(int attemptId, int switchCount);
        Task<int> OnExerciseCompletedAsync(int attemptId);
        Task<int> RunInactivitySweepAsync();
    }
}
