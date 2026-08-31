using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // ============================================================
    // NỘI DUNG BÀI HỌC — ContentBlock (đổi tên từ LessonContent)
    // + LessonResource · FlashcardDeck/Flashcard · MediaAsset · ContentImportJob
    // ============================================================

    public enum LessonBlockType
    {
        Heading = 0,
        Text = 1,
        Definition = 2,
        Example = 3,
        Note = 4,
        Formula = 5,
        Image = 6,
        Video = 7,
        Animation = 8,
        Embed = 9,
        Audio = 10,
        Pdf = 11
    }

    [Table("ContentBlock")]
    public class ContentBlock
    {
        [Key]
        public int BlockId { get; set; }

        public int NodeId { get; set; }

        public LessonBlockType BlockType { get; set; }

        public string? ContentText { get; set; }      // Markdown / LaTeX

        [MaxLength(500)]
        public string? ContentUrl { get; set; }

        public string? MetadataJson { get; set; }

        public int OrderIndex { get; set; }

        // Navigation
        public ContentNode? Node { get; set; }
    }

    public enum ResourceType
    {
        Pdf,
        Slide,
        Doc,
        Sheet,
        ExternalLink
    }

    [Table("LessonResource")]
    public class LessonResource
    {
        [Key]
        public int ResourceId { get; set; }

        public int NodeId { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; }

        public ResourceType ResourceType { get; set; }

        public int? MediaAssetId { get; set; }

        [MaxLength(1000)]
        public string? ExternalUrl { get; set; }

        public bool IsDownloadable { get; set; } = true;

        public int OrderIndex { get; set; }

        // Navigation
        public ContentNode? Node { get; set; }
        public MediaAsset? MediaAsset { get; set; }
    }

    [Table("FlashcardDeck")]
    public class FlashcardDeck
    {
        [Key]
        public int DeckId { get; set; }

        public int NodeId { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ContentNode? Node { get; set; }
        public ICollection<Flashcard> Cards { get; set; }
    }

    [Table("Flashcard")]
    public class Flashcard
    {
        [Key]
        public int CardId { get; set; }

        public int DeckId { get; set; }

        [Required]
        public string FrontText { get; set; }

        [Required]
        public string BackText { get; set; }

        [MaxLength(500)]
        public string? FrontImageUrl { get; set; }

        [MaxLength(500)]
        public string? BackImageUrl { get; set; }

        public string? Hint { get; set; }

        public int OrderIndex { get; set; }

        // Navigation
        public FlashcardDeck? Deck { get; set; }
    }

    // Thư viện file dùng chung — mọi ContentUrl / FileUrl nên trỏ qua đây.
    [Table("MediaAsset")]
    public class MediaAsset
    {
        [Key]
        public int MediaAssetId { get; set; }

        [Required, MaxLength(500)]
        public string StorageKey { get; set; }

        [Required, MaxLength(1000)]
        public string Url { get; set; }

        [Required, MaxLength(120)]
        public string MimeType { get; set; }

        public long SizeBytes { get; set; }

        [Required, MaxLength(400)]
        public string OriginalFileName { get; set; }

        public int UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User? Uploader { get; set; }
    }

    public enum ImportTargetType
    {
        ContentNode,
        Question,
        Exercise
    }

    public enum ImportJobStatus
    {
        Pending,
        Processing,
        Completed,
        CompletedWithErrors,
        Failed
    }

    [Table("ContentImportJob")]
    public class ContentImportJob
    {
        [Key]
        public int ImportJobId { get; set; }

        public int UploadedBy { get; set; }

        [Required, MaxLength(1000)]
        public string FileUrl { get; set; }

        public ImportTargetType TargetType { get; set; }

        public int? CourseVersionId { get; set; }

        public ImportJobStatus Status { get; set; } = ImportJobStatus.Pending;

        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }

        public string? ErrorReport { get; set; }      // JSON

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Navigation
        public User? Uploader { get; set; }
        public CourseVersion? CourseVersion { get; set; }
    }
}
