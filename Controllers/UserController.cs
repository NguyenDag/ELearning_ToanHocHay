using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/user — only an admin may list every user (paged)
        [HttpGet]
        [AuthorizeUserType(UserType.SystemAdmin)]
        public async Task<IActionResult> GetAll([FromQuery] PagedRequest request)
        {
            var response = await _userService.GetPagedAsync(request);
            return response.ToActionResult();
        }

        // GET: api/user/5 — admin or the user themselves
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!User.IsSystemAdmin() && User.GetUserId() != id)
                return this.Forbidden();

            var response = await _userService.GetByIdAsync(id);
            return response.ToActionResult();
        }

        // GET: api/user/email/test@gmail.com — admin or the user themselves
        [HttpGet("email/{email}")]
        public async Task<IActionResult> GetByEmail(string email)
        {
            if (!User.IsSystemAdmin() &&
                !string.Equals(User.GetEmail(), email, StringComparison.OrdinalIgnoreCase))
                return this.Forbidden();

            var response = await _userService.GetByEmailAsync(email);
            return response.ToActionResult();
        }

        // POST: api/user
        [HttpPost]
        [AuthorizeUserType(UserType.SystemAdmin)]
        public async Task<IActionResult> Create([FromBody] CreateUserDto user)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            var response = await _userService.CreateUserAsync(user);
            return response.ToActionResult();
        }

        // PUT: api/user/5 — admin or the user themselves
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto user)
        {
            if (!User.IsSystemAdmin() && User.GetUserId() != id)
                return this.Forbidden();

            var response = await _userService.UpdateUserAsync(id, user);
            return response.ToActionResult();
        }

        // DELETE: api/user/5 — admin only
        [HttpDelete("{id:int}")]
        [AuthorizeUserType(UserType.SystemAdmin)]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _userService.DeleteUserAsync(id);
            return response.ToActionResult();
        }

        [HttpPost("update-profile/{id:int}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileDto model)
        {
            if (!User.IsSystemAdmin() && User.GetUserId() != id)
                return this.Forbidden();

            var response = await _userService.UpdateProfileAsync(id, model);
            return response.ToActionResult();
        }
    }
}
