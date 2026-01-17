using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("QuestionTag")]
    public class QuestionTag
    {
        public int QuestionId { get; set; }
        public int TagId { get; set; }

        // Navigation
        public Question? Question { get; set; }
        public Tag? Tag { get; set; }
    }
}
