using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    /// <summary>A3/P2 — StudentCourse enrolment. A student only ever enrols themselves.</summary>
    [Route("api/enrollments")]
    [ApiController]
    [Authorize]
    public class EnrollmentController : ControllerBase
    {
        private readonly IEnrollmentService _enrollment;

        public EnrollmentController(IEnrollmentService enrollment)
        {
            _enrollment = enrollment;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMine()
        {
            var studentId = User.GetStudentId();
            if (studentId == null) return this.Forbidden("Only students have enrolments");
            return Ok(await _enrollment.GetMyEnrolmentsAsync(studentId.Value));
        }

        [HttpPost("courses/{courseId:int}")]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var studentId = User.GetStudentId();
            if (studentId == null) return this.Forbidden("Only students can enrol");

            var r = await _enrollment.EnrollAsync(studentId.Value, courseId);
            return r.ToActionResult();
        }
    }
}
