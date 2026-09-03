using AutoMapper;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.ExerciseAttempt;
using ELearning_ToanHocHay_Control.Models.DTOs.AIFeedback;
using ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


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
        private readonly IAIFeedbackRepository _feedbackRepository;
        private readonly IAiFeedbackQueue _aiFeedbackQueue;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IProgressProjectionService _projection;
        private readonly INotificationRuleEngine _rules;
        private readonly IBackgroundEmailService _backgroundEmail;

        public ExerciseAttemptService(
            IExerciseAttemptRepository attemptRepository,
            IExerciseRepository exerciseRepository,
            IStudentAnswerRepository answerRepository,
            IUserRepository userRepository,
            IQuestionBankRepository questionBankRepository,
            IExerciseQuestionRepository exerciseQuestionRepository,
            IAIFeedbackRepository feedbackRepository,
            IAiFeedbackQueue aiFeedbackQueue,
            IMapper mapper,
            AppDbContext context,
            IEmailService emailService,
            IProgressProjectionService projection,
            INotificationRuleEngine rules,
            IBackgroundEmailService backgroundEmail)
        {
            _projection = projection;
            _rules = rules;
            _backgroundEmail = backgroundEmail;
            _attemptRepository = attemptRepository;
            _exerciseRepository = exerciseRepository;
            _answerRepository = answerRepository;
            _userRepository = userRepository;
            _questionBankRepository = questionBankRepository;
            _exerciseQuestionRepository = exerciseQuestionRepository;
            _feedbackRepository = feedbackRepository;
            _aiFeedbackQueue = aiFeedbackQueue;
            _mapper = mapper;
            _context = context;
            _emailService = emailService;
        }

        public async Task<ApiResponse<ExerciseResultDto>> CompleteExerciseAsync(CompleteExerciseDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Load the attempt and validate it
                var attempt = await _attemptRepository.GetAttemptWithDetailsAsync(dto.AttemptId);

                if (attempt == null)
                {
                    return ApiResponse<ExerciseResultDto>.ErrorResponse(
                        "Attempt not found",
                        new List<string> { $"No attempt found with ID: {dto.AttemptId}" }
                    );
                }

                if (attempt.Status != AttemptStatus.InProgress)
                {
                    return ApiResponse<ExerciseResultDto>.ErrorResponse(
                        "Attempt already completed",
                        new List<string> { "This attempt has already been completed" }
                    );
                }

                var now = DateTime.UtcNow;
                var isTimeout = attempt.PlannedEndTime.HasValue && now >= attempt.PlannedEndTime.Value;

                // 2. Load the exercise's questions
                var exerciseQuestions = await _exerciseQuestionRepository
                    .GetByExerciseIdAsync(attempt.ExerciseId);

                // Even split fallback when eq.Score is 0
                var totalQuestions = exerciseQuestions.Count;
                var scorePerQuestion = totalQuestions > 0
                    ? attempt.MaxScore / totalQuestions
                    : 0;

                // 3. Load the saved answers
                var answers = await _answerRepository
                    .GetAttemptAnswersAsync(dto.AttemptId);

                var answerLookup = answers.ToDictionary(a => a.QuestionId);

                // 4. Grading accumulators
                double totalScore = 0;
                int correctAnswers = 0;
                int wrongAnswers = 0;
                bool hasPendingManualGrading = false;

                var answerDetails = new List<AnswerDetailDto>();

                // 5. Grade question by question
                foreach (var eq in exerciseQuestions)
                {
                    answerLookup.TryGetValue(eq.QuestionId, out var answer);
                    var question = eq.Question;

                    var (isCorrect, needsManual) = AnswerGrading.GradeAnswer(question, answer);

                    // Use eq.Score when set, otherwise the even split.
                    var maxScore = eq.Score > 0 ? eq.Score : scorePerQuestion;
                    var pointsEarned = isCorrect ? maxScore : 0;

                    if (isCorrect)
                    {
                        totalScore += pointsEarned;
                        correctAnswers++;
                    }
                    else if (answer != null && !needsManual)
                    {
                        wrongAnswers++;
                    }

                    if (needsManual) hasPendingManualGrading = true;

                    if (answer != null)
                    {
                        answer.IsCorrect = isCorrect;
                        answer.PointsEarned = pointsEarned;
                        answer.NeedsManualGrading = needsManual;
                        _answerRepository.Update(answer);
                    }

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
                        NeedsManualGrading = needsManual,
                        PointsEarned = pointsEarned,
                        MaxScores = maxScore,
                        Explanation = question.Explanation
                    });
                }

                // 6. Update the attempt
                attempt.TotalScore = totalScore;
                attempt.CorrectAnswers = correctAnswers;
                attempt.WrongAnswers = wrongAnswers;
                attempt.CompletionPercentage = attempt.MaxScore > 0
                    ? (decimal)(totalScore / attempt.MaxScore) * 100
                    : 0;
                attempt.Status = isTimeout ? AttemptStatus.Timeout : AttemptStatus.Submitted;
                attempt.SubmittedAt = now;

                _attemptRepository.Update(attempt);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 7. Queue AI feedback for wrong (answered) questions — do NOT wait for it (A2-04).
                var wrongAnswerDetails = answerDetails
                    .Where(d => !d.IsCorrect && !d.NeedsManualGrading && d.StudentAnswer != null)
                    .ToList();

                foreach (var w in wrongAnswerDetails)
                    _aiFeedbackQueue.Enqueue(attempt.AttemptId, w.QuestionId, w.StudentAnswer);

                // P4 (A2-06): fold this attempt into NodeProgress + the activity snapshot.
                await _projection.ProjectAttemptAsync(attempt.AttemptId);

                // P6: low-score notification rule (never blocks the response).
                try { await _rules.OnExerciseCompletedAsync(attempt.AttemptId); }
                catch (Exception ex) { Console.WriteLine($"[Notify] low-score rule failed: {ex.Message}"); }

                // 8. Return the result immediately (AI fields fill in later via /result)
                var result = new ExerciseResultDto
                {
                    AttemptId = attempt.AttemptId,
                    StudentId = attempt.StudentId ?? 0,
                    StudentName = attempt.Student?.User?.FullName,
                    ExerciseName = attempt.Exercise?.ExerciseName,
                    Status = attempt.Status,
                    StartTime = attempt.StartTime,
                    SubmittedAt = now,
                    Duration = now - attempt.StartTime,
                    TotalScore = attempt.TotalScore,
                    MaxScore = attempt.MaxScore,
                    CompletionPercentage = attempt.CompletionPercentage,
                    CorrectAnswers = attempt.CorrectAnswers,
                    WrongAnswers = attempt.WrongAnswers,
                    TotalQuestions = exerciseQuestions.Count,
                    IsPassed = attempt.Exercise != null &&
                               attempt.TotalScore >= attempt.Exercise.PassingScore,
                    HasPendingManualGrading = hasPendingManualGrading,
                    AnswerDetails = answerDetails
                };

                return ApiResponse<ExerciseResultDto>.SuccessResponse(
                    result,
                    isTimeout ? "Exercise auto-submitted due to timeout" : "Exercise submitted successfully"
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
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
                // 1. Load the attempt
                var attempt = await _attemptRepository.GetAttemptWithDetailsAsync(attemptId);
                if (attempt == null)
                    return ApiResponse<ExerciseResultDto>.ErrorResponse("Không tìm thấy lượt làm bài");

                // Results are only available once the attempt has been submitted
                if (attempt.Status == AttemptStatus.InProgress)
                {
                    return ApiResponse<ExerciseResultDto>.ErrorResponse(
                        "Bài làm chưa được nộp, không thể xem kết quả"
                    );
                }

                // 2. Load the exercise's questions
                var exerciseQuestions = await _exerciseQuestionRepository.GetByExerciseIdAsync(attempt.ExerciseId);

                // 3. Load the student's answers
                var studentAnswers =
                    await _answerRepository.GetAttemptAnswersAsync(attemptId);

                var answerLookup = studentAnswers.ToDictionary(a => a.QuestionId);

                // Load AI feedback if any
                var aiFeedbacks = await _feedbackRepository.GetByAttemptAsync(attemptId);
                var feedbackLookup = aiFeedbacks.GroupBy(f => f.QuestionId).ToDictionary(g => g.Key, g => g.First());

                var answerDetails = new List<AnswerDetailDto>();

                // 4. Iterate the exercise's original question list
                foreach (var eq in exerciseQuestions)
                {
                    var question = eq.Question;
                    answerLookup.TryGetValue(question.QuestionId, out var answer);

                    bool isAnswered = answer != null;
                    bool isCorrect = isAnswered && answer!.IsCorrect;

                    answerDetails.Add(new AnswerDetailDto
                    {
                        QuestionId = question.QuestionId,
                        QuestionText = question.QuestionText,

                        StudentAnswer = !isAnswered
                            ? "Bạn chưa trả lời câu hỏi này"
                            : (answer.AnswerText ?? question.QuestionOptions?.FirstOrDefault(o => o.OptionId == answer.SelectedOptionId)?.OptionText),

                        CorrectAnswer = question.CorrectAnswer ??
                                        question.QuestionOptions?.FirstOrDefault(o => o.IsCorrect)?.OptionText,

                        IsCorrect = isCorrect,
                        NeedsManualGrading = isAnswered && answer!.NeedsManualGrading,
                        PointsEarned = isAnswered ? answer!.PointsEarned : 0,
                        MaxScores = eq.Score,
                        Explanation = question.Explanation,
                        
                        // AI feedback
                        FullSolution = feedbackLookup.TryGetValue(question.QuestionId, out var fb) ? fb.FullSolution : null,
                        MistakeAnalysis = fb?.MistakeAnalysis,
                        ImprovementAdvice = fb?.ImprovementAdvice
                    });
                }

                // 5. Map the result
                var result = new ExerciseResultDto
                {
                    AttemptId = attempt.AttemptId,
                    StudentId = attempt.StudentId ?? 0,
                    StudentName = attempt.Student?.User?.FullName,

                    ExerciseId = attempt.ExerciseId,
                    ExerciseName = attempt.Exercise?.ExerciseName,

                    Status = attempt.Status,

                    StartTime = attempt.StartTime,
                    SubmittedAt = attempt.SubmittedAt ?? DateTime.UtcNow,
                    Duration = attempt.SubmittedAt.HasValue
                        ? attempt.SubmittedAt.Value - attempt.StartTime
                        : TimeSpan.Zero,

                    TotalScore = attempt.TotalScore,
                    MaxScore = attempt.MaxScore,
                    CorrectAnswers = attempt.CorrectAnswers,
                    WrongAnswers = attempt.WrongAnswers,
                    TotalQuestions = exerciseQuestions.Count,
                    CompletionPercentage = attempt.CompletionPercentage,
                    HasPendingManualGrading = answerDetails.Any(d => d.NeedsManualGrading),
                    AnswerDetails = answerDetails
                };

                return ApiResponse<ExerciseResultDto>.SuccessResponse(result, "Lấy kết quả thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<ExerciseResultDto>.ErrorResponse("Lỗi hệ thống khi tính điểm", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<ExerciseResultDto>>> GetStudentHistoryAsync(int studentId)
        {
            try
            {
                var attempts = await _attemptRepository.GetStudentAttemptsAsync(studentId);

                var results = attempts
                    .Where(a => a.Status != AttemptStatus.InProgress)
                    .Select(a => new ExerciseResultDto
                    {
                        AttemptId = a.AttemptId,
                        StudentId = a.StudentId ?? 0,
                        StudentName = a.Student?.User?.FullName,
                        ExerciseId = a.ExerciseId,
                        ExerciseName = a.Exercise?.ExerciseName,
                        StartTime = a.StartTime,
                        SubmittedAt = a.SubmittedAt.Value,
                        Duration = a.SubmittedAt.Value - a.StartTime,
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

        public async Task<ApiResponse<bool>> SaveAnswerAsync(SaveAnswerDto dto)
        {
            try
            {
                var attempt = await _attemptRepository.GetAttemptByIdAsync(dto.AttemptId);

                if (attempt == null)
                {
                    return ApiResponse<bool>.ErrorResponse(
                        "Attempt not found",
                        new List<string> { $"No attempt found with ID: {dto.AttemptId}" }
                    );
                }

                // No saving once the attempt has been submitted
                if (attempt.Status != AttemptStatus.InProgress)
                {
                    return ApiResponse<bool>.ErrorResponse(
                        "Attempt is not active",
                        new List<string> { "Cannot save answer for completed attempt" }
                    );
                }

                // Reject saves once the time limit has passed (null = no limit).
                if (attempt.PlannedEndTime.HasValue && attempt.PlannedEndTime.Value <= DateTime.UtcNow)
                {
                    return ApiResponse<bool>.ErrorResponse(
                        "Time is up",
                        new List<string> { "Exam time has expired" }
                    );
                }

                var existingAnswer = await _answerRepository.GetAnswerAsync(
                    dto.AttemptId,
                    dto.QuestionId
                );

                if (existingAnswer != null)
                {
                    // Update (autosave)
                    existingAnswer.AnswerText = dto.AnswerText;
                    existingAnswer.SelectedOptionId = dto.SelectedOptionId;
                    existingAnswer.AnsweredAt = DateTime.UtcNow;

                    await _answerRepository.UpdateAnswerAsync(existingAnswer);
                }
                else
                {
                    // Insert
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
                    "Answer saved"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(
                    "Error saving answer",
                    new List<string> { ex.Message, ex.InnerException?.Message ?? "no inner exception" }
                );
            }
        }

        public async Task<ApiResponse<ExerciseAttemptDto>> StartExerciseAsync(StartExerciseDto dto)
        {
            try
            {
                var user = await _userRepository.GetUserByStudentIdAsync(dto.StudentId);
                if (user == null) 
                    return ApiResponse<ExerciseAttemptDto>.ErrorResponse($"Không tìm thấy học sinh với Id: ${dto.StudentId}");

                var now = DateTime.UtcNow;

                // 1. RESUME: return an in-progress attempt so the student can carry on.
                var existingAttempt = await _context.ExerciseAttempts
                    .Where(a => a.StudentId == dto.StudentId && a.ExerciseId == dto.ExerciseId && a.Status == AttemptStatus.InProgress)
                    .OrderByDescending(a => a.StartTime)
                    .FirstOrDefaultAsync();

                if (existingAttempt != null)
                {
                    if (existingAttempt.PlannedEndTime == null || now < existingAttempt.PlannedEndTime.Value)
                    {
                        var exerciseInfo = await _exerciseRepository.GetExerciseWithQuestionsAsync(dto.ExerciseId);

                        var resumeDto =
                            MapToAttemptDto(existingAttempt, exerciseInfo);

                        return ApiResponse<ExerciseAttemptDto>
                            .SuccessResponse(resumeDto, "Resuming your previous attempt");
                    }

                    // Time is up -> mark it as timed out.
                    existingAttempt.Status = AttemptStatus.Timeout;
                    existingAttempt.SubmittedAt = existingAttempt.PlannedEndTime;
                    await _context.SaveChangesAsync();
                }

                // 2. CREATE A NEW ATTEMPT (only when there is no in-progress one)
                var exercise = await _exerciseRepository.GetExerciseWithQuestionsAsync(dto.ExerciseId);
                if (exercise == null) return ApiResponse<ExerciseAttemptDto>.ErrorResponse("Exercise not found");

                if (!exercise.IsActive || exercise.Status != ExerciseStatus.Published)
                {
                    return ApiResponse<ExerciseAttemptDto>.ErrorResponse("This exercise is not available.");
                }

                // A2-08: enforce the access tier (a free exercise is always allowed).
                if (!exercise.IsFree && exercise.RequiredTier != AccessTier.Free)
                {
                    var studentTier = await GetStudentTierAsync(dto.StudentId);
                    if ((int)studentTier < (int)exercise.RequiredTier)
                        return ApiResponse<ExerciseAttemptDto>.ErrorResponse(
                            $"This exercise requires the {exercise.RequiredTier} package");
                }

                // A2-08: enforce MaxAttempts (null = unlimited).
                if (exercise.MaxAttempts.HasValue)
                {
                    var used = await _context.ExerciseAttempts.CountAsync(a =>
                        a.StudentId == dto.StudentId && a.ExerciseId == dto.ExerciseId
                        && a.Status != AttemptStatus.InProgress);

                    if (used >= exercise.MaxAttempts.Value)
                        return ApiResponse<ExerciseAttemptDto>.ErrorResponse(
                            "You have used all attempts for this exercise");
                }

                var startTime = DateTime.UtcNow;

                var attempt = new ExerciseAttempt
                {
                    StudentId = dto.StudentId,
                    ExerciseId = dto.ExerciseId,
                    StartTime = startTime,
                    PlannedEndTime = exercise.DurationMinutes.HasValue
                        ? startTime.AddMinutes(exercise.DurationMinutes.Value)
                        : (DateTime?)null,
                    MaxScore = exercise.TotalScores,
                    Status = AttemptStatus.InProgress
                };

                var createdAttempt = await _attemptRepository.CreateAttemptAsync(attempt);
                var attemptDto = MapToAttemptDto(createdAttempt, exercise);

                return ApiResponse<ExerciseAttemptDto>.SuccessResponse(attemptDto, "Bắt đầu bài thi mới");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL ERROR] StartExercise: {ex.Message}");
                return ApiResponse<ExerciseAttemptDto>.ErrorResponse("Lỗi khởi tạo: " + ex.Message);
            }
        }

        public async Task<ApiResponse<ExerciseAttemptDto>> StartRandomExerciseAsync(StartRandomExerciseDto dto)
        {
            try
            {
                // Pull random questions from the question bank
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

                // Create a throwaway exercise for this random session
                var exercise = new Exercise
                {
                    ExerciseName = $"Random {dto.ExerciseType} - {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                    NodeId = questionBank.PrimaryNodeId,
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

                // Create the ExerciseQuestion rows (these hold the per-question score)
                var exerciseQuestions = questions.Select((q, index) => new ExerciseQuestion
                {
                    ExerciseId = exercise.ExerciseId,
                    QuestionId = q.QuestionId,
                    Score = dto.MaxScore / questions.Count,   // even split
                    OrderIndex = index + 1
                }).ToList();

                await _exerciseQuestionRepository.AddRangeAsync(exerciseQuestions);

                // Create the attempt (A2-03: set PlannedEndTime + Status)
                var randomStart = DateTime.UtcNow;
                var attempt = new ExerciseAttempt
                {
                    StudentId = dto.StudentId,
                    ExerciseId = exercise.ExerciseId,
                    StartTime = randomStart,
                    PlannedEndTime = dto.DurationMinutes.HasValue
                        ? randomStart.AddMinutes(dto.DurationMinutes.Value)
                        : (DateTime?)null,
                    MaxScore = dto.MaxScore,
                    Status = AttemptStatus.InProgress
                };

                var createdAttempt = await _attemptRepository.CreateAttemptAsync(attempt);

                var scoreLookup = exerciseQuestions.ToDictionary(
                    eq => eq.QuestionId,
                    eq => eq.Score
                );

                // Map to the DTO with its questions
                var attemptDto = new ExerciseAttemptDto
                {
                    AttemptId = createdAttempt.AttemptId,
                    StudentId = createdAttempt.StudentId ?? 0,
                    ExerciseId = createdAttempt.ExerciseId,
                    ExerciseName = exercise.ExerciseName,
                    ExerciseType = exercise.ExerciseType,
                    StartTime = createdAttempt.StartTime,
                    PlannedEndTime = createdAttempt.PlannedEndTime,
                    Status = createdAttempt.Status,
                    TotalQuestions = questions.Count,
                    Questions = questions.Select(q => new QuestionInAttemptDto
                    {
                        QuestionId = q.QuestionId,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
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

        public async Task<ApiResponse<FeedbackStatusDto>> GetFeedbackStatusAsync(int attemptId)
        {
            try
            {
                var answers = await _answerRepository.GetAttemptAnswersAsync(attemptId);
                var wrong = answers.Count(a => !a.IsCorrect && !a.NeedsManualGrading
                                               && (a.AnswerText != null || a.SelectedOptionId != null));

                var feedbacks = await _feedbackRepository.GetByAttemptAsync(attemptId);
                var ready = feedbacks.Select(f => f.QuestionId).Distinct().Count();

                return ApiResponse<FeedbackStatusDto>.SuccessResponse(new FeedbackStatusDto
                {
                    TotalWrong = wrong,
                    Ready = ready,
                    Pending = Math.Max(0, wrong - ready)
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<FeedbackStatusDto>.ErrorResponse("Error reading feedback status", new List<string> { ex.Message });
            }
        }

        private ExerciseAttemptDto MapToAttemptDto(ExerciseAttempt attempt, Exercise exercise)
        {
            var questionsDto = new List<QuestionInAttemptDto>();

            if (exercise.ExerciseQuestions != null)
            {
                foreach (var eq in exercise.ExerciseQuestions)
                {
                    if (eq.Question != null)
                    {
                        questionsDto.Add(new QuestionInAttemptDto
                        {
                            QuestionId = eq.Question.QuestionId,
                            QuestionText = eq.Question.QuestionText,
                            QuestionType = eq.Question.QuestionType,
                            Score = eq.Score,
                            ImageUrl = eq.Question.QuestionImageUrl,
                            Options = eq.Question.QuestionOptions?.Select(o => new AnswerOptionDto
                            {
                                OptionId = o.OptionId,
                                OptionText = o.OptionText,
                                ImageUrl = o.ImageUrl,
                                // Never send IsCorrect to the client (anti-cheat)
                            }).ToList() ?? new List<AnswerOptionDto>()
                        });
                    }
                }
            }

            return new ExerciseAttemptDto
            {
                AttemptId = attempt.AttemptId,
                StudentId = attempt.StudentId ?? 0,
                ExerciseId = attempt.ExerciseId,
                ExerciseName = exercise.ExerciseName ?? "Không tên",
                ExerciseType = exercise.ExerciseType,
                StartTime = attempt.StartTime,
                PlannedEndTime = attempt.PlannedEndTime,
                SubmittedAt = attempt.SubmittedAt,
                Status = attempt.Status,
                TotalQuestions = questionsDto.Count,
                Questions = questionsDto
            };
        }
        public async Task<ApiResponse<StudentDashboardDto>> GetDashboardStatsAsync(int userId)
        {
            try
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (student == null) return ApiResponse<StudentDashboardDto>.ErrorResponse("Không tìm thấy học sinh.");

                // 1. Chapters = Chapter-type ContentNodes in the CourseVersions the student is enrolled in.
                var versionIds = await _context.StudentCourses
                    .Where(sc => sc.StudentId == student.StudentId)
                    .Select(sc => sc.CourseVersionId)
                    .ToListAsync();

                var allChapters = await _context.ContentNodes
                    .Where(n => n.NodeType == NodeType.Chapter && versionIds.Contains(n.CourseVersionId))
                    .OrderBy(n => n.OrderIndex)
                    .Select(n => new { n.NodeId, n.Title, Prefix = n.MaterializedPath })
                    .ToListAsync();

                // 2. Load the student's attempt history
                var attempts = await _context.ExerciseAttempts
                    .Include(a => a.Exercise).ThenInclude(e => e!.Node)
                    .Where(a => a.StudentId == student.StudentId && a.Status != AttemptStatus.InProgress)
                    .ToListAsync();

                // A2-14: load every exercise of the enrolled versions once, then bucket in memory.
                var courseExercises = await _context.Exercises
                    .Where(e => e.Node != null && versionIds.Contains(e.Node.CourseVersionId))
                    .Select(e => new { e.ExerciseId, e.NodeId, Path = e.Node!.MaterializedPath })
                    .ToListAsync();

                var stats = new StudentDashboardDto();
                stats.TotalAttempts = attempts.Count;
                stats.AverageScore = attempts.Any() ? Math.Round(attempts.Average(a => a.TotalScore), 1) : 0;

                foreach (var ch in allChapters)
                {
                    var exInChapter = courseExercises
                        .Where(e => e.NodeId == ch.NodeId || e.Path.StartsWith(ch.Prefix))
                        .Select(e => e.ExerciseId)
                        .ToHashSet();

                    int totalExercisesInChapter = exInChapter.Count;
                    int completedInChapter = attempts
                        .Where(a => exInChapter.Contains(a.ExerciseId))
                        .Select(a => a.ExerciseId).Distinct().Count();

                    int progress = totalExercisesInChapter > 0
                        ? (int)((double)completedInChapter / totalExercisesInChapter * 100) : 0;

                    stats.Chapters.Add(new ChapterProgressDto
                    {
                        ChapterId = ch.NodeId,
                        ChapterName = ch.Title,
                        TotalLessons = totalExercisesInChapter,
                        ProgressPercentage = progress > 100 ? 100 : progress
                    });
                }

                stats.CompletedChapters = stats.Chapters.Count(x => x.ProgressPercentage == 100);

                stats.ChartData = attempts
                    .Where(a => a.Exercise?.NodeId != null)
                    .GroupBy(a => a.Exercise!.NodeId!.Value)
                    .Select(g => new ScoreChartItemDto
                    {
                        ChapterName = stats.Chapters.FirstOrDefault(c => c.ChapterId == g.Key)?.ChapterName ?? "Node " + g.Key,
                        AvgScore = Math.Round(g.Average(a => a.TotalScore), 1)
                    }).ToList();

                stats.RecentAttempts = attempts.OrderByDescending(a => a.StartTime).Take(5).Select(a => new ExerciseAttemptDto
                {
                    AttemptId = a.AttemptId,
                    ExerciseName = a.Exercise?.ExerciseName ?? "Bài tập",
                    //Score = a.TotalScore,
                    StartTime = a.StartTime
                }).ToList();

                return ApiResponse<StudentDashboardDto>.SuccessResponse(stats, "Thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<StudentDashboardDto>.ErrorResponse(ex.Message);
            }
        }
        /// <summary>Current access tier of a student, derived from their active subscription (default Free).</summary>
        private async Task<AccessTier> GetStudentTierAsync(int studentId)
        {
            var now = DateTime.UtcNow;
            var tier = await _context.Subscriptions
                .Where(s => s.StudentId == studentId
                            && s.Status == SubscriptionStatus.Active
                            && s.StartDate <= now && s.EndDate > now)
                .Include(s => s.Package)
                .Select(s => (PackageTier?)s.Package!.Tier)
                .OrderByDescending(t => t)
                .FirstOrDefaultAsync();

            return tier switch
            {
                PackageTier.Premium or PackageTier.Yearly => AccessTier.Premium,
                PackageTier.Standard => AccessTier.Standard,
                _ => AccessTier.Free
            };
        }

        // Ignore repeat tab-switch reports fired within this window (browsers can fire several).
        private static readonly TimeSpan TabSwitchDebounce = TimeSpan.FromSeconds(15);
        // Stop emailing parents after this many switches in one attempt (log still records them all).
        private const int TabSwitchEmailCap = 5;

        public async Task<ApiResponse<bool>> ReportTabSwitchAsync(int attemptId)
        {
            try
            {
                // Load attempt with Student → User and StudentParents → Parent → User
                var attempt = await _context.ExerciseAttempts
                    .Include(a => a.Exercise)
                    .Include(a => a.Student)
                        .ThenInclude(s => s.User)
                    .Include(a => a.Student)
                        .ThenInclude(s => s.ParentLinks)
                            .ThenInclude(sp => sp.Parent)
                                .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(a => a.AttemptId == attemptId);

                if (attempt == null)
                    return ApiResponse<bool>.ErrorResponse("Không tìm thấy lượt làm bài.");

                if (attempt.Status != AttemptStatus.InProgress)
                    return ApiResponse<bool>.ErrorResponse("Bài thi đã kết thúc.");

                var studentName = attempt.Student?.User?.FullName ?? "Học sinh";
                var exerciseName = attempt.Exercise?.ExerciseName ?? "Bài kiểm tra";
                var switchedAt = DateTime.UtcNow;

                // Debounce: swallow bursts of reports for the same attempt.
                var lastSwitch = await _context.TabSwitchLogs
                    .Where(l => l.AttemptId == attemptId)
                    .OrderByDescending(l => l.SwitchedAt)
                    .Select(l => (DateTime?)l.SwitchedAt)
                    .FirstOrDefaultAsync();

                if (lastSwitch.HasValue && switchedAt - lastSwitch.Value < TabSwitchDebounce)
                    return ApiResponse<bool>.SuccessResponse(true, "Đã ghi nhận (bỏ qua báo cáo trùng).");

                // Persist the violation
                var log = new TabSwitchLog { AttemptId = attemptId, SwitchedAt = switchedAt };
                _context.TabSwitchLogs.Add(log);
                await _context.SaveChangesAsync();

                // Count how many times it has happened
                var switchCount = await _context.TabSwitchLogs.CountAsync(l => l.AttemptId == attemptId);

                // P6: in-app notification for student + parents (respects opt-outs).
                try { await _rules.OnTabSwitchAsync(attemptId, switchCount); }
                catch (Exception ex) { Console.WriteLine($"[Notify] tab-switch rule failed: {ex.Message}"); }

                // Stop emailing parents once the cap is reached (the log still grows).
                if (switchCount > TabSwitchEmailCap)
                    return ApiResponse<bool>.SuccessResponse(true, "Đã ghi nhận (đã đạt giới hạn gửi email).");

                var parents = attempt.Student?.ParentLinks
                    ?.Where(sp => sp.Status == LinkStatus.Active)
                    .Select(sp => sp.Parent)
                    .Where(p => p?.User?.Email != null)
                    .ToList() ?? new List<Parent>();

                // P6: parent emails go to the background queue — no longer awaited in the request.
                foreach (var p in parents)
                    _backgroundEmail.QueueTabSwitchEmail(
                        p.User!.Email, p.User.FullName, studentName, exerciseName, switchedAt, switchCount);

                return ApiResponse<bool>.SuccessResponse(true, "Đã ghi nhận và thông báo cho phụ huynh.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportTabSwitch Error] {ex.Message}");
                return ApiResponse<bool>.ErrorResponse("Lỗi khi gửi thông báo: " + ex.Message);
            }
        }

        public async Task<ApiResponse<List<DateTime>>> GetTabSwitchLogsAsync(int attemptId)
        {
            try
            {
                var logs = await _context.TabSwitchLogs
                    .Where(l => l.AttemptId == attemptId)
                    .OrderBy(l => l.SwitchedAt)
                    .Select(l => l.SwitchedAt)
                    .ToListAsync();

                return ApiResponse<List<DateTime>>.SuccessResponse(logs, "Thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<DateTime>>.ErrorResponse("Lỗi truy xuất lịch sử: " + ex.Message);
            }
        }
    }
}
