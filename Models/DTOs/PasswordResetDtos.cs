using System.ComponentModel.DataAnnotations;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class ForgotPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";
    }

    public class ResetPasswordDto
    {
        [Required]
        public string Token { get; set; } = "";

        [Required, MinLength(6)]
        public string NewPassword { get; set; } = "";
    }

    public class ResendConfirmationDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";
    }

    public class LogoutDto
    {
        /// <summary>Optional — the specific refresh token to revoke. Omit to revoke all.</summary>
        public string? RefreshToken { get; set; }
    }
}
