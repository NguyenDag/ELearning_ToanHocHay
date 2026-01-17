using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class RegisterRequestDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; }

        [Required, Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string FullName { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        public DateTime? Dob { get; set; }

        [Required]
        public UserType UserType { get; set; }

        // ===== Student =====
        [Range(6, 9)]
        public int? GradeLevel { get; set; }

        public string? SchoolName { get; set; }

        // ===== Parent =====
        [MaxLength(50)]
        public string? Job { get; set; }
    }
}
