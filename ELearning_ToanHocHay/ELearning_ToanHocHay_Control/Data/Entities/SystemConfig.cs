using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("SystemConfig")]
    public class SystemConfig
    {
        [Key]
        public int ConfigId { get; set; }

        [Required, MaxLength(100)]
        public string ConfigKey { get; set; }

        public string? ConfigValue { get; set; }

        public string? Description { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public int? UpdatedBy { get; set; }

        // Navigation
        public User? UpdatedByUser { get; set; }
    }
}
