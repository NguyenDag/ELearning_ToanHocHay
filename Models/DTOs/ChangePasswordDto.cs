using System.ComponentModel.DataAnnotations;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required, MinLength(6)]
        public string NewPassword { get; set; }
    }
}
