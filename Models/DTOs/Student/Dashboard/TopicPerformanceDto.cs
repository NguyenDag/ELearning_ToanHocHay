namespace ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard
{
    public class TopicPerformanceDto
    {
        public string TopicName { get; set; }
        public string ChapterName { get; set; }
        public decimal AverageScore { get; set; } // Thang 10
        public int TotalAttempts { get; set; }
        public bool IsStrength => AverageScore >= 8.0m;
        public bool IsWeakness => AverageScore < 5.0m;

        /// <summary>
        /// Chênh lệch điểm so với cùng window kỳ trước (null = chưa đủ dữ liệu kỳ trước).
        /// Dương = tiến bộ, âm = giảm sút.
        /// </summary>
        public decimal? ScoreDelta { get; set; }

        /// <summary>"up" | "down" | "stable" | null</summary>
        public string? TrendDirection { get; set; }
    }
}
