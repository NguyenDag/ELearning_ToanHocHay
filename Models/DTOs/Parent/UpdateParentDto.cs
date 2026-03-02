using System.ComponentModel.DataAnnotations;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Parent
{
    public class UpdateParentDto
    {
        [MaxLength(50)]
        public string? Job { get; set; }
    }
}
