using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("AuditLog")]
    public class AuditLog
    {
        [Key]
        public int LogId { get; set; }
        public int? UserId { get; set; }

        [Required, MaxLength(100)]
        public required string Action { get; set; }

        [Required, MaxLength(100)]
        public required string EntityType { get; set; }

        public int? EntityId { get; set; }

        public string? OldValueJson { get; set; }
        public string? NewValueJson { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public User? User { get; set; }
    }
}
