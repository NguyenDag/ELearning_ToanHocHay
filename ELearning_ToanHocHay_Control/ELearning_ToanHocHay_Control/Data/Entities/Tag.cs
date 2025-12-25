using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum TagType
    {
        Difficulty,
        Skill,
        Knowledge,
        Topic
    }

    [Table("Tag")]
    public class Tag
    {
        [Key]
        public int TagId { get; set; }

        [Required, MaxLength(100)]
        public string TagName { get; set; }

        public TagType TagType { get; set; }

        // Navigation
        public ICollection<QuestionTag> QuestionTags { get; set; }
    }
}
