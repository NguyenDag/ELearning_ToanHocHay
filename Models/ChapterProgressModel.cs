using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models
{
    public class ChapterProgressModel
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; }
        public int OrderIndex { get; set; }
        public decimal CompletionPercentage { get; set; }
        public int CompletedTopics { get; set; }
        public int TotalTopics { get; set; }
        public bool IsLocked { get; set; }
        public MasteryLevel? AverageMastery { get; set; }
    }
}
