namespace ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard
{
    public class ChapterProgressDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = "";
        public int TotalLessons { get; set; }
        public int ProgressPercentage { get; set; }
    }
}
