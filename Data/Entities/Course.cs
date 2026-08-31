using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // ============================================================
    // TẦNG 2 — KHOÁ HỌC (Course · CourseVersion) + Bundle liên môn
    // Course = đơn vị học sinh ghi danh / duyệt trong catalog.
    // Unique theo (Môn × Lớp × Bộ sách).
    // ============================================================

    public enum CourseStatus
    {
        Draft,
        Published,
        Archived
    }

    public enum VersionState
    {
        Draft,
        InReview,
        Approved,
        Published,
        Archived,
        Cloning
    }

    [Table("Course")]
    public class Course
    {
        [Key]
        public int CourseId { get; set; }

        public int SubjectId { get; set; }
        public int GradeLevelId { get; set; }
        public int? FrameworkId { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; }

        [Required, MaxLength(255)]
        public string Slug { get; set; }              // unique

        public string? Description { get; set; }

        [MaxLength(500)]
        public string? ThumbnailUrl { get; set; }

        public CourseStatus Status { get; set; } = CourseStatus.Draft;

        // Bán lẻ theo khoá (§5.10)
        [Column(TypeName = "decimal(18,2)")]
        public decimal ListPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalePrice { get; set; }

        public bool IsPurchasable { get; set; } = true;

        public int? AccessDurationDays { get; set; }  // null = trọn đời

        public int DisplayOrder { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public Subject? Subject { get; set; }
        public GradeLevel? GradeLevel { get; set; }
        public CurriculumFramework? Framework { get; set; }
        public User? Creator { get; set; }

        public ICollection<CourseVersion> Versions { get; set; }
        public ICollection<StudentCourse> StudentCourses { get; set; }
        public ICollection<PackageEntitlement> PackageEntitlements { get; set; }
        public ICollection<CourseBundleItem> BundleItems { get; set; }
    }

    [Table("CourseVersion")]
    public class CourseVersion
    {
        [Key]
        public int CourseVersionId { get; set; }

        public int CourseId { get; set; }

        public int VersionNumber { get; set; }

        [MaxLength(150)]
        public string? Label { get; set; }            // "Năm học 2026–2027"

        public VersionState State { get; set; } = VersionState.Draft;

        public int? SubmittedBy { get; set; }
        public DateTime? SubmittedAt { get; set; }

        public DateTime? PublishedAt { get; set; }
        public int? PublishedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Course? Course { get; set; }
        public User? Submitter { get; set; }
        public User? Publisher { get; set; }

        public ICollection<ContentNode> Nodes { get; set; }
        public ICollection<ContentReview> Reviews { get; set; }
        public ICollection<StudentCourse> StudentCourses { get; set; }
    }

    // ---- Bundle liên môn (§11 — sản phẩm ôn thi) ----
    [Table("CourseBundle")]
    public class CourseBundle
    {
        [Key]
        public int CourseBundleId { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; }

        [Required, MaxLength(255)]
        public string Slug { get; set; }

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ListPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalePrice { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<CourseBundleItem> Items { get; set; }
    }

    [Table("CourseBundleItem")]
    public class CourseBundleItem
    {
        public int CourseBundleId { get; set; }
        public int CourseId { get; set; }
        public int OrderIndex { get; set; }

        // Navigation
        public CourseBundle? Bundle { get; set; }
        public Course? Course { get; set; }
    }
}
