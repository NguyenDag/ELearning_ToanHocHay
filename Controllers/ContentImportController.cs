using System.Text;
using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Content;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    /// <summary>
    /// A3/P2 — import khung chương trình từ bộ file CSV (course / nodes / blocks / flashcards / resources).
    /// Vai trò quản lý nội dung: ContentEditor, AcademicReviewer, SystemAdmin. Ghi vào CourseVersion ở Draft.
    /// </summary>
    [Route("api/content/import")]
    [ApiController]
    [AuthorizeContentRole]
    [RequestSizeLimit(30_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 30_000_000)]
    public class ContentImportController : ControllerBase
    {
        private const long MaxFileBytes = 10_000_000;

        private readonly IContentImportService _import;

        public ContentImportController(IContentImportService import)
        {
            _import = import;
        }

        private int Uid => User.GetUserId()!.Value;

        /// <summary>Kiểm tra bộ file, không ghi gì. Truyền <c>versionId</c> để kiểm tra theo ngữ cảnh version.</summary>
        [HttpPost("validate")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Validate([FromForm] ContentImportFormDto form, [FromQuery] int? versionId)
        {
            var (sources, error) = await ReadSourcesAsync(form, requireCourse: false);
            if (error != null) return error.ToActionResult();

            var r = await _import.ValidateAsync(sources, versionId, Uid);
            return r.ToActionResult();
        }

        /// <summary>Import cây nội dung vào một CourseVersion đang ở Draft.</summary>
        [HttpPost("versions/{versionId:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportIntoVersion(
            int versionId,
            [FromForm] ContentImportFormDto form,
            [FromQuery] bool replace = false,
            [FromQuery] bool dryRun = false)
        {
            var (sources, error) = await ReadSourcesAsync(form, requireCourse: false);
            if (error != null) return error.ToActionResult();

            var r = await _import.ImportIntoVersionAsync(sources, versionId, replace, dryRun, Uid);
            return r.ToActionResult();
        }

        /// <summary>Tạo Course + CourseVersion (Draft) mới từ course.csv rồi import cây nội dung.</summary>
        [HttpPost("course")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportAsNewCourse([FromForm] ContentImportFormDto form, [FromQuery] bool dryRun = false)
        {
            var (sources, error) = await ReadSourcesAsync(form, requireCourse: true);
            if (error != null) return error.ToActionResult();

            var r = await _import.ImportAsNewCourseAsync(sources, dryRun, Uid);
            return r.ToActionResult();
        }

        /// <summary>
        /// Import ngân hàng câu hỏi + bài tập / đề độc lập (không kèm khung chương trình).
        /// <c>bankId</c> → thêm câu hỏi vào ngân hàng đó; ngược lại cần <c>subjectId</c> + <c>gradeLevelId</c> để tạo ngân hàng mới.
        /// </summary>
        [HttpPost("question-bank")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportQuestionBank(
            [FromForm] ContentImportFormDto form,
            [FromQuery] int? bankId,
            [FromQuery] int? subjectId,
            [FromQuery] int? gradeLevelId,
            [FromQuery] bool dryRun = false)
        {
            foreach (var f in new[] { form.QuestionBank, form.Questions, form.QuestionOptions, form.Exercises, form.ExerciseQuestions })
            {
                if (f != null && f.Length > MaxFileBytes)
                    return ApiResponse<ContentImportResultDto>.ErrorResponse(
                        $"File '{f.FileName}' vượt quá {MaxFileBytes / 1_000_000} MB.").ToActionResult();
            }

            var sources = new ContentImportSources(
                null, null, null, null, null,
                await ReadAsync(form.QuestionBank),
                await ReadAsync(form.Questions),
                await ReadAsync(form.QuestionOptions),
                await ReadAsync(form.Exercises),
                await ReadAsync(form.ExerciseQuestions));

            var r = await _import.ImportAssessmentAsync(sources, bankId, subjectId, gradeLevelId, dryRun, Uid);
            return r.ToActionResult();
        }

        /// <summary>Lịch sử các lần import gần đây.</summary>
        [HttpGet("jobs")]
        public async Task<IActionResult> GetJobs([FromQuery] int take = 20)
            => (await _import.GetJobsAsync(take)).ToActionResult();

        /// <summary>Chi tiết một lần import kèm danh sách issue đã lưu.</summary>
        [HttpGet("jobs/{id:int}")]
        public async Task<IActionResult> GetJob(int id)
            => (await _import.GetJobAsync(id)).ToActionResult();

        // ----------------------------------------------------------------
        private async Task<(ContentImportSources Sources, ApiResponse<ContentImportResultDto>? Error)> ReadSourcesAsync(
            ContentImportFormDto form, bool requireCourse)
        {
            if (form.Nodes == null)
                return (default!, ApiResponse<ContentImportResultDto>.ErrorResponse("Thiếu file nodes.csv (bắt buộc)."));
            if (requireCourse && form.Course == null)
                return (default!, ApiResponse<ContentImportResultDto>.ErrorResponse("Thiếu file course.csv (bắt buộc khi tạo khoá học mới)."));

            var all = new[]
            {
                form.Course, form.Nodes, form.Blocks, form.Flashcards, form.Resources,
                form.QuestionBank, form.Questions, form.QuestionOptions, form.Exercises, form.ExerciseQuestions
            };
            foreach (var f in all)
            {
                if (f != null && f.Length > MaxFileBytes)
                    return (default!, ApiResponse<ContentImportResultDto>.ErrorResponse(
                        $"File '{f.FileName}' vượt quá {MaxFileBytes / 1_000_000} MB."));
            }

            var sources = new ContentImportSources(
                await ReadAsync(form.Course),
                await ReadAsync(form.Nodes),
                await ReadAsync(form.Blocks),
                await ReadAsync(form.Flashcards),
                await ReadAsync(form.Resources),
                await ReadAsync(form.QuestionBank),
                await ReadAsync(form.Questions),
                await ReadAsync(form.QuestionOptions),
                await ReadAsync(form.Exercises),
                await ReadAsync(form.ExerciseQuestions));

            return (sources, null);
        }

        private static async Task<string?> ReadAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            await using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return await reader.ReadToEndAsync();
        }
    }
}
