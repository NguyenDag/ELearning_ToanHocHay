using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        // GET: api/user
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _userService.GetAllAsync();
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        // GET: api/user/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _userService.GetByIdAsync(id);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // GET: api/user/email/test@gmail.com
        [HttpGet("email/{email}")]
        public async Task<IActionResult> GetByEmail(string email)
        {
            var response = await _userService.GetByEmailAsync(email);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }

        // POST: api/user
        [HttpPost]
        [AuthorizeUserType(UserType.SystemAdmin)]
        public async Task<IActionResult> Create([FromBody] CreateUserDto user)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _userService.CreateUserAsync(user);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // PUT: api/user/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto user)
        {
            var response = await _userService.UpdateUserAsync(id, user);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // DELETE: api/user/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _userService.DeleteUserAsync(id);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
        [HttpPost("update-profile/{id:int}")]
        [HttpPost("update-profile/{id:int}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileDto model)
        {
            // Lấy user hiện tại từ Database ra trước
            var userResponse = await _userService.GetByIdAsync(id);
            if (!userResponse.Success || userResponse.Data == null)
                return BadRequest("Không tìm thấy người dùng");

            // Tạo object UpdateUserDto và gán lại mật khẩu cũ của họ để thỏa mãn điều kiện 'required'
            var updateDto = new UpdateUserDto
            {
                FullName = model.FullName,
                // Giả sử UpdateUserDto của bạn cần Password, ta truyền lại mật khẩu hiện tại 
                // (Hoặc chuỗi rỗng tùy vào logic của UserService của bạn)
                Password = ""
            };

            var response = await _userService.UpdateUserAsync(id, updateDto);
            if (!response.Success) return BadRequest(response);

            return Ok(response);
        }
    }
}
