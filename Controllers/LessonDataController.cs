using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonDataController : ControllerBase
    {
        private readonly ILessonDataService _lessonDataService;

        public LessonDataController(ILessonDataService lessonDataService)
        {
            _lessonDataService = lessonDataService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateLessonData([FromBody] CreateOrAddLessonDataDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _lessonDataService.CreateOrAddLessonDataAsync(dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
