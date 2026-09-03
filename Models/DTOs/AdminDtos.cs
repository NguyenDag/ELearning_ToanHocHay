using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class LockUserDto
    {
        [Required, MaxLength(500)]
        public string Reason { get; set; } = "";
    }

    public class ChangeRoleDto
    {
        [Required]
        public UserType NewRole { get; set; }
    }

    public class AuditLogDto
    {
        public long LogId { get; set; }
        public int? UserId { get; set; }
        public string Action { get; set; } = "";
        public string EntityType { get; set; } = "";
        public int? EntityId { get; set; }
        public string? OldValueJson { get; set; }
        public string? NewValueJson { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
