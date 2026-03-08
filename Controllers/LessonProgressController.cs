using ELearning_ToanHocHay_Control.Models.DTOs.LessonProgress;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonProgressController : ControllerBase
    {
        private readonly ILessonProgressService _lessonProgressService;

        public LessonProgressController(ILessonProgressService lessonProgressService)
        {
            _lessonProgressService = lessonProgressService;
        }
        [HttpPost("update-progress")]
        public async Task<IActionResult> UpdateProgress(UpdateProgressRequest request)
        {
            await _lessonProgressService.UpdateLessonProgress(
                request.StudentId,
                request.LessonId,
                request.WatchTime
            );

            return Ok();
        }

    }
}
