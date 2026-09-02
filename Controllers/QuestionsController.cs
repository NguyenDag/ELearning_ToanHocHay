using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Models.DTOs.Question;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuestionsController : ControllerBase
    {
        private readonly IQuestionService _questionService;

        public QuestionsController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        // POST /api/Questions — content-management roles only
        [HttpPost]
        [AuthorizeContentRole]
        public async Task<IActionResult> Create([FromBody] List<CreateQuestionDto> requests)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _questionService.CreateQuestionsAsync(requests, User.GetUserId()!.Value);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
