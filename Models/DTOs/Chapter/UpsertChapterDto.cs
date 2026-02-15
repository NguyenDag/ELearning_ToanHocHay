namespace ELearning_ToanHocHay_Control.Models.DTOs.Chapter
{
    public class UpsertChapterDto
    {
        public int? ChapterId { get; set; }

        // Chỉ bắt buộc khi tạo mới
        public int? CurriculumId { get; set; }
        public string? ChapterName { get; set; }
        public string? Description { get; set; }
        public int OrderIndex { get; set; }
    }
}
