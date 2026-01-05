using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExercisesController : ControllerBase
    {
        private readonly IExerciseService _exerciseService;

        public ExercisesController(IExerciseService exerciseService)
        {
            _exerciseService = exerciseService;
        }

        // GET: /api/exercises/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _exerciseService.GetByIdAsync(id);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        // POST: /api/exercises/{id}/start
        /*[Authorize(Roles = "Student")]
        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartExercise(int id)
        {
            var studentId = GetStudentIdFromToken();

            var attempt = await _exerciseService.StartExerciseAsync(studentId, exerciseId);
        x`
            return Ok(attempt);
        }*/

        // POST: /api/attempts/{attemptId}/answer
        // POST: /api/attempts/{attemptId}/submit
        // GET: /api/attempts/{ attemptId}/result

    }
}
