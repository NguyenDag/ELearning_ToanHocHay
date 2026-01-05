using AutoMapper;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class ExerciseAttemptService : IExerciseAttemptService
    {
        private readonly IExerciseAttemptRepository _attemptRepository;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IStudentAnswerRepository _answerRepository;
        private readonly IMapper _mapper;

        public ExerciseAttemptService(
            IExerciseAttemptRepository attemptRepository,
            IExerciseRepository exerciseRepository,
            IStudentAnswerRepository answerRepository,
            IMapper mapper)
        {
            _attemptRepository = attemptRepository;
            _exerciseRepository = exerciseRepository;
            _answerRepository = answerRepository;
            _mapper = mapper;
        }
        public async Task<ApiResponse<ExerciseResultDto>> CompleteExerciseAsync(CompleteExerciseDto dto)
        {
            try
            {
                var attempt = await _attemptRepository.GetAttemptWithDetailsAsync(dto.AttemptId);

                if (attempt == null)
                {
                    return ApiResponse<ExerciseResultDto>.ErrorResponse(
                        "Attempt not found",
                        new List<string> { $"No attempt found with ID: {dto.AttemptId}" }
                    );
                }

                if (attempt.EndTime != null)
                {
                    return ApiResponse<ExerciseResultDto>.ErrorResponse(
                        "Attempt already completed",
                        new List<string> { "This attempt has already been completed" }
                    );
                }

                // Lấy tất cả câu trả lời
                var answers = await _answerRepository.GetAttemptAnswersAsync(dto.AttemptId);

                // Tính điểm
                int totalScore = 0;
                int correctAnswers = 0;
                int wrongAnswers = 0;

                var answerDetails = new List<AnswerDetailDto>();

                foreach (var answer in answers)
                {
                    var question = answer.Question;
                    bool isCorrect = false;
                    double pointsEarned = 0;

                    // Kiểm tra đáp án đúng
                    if (question.QuestionType == QuestionType.MultipleChoice && answer.SelectedOptionId.HasValue)
                    {
                        var correctOption = question.QuestionOptions
                            ?.FirstOrDefault(o => o.IsCorrect);

                        isCorrect = correctOption?.OptionId == answer.SelectedOptionId;
                    }
                    else if (question.QuestionType == QuestionType.TrueFalse)
                    {
                        isCorrect = answer.AnswerText?.ToLower() == question.CorrectAnswer?.ToLower();
                    }
                    else if (question.QuestionType == QuestionType.FillBlank)
                    {
                        isCorrect = answer.AnswerText?.Trim().ToLower() ==
                                   question.CorrectAnswer?.Trim().ToLower();
                    }

                    if (isCorrect)
                    {
                        pointsEarned = question.Points;
                        totalScore += (int)pointsEarned;
                        correctAnswers++;
                    }
                    else
                    {
                        wrongAnswers++;
                    }

                    // Cập nhật điểm cho câu trả lời
                    answer.IsCorrect = isCorrect;
                    answer.PointsEarned = pointsEarned;
                    await _answerRepository.UpdateAnswerAsync(answer);

                    answerDetails.Add(new AnswerDetailDto
                    {
                        QuestionId = question.QuestionId,
                        QuestionText = question.QuestionText,
                        StudentAnswer = answer.AnswerText ??
                            question.QuestionOptions?.FirstOrDefault(o => o.OptionId == answer.SelectedOptionId)?.OptionText,
                        CorrectAnswer = question.CorrectAnswer ??
                            question.QuestionOptions?.FirstOrDefault(o => o.IsCorrect)?.OptionText,
                        IsCorrect = isCorrect,
                        PointsEarned = pointsEarned,
                        MaxPoints = question.Points
                    });
                }

                // Cập nhật attempt
                attempt.EndTime = DateTime.Now;
                attempt.TotalScore = totalScore;
                attempt.CorrectAnswers = correctAnswers;
                attempt.WrongAnswers = wrongAnswers;
                attempt.CompletionPercentage = attempt.MaxScore > 0
                    ? (decimal)totalScore / attempt.MaxScore * 100
                    : 0;

                await _attemptRepository.UpdateAttemptAsync(attempt);

                // Tạo result DTO
                var result = new ExerciseResultDto
                {
                    AttemptId = attempt.AttemptId,
                    StudentId = attempt.StudentId,
                    StudentName = attempt.Student?.User?.FullName,
                    ExerciseName = attempt.Exercise?.ExerciseName,
                    StartTime = attempt.StartTime,
                    EndTime = attempt.EndTime.Value,
                    Duration = attempt.EndTime.Value - attempt.StartTime,
                    TotalScore = attempt.TotalScore,
                    MaxScore = attempt.MaxScore,
                    CompletionPercentage = attempt.CompletionPercentage,
                    CorrectAnswers = attempt.CorrectAnswers,
                    WrongAnswers = attempt.WrongAnswers,
                    TotalQuestions = answers.Count,
                    IsPassed = attempt.Exercise != null &&
                               attempt.TotalScore >= attempt.Exercise.PassingScore,
                    AnswerDetails = answerDetails
                };

                return ApiResponse<ExerciseResultDto>.SuccessResponse(
                    result,
                    "Exercise completed successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<ExerciseResultDto>.ErrorResponse(
                    "Error completing exercise",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<ExerciseResultDto>> GetExerciseResultAsync(int attemptId)
        {
            try
            {
                var attempt = await _attemptRepository.GetAttemptWithDetailsAsync(attemptId);

                if (attempt == null)
                {
                    return ApiResponse<ExerciseResultDto>.ErrorResponse(
                        "Attempt not found",
                        new List<string> { $"No attempt found with ID: {attemptId}" }
                    );
                }

                if (attempt.EndTime == null)
                {
                    return ApiResponse<ExerciseResultDto>.ErrorResponse(
                        "Attempt not completed",
                        new List<string> { "Cannot get result for incomplete attempt" }
                    );
                }

                var answers = await _answerRepository.GetAttemptAnswersAsync(attemptId);

                var answerDetails = answers.Select(a => new AnswerDetailDto
                {
                    QuestionId = a.QuestionId,
                    QuestionText = a.Question.QuestionText,
                    StudentAnswer = a.AnswerText ??
                        a.Question.QuestionOptions?.FirstOrDefault(o => o.OptionId == a.SelectedOptionId)?.OptionText,
                    CorrectAnswer = a.Question.CorrectAnswer ??
                        a.Question.QuestionOptions?.FirstOrDefault(o => o.IsCorrect)?.OptionText,
                    IsCorrect = a.IsCorrect,
                    PointsEarned = a.PointsEarned,
                    MaxPoints = a.Question.Points
                }).ToList();

                var result = new ExerciseResultDto
                {
                    AttemptId = attempt.AttemptId,
                    StudentId = attempt.StudentId,
                    StudentName = attempt.Student?.User?.FullName,
                    ExerciseName = attempt.Exercise?.ExerciseName,
                    StartTime = attempt.StartTime,
                    EndTime = attempt.EndTime.Value,
                    Duration = attempt.EndTime.Value - attempt.StartTime,
                    TotalScore = attempt.TotalScore,
                    MaxScore = attempt.MaxScore,
                    CompletionPercentage = attempt.CompletionPercentage,
                    CorrectAnswers = attempt.CorrectAnswers,
                    WrongAnswers = attempt.WrongAnswers,
                    TotalQuestions = answers.Count,
                    IsPassed = attempt.Exercise != null &&
                               attempt.TotalScore >= attempt.Exercise.PassingScore,
                    AnswerDetails = answerDetails
                };

                return ApiResponse<ExerciseResultDto>.SuccessResponse(
                    result,
                    "Result retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<ExerciseResultDto>.ErrorResponse(
                    "Error retrieving result",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<List<ExerciseResultDto>>> GetStudentHistoryAsync(int studentId)
        {
            try
            {
                var attempts = await _attemptRepository.GetStudentAttemptsAsync(studentId);

                var results = attempts
                    .Where(a => a.EndTime != null)
                    .Select(a => new ExerciseResultDto
                    {
                        AttemptId = a.AttemptId,
                        StudentId = a.StudentId,
                        StudentName = a.Student?.User?.FullName,
                        ExerciseName = a.Exercise?.ExerciseName,
                        StartTime = a.StartTime,
                        EndTime = a.EndTime.Value,
                        Duration = a.EndTime.Value - a.StartTime,
                        TotalScore = a.TotalScore,
                        MaxScore = a.MaxScore,
                        CompletionPercentage = a.CompletionPercentage,
                        CorrectAnswers = a.CorrectAnswers,
                        WrongAnswers = a.WrongAnswers,
                        TotalQuestions = a.Exercise?.TotalQuestions ?? 0,
                        IsPassed = a.Exercise != null &&
                                   a.TotalScore >= a.Exercise.PassingScore
                    })
                    .ToList();

                return ApiResponse<List<ExerciseResultDto>>.SuccessResponse(
                    results,
                    "History retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<List<ExerciseResultDto>>.ErrorResponse(
                    "Error retrieving history",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<ExerciseAttemptDto>> StartExerciseAsync(StartExerciseDto dto)
        {
            try
            {
                // Kiểm tra xem đã có bài làm đang active chưa
                var hasActive = await _attemptRepository.HasActiveAttemptAsync(
                    dto.StudentId, dto.ExerciseId);

                if (hasActive)
                {
                    return ApiResponse<ExerciseAttemptDto>.ErrorResponse(
                        "Student already has an active attempt for this exercise",
                        new List<string> { "Please complete or cancel the current attempt first" }
                    );
                }

                // Lấy thông tin bài tập
                var exercise = await _exerciseRepository.GetExerciseWithQuestionsAsync(dto.ExerciseId);

                if (exercise == null)
                {
                    return ApiResponse<ExerciseAttemptDto>.ErrorResponse(
                        "Exercise not found",
                        new List<string> { $"No exercise found with ID: {dto.ExerciseId}" }
                    );
                }

                if (!exercise.IsActive || exercise.Status != ExerciseStatus.Published)
                {
                    return ApiResponse<ExerciseAttemptDto>.ErrorResponse(
                        "Exercise is not available",
                        new List<string> { "This exercise is not currently available" }
                    );
                }

                // Tạo attempt mới
                var attempt = new ExerciseAttempt
                {
                    StudentId = dto.StudentId,
                    ExerciseId = dto.ExerciseId,
                    StartTime = DateTime.Now,
                    MaxScore = (int)exercise.TotalPoints
                };

                var createdAttempt = await _attemptRepository.CreateAttemptAsync(attempt);

                // Map sang DTO
                var attemptDto = _mapper.Map<ExerciseAttemptDto>(createdAttempt);

                return ApiResponse<ExerciseAttemptDto>.SuccessResponse(
                    attemptDto,
                    "Exercise started successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<ExerciseAttemptDto>.ErrorResponse(
                    "Error starting exercise",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<ExerciseAttemptDto>> StartRandomExerciseAsync(StartRandomExerciseDto dto)
        {
            try
            {
                // Lấy câu hỏi random từ question bank
                var questions = await _exerciseRepository.GetRandomQuestionsAsync(
                    dto.TopicId,
                    dto.ChapterId,
                    dto.NumberOfQuestions
                );

                if (!questions.Any())
                {
                    return ApiResponse<ExerciseAttemptDto>.ErrorResponse(
                        "No questions available",
                        new List<string> { "Cannot find enough questions for this exercise" }
                    );
                }

                // Tạo exercise tạm thời (hoặc lưu vào DB nếu cần)
                var exercise = new Exercise
                {
                    ExerciseName = $"Random {dto.ExerciseType} - {DateTime.Now:yyyy-MM-dd HH:mm}",
                    ExerciseType = dto.ExerciseType,
                    TotalQuestions = questions.Count,
                    DurationMinutes = dto.DurationMinutes,
                    TotalPoints = questions.Sum(q => q.Points),
                    Status = ExerciseStatus.Published,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                // Tạo attempt
                var attempt = new ExerciseAttempt
                {
                    StudentId = dto.StudentId,
                    ExerciseId = 0, // Hoặc tạo exercise trong DB trước
                    StartTime = DateTime.Now,
                    MaxScore = (int)exercise.TotalPoints
                };

                var createdAttempt = await _attemptRepository.CreateAttemptAsync(attempt);

                // Map sang DTO với questions
                var attemptDto = new ExerciseAttemptDto
                {
                    AttemptId = createdAttempt.AttemptId,
                    StudentId = createdAttempt.StudentId,
                    //ExerciseId = createdAttempt.ExerciseId,
                    ExerciseName = exercise.ExerciseName,
                    ExerciseType = exercise.ExerciseType,
                    StartTime = createdAttempt.StartTime,
                    DurationMinutes = exercise.DurationMinutes,
                    TotalQuestions = questions.Count,
                    IsCompleted = false,
                    Questions = questions.Select(q => new QuestionInAttemptDto
                    {
                        QuestionId = q.QuestionId,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType.ToString(),
                        Points = q.Points,
                        ImageUrl = q.QuestionImageUrl,
                        Options = q.QuestionOptions?.Select(o => new AnswerOptionDto
                        {
                            OptionId = o.OptionId,
                            OptionText = o.OptionText,
                            ImageUrl = o.ImageUrl
                        }).ToList() ?? new List<AnswerOptionDto>()
                    }).ToList()
                };

                return ApiResponse<ExerciseAttemptDto>.SuccessResponse(
                    attemptDto,
                    "Random exercise created successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<ExerciseAttemptDto>.ErrorResponse(
                    "Error creating random exercise",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<bool>> SubmitAnswerAsync(SubmitAnswerDto dto)
        {
            try
            {
                // Kiểm tra attempt có tồn tại không
                var attempt = await _attemptRepository.GetAttemptByIdAsync(dto.AttemptId);

                if (attempt == null)
                {
                    return ApiResponse<bool>.ErrorResponse(
                        "Attempt not found",
                        new List<string> { $"No attempt found with ID: {dto.AttemptId}" }
                    );
                }

                if (attempt.EndTime != null)
                {
                    return ApiResponse<bool>.ErrorResponse(
                        "Attempt already completed",
                        new List<string> { "Cannot submit answer for completed attempt" }
                    );
                }

                // Kiểm tra xem đã trả lời câu này chưa
                var existingAnswer = await _answerRepository.GetAnswerAsync(
                    dto.AttemptId,
                    dto.QuestionId
                );

                if (existingAnswer != null)
                {
                    // Cập nhật câu trả lời
                    existingAnswer.AnswerText = dto.AnswerText;
                    existingAnswer.SelectedOptionId = dto.SelectedOptionId;
                    existingAnswer.AnsweredAt = DateTime.Now;

                    await _answerRepository.UpdateAnswerAsync(existingAnswer);
                }
                else
                {
                    // Tạo câu trả lời mới
                    var answer = new StudentAnswer
                    {
                        AttemptId = dto.AttemptId,
                        QuestionId = dto.QuestionId,
                        AnswerText = dto.AnswerText,
                        SelectedOptionId = dto.SelectedOptionId,
                        AnsweredAt = DateTime.Now
                    };

                    await _answerRepository.CreateAnswerAsync(answer);
                }

                return ApiResponse<bool>.SuccessResponse(
                    true,
                    "Answer submitted successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(
                    "Error submitting answer",
                    new List<string> { ex.Message }
                );
            }
        }
        // Helper method
        private async Task<ExerciseAttemptDto> MapToAttemptDto(ExerciseAttempt attempt, Exercise exercise)
        {
            return new ExerciseAttemptDto
            {
                AttemptId = attempt.AttemptId,
                StudentId = attempt.StudentId,
                //ExerciseId = attempt.ExerciseId,
                ExerciseName = exercise.ExerciseName,
                ExerciseType = exercise.ExerciseType,
                StartTime = attempt.StartTime,
                EndTime = attempt.EndTime,
                DurationMinutes = exercise.DurationMinutes,
                TotalQuestions = exercise.TotalQuestions,
                IsCompleted = attempt.EndTime != null,
                Questions = exercise.ExerciseQuestions?.Select(eq => new QuestionInAttemptDto
                {
                    QuestionId = eq.Question.QuestionId,
                    QuestionText = eq.Question.QuestionText,
                    QuestionType = eq.Question.QuestionType.ToString(),
                    Points = eq.Question.Points,
                    //ImageUrl = eq.Question.ImageUrl,
                    Options = eq.Question.QuestionOptions?.Select(o => new AnswerOptionDto
                    {
                        OptionId = o.OptionId,
                        OptionText = o.OptionText,
                        ImageUrl = o.ImageUrl
                    }).ToList() ?? new List<AnswerOptionDto>()
                }).ToList() ?? new List<QuestionInAttemptDto>()
            };
        }
    }
}
