using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Content
{
    public class NodeProgressDto
    {
        public int NodeId { get; set; }
        public NodeType NodeType { get; set; }
        public string Title { get; set; } = "";
        public ProgressStatus Status { get; set; }
        public MasteryLevel MasteryLevel { get; set; }
        public decimal CompletionPercent { get; set; }
        public int TimeSpentSeconds { get; set; }
        public int TotalAttempts { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public DateTime LastAccessedAt { get; set; }
    }

    public class MarkLessonCompleteDto
    {
        public int SecondsViewed { get; set; }
    }

    public class DailyActivityDto
    {
        public DateOnly Date { get; set; }
        public int MinutesStudied { get; set; }
        public int ExercisesDone { get; set; }
        public int LessonsDone { get; set; }
        public int QuestionsAnswered { get; set; }
    }
}
