using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly IQuestionService _questionService;

        public QuestionsController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        // API: POST /api/Questions
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateQuestionDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _questionService.CreateQuestionAsync(request);

            if (result.Success)
            {
                // Trả về 200 OK kèm dữ liệu
                return Ok(result);
            }
            // Trả về 400 Bad Request nếu lỗi
            return BadRequest(result);
        }
    }
}