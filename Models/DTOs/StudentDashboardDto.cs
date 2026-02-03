namespace ELearning_ToanHocHay_Control.Models.DTOs // Lưu ý: Ở WebApp thì đổi namespace tương ứng
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

    public class ScoreChartItemDto
    {
        public string ChapterName { get; set; } = "";
        public double AvgScore { get; set; }
    }

    public class ChapterProgressDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = "";
        public int TotalLessons { get; set; }
        public int ProgressPercentage { get; set; }
    }
}