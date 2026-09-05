namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>
    /// Công thức roll-up tiến độ thuần: ngưỡng đánh dấu bài hoàn thành và % hoàn thành của
    /// một node cha từ số bài con. Tách khỏi <see cref="Implementations.ProgressProjectionService"/> để test không cần DB.
    /// </summary>
    public static class ProgressRollup
    {
        /// <summary>Điểm % của attempt gắn bài học ≥ mốc này thì bài coi như hoàn thành.</summary>
        public const decimal LessonCompleteScorePct = 70m;

        /// <summary>Thời lượng xem tối thiểu (giây) trước khi được đánh dấu đã đọc bài.</summary>
        public const int MinViewSeconds = 20;

        public static bool IsLessonComplete(decimal attemptScorePct) => attemptScorePct >= LessonCompleteScorePct;

        /// <summary>% bài con hoàn thành, làm tròn 2 chữ số. Không có bài con ⇒ 0.</summary>
        public static decimal PercentComplete(int completedChildren, int totalChildren)
            => totalChildren <= 0
                ? 0m
                : Math.Round((decimal)completedChildren / totalChildren * 100m, 2);
    }
}
