namespace ELearning_ToanHocHay_Control.Models.DTOs.Parent
{
    public class ParentDto
    {
        public int ParentId { get; set; }
        public int UserId { get; set; }
        public string? Job { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
    }
}
