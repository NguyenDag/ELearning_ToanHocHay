using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mime;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum ContentType
    {
        Video,
        Pdf,
        Text,
        Image,
        Audio
    }

    [Table("LessonContent")]
    public class LessonContent
    {
        [Key]
        public int ContentId { get; set; }

        public int LessonId { get; set; }

        public ContentType ContentType { get; set; }

        [MaxLength(500)]
        public string? ContentUrl { get; set; }

        public string? ContentText { get; set; }

        public int OrderIndex { get; set; }

        // Navigation
        public Lesson? Lesson { get; set; }
    }
}
