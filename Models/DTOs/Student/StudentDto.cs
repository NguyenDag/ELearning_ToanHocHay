namespace ELearning_ToanHocHay_Control.Models.DTOs.Student
{
    public class StudentDto
    {
        public int StudentId { get; set; }
        public int UserId { get; set; }
        public int GradeLevel { get; set; }
        public string? SchoolName { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
    }
}
