using System.Collections.Concurrent;
using ELearning_ToanHocHay_Control.Models.DTOs.AIFeedback;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    /// <summary>
    /// Generates AI feedback for wrong answers in the background (A2-04).
    /// Mirrors <see cref="BackgroundEmailService"/>.
    /// </summary>
    public class AiFeedbackBackgroundService : BackgroundService, IAiFeedbackQueue
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AiFeedbackBackgroundService> _logger;
        private readonly ConcurrentQueue<FeedbackJob> _queue = new();
        private readonly SemaphoreSlim _signal = new(0);

        public AiFeedbackBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<AiFeedbackBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void Enqueue(int attemptId, int questionId, string? studentAnswer)
        {
            _queue.Enqueue(new FeedbackJob(attemptId, questionId, studentAnswer));
            _signal.Release();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _signal.WaitAsync(stoppingToken);

                if (!_queue.TryDequeue(out var job)) continue;

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var feedbackService = scope.ServiceProvider.GetRequiredService<IAIFeedbackService>();

                    var result = await feedbackService.CreateAsync(new CreateAIFeedbackDto
                    {
                        AttemptId = job.AttemptId,
                        QuestionId = job.QuestionId,
                        StudentAnswer = job.StudentAnswer
                    });

                    if (!result.Success)
                        _logger.LogWarning("AI feedback failed for attempt {AttemptId} question {QuestionId}: {Message}",
                            job.AttemptId, job.QuestionId, result.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AI feedback job crashed for attempt {AttemptId} question {QuestionId}",
                        job.AttemptId, job.QuestionId);
                }
            }
        }

        private record FeedbackJob(int AttemptId, int QuestionId, string? StudentAnswer);
    }
}
