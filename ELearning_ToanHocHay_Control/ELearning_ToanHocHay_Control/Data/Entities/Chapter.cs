using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("Chapter")]
    public class Chapter
    {
        [Key]
        public int ChapterId { get; set; }

        public int CurriculumId { get; set; }

        [Required, MaxLength(255)]
        public required string ChapterName { get; set; }

        public int OrderIndex { get; set; }

        public string? Description { get; set; }

        // Navigation
        public Curriculum? Curriculum { get; set; }
        public ICollection<Topic> Topics { get; set; }
        public ICollection<QuestionBank> QuestionBanks { get; set; }
        public ICollection<Exercise> Exercises { get; set; }
    }
}
