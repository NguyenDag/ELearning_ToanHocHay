using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Content;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    /// <summary>Nội dung các file CSV import (đã đọc thành chuỗi ở tầng controller).</summary>
    public sealed record ContentImportSources(
        string? Course,
        string? Nodes,
        string? Blocks,
        string? Flashcards,
        string? Resources,
        string? QuestionBank = null,
        string? Questions = null,
        string? QuestionOptions = null,
        string? Exercises = null,
        string? ExerciseQuestions = null)
    {
        /// <summary>Có file phần đánh giá (ngân hàng câu hỏi / bài tập) đi kèm.</summary>
        public bool HasAssessment =>
            !string.IsNullOrWhiteSpace(Questions) || !string.IsNullOrWhiteSpace(Exercises);
    }

    /// <summary>
    /// A3/P2 — import khung chương trình từ bộ file CSV (ContentImportJob).
    /// Vai trò quản lý nội dung + SystemAdmin. Mọi thao tác ghi yêu cầu CourseVersion ở trạng thái Draft.
    /// </summary>
    public interface IContentImportService
    {
        /// <summary>Chỉ kiểm tra file, không ghi gì. <paramref name="courseVersionId"/> có thì kiểm tra theo ngữ cảnh version đó.</summary>
        Task<ApiResponse<ContentImportResultDto>> ValidateAsync(ContentImportSources sources, int? courseVersionId, int userId);

        /// <summary>Import cây nội dung vào một CourseVersion đang ở Draft.</summary>
        Task<ApiResponse<ContentImportResultDto>> ImportIntoVersionAsync(
            ContentImportSources sources, int courseVersionId, bool replaceExisting, bool dryRun, int userId);

        /// <summary>Tạo Course + CourseVersion (Draft) mới từ course.csv rồi import cây nội dung.</summary>
        Task<ApiResponse<ContentImportResultDto>> ImportAsNewCourseAsync(ContentImportSources sources, bool dryRun, int userId);

        /// <summary>
        /// Import phần đánh giá độc lập (không kèm khung chương trình): ngân hàng câu hỏi + bài tập / đề.
        /// <paramref name="bankId"/> có → thêm câu hỏi vào ngân hàng đó; ngược lại tạo ngân hàng mới cần
        /// <paramref name="subjectId"/> + <paramref name="gradeLevelId"/>. Bài tập tạo độc lập (không gắn bài học).
        /// </summary>
        Task<ApiResponse<ContentImportResultDto>> ImportAssessmentAsync(
            ContentImportSources sources, int? bankId, int? subjectId, int? gradeLevelId, bool dryRun, int userId);

        Task<ApiResponse<List<ContentImportJobDto>>> GetJobsAsync(int take);
        Task<ApiResponse<ContentImportJobDto>> GetJobAsync(int jobId);
    }
}
