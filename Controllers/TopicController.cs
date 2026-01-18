using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TopicController : ControllerBase
    {
        private readonly ITopicService _topicService;

        public TopicController(ITopicService topicService)
        {
            _topicService = topicService;
        }

        [HttpGet("chapter/{chapterId:int}")]
        public async Task<IActionResult> GetByChapter(int chapterId)
        {
            var result = await _topicService.GetByChapterAsync(chapterId);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _topicService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateTopicDto dto)
        {
            var result = await _topicService.CreateAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateTopicDto dto)
        {
            var result = await _topicService.UpdateAsync(id, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _topicService.DeleteAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}
