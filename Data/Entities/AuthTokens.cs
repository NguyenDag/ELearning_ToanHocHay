using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // ============================================================
    // P1 — Xác thực: refresh token (xoay vòng + thu hồi) và reset mật khẩu
    // ============================================================

    [Table("RefreshToken")]
    public class RefreshToken
    {
        [Key]
        public long RefreshTokenId { get; set; }

        [Required]
        public int UserId { get; set; }

        /// <summary>SHA-256 of the raw token — the raw value is only ever sent to the client.</summary>
        [Required, MaxLength(88)]
        public string TokenHash { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        [MaxLength(88)]
        public string? ReplacedByTokenHash { get; set; }

        [MaxLength(50)]
        public string? CreatedByIp { get; set; }

        // Navigation
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [NotMapped]
        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }

    [Table("PasswordResetToken")]
    public class PasswordResetToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string Token { get; set; } = "";

        public DateTime ExpiredAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
