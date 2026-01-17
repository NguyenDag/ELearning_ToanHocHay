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
        private readonly IUserRepository _userRepository;
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IMapper _mapper;
        private readonly IExerciseQuestionRepository _exerciseQuestionRepository;

        public ExerciseAttemptService(
            IExerciseAttemptRepository attemptRepository,
            IExerciseRepository exerciseRepository,
            IStudentAnswerRepository answerRepository,
            IUserRepository userRepository,
            IQuestionBankRepository questionBankRepository,
            IExerciseQuestionRepository exerciseQuestionRepository,
            IMapper mapper)
        {
            _attemptRepository = attemptRepository;
            _exerciseRepository = exerciseRepository;
            _answerRepository = answerRepository;
            _userRepository = userRepository;
            _questionBankRepository = questionBankRepository;
            _mapper = mapper;
            _exerciseQuestionRepository = exerciseQuestionRepository;
        }
        /*public async Task<ApiResponse<ExerciseResultDto>> CompleteExerciseAsync(CompleteExerciseDto dto)
        {
            try
            {
                // Get attempt detail (not scored)
                var attempt = await _attemptRepository.GetAttemptWithDetailsAsync(dto.AttemptId);

                // Check has attempt or not
                if (attempt == null)
                {
                    return ApiResponse<ExerciseResultDto>.ErrorResponse(
                        "Attempt not found",
                        new List<string> { $"No attempt found with ID: {dto.AttemptId}" }
                    );
                }

                // Check attempt finished or not
                if (attempt.EndTime != null)
                {
                    return ApiResponse<ExerciseResultDto>.ErrorResponse(
                        "Attempt already completed",
                        new List<string> { "This attempt has already been completed" }
                    );
                }

                // Lấy tất cả câu trả lời
                var answers = await _answerRepository.GetAttemptAnswersAsync(dto.AttemptId);

                // Lấy ExerciseQuestion để biết điểm từng câu
                var exerciseQuestions = await _exerciseQuestionRepository
                    .GetByExerciseIdAsync(attempt.ExerciseId);

                var scoreLookup = exerciseQuestions.ToDictionary(
                    eq => eq.QuestionId,
                    eq => eq.Score
                );

                // Map Answer theo QuestionId
                var answerLookup = answers.ToDictionary(a => a.QuestionId);

                // Tính điểm
                double totalScore = 0;
                int correctAnswers = 0;
                int wrongAnswers = 0;

                var answerDetails = new List<AnswerDetailDto>();

                foreach (var eq in exerciseQuestions)
                {
                    answerLookup.TryGetValue(eq.QuestionId, out var answer);
                    bool isCorrect = false;

                    var question = answer.Question;

                    if (answer != null)
                    {
                        

                        if (question.QuestionType == QuestionType.MultipleChoice &&
                            answer.SelectedOptionId.HasValue)
                        {
                            var correctOption = question.QuestionOptions
                                .FirstOrDefault(o => o.IsCorrect);

                            isCorrect = correctOption?.OptionId == answer.SelectedOptionId;
                        }
                        else if (question.QuestionType == QuestionType.TrueFalse)
                        {
                            isCorrect = answer.AnswerText?.ToLower() ==
                                        question.CorrectAnswer?.ToLower();
                        }
                        else if (question.QuestionType == QuestionType.FillBlank)
                        {
                            isCorrect = answer.AnswerText?.Trim().ToLower() ==
                                        question.CorrectAnswer?.Trim().ToLower();
                        }
                    }

                    // LẤY ĐIỂM TỪ ExerciseQuestion
                    //var maxScore = scoreLookup.TryGetValue(question.QuestionId, out var s) ? s : 0;
                    var maxScore = eq.Score;
                    var pointsEarned = isCorrect ? maxScore : 0;

                    if (isCorrect)
                    {
                        totalScore += pointsEarned;
                        correctAnswers++;
                    }
                    else
                    {
                        wrongAnswers++;
                    }

                    // Nếu có answer thì update
                    if (answer != null)
                    {
                        answer.IsCorrect = isCorrect;
                        answer.PointsEarned = pointsEarned;
                        await _answerRepository.UpdateAnswerAsync(answer);
                    }

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
                        MaxScores = maxScore,
                        Explanation = question.Explanation
                    });
                }

                // Cập nhật attempt
                attempt.EndTime = DateTime.UtcNow;
                attempt.TotalScore = totalScore;
                attempt.CorrectAnswers = correctAnswers;
                attempt.WrongAnswers = wrongAnswers;
                attempt.CompletionPercentage = attempt.MaxScore > 0
                    ? (decimal)(totalScore / attempt.MaxScore) * 100
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
        }*/

        public async Task<ApiResponse<ExerciseResultDto>> CompleteExerciseAsync(CompleteExerciseDto dto)
        {
            try
            {
                // 1. Lấy attempt + kiểm tra
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

                // 2. Lấy danh sách câu hỏi của bài
                var exerciseQuestions = await _exerciseQuestionRepository
                    .GetByExerciseIdAsync(attempt.ExerciseId);

                // 3. Lấy các câu trả lời đã làm
                var answers = await _answerRepository
                    .GetAttemptAnswersAsync(dto.AttemptId);

                // Map Answer theo QuestionId
                var answerLookup = answers.ToDictionary(a => a.QuestionId);

                // 4. Biến chấm điểm
                double totalScore = 0;
                int correctAnswers = 0;
                int wrongAnswers = 0;

                var answerDetails = new List<AnswerDetailDto>();

                // 5. DUYỆT THEO CÂU HỎI (CHUẨN)
                foreach (var eq in exerciseQuestions)
                {
                    answerLookup.TryGetValue(eq.QuestionId, out var answer);
                    var question = eq.Question;

                    bool isCorrect = false;

                    // Nếu có trả lời thì mới chấm
                    if (answer != null)
                    {
                        switch (question.QuestionType)
                        {
                            case QuestionType.MultipleChoice:
                                if (answer.SelectedOptionId.HasValue)
                                {
                                    var correctOption = question.QuestionOptions
                                        ?.FirstOrDefault(o => o.IsCorrect);

                                    isCorrect = correctOption?.OptionId == answer.SelectedOptionId;
                                }
                                break;

                            case QuestionType.TrueFalse:
                                isCorrect = answer.AnswerText?.Trim().ToLower() ==
                                            question.CorrectAnswer?.Trim().ToLower();
                                break;

                            case QuestionType.FillBlank:
                                isCorrect = answer.AnswerText?.Trim().ToLower() ==
                                            question.CorrectAnswer?.Trim().ToLower();
                                break;
                        }
                    }
                    // else: không trả lời => sai

                    var maxScore = eq.Score;
                    var pointsEarned = isCorrect ? maxScore : 0;

                    if (isCorrect)
                    {
                        totalScore += pointsEarned;
                        correctAnswers++;
                    }
                    else
                    {
                        wrongAnswers++;
                    }

                    // Cập nhật Answer nếu có
                    if (answer != null)
                    {
                        answer.IsCorrect = isCorrect;
                        answer.PointsEarned = pointsEarned;
                        await _answerRepository.UpdateAnswerAsync(answer);
                    }

                    // Chi tiết câu trả lời
                    answerDetails.Add(new AnswerDetailDto
                    {
                        QuestionId = question.QuestionId,
                        QuestionText = question.QuestionText,
                        StudentAnswer = answer == null
                            ? null
                            : answer.AnswerText ??
                              question.QuestionOptions?
                                  .FirstOrDefault(o => o.OptionId == answer.SelectedOptionId)
                                  ?.OptionText,
                        CorrectAnswer = question.CorrectAnswer ??
                            question.QuestionOptions?
                                .FirstOrDefault(o => o.IsCorrect)
                                ?.OptionText,
                        IsCorrect = isCorrect,
                        PointsEarned = pointsEarned,
                        MaxScores = maxScore,
                        Explanation = question.Explanation
                    });
                }

                // 6. Cập nhật attempt
                attempt.EndTime = DateTime.UtcNow;
                attempt.TotalScore = totalScore;
                attempt.CorrectAnswers = correctAnswers;
                attempt.WrongAnswers = wrongAnswers;
                attempt.CompletionPercentage = attempt.MaxScore > 0
                    ? (decimal)(totalScore / attempt.MaxScore) * 100
                    : 0;

                await _attemptRepository.UpdateAttemptAsync(attempt);

                // 7. Trả kết quả
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
                    TotalQuestions = exerciseQuestions.Count, // ✅ ĐÚNG
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
                // Lấy attempt + exercise + student
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

                // Lấy answers
                var answers = await _answerRepository.GetAttemptAnswersAsync(attemptId);

                // Lấy ExerciseQuestion để biết điểm từng câu
                var exerciseQuestions = await _exerciseQuestionRepository
                    .GetByExerciseIdAsync(attempt.ExerciseId);

                var scoreLookup = exerciseQuestions.ToDictionary(
                    eq => eq.QuestionId,
                    eq => eq.Score
                );

                // Map AnswerDetails (LẤY SCORE TỪ ExerciseQuestion)
                var answerDetails = answers.Select(a =>
                {
                    var maxScore = scoreLookup.TryGetValue(a.QuestionId, out var s) ? s : 0;

                    return new AnswerDetailDto
                    {
                        QuestionId = a.QuestionId,
                        QuestionText = a.Question.QuestionText,
                        StudentAnswer = a.AnswerText ??
                        a.Question.QuestionOptions?.FirstOrDefault(o => o.OptionId == a.SelectedOptionId)?.OptionText,
                        CorrectAnswer = a.Question.CorrectAnswer ??
                        a.Question.QuestionOptions?.FirstOrDefault(o => o.IsCorrect)?.OptionText,
                        IsCorrect = a.IsCorrect,
                        PointsEarned = a.PointsEarned,
                        MaxScores = maxScore,
                        Explanation = a.Question.Explanation
                    };
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
                    TotalScore = answerDetails.Sum(a => a.PointsEarned),
                    MaxScore = scoreLookup.Values.Sum(),
                    CompletionPercentage = attempt.CompletionPercentage,
                    CorrectAnswers = answerDetails.Count(a => a.IsCorrect),
                    WrongAnswers = answerDetails.Count(a => !a.IsCorrect),
                    TotalQuestions = answerDetails.Count,
                    IsPassed = attempt.Exercise != null &&
                       answerDetails.Sum(a => a.PointsEarned) >= attempt.Exercise.PassingScore,
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

                if (!exercise.IsActive || exercise.Status != ExerciseStatus.Published || !exercise.IsFree)
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
                    StartTime = DateTime.UtcNow,
                    MaxScore = exercise.TotalScores
                };

                var createdAttempt = await _attemptRepository.CreateAttemptAsync(attempt);

                // Map sang DTO
                //var attemptDto = _mapper.Map<ExerciseAttemptDto>(createdAttempt);
                var attemptDto = await MapToAttemptDto(createdAttempt, exercise);

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
                    dto.BankId,
                    dto.NumberOfQuestions
                );

                if (!questions.Any())
                {
                    return ApiResponse<ExerciseAttemptDto>.ErrorResponse(
                        "No questions available",
                        new List<string> { "Cannot find enough questions for this exercise" }
                    );
                }

                // Get UserId from StudentId to save to CreatedBy attribute
                var user = await _userRepository.GetUserByStudentIdAsync(dto.StudentId);

                if (user == null)
                {
                    return ApiResponse<ExerciseAttemptDto>.ErrorResponse(
                        "User not found",
                        new List<string> { "Invalid StudentId" }
                    );
                }

                // Get QuestionBank from bankId
                var questionBank = await _questionBankRepository.GetQuestionBankByIdAsync(dto.BankId);

                if (questionBank == null)
                {
                    return ApiResponse<ExerciseAttemptDto>.ErrorResponse(
                        "Question bank not found",
                        new List<string> { "Invalid BankId" }
                    );
                }

                // Tạo exercise tạm thời (hoặc lưu vào DB nếu cần)
                var exercise = new Exercise
                {
                    ExerciseName = $"Random {dto.ExerciseType} - {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                    ChapterId = questionBank.ChapterId,
                    TopicId = questionBank.TopicId,
                    ExerciseType = dto.ExerciseType,
                    TotalQuestions = questions.Count,
                    DurationMinutes = dto.DurationMinutes,
                    TotalScores = dto.MaxScore,
                    Status = ExerciseStatus.Published,
                    IsActive = true,
                    CreatedBy = user.UserId,
                    CreatedAt = DateTime.UtcNow
                };

                await _exerciseRepository.CreateExerciseAsync(exercise);

                // Tạo ExerciseQuestion (NƠI LƯU SCORE)
                var exerciseQuestions = questions.Select((q, index) => new ExerciseQuestion
                {
                    ExerciseId = exercise.ExerciseId,
                    QuestionId = q.QuestionId,
                    Score = dto.MaxScore / questions.Count,   // chia đều điểm
                    OrderIndex = index + 1
                }).ToList();

                await _exerciseQuestionRepository.AddRangeAsync(exerciseQuestions);

                // Tạo attempt
                var attempt = new ExerciseAttempt
                {
                    StudentId = dto.StudentId,
                    ExerciseId = exercise.ExerciseId,
                    StartTime = DateTime.UtcNow,
                    MaxScore = dto.MaxScore,
                };

                var createdAttempt = await _attemptRepository.CreateAttemptAsync(attempt);

                // Tạo lookup để lấy Score nhanh
                var scoreLookup = exerciseQuestions.ToDictionary(
                    eq => eq.QuestionId,
                    eq => eq.Score
                );

                // Map sang DTO với questions
                var attemptDto = new ExerciseAttemptDto
                {
                    AttemptId = createdAttempt.AttemptId,
                    StudentId = createdAttempt.StudentId,
                    ExerciseId = createdAttempt.ExerciseId,
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
                        Score = scoreLookup[q.QuestionId],
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
                    existingAnswer.AnsweredAt = DateTime.UtcNow;

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
                        AnsweredAt = DateTime.UtcNow
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
                ExerciseId = attempt.ExerciseId,
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
                    Score = eq.Score,
                    ImageUrl = eq.Question.QuestionImageUrl,
                    Options = eq.Question.QuestionOptions?.Select(o => new AnswerOptionDto
                    {
                        OptionId = o.OptionId,
                        OptionText = o.OptionText,
                        ImageUrl = o.ImageUrl,
                        IsCorrect = o.IsCorrect,
                    }).ToList() ?? new List<AnswerOptionDto>()
                }).ToList() ?? new List<QuestionInAttemptDto>()
            };
        }
    }
}
