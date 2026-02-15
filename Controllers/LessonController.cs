using ELearning_ToanHocHay_Control.Models.DTOs.Lesson;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonController : ControllerBase
    {
        private readonly ILessonSevice _lessonService;

        public LessonController(ILessonSevice lessonService)
        {
            _lessonService = lessonService;
        }

        /// <summary>
        /// Lấy danh sách lesson theo topic
        /// </summary>
        [HttpGet("by-topic/{topicId}")]
        public async Task<IActionResult> GetByTopic(int topicId)
        {
            var response = await _lessonService.GetByTopicAsync(topicId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// Lấy chi tiết lesson + nội dung
        /// </summary>
        [HttpGet("{lessonId}")]
        public async Task<IActionResult> GetById(int lessonId)
        {
            var response = await _lessonService.GetByIdAsync(lessonId);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }

        /// <summary>
        /// Tạo lesson mới (Teacher)
        /// </summary>
        [HttpPost]
        // [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Create([FromBody] CreateLessonDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // TODO: lấy từ JWT
            //int creatorId = User.GetUserId();
            int creatorId = 6; // ID của User đã tồn tại trong DB của bạn

            var response = await _lessonService.CreateAsync(dto, creatorId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// Cập nhật lesson (Draft / Rejected)
        /// </summary>
        [HttpPut("{lessonId}")]
        // [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Update(int lessonId, [FromBody] UpdateLessonDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _lessonService.UpdateAsync(lessonId, dto);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// Gửi lesson lên chờ duyệt
        /// </summary>
        [HttpPost("{lessonId}/submit")]
        // [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> SubmitForReview(int lessonId)
        {
            var response = await _lessonService.SubmitForReviewAsync(lessonId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// Duyệt hoặc từ chối lesson (Admin)
        /// </summary>
        [HttpPost("{lessonId}/review")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Review(
            int lessonId,
            [FromBody] ReviewLessonDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // TODO: lấy reviewerId từ JWT
            //int reviewerId = User.GetUserId();
            int reviewerId = 5; // ID của Admin đã tồn tại trong DB của bạn

            var response = await _lessonService.ReviewAsync(lessonId, dto, reviewerId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// Publish lesson
        /// </summary>
        [HttpPost("{lessonId}/publish")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Publish(int lessonId)
        {
            var response = await _lessonService.PublishAsync(lessonId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
    }
}