namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class PackageDto
    {
        public int PackageId { get; set; }
        public string PackageName { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }

        public bool UnlimitedAiHint { get; set; }
        public bool PersonalizedPath { get; set; }
        public bool MistakeRetry { get; set; }
        public bool SmartReminder { get; set; }
        public bool PrioritySupport { get; set; }

        public bool IsActive { get; set; }
    }
}
