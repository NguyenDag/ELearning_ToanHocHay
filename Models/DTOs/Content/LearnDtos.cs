using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Content
{
    public class EnrolmentDto
    {
        public int StudentCourseId { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = "";
        public string? SubjectName { get; set; }
        public string? GradeLevelName { get; set; }
        public int CourseVersionId { get; set; }
        public EnrollSource Source { get; set; }
        public StudentCourseStatus Status { get; set; }
        public decimal ProgressPercent { get; set; }
        public DateTime EnrolledAt { get; set; }
        public DateTime? AccessExpiresAt { get; set; }
    }

    public class CourseContentDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? Description { get; set; }
        public int CourseVersionId { get; set; }
        public int VersionNumber { get; set; }

        /// <summary>Full / FreeOnly — tells the client whether the tree is the complete one.</summary>
        public string AccessLevel { get; set; } = "";
        public bool IsEntitled { get; set; }
        public List<ContentNodeDto> Tree { get; set; } = new();
    }
}
