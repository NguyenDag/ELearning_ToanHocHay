using Microsoft.AspNetCore.Http;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Content
{
    // ============================================================
    // A3/P2 — Import khung chương trình từ file CSV
    // (ContentImportJob). Bộ file: course / nodes / blocks / flashcards / resources.
    // ============================================================

    public enum ImportIssueSeverity
    {
        /// <summary>Chặn import — phải sửa file.</summary>
        Error,

        /// <summary>Không chặn — nội dung vẫn vào được nhưng nên xem lại.</summary>
        Warning
    }

    /// <summary>Một phát hiện khi kiểm tra file import.</summary>
    public class ImportIssueDto
    {
        /// <summary>Tên file logic: <c>course</c> / <c>nodes</c> / <c>blocks</c> / <c>flashcards</c> / <c>resources</c>.</summary>
        public string File { get; set; } = "";

        /// <summary>Số dòng dữ liệu (1-based, không tính dòng tiêu đề). <c>null</c> = lỗi ở mức file.</summary>
        public int? Row { get; set; }

        /// <summary>Khoá node liên quan (nếu xác định được).</summary>
        public string? NodeKey { get; set; }

        /// <summary>Mã lỗi ổn định để client xử lí (vd <c>BAD_NODE_TYPE</c>).</summary>
        public string Code { get; set; } = "";

        public ImportIssueSeverity Severity { get; set; }

        public string Message { get; set; } = "";
    }

    /// <summary>Số lượng thực thể sẽ (hoặc đã) được tạo.</summary>
    public class ContentImportCountsDto
    {
        public int Chapters { get; set; }
        public int Lessons { get; set; }
        public int OtherNodes { get; set; }
        public int Blocks { get; set; }
        public int FlashcardDecks { get; set; }
        public int Flashcards { get; set; }
        public int Resources { get; set; }
    }

    /// <summary>Kết quả kiểm tra / thực hiện import.</summary>
    public class ContentImportResultDto
    {
        /// <summary>File hợp lệ để import (không có lỗi mức Error).</summary>
        public bool Valid { get; set; }

        /// <summary>Đã ghi vào CSDL. <c>false</c> khi <c>dryRun</c> hoặc khi có lỗi.</summary>
        public bool Committed { get; set; }

        public bool DryRun { get; set; }

        public int? ImportJobId { get; set; }
        public int? CourseId { get; set; }
        public int? CourseVersionId { get; set; }

        public ContentImportCountsDto Counts { get; set; } = new();

        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }

        public List<ImportIssueDto> Issues { get; set; } = new();
    }

    /// <summary>Các file CSV gửi lên qua multipart/form-data.</summary>
    public class ContentImportFormDto
    {
        /// <summary>course.csv — bắt buộc khi tạo khoá học mới, bỏ qua khi import vào version có sẵn.</summary>
        public IFormFile? Course { get; set; }

        /// <summary>nodes.csv — luôn bắt buộc.</summary>
        public IFormFile? Nodes { get; set; }

        public IFormFile? Blocks { get; set; }
        public IFormFile? Flashcards { get; set; }
        public IFormFile? Resources { get; set; }
    }

    /// <summary>Tóm tắt một lần chạy import (ContentImportJob).</summary>
    public class ContentImportJobDto
    {
        public int ImportJobId { get; set; }
        public int UploadedBy { get; set; }
        public string? UploadedByName { get; set; }
        public string FileUrl { get; set; } = "";
        public string TargetType { get; set; } = "";
        public int? CourseVersionId { get; set; }
        public string Status { get; set; } = "";
        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        /// <summary>Chỉ trả trong endpoint chi tiết — danh sách issue đã lưu.</summary>
        public List<ImportIssueDto>? Issues { get; set; }
    }
}
