using System.ComponentModel.DataAnnotations;

namespace ELearning_ToanHocHay_Control.Models.DTOs.Student
{
    public class StudentInfoDto
    {
        public int StudentId { get; set; }

        public string FullName { get; set; }

        public int GradeLevel { get; set; }

        public string? SchoolName { get; set; }
    }
}
