using ELearning_ToanHocHay_Control.Models.DTOs.AIFeedback;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIFeedbackController : ControllerBase
    {
        private readonly IAIFeedbackService _feedbackService;

        public AIFeedbackController(IAIFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpGet("by-attempt/{attemptId:int}")]
        public async Task<IActionResult> GetByAttempt(int attemptId)
        {
            var result = await _feedbackService.GetByAttemptAsync(attemptId);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _feedbackService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateAIFeedbackDto dto)
        {
            var result = await _feedbackService.CreateAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateAIFeedbackDto dto)
        {
            var result = await _feedbackService.UpdateAsync(id, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _feedbackService.DeleteAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}
