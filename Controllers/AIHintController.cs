using ELearning_ToanHocHay_Control.Models.DTOs.AIHint;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIHintController : ControllerBase
    {
        private readonly IAIHintService _hintService;

        public AIHintController(IAIHintService hintService)
        {
            _hintService = hintService;
        }

        [HttpGet("by-attempt/{attemptId:int}")]
        public async Task<IActionResult> GetByAttempt(int attemptId)
        {
            var result = await _hintService.GetByAttemptAsync(attemptId);
            return Ok(result);
        }

        [HttpGet("by-attempt-question")]
        public async Task<IActionResult> GetByAttemptAndQuestion(
            [FromQuery] int attemptId,
            [FromQuery] int questionId)
        {
            var result = await _hintService.GetByAttemptAndQuestionAsync(attemptId, questionId);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _hintService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateAIHintDto dto)
        {
            var result = await _hintService.CreateAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateAIHintDto dto)
        {
            var result = await _hintService.UpdateAsync(id, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _hintService.DeleteAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}
