using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    /// <summary>
    /// P6 — per-student, per-day AI usage counters. Backs the package hint quota
    /// (<see cref="Package.AiHintLimitDaily"/> / <see cref="Package.UnlimitedAiHint"/>).
    /// </summary>
    [Table("AiUsageDaily")]
    public class AiUsageDaily
    {
        public int StudentId { get; set; }
        public DateOnly Date { get; set; }

        public int HintCount { get; set; }
        public int FeedbackCount { get; set; }
        public int ChatCount { get; set; }

        // Navigation
        public Student? Student { get; set; }
    }
}
