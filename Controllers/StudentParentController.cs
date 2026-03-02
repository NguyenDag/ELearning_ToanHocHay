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
    public class StudentParentController : ControllerBase
    {
        private readonly IStudentParentService _service;

        public StudentParentController(IStudentParentService service)
        {
            _service = service;
        }

        // POST: api/student-parent/connect
        [HttpPost("connect")]
        [AuthorizeUserType(UserType.Student)]
        public async Task<IActionResult> ConnectParent([FromBody] ConnectParentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Lấy userId từ JWT
            var userId = int.Parse(User.FindFirst("UserId").Value);

            var response = await _service.ConnectParentAsync(userId, dto);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
    }
}
