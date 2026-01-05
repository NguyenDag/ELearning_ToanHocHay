using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("QuestionOption")]
    public class QuestionOption
    {
        [Key]
        public int OptionId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        public required string OptionText { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsCorrect { get; set; } = false;

        public int OrderIndex { get; set; }

        // Navigation
        public Question Question { get; set; }
        public StudentAnswer StudentAnswer { get; set; }
    }
}