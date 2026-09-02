using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.ExerciseAttempt;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExerciseAttemptsController : ControllerBase
    {
        private readonly IExerciseAttemptService _attemptService;
        private readonly IResourceAccessService _access;

        public ExerciseAttemptsController(
            IExerciseAttemptService attemptService,
            IResourceAccessService access)
        {
            _attemptService = attemptService;
            _access = access;
        }

        /// <summary>
        /// Start an attempt on an existing exercise.
        /// </summary>
        [HttpPost("start")]
        public async Task<ActionResult<ApiResponse<ExerciseAttemptDto>>> StartExercise(
            [FromBody] StartExerciseDto dto)
        {
            var studentId = User.GetStudentId();
            if (studentId == null)
                return this.Forbidden("Only students can take exercises");

            dto.StudentId = studentId.Value; // never trust the studentId from the client

            var response = await _attemptService.StartExerciseAsync(dto);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// Start a random attempt drawn from a question bank.
        /// </summary>
        [HttpPost("start-random")]
        public async Task<ActionResult<ApiResponse<ExerciseAttemptDto>>> StartRandomExercise(
            [FromBody] StartRandomExerciseDto dto)
        {
            var studentId = User.GetStudentId();
            if (studentId == null)
                return this.Forbidden("Only students can take exercises");

            dto.StudentId = studentId.Value;

            var response = await _attemptService.StartRandomExerciseAsync(dto);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// Auto-save a single answer.
        /// </summary>
        [HttpPost("save-answer")]
        public async Task<ActionResult<ApiResponse<bool>>> SaveAnswer([FromBody] SaveAnswerDto dto)
        {
            if (!await _access.CanModifyAttemptAsync(User, dto.AttemptId))
                return this.Forbidden();

            var response = await _attemptService.SaveAnswerAsync(dto);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }


        /// <summary>
        /// Complete an attempt and compute the score. AI feedback is generated in the
        /// background — poll <c>{attemptId}/feedback-status</c> or re-fetch <c>{attemptId}/result</c>.
        /// </summary>
        [HttpPost("complete")]
        public async Task<ActionResult<ApiResponse<ExerciseResultDto>>> CompleteExercise(
            [FromBody] CompleteExerciseDto dto)
        {
            if (!await _access.CanModifyAttemptAsync(User, dto.AttemptId))
                return this.Forbidden();

            var response = await _attemptService.CompleteExerciseAsync(dto);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// View the result of a completed attempt.
        /// </summary>
        [HttpGet("{attemptId}/result")]
        public async Task<ActionResult<ApiResponse<ExerciseResultDto>>> GetExerciseResult(
            int attemptId)
        {
            if (!await _access.CanViewAttemptAsync(User, attemptId))
                return this.Forbidden();

            var response = await _attemptService.GetExerciseResultAsync(attemptId);

            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }

        /// <summary>
        /// Get a student's attempt history.
        /// </summary>
        [HttpGet("student/{studentId}/history")]
        public async Task<ActionResult<ApiResponse<List<ExerciseResultDto>>>> GetStudentHistory(
            int studentId)
        {
            if (!await _access.CanAccessStudentAsync(User, studentId))
                return this.Forbidden();

            var response = await _attemptService.GetStudentHistoryAsync(studentId);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// Report that a student switched tabs during an attempt — emails the parent.
        /// </summary>
        [HttpPost("{attemptId}/report-tab-switch")]
        public async Task<ActionResult<ApiResponse<bool>>> ReportTabSwitch(int attemptId)
        {
            if (!await _access.CanModifyAttemptAsync(User, attemptId))
                return this.Forbidden();

            var response = await _attemptService.ReportTabSwitchAsync(attemptId);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// AI feedback generation progress for an attempt.
        /// </summary>
        [HttpGet("{attemptId}/feedback-status")]
        public async Task<ActionResult<ApiResponse<FeedbackStatusDto>>> GetFeedbackStatus(int attemptId)
        {
            if (!await _access.CanViewAttemptAsync(User, attemptId))
                return this.Forbidden();

            var response = await _attemptService.GetFeedbackStatusAsync(attemptId);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// Get the tab-switch log for a single attempt.
        /// </summary>
        [HttpGet("{attemptId}/tab-switch-logs")]
        public async Task<ActionResult<ApiResponse<List<DateTime>>>> GetTabSwitchLogs(int attemptId)
        {
            if (!await _access.CanViewAttemptAsync(User, attemptId))
                return this.Forbidden();

            var response = await _attemptService.GetTabSwitchLogsAsync(attemptId);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
    }
}
