using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.LessonContent;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonContentController : ControllerBase
    {
        private readonly ILessonContentService _lessonContentService;

        public LessonContentController(ILessonContentService lessonContentService)
        {
            _lessonContentService = lessonContentService;
        }

        // GET: api/lesson-contents/{id}
        [HttpGet("{lessonContentId:int}")]
        public async Task<IActionResult> GetById(int lessonContentId)
        {
            var response = await _lessonContentService.GetByIdAsync(lessonContentId);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }

        // GET: api/lesson-contents/lesson/{lessonId}
        [HttpGet("by-lesson/{lessonId:int}")]
        public async Task<IActionResult> GetByLesson(int lessonId)
        {
            var response = await _lessonContentService.GetByLessonAsync(lessonId);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }

        // POST: api/lesson-contents/lesson/{lessonId}
        [HttpPost("lesson/{lessonId:int}")]
        //[Authorize]
        public async Task<IActionResult> Create(
            int lessonId,
            [FromBody] CreateLessonContentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _lessonContentService.CreateAsync(lessonId, dto);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // PUT: api/lesson-contents/{id}
        [HttpPut("{lessonContentId:int}")]
        //[Authorize]
        public async Task<IActionResult> Update(
            int lessonContentId,
            [FromBody] UpdateLessonContentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _lessonContentService.UpdateAsync(lessonContentId, dto);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // DELETE: api/lesson-contents/{id}
        [HttpDelete("{lessonContentId:int}")]
        //[Authorize]
        public async Task<IActionResult> Delete(int lessonContentId)
        {
            var response = await _lessonContentService.DeleteAsync(lessonContentId);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }
    }
}
