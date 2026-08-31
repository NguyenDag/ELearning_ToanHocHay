using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // Mức độ chia sẻ dữ liệu học tập cho AI chat (§11).
    public enum AiDataSharingLevel
    {
        SummaryOnly,
        Detailed,
        None
    }

    [Table("Student")]
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        [Required]
        public int UserId { get; set; }

        // "Lớp mặc định" cho UX; quyền truy cập thật đi qua enrollment / entitlement (§5.8).
        public int? CurrentGradeLevelId { get; set; }

        [MaxLength(100)]
        public string? SchoolName { get; set; }

        public AiDataSharingLevel AiDataSharingLevel { get; set; } = AiDataSharingLevel.SummaryOnly;

        // Navigation
        public User? User { get; set; }
        public GradeLevel? CurrentGradeLevel { get; set; }

        public ICollection<ParentLink> ParentLinks { get; set; }
        public ICollection<Subscription> Subscriptions { get; set; }
        public ICollection<StudentCourse> StudentCourses { get; set; }
        public ICollection<ExerciseAttempt> ExerciseAttempts { get; set; }
        public ICollection<LearningPath> LearningPaths { get; set; }
        public ICollection<NodeProgress> NodeProgresses { get; set; }
        public ICollection<SkillProgress> SkillProgresses { get; set; }
        public ICollection<DailyActivitySnapshot> DailyActivitySnapshots { get; set; }
        public ICollection<OrderItem> BenefitedOrderItems { get; set; }
        public ICollection<SubscriptionMember> SubscriptionMemberships { get; set; }
    }
}
