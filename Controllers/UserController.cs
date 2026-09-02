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
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/user — only an admin may list every user
        [HttpGet]
        [AuthorizeUserType(UserType.SystemAdmin)]
        public async Task<IActionResult> GetAll()
        {
            var response = await _userService.GetAllAsync();
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        // GET: api/user/5 — admin or the user themselves
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!User.IsSystemAdmin() && User.GetUserId() != id)
                return this.Forbidden();

            var response = await _userService.GetByIdAsync(id);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // GET: api/user/email/test@gmail.com — admin or the user themselves
        [HttpGet("email/{email}")]
        public async Task<IActionResult> GetByEmail(string email)
        {
            if (!User.IsSystemAdmin() &&
                !string.Equals(User.GetEmail(), email, StringComparison.OrdinalIgnoreCase))
                return this.Forbidden();

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

        // PUT: api/user/5 — admin or the user themselves
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto user)
        {
            if (!User.IsSystemAdmin() && User.GetUserId() != id)
                return this.Forbidden();

            var response = await _userService.UpdateUserAsync(id, user);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // DELETE: api/user/5 — admin only
        [HttpDelete("{id:int}")]
        [AuthorizeUserType(UserType.SystemAdmin)]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _userService.DeleteUserAsync(id);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPost("update-profile/{id:int}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileDto model)
        {
            if (!User.IsSystemAdmin() && User.GetUserId() != id)
                return this.Forbidden();

            // Load the current user first.
            var userResponse = await _userService.GetByIdAsync(id);
            if (!userResponse.Success || userResponse.Data == null)
                return BadRequest("User not found");

            // UpdateUserDto requires a Password value; pass an empty string
            // (UserService decides how to treat it).
            var updateDto = new UpdateUserDto
            {
                FullName = model.FullName,
                Password = ""
            };

            var response = await _userService.UpdateUserAsync(id, updateDto);
            if (!response.Success) return BadRequest(response);

            return Ok(response);
        }
    }
}
