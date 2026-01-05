using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("Topic")]
    public class Topic
    {
        [Key]
        public int TopicId { get; set; }

        public int ChapterId { get; set; }

        [Required, MaxLength(255)]
        public string TopicName { get; set; }

        public int OrderIndex { get; set; }

        public string? Description { get; set; }

        public bool IsFree { get; set; } = false;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public Chapter? Chapter { get; set; }
        public ICollection<Lesson> Lessons { get; set; }
        public ICollection<QuestionBank> QuestionBanks { get; set; }
        public ICollection<Exercise> Exercises { get; set; }
        public ICollection<StudentProgress> studentProgresses { get; set; }
    }
}
