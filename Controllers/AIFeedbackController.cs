using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Models.DTOs.AIFeedback;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("ai")]
    public class AIFeedbackController : ControllerBase
    {
        private readonly IAIFeedbackService _feedbackService;
        private readonly IResourceAccessService _access;

        public AIFeedbackController(IAIFeedbackService feedbackService, IResourceAccessService access)
        {
            _feedbackService = feedbackService;
            _access = access;
        }

        [HttpGet("by-attempt/{attemptId:int}")]
        public async Task<IActionResult> GetByAttempt(int attemptId)
        {
            if (!await _access.CanViewAttemptAsync(User, attemptId))
                return this.Forbidden();

            var result = await _feedbackService.GetByAttemptAsync(attemptId);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _feedbackService.GetByIdAsync(id);
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateAIFeedbackDto dto)
        {
            if (!await _access.CanModifyAttemptAsync(User, dto.AttemptId))
                return this.Forbidden();

            var result = await _feedbackService.CreateAsync(dto);
            return result.ToActionResult();
        }

        [HttpPut("{id:int}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> Update(int id, UpdateAIFeedbackDto dto)
        {
            var result = await _feedbackService.UpdateAsync(id, dto);
            return result.ToActionResult();
        }

        [HttpDelete("{id:int}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _feedbackService.DeleteAsync(id);
            return result.ToActionResult();
        }
    }
}
