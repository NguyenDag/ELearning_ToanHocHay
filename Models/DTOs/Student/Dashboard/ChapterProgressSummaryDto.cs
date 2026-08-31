using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard
{
    /// <summary>
    /// Tóm tắt tiến độ theo "chương" (ContentNode kiểu Chapter) trong các khoá học sinh đã ghi danh.
    /// </summary>
    public class ChapterProgressSummaryDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = "";
        public int OrderIndex { get; set; }
        public decimal CompletionPercentage { get; set; }
        public int CompletedTopics { get; set; }
        public int TotalTopics { get; set; }
        public bool IsLocked { get; set; }
        public MasteryLevel? CurrentMastery { get; set; }
    }
}
