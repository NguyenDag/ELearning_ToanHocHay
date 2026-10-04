using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models;
using ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IDashboardRepository
    {
        Task<WeeklyStatsModel> GetWeeklyStatsAsync(int studentId, DateTime startDate, DateTime endDate);
        Task<OverallStatsModel> GetOverallStatsAsync(int studentId);
        Task<StreakDataModel> GetStreakDataAsync(int studentId);
        Task<List<RecentLessonModel>> GetRecentLessonsAsync(int studentId, int limit);
        Task<List<ChapterProgressModel>> GetChapterProgressAsync(int studentId);
        Task<List<ChapterScoreComparisonDto>> GetChapterComparisonAsync(int studentId);
        Task<List<WeakTopicDto>> GetWeakTopicsAsync(int studentId, int limit);
        /// <summary>
        /// Điểm trung bình mỗi topic, tính bằng trung-bình-của-trung-bình-ngày trong `windowDays` gần nhất
        /// (một ngày làm nhiều đề chỉ tính là 1 điểm dữ liệu, không lấn át các ngày khác).
        /// Tự fallback về toàn bộ lịch sử nếu window không đủ dữ liệu.
        /// </summary>
        Task<List<TopicPerformanceDto>> GetFullPerformanceAsync(int studentId, int windowDays = 30);
    }
}
