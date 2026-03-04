// FILE: ELearning_ToanHocHay_Control/Models/DTOs/Parent/ParentDto.cs
namespace ELearning_ToanHocHay_Control.Models.DTOs.Parent
{
    public class ParentDto
    {
        public int ParentId { get; set; }
        public int UserId { get; set; }
        public string? Job { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string ConnectionCode { get; set; } = "";
        public List<ChildDto> Children { get; set; } = new();
    }

    public class ChildDto
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = "";
        public int GradeLevel { get; set; }
        public string Relationship { get; set; } = "";
    }
}