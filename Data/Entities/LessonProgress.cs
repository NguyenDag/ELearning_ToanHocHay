using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Index(nameof(StudentId), nameof(LessonId), IsUnique = true)]
    [Table("LessonProgress")]
    public class LessonProgress
    {
        [Key]
        public int LessonProgressId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int LessonId { get; set; }

        public bool IsCompleted { get; set; } = false;

        public int WatchTime { get; set; } = 0;

        public DateTime LastAccessed { get; set; } = DateTime.UtcNow;

        // Navigation
        public Student? Student { get; set; }
        public Lesson? Lesson { get; set; }
    }
}
