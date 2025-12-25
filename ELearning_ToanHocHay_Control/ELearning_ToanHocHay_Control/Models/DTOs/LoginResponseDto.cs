namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class LoginResponseDto
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string UserType { get; set; }
        public string Token { get; set; }
        public DateTime TokenExpiration { get; set; }
        public string AvatarUrl { get; set; }
    }
}
