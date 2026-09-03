using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Models.DTOs;
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
        private readonly IAiQuotaService _quota;

        public AIHintController(IAIHintService hintService, IResourceAccessService access, IAiQuotaService quota)
        {
            _hintService = hintService;
            _access = access;
            _quota = quota;
        }

        /// <summary>Remaining AI hints for today (per the caller's package).</summary>
        [HttpGet("quota")]
        public async Task<IActionResult> GetQuota()
        {
            var studentId = User.GetStudentId();
            if (studentId == null) return this.Forbidden("Only students have an AI hint quota");

            var q = await _quota.PeekHintAsync(studentId.Value);
            return Ok(new { q.Used, q.Limit, q.Unlimited, q.Remaining });
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
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateAIHintDto dto)
        {
            if (!await _access.CanModifyAttemptAsync(User, dto.AttemptId))
                return this.Forbidden();

            // P6 — an AI-generated hint (no HintText supplied) counts against the daily quota.
            var studentId = User.GetStudentId();
            var aiGenerated = string.IsNullOrWhiteSpace(dto.HintText);
            if (aiGenerated && studentId != null)
            {
                var q = await _quota.TryConsumeHintAsync(studentId.Value);
                if (!q.Allowed)
                    return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse<object>.ErrorResponse(
                        $"Đã hết lượt gợi ý AI hôm nay ({q.Used}/{q.Limit}). Nâng cấp gói để dùng không giới hạn."));
            }

            var result = await _hintService.CreateAsync(dto);
            return result.ToActionResult();
        }

        [HttpPut("{id:int}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> Update(int id, UpdateAIHintDto dto)
        {
            var result = await _hintService.UpdateAsync(id, dto);
            return result.ToActionResult();
        }

        [HttpDelete("{id:int}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _hintService.DeleteAsync(id);
            return result.ToActionResult();
        }
    }
}
