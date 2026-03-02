using System.ComponentModel.DataAnnotations;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Parent
{
    public class CreateParentDto
    {
        [Required]
        public int UserId { get; set; }

        [MaxLength(50)]
        public string? Job { get; set; }
    }
}
