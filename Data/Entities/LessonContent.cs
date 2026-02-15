using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mime;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum LessonBlockType
    {
        Heading = 0,        // Tiêu đề
        Text = 1,           // Đoạn văn thường
        Definition = 2,     // Khung định nghĩa (màu xanh)
        Example = 3,        // Ví dụ (màu xanh lá)
        Note = 4,           // Ghi chú (màu vàng)
        Formula = 5,        // Công thức
        Image = 6,
        Video = 7,
        Pdf = 8,
        Audio = 9
    }

    [Table("LessonContent")]
    public class LessonContent
    {
        [Key]
        public int ContentId { get; set; }

        public int LessonId { get; set; }

        public LessonBlockType BlockType { get; set; }

        // Nội dung text (Markdown / LaTeX)
        public string? ContentText { get; set; }

        // Cho video, image, pdf
        [MaxLength(500)]
        public string? ContentUrl { get; set; }

        public int OrderIndex { get; set; }

        // Navigation
        public Lesson? Lesson { get; set; }
    }
}
