using System.ComponentModel.DataAnnotations;

namespace ToanHocHay.WebApp.Models.Dtos
{
    public class LoginRequestDto
    {
        public string Email { get; set; }

        public string Password { get; set; }
    }
}
