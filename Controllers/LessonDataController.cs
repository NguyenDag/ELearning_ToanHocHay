using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonDataController : ControllerBase
    {
        private readonly ILessonDataService _lessonDataService;

        public LessonDataController(ILessonDataService lessonDataService)
        {
            _lessonDataService = lessonDataService;
        }

        /// <summary>
        /// API chính: Tạo toàn bộ dữ liệu (Chapter/Topic/Lesson/Contents) cùng lúc
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/lessondata
        ///     {
        ///         "chapter": {
        ///             "curriculumId": 1,
        ///             "chapterName": "Số tự nhiên",
        ///             "orderIndex": 1,
        ///             "description": "Các khái niệm về số tự nhiên"
        ///         },
        ///         "topic": {
        ///             "chapterId": 0,
        ///             "topicName": "Tập hợp số tự nhiên",
        ///             "orderIndex": 1,
        ///             "description": "Khái niệm tập hợp",
        ///             "isFree": true
        ///         },
        ///         "lesson": {
        ///             "topicId": 0,
        ///             "lessonName": "Tập hợp và ký hiệu",
        ///             "description": "Giới thiệu khái niệm tập hợp",
        ///             "durationMinutes": 30,
        ///             "orderIndex": 1,
        ///             "isFree": true,
        ///             "isActive": true,
        ///             "status": 3,
        ///             "createdBy": 1
        ///         },
        ///         "lessonContents": [
        ///             {
        ///                 "lessonId": 0,
        ///                 "blockType": 0,
        ///                 "contentText": "Khái niệm tập hợp",
        ///                 "orderIndex": 1
        ///             },
        ///             {
        ///                 "lessonId": 0,
        ///                 "blockType": 2,
        ///                 "contentText": "Tập hợp là một nhóm các đối tượng xác định",
        ///                 "orderIndex": 2
        ///             }
        ///         ]
        ///     }
        ///     
        /// Notes:
        /// - Chapter, Topic có thể null nếu muốn thêm vào có sẵn
        /// - Lesson và LessonContents là bắt buộc
        /// - ID = 0 sẽ tự động được tạo bởi database
        /// </remarks>
        [HttpPost]
        public async Task<IActionResult> CreateLessonData([FromBody] CreateOrAddLessonDataDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _lessonDataService.CreateOrAddLessonDataAsync(dto);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
