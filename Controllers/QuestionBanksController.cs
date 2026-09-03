using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Question;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    /// <summary>
    /// A3/P2 — question bank + question management and the Draft → PendingReview →
    /// Approved / Rejected workflow. Content-management roles only.
    /// </summary>
    [Route("api/question-banks")]
    [ApiController]
    [AuthorizeContentRole]
    public class QuestionBanksController : ControllerBase
    {
        private readonly IQuestionBankService _service;

        public QuestionBanksController(IQuestionBankService service)
        {
            _service = service;
        }

        // ---------------- banks ----------------
        [HttpGet]
        public async Task<IActionResult> GetBanks(
            [FromQuery] int? subjectId, [FromQuery] int? gradeLevelId, [FromQuery] bool includeInactive = true)
            => (await _service.GetBanksAsync(subjectId, gradeLevelId, includeInactive)).ToActionResult();

        [HttpGet("{bankId:int}")]
        public async Task<IActionResult> GetBank(int bankId)
        {
            var r = await _service.GetBankAsync(bankId);
            return r.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> CreateBank([FromBody] QuestionBankRequestDto dto)
        {
            var r = await _service.CreateBankAsync(dto, User.GetUserId()!.Value);
            return r.ToActionResult();
        }

        [HttpPut("{bankId:int}")]
        public async Task<IActionResult> UpdateBank(int bankId, [FromBody] QuestionBankRequestDto dto)
        {
            var r = await _service.UpdateBankAsync(bankId, dto);
            return r.ToActionResult();
        }

        [HttpDelete("{bankId:int}")]
        public async Task<IActionResult> DeleteBank(int bankId)
        {
            var r = await _service.DeleteBankAsync(bankId);
            return r.ToActionResult();
        }

        // ---------------- questions ----------------
        [HttpGet("{bankId:int}/questions")]
        public async Task<IActionResult> GetQuestions(
            int bankId,
            [FromQuery] QuestionStatus? status,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var r = await _service.GetQuestionsAsync(bankId, status, search, page, pageSize);
            return r.ToActionResult();
        }

        [HttpGet("questions/{questionId:int}")]
        public async Task<IActionResult> GetQuestion(int questionId)
        {
            var r = await _service.GetQuestionAsync(questionId);
            return r.ToActionResult();
        }

        [HttpPut("questions/{questionId:int}")]
        public async Task<IActionResult> UpdateQuestion(int questionId, [FromBody] UpdateQuestionDto dto)
        {
            var r = await _service.UpdateQuestionAsync(questionId, dto);
            return r.ToActionResult();
        }

        [HttpDelete("questions/{questionId:int}")]
        public async Task<IActionResult> DeleteQuestion(int questionId)
        {
            var r = await _service.DeleteQuestionAsync(questionId);
            return r.ToActionResult();
        }

        // ---------------- review workflow ----------------
        [HttpPost("questions/{questionId:int}/submit")]
        public async Task<IActionResult> SubmitQuestion(int questionId)
        {
            var r = await _service.SubmitQuestionAsync(questionId);
            return r.ToActionResult();
        }

        [HttpPost("questions/{questionId:int}/review")]
        [AuthorizeUserType(UserType.AcademicReviewer, UserType.SystemAdmin)]
        public async Task<IActionResult> ReviewQuestion(int questionId, [FromBody] ReviewQuestionDto dto)
        {
            var r = await _service.ReviewQuestionAsync(questionId, dto, User.GetUserId()!.Value);
            return r.ToActionResult();
        }
    }
}
