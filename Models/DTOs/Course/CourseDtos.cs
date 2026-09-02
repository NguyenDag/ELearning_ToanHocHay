using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Course
{
    public class CourseDto
    {
        public int CourseId { get; set; }
        public int SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public int GradeLevelId { get; set; }
        public string? GradeLevelName { get; set; }
        public int? FrameworkId { get; set; }
        public string? FrameworkName { get; set; }
        public string Title { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public CourseStatus Status { get; set; }
        public decimal ListPrice { get; set; }
        public decimal? SalePrice { get; set; }
        public bool IsPurchasable { get; set; }
        public int? AccessDurationDays { get; set; }
        public int DisplayOrder { get; set; }
        public int? PublishedVersionId { get; set; }
        public int? PublishedVersionNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CourseVersionDto>? Versions { get; set; }
    }

    public class CourseRequestDto
    {
        [Required] public int SubjectId { get; set; }
        [Required] public int GradeLevelId { get; set; }
        public int? FrameworkId { get; set; }
        [Required, MaxLength(255)] public string Title { get; set; } = "";
        [Required, MaxLength(255)] public string Slug { get; set; } = "";
        public string? Description { get; set; }
        [MaxLength(500)] public string? ThumbnailUrl { get; set; }
        [Range(0, double.MaxValue)] public decimal ListPrice { get; set; }
        [Range(0, double.MaxValue)] public decimal? SalePrice { get; set; }
        public bool IsPurchasable { get; set; } = true;
        public int? AccessDurationDays { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class CourseVersionDto
    {
        public int CourseVersionId { get; set; }
        public int CourseId { get; set; }
        public int VersionNumber { get; set; }
        public string? Label { get; set; }
        public VersionState State { get; set; }
        public int? SubmittedBy { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? PublishedBy { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCourseVersionDto
    {
        [MaxLength(150)] public string? Label { get; set; }

        /// <summary>Optional — clone the content tree of an existing version of the same course.</summary>
        public int? CloneFromVersionId { get; set; }
    }

    public class ReviewCourseVersionDto
    {
        [Required] public ReviewDecision Decision { get; set; }
        public string? Summary { get; set; }
    }
}
