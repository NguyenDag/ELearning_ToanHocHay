using ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface ICoreDashboardService
    {
        Task<CoreDashboardDto> GetCoreDashboardAsync(int studentId);
        Task<bool> VerifyStudentAccessAsync(int studentId, int userId);
        Task<List<ChapterScoreComparisonDto>> GetChapterScoreComparisonAsync(int studentId);
        Task<PackageType> GetPackageTypeAsync(int studentId);
    }
}
