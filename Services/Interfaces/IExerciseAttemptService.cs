using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.ExerciseAttempt;
using ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface IExerciseAttemptService
    {
        // Bắt đầu làm bài theo đề có sẵn
        Task<ApiResponse<ExerciseAttemptDto>> StartExerciseAsync(StartExerciseDto dto);

        // Bắt đầu làm bài random
        Task<ApiResponse<ExerciseAttemptDto>> StartRandomExerciseAsync(StartRandomExerciseDto dto);

        // Autosave a single answer
        Task<ApiResponse<bool>> SaveAnswerAsync(SaveAnswerDto dto);

        // Finalise an attempt and compute the score
        Task<ApiResponse<ExerciseResultDto>> CompleteExerciseAsync(CompleteExerciseDto dto);

        // View a completed attempt
        Task<ApiResponse<ExerciseResultDto>> GetExerciseResultAsync(int attemptId);

        // A student's attempt history
        Task<ApiResponse<List<ExerciseResultDto>>> GetStudentHistoryAsync(int studentId);

        // AI feedback generation progress for an attempt
        Task<ApiResponse<FeedbackStatusDto>> GetFeedbackStatusAsync(int attemptId);

        Task<ApiResponse<StudentDashboardDto>> GetDashboardStatsAsync(int userId);

        // Báo cáo chuyển tab
        Task<ApiResponse<bool>> ReportTabSwitchAsync(int attemptId);

        // Lấy lịch sử chuyển tab
        Task<ApiResponse<List<DateTime>>> GetTabSwitchLogsAsync(int attemptId);
    }
}
