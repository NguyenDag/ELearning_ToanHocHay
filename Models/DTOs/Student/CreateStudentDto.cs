using System.ComponentModel.DataAnnotations;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Student
{
    public class CreateStudentDto
    {
        [Required]
        public int UserId { get; set; }

        [Range(6, 9)]
        public int GradeLevel { get; set; }

        [MaxLength(100)]
        public string? SchoolName { get; set; } = null;
    }
}
