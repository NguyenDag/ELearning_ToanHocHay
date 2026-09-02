using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Models.DTOs.AIHint;
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
    public class AIHintController : ControllerBase
    {
        private readonly IAIHintService _hintService;
        private readonly IResourceAccessService _access;

        public AIHintController(IAIHintService hintService, IResourceAccessService access)
        {
            _hintService = hintService;
            _access = access;
        }

        [HttpGet("by-attempt/{attemptId:int}")]
        public async Task<IActionResult> GetByAttempt(int attemptId)
        {
            if (!await _access.CanViewAttemptAsync(User, attemptId))
                return this.Forbidden();

            var result = await _hintService.GetByAttemptAsync(attemptId);
            return Ok(result);
        }

        [HttpGet("by-attempt-question")]
        public async Task<IActionResult> GetByAttemptAndQuestion(
            [FromQuery] int attemptId,
            [FromQuery] int questionId)
        {
            if (!await _access.CanViewAttemptAsync(User, attemptId))
                return this.Forbidden();

            var result = await _hintService.GetByAttemptAndQuestionAsync(attemptId, questionId);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _hintService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateAIHintDto dto)
        {
            if (!await _access.CanModifyAttemptAsync(User, dto.AttemptId))
                return this.Forbidden();

            var result = await _hintService.CreateAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:int}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> Update(int id, UpdateAIHintDto dto)
        {
            var result = await _hintService.UpdateAsync(id, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:int}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _hintService.DeleteAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}
