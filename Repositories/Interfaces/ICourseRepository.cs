using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface ICourseRepository
    {
        // ----- Course -----
        Task<List<Course>> GetCoursesAsync(int? subjectId, int? gradeLevelId, CourseStatus? status);
        Task<Course?> GetCourseAsync(int courseId, bool withVersions = false);
        Task<Course?> GetCourseBySlugAsync(string slug);
        Task<bool> SlugExistsAsync(string slug, int? exceptId = null);
        Task<bool> SubjectGradeFrameworkExistsAsync(int subjectId, int gradeLevelId, int? frameworkId, int? exceptId = null);
        Task<Course> AddCourseAsync(Course course);
        Task UpdateCourseAsync(Course course);

        // ----- CourseVersion -----
        Task<CourseVersion?> GetVersionAsync(int versionId);
        Task<List<CourseVersion>> GetVersionsAsync(int courseId);
        Task<int> NextVersionNumberAsync(int courseId);
        Task<CourseVersion?> GetPublishedVersionAsync(int courseId);
        Task<CourseVersion> AddVersionAsync(CourseVersion version);
        Task UpdateVersionAsync(CourseVersion version);
        Task SaveAsync();

        /// <summary>Deep-copies the content tree (nodes + blocks + resources + decks/cards) into a target version.</summary>
        Task CloneContentTreeAsync(int sourceVersionId, int targetVersionId, int userId);
    }
}
