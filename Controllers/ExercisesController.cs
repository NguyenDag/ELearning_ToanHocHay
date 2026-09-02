using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Models.DTOs.Exercise;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExercisesController : ControllerBase
    {
        private readonly IExerciseService _exerciseService;

        public ExercisesController(IExerciseService exerciseService)
        {
            _exerciseService = exerciseService;
        }

        // ============================
        // POST: /api/exercises
        // Create an exercise
        // ============================
        [HttpPost]
        [AuthorizeContentRole]
        public async Task<IActionResult> CreateExercise([FromBody] ExerciseRequestDto dto)
        {
            var result = await _exerciseService.CreateExerciseAsync(dto);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // ============================
        // GET: /api/exercises
        // List exercises
        // ============================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _exerciseService.GetAllAsync();
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // ============================
        // GET: /api/exercises/{id}
        // Get exercise detail
        // ============================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _exerciseService.GetByIdAsync(id);
            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // ============================
        // PUT: /api/exercises/{id}
        // Update an exercise
        // ============================
        [HttpPut("{id}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> UpdateExercise(int id, [FromBody] ExerciseRequestDto dto)
        {
            var result = await _exerciseService.UpdateExerciseAsync(id, dto);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // ============================
        // DELETE: /api/exercises/{id}
        // Delete an exercise
        // ============================
        [HttpDelete("{id}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> DeleteExercise(int id)
        {
            var result = await _exerciseService.DeleteExerciseAsync(id);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // ============================
        // POST: /api/exercises/{id}/questions
        // Add questions to an exercise
        // ============================
        [HttpPost("{exerciseId}/questions")]
        [AuthorizeContentRole]
        public async Task<IActionResult> AddQuestions(int exerciseId, [FromBody] AddQuestionsToExerciseDto dto)
        {
            var result = await _exerciseService.AddQuestionsToExerciseAsync(exerciseId, dto);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // =================== FILTER ===================
        [HttpGet("by-lesson/{lessonId}")]
        public async Task<IActionResult> GetByLesson(int lessonId)
            => Ok(await _exerciseService.GetByLessonIdAsync(lessonId));

        [HttpGet("by-chapter/{chapterId}")]
        public async Task<IActionResult> GetByChapter(int chapterId)
            => Ok(await _exerciseService.GetByChapterIdAsync(chapterId));

        [HttpGet("by-topic/{topicId}")]
        public async Task<IActionResult> GetByTopic(int topicId)
            => Ok(await _exerciseService.GetByTopicIdAsync(topicId));

        // =================== QUESTIONS ===================
        /*[HttpGet("{id}/questions")]
        public async Task<IActionResult> GetQuestions(int id)
            => Ok(await _exerciseService.GetExerciseQuestionsAsync(id));*/

        [HttpDelete("{exerciseId}/questions/{questionId}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> RemoveQuestion(int exerciseId, int questionId)
            => Ok(await _exerciseService
                .RemoveQuestionFromExerciseAsync(exerciseId, questionId));

        [HttpPut("{exerciseId}/questions/{questionId}")]
        [AuthorizeContentRole]
        public async Task<IActionResult> UpdateQuestionScore(
            int exerciseId,
            int questionId,
            [FromBody] double score)
            => Ok(await _exerciseService
                .UpdateExerciseQuestionScoreAsync(exerciseId, questionId, score));

    }
}
