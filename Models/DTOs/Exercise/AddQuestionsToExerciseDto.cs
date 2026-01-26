namespace ELearning_ToanHocHay_Control.Models.DTOs.Exercise
{
    public class AddQuestionsToExerciseDto
    {
        public List<int> QuestionIds { get; set; } = new();
        public double? ScorePerQuestion { get; set; }
    }
}
