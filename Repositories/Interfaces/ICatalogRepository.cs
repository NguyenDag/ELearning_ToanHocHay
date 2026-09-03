using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    /// <summary>Catalog layer (§5.1): Subject · GradeLevel · CurriculumFramework.</summary>
    public interface ICatalogRepository
    {
        // Subject
        Task<List<Subject>> GetSubjectsAsync(bool includeInactive);
        Task<Subject?> GetSubjectAsync(int subjectId);
        Task<bool> SubjectCodeExistsAsync(string code, int? exceptId = null);
        Task<Subject> AddSubjectAsync(Subject subject);
        Task UpdateSubjectAsync(Subject subject);

        // GradeLevel
        Task<List<GradeLevel>> GetGradeLevelsAsync(bool includeInactive);
        Task<GradeLevel?> GetGradeLevelAsync(int gradeLevelId);
        Task<bool> GradeLevelCodeExistsAsync(string code, int? exceptId = null);
        Task<GradeLevel> AddGradeLevelAsync(GradeLevel gradeLevel);
        Task UpdateGradeLevelAsync(GradeLevel gradeLevel);

        // CurriculumFramework
        Task<List<CurriculumFramework>> GetFrameworksAsync(bool includeInactive);
        Task<CurriculumFramework?> GetFrameworkAsync(int frameworkId);
        Task<bool> FrameworkCodeExistsAsync(string code, int? exceptId = null);
        Task<CurriculumFramework> AddFrameworkAsync(CurriculumFramework framework);
        Task UpdateFrameworkAsync(CurriculumFramework framework);
    }
}
