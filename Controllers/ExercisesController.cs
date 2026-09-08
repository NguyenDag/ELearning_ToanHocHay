using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Models.DTOs.Exercise;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/exercises")]
    [ApiController]
    [Authorize]
    public class ExercisesController : ControllerBase
    {
        private readonly IExerciseService _exerciseService;

        public ExercisesController(IExerciseService exerciseService)
        {
            _exerciseService = exerciseService;
        }

        [HttpPost]
        [AuthorizeContentRole]
        public async Task<IActionResult> CreateExercise([FromBody] ExerciseRequestDto dto)
            => (await _exerciseService.CreateExerciseAsync(dto, User.GetUserId()!.Value)).ToActionResult();

        /// <summary>
        /// Danh sách bài tập / đề đang xuất bản. Khách chưa đăng nhập chỉ thấy các bài
        /// <see cref="Data.Entities.Exercise.IsFree"/> (§5.7 — nhánh ẩn danh). Làm bài vẫn phải đăng nhập.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
            => (await _exerciseService.GetAllAsync(freeOnly: User.Identity?.IsAuthenticated != true)).ToActionResult();

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
            => (await _exerciseService.GetByIdAsync(id)).ToActionResult();

        [HttpPut("{id}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> UpdateExercise(int id, [FromBody] ExerciseRequestDto dto)
            => (await _exerciseService.UpdateExerciseAsync(id, dto)).ToActionResult();

        [HttpDelete("{id}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> DeleteExercise(int id)
            => (await _exerciseService.DeleteExerciseAsync(id)).ToActionResult();

        [HttpPost("{exerciseId}/questions")]
        [AuthorizeContentRole]
        public async Task<IActionResult> AddQuestions(int exerciseId, [FromBody] AddQuestionsToExerciseDto dto)
            => (await _exerciseService.AddQuestionsToExerciseAsync(exerciseId, dto)).ToActionResult();

        // =================== FILTER ===================
        // Khách chưa đăng nhập chỉ thấy bài IsFree (giống GetAll).
        [HttpGet("by-lesson/{lessonId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByLesson(int lessonId)
            => (await _exerciseService.GetByLessonIdAsync(lessonId, User.Identity?.IsAuthenticated != true)).ToActionResult();

        [HttpGet("by-chapter/{chapterId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByChapter(int chapterId)
            => (await _exerciseService.GetByChapterIdAsync(chapterId, User.Identity?.IsAuthenticated != true)).ToActionResult();

        [HttpGet("by-topic/{topicId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByTopic(int topicId)
            => (await _exerciseService.GetByTopicIdAsync(topicId, User.Identity?.IsAuthenticated != true)).ToActionResult();

        // =================== PUBLISH WORKFLOW ===================
        [HttpPost("{id}/publish")]
        [AuthorizeContentRole]
        public async Task<IActionResult> Publish(int id)
            => (await _exerciseService.SetPublishedAsync(id, true)).ToActionResult();

        [HttpPost("{id}/unpublish")]
        [AuthorizeContentRole]
        public async Task<IActionResult> Unpublish(int id)
            => (await _exerciseService.SetPublishedAsync(id, false)).ToActionResult();

        [HttpGet("{id}/for-edit")]
        [AuthorizeContentRole]
        public async Task<IActionResult> GetForEdit(int id)
            => (await _exerciseService.GetForEditAsync(id)).ToActionResult();

        // =================== QUESTIONS ===================
        [HttpGet("{id}/questions")]
        [AuthorizeContentRole]
        public async Task<IActionResult> GetQuestions(int id)
            => (await _exerciseService.GetExerciseQuestionsAsync(id)).ToActionResult();

        [HttpDelete("{exerciseId}/questions/{questionId}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> RemoveQuestion(int exerciseId, int questionId)
            => (await _exerciseService.RemoveQuestionFromExerciseAsync(exerciseId, questionId)).ToActionResult();

        [HttpPut("{exerciseId}/questions/{questionId}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> UpdateQuestionScore(int exerciseId, int questionId, [FromBody] double score)
            => (await _exerciseService.UpdateExerciseQuestionScoreAsync(exerciseId, questionId, score)).ToActionResult();
    }
}
