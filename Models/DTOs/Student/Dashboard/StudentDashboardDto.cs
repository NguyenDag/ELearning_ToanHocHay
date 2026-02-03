namespace ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard 
{
    public class StudentDashboardDto
    {
        public double AverageScore { get; set; }
        public int TotalAttempts { get; set; }
        public int CompletedChapters { get; set; }
        public List<ScoreChartItemDto> ChartData { get; set; } = new();
        public List<ChapterProgressDto> Chapters { get; set; } = new();
        public List<ExerciseAttemptDto> RecentAttempts { get; set; } = new();
    }
}