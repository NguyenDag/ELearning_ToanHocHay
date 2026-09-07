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

    public class ResetPasswordByAdminDto
    {
        [Required, MinLength(6), MaxLength(100)]
        public string NewPassword { get; set; } = "";
    }

    /// <summary>Bộ lọc danh sách người dùng cho khu quản trị (bind từ query string).</summary>
    public class UserListFilter
    {
        public UserType? UserType { get; set; }
        public bool? IsActive { get; set; }
        public bool? Locked { get; set; }
        public bool? EmailConfirmed { get; set; }
    }

    public class SetConfigDto
    {
        public string? Value { get; set; }
    }

    public class AuditLogDto
    {
        public long LogId { get; set; }
        public int? UserId { get; set; }
        public string? ActorName { get; set; }
        public string? ActorEmail { get; set; }
        public string Action { get; set; } = "";
        public string EntityType { get; set; } = "";
        public int? EntityId { get; set; }
        public string? OldValueJson { get; set; }
        public string? NewValueJson { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuditLogFacetsDto
    {
        public List<string> EntityTypes { get; set; } = new();
        public List<string> Actions { get; set; } = new();
    }

    /// <summary>Bộ lọc nhật ký cho khu quản trị (bind từ query string).</summary>
    public class AuditLogFilter
    {
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public int? UserId { get; set; }
        public string? Action { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public string? Q { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
