using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mime;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum LessonBlockType
    {
        Heading,        // Tiêu đề
        Text,           // Đoạn văn thường
        Definition,     // Khung định nghĩa (màu xanh)
        Example,        // Ví dụ (màu xanh lá)
        Note,           // Ghi chú (màu vàng)
        Formula,        // Công thức
        Image,
        Video,
        Pdf,
        Audio
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
