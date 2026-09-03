using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard;
using ELearning_ToanHocHay_Control.Models.DTOs.AI;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface ICoreDashboardService
    {
        Task<CoreDashboardDto> GetCoreDashboardAsync(int studentId);
        Task<bool> VerifyStudentAccessAsync(int studentId, int userId);
        Task<List<ChapterScoreComparisonDto>> GetChapterScoreComparisonAsync(int studentId);
        Task<PackageTier> GetPackageTierAsync(int studentId);
        Task<AIInsightResponse?> GetAIInsightAsync(int studentId);
        Task<AIInsightResponse?> GetAIRoadmapAsync(int studentId);
    }
}
