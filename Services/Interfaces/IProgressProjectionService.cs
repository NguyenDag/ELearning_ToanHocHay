using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Content;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    /// <summary>
    /// P4 (A2-06) — writes NodeProgress from real activity and rolls it up the tree
    /// via MaterializedPath; also keeps the DailyActivitySnapshot fed.
    /// </summary>
    public interface IProgressProjectionService
    {
        /// <summary>Fold a just-submitted attempt into the student's progress. Never throws.</summary>
        Task ProjectAttemptAsync(int attemptId);

        /// <summary>Student marks a lesson read after viewing it long enough.</summary>
        Task<ApiResponse<NodeProgressDto>> MarkLessonCompleteAsync(int studentId, int nodeId, int secondsViewed);

        /// <summary>Recompute every aggregate node of a course version from its leaf lessons.</summary>
        Task RecomputeCourseVersionAsync(int studentId, int courseVersionId);

        // ---- reads ----
        Task<ApiResponse<List<NodeProgressDto>>> GetVersionProgressAsync(int studentId, int courseVersionId);
        Task<List<DailyActivityDto>> GetHeatmapAsync(int studentId, int days);
    }
}
