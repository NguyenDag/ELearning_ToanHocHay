namespace ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard
{
    public class OverviewStatsDto
    {
        // Thống kê tuần này
        public int WeeklyStudyMinutes { get; set; }
        public int WeeklyExercisesCompleted { get; set; }

        // Thống kê tổng thể
        public decimal AverageScore { get; set; }
        public int TotalExercisesCompleted { get; set; }
        public int TotalLessonsCompleted { get; set; }

        // So sánh với tuần trước
        public ComparisonDto WeekComparison { get; set; }

        // Streak
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public bool StudiedToday { get; set; }
    }

    public enum TrendDirection
    {
        Up,      // ↑
        Down,    // ↓
        Same     // →
    }

    public class ComparisonDto
    {
        public int ScoreChange { get; set; }           // +5 hoặc -3 (điểm)
        public int StudyTimeChange { get; set; }       // +20 hoặc -15 (phút)
        public int ExerciseCountChange { get; set; }   // +2 hoặc -1 (bài)
        public TrendDirection Direction { get; set; }  // Up, Down, Same
    }
}
