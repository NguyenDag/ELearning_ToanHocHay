using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("QuestionBank")]
    public class QuestionBank
    {
        [Key]
        public int BankId { get; set; }

        [Required, MaxLength(255)]
        public string BankName { get; set; }

        [Range(6, 9)]
        public int GradeLevel { get; set; }

        public int? ChapterId { get; set; }
        public int? TopicId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Chapter? Chapter { get; set; }
        public Topic? Topic { get; set; }
        public ICollection<Question> Questions { get; set; }
    }
}
