using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    // Request DTOs
    public class StartExerciseDto
    {
        public int ExerciseId { get; set; }
        public int StudentId { get; set; }
    }

    public class StartRandomExerciseDto
    {
        public int StudentId { get; set; }
        public int BankId { get; set; }
        public ExerciseType ExerciseType { get; set; }
        public int NumberOfQuestions { get; set; } = 10;
        public double MaxScore { get; set; } = 10;
        public int? DurationMinutes { get; set; }
    }

    public class SubmitAnswerDto
    {
        public int AttemptId { get; set; }
        public int QuestionId { get; set; }
        public string? AnswerText { get; set; }
        public int? SelectedOptionId { get; set; }
    }

    public class CompleteExerciseDto
    {
        public int AttemptId { get; set; }
    }

    // Response DTOs
    public class ExerciseAttemptDto
    {
        public int AttemptId { get; set; }
        public int StudentId { get; set; }
        public int? ExerciseId { get; set; }
        public string ExerciseName { get; set; }
        public ExerciseType ExerciseType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public List<QuestionInAttemptDto> Questions { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class QuestionInAttemptDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public double Score { get; set; }
        public List<AnswerOptionDto> Options { get; set; }
        public string ImageUrl { get; set; }
    }

    public class AnswerOptionDto
    {
        public int OptionId { get; set; }
        public string OptionText { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class ExerciseResultDto
    {
        public int AttemptId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string ExerciseName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public double TotalScore { get; set; }
        public double MaxScore { get; set; }
        public decimal CompletionPercentage { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsPassed { get; set; }
        public List<AnswerDetailDto> AnswerDetails { get; set; }
    }

    public class AnswerDetailDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string StudentAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public double PointsEarned { get; set; }
        public double MaxScores { get; set; }
        public string? Explanation { get; set; }
    }
}
