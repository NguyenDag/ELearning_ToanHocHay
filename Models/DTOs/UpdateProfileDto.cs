namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    /// <summary>
    /// Student self-service profile edit. All fields optional (patch).
    /// </summary>
    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? SchoolName { get; set; }
    }
}
