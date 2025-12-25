using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("Student")]
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Range(6, 9)]
        public int GradeLevel { get; set; }

        [MaxLength(100)]
        public string? SchoolName { get; set; }

        // Navigation
        public User? User { get; set; }

        public ICollection<StudentParent> StudentParents { get; set; }
        public ICollection<Payment> Payments { get; set; }
        public ICollection<Subscription> Subscriptions { get; set; }
        public ICollection<ExerciseAttempt> ExerciseAttempts { get; set; }
        public ICollection<LearningPath> LearningPaths { get; set; }
        public ICollection<StudentProgress> StudentProgresses { get; set; }
    }
}
