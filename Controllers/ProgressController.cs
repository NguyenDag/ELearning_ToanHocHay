using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Models.DTOs.Content;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    /// <summary>P4 — learning progress: mark a lesson read, read NodeProgress, activity heatmap.</summary>
    [Route("api/progress")]
    [ApiController]
    [Authorize]
    public class ProgressController : ControllerBase
    {
        private readonly IProgressProjectionService _progress;
        private readonly IResourceAccessService _access;

        public ProgressController(IProgressProjectionService progress, IResourceAccessService access)
        {
            _progress = progress;
            _access = access;
        }

        [HttpPost("lessons/{nodeId:int}/complete")]
        public async Task<IActionResult> MarkLessonComplete(int nodeId, [FromBody] MarkLessonCompleteDto dto)
        {
            var studentId = User.GetStudentId();
            if (studentId == null) return this.Forbidden("Only students track lesson progress");

            var r = await _progress.MarkLessonCompleteAsync(studentId.Value, nodeId, dto.SecondsViewed);
            return r.ToActionResult();
        }

        [HttpGet("versions/{courseVersionId:int}")]
        public async Task<IActionResult> GetVersionProgress(int courseVersionId)
        {
            var studentId = User.GetStudentId();
            if (studentId == null) return this.Forbidden("Only students track lesson progress");

            return Ok(await _progress.GetVersionProgressAsync(studentId.Value, courseVersionId));
        }

        [HttpGet("students/{studentId:int}/heatmap")]
        public async Task<IActionResult> GetHeatmap(int studentId, [FromQuery] int days = 90)
        {
            if (!await _access.CanAccessStudentAsync(User, studentId))
                return this.Forbidden();

            return Ok(await _progress.GetHeatmapAsync(studentId, days));
        }
    }
}
