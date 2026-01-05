using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("LearningPath")]
    public class LearningPath
    {
        [Key]
        public int PathId { get; set; }

        public int StudentId { get; set; }

        public string? RecommendedTopicsJson { get; set; }
        public string? WeakAreasJson { get; set; }
        public string? StrongAreasJson { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        public bool IsPersonalized { get; set; } = false;

        // Navigation
        public Student? Student { get; set; }
    }
}
