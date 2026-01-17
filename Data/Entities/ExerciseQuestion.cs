using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("ExerciseQuestion")]
    public class ExerciseQuestion
    {
        public int ExerciseId { get; set; }
        public int QuestionId { get; set; }
        public double Score { get; set; }
        public int OrderIndex { get; set; }

        // Navigation
        public Exercise? Exercise { get; set; }
        public Question? Question { get; set; }
    }
}
