using System.Collections.Concurrent;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class BackgroundEmailService : BackgroundService, IBackgroundEmailService
    {
        // B5 — số lần thử và độ trễ tăng dần giữa các lần (giây).
        private static readonly TimeSpan[] RetryBackoff =
        {
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(60),
        };

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundEmailService> _logger;
        private readonly ConcurrentQueue<EmailJob> _emailQueue = new();
        private readonly SemaphoreSlim _signal = new(0);

        public BackgroundEmailService(
            IServiceProvider serviceProvider,
            ILogger<BackgroundEmailService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void QueueConfirmationEmail(string toEmail, string fullName, string confirmLink)
        {
            _emailQueue.Enqueue(new EmailJob(EmailKind.Confirmation, toEmail, fullName) { Link = confirmLink });
            _signal.Release();
        }

        public void QueuePasswordResetEmail(string toEmail, string fullName, string resetLink)
        {
            _emailQueue.Enqueue(new EmailJob(EmailKind.PasswordReset, toEmail, fullName) { Link = resetLink });
            _signal.Release();
        }

        public void QueueTabSwitchEmail(
            string toEmail, string parentName, string studentName, string exerciseName,
            DateTime switchedAt, int switchCount)
        {
            _emailQueue.Enqueue(new EmailJob(EmailKind.TabSwitch, toEmail, parentName)
            {
                StudentName = studentName,
                ExerciseName = exerciseName,
                SwitchedAt = switchedAt,
                SwitchCount = switchCount
            });
            _signal.Release();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _signal.WaitAsync(stoppingToken);

                if (!_emailQueue.TryDequeue(out var job)) continue;

                await ProcessWithRetryAsync(job, stoppingToken);
            }
        }

        // B5 — thử lại có backoff; nếu vẫn thất bại thì log ở mức Error để còn lần ra được.
        private async Task ProcessWithRetryAsync(EmailJob job, CancellationToken stoppingToken)
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    await SendAsync(job);
                    if (attempt > 0)
                        _logger.LogInformation(
                            "✅ Email {Kind} tới {Email} gửi thành công ở lần thử {Attempt}",
                            job.Kind, job.ToEmail, attempt + 1);
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt >= RetryBackoff.Length)
                    {
                        _logger.LogError(ex,
                            "❌ Email {Kind} tới {Email} thất bại sau {Attempts} lần thử — bỏ cuộc. " +
                            "Người dùng có thể tự yêu cầu gửi lại.",
                            job.Kind, job.ToEmail, attempt + 1);
                        return;
                    }

                    var delay = RetryBackoff[attempt];
                    _logger.LogWarning(ex,
                        "⚠️ Email {Kind} tới {Email} lỗi (lần {Attempt}/{Max}). Thử lại sau {Delay}s.",
                        job.Kind, job.ToEmail, attempt + 1, RetryBackoff.Length + 1, delay.TotalSeconds);

                    try { await Task.Delay(delay, stoppingToken); }
                    catch (OperationCanceledException) { return; }
                }
            }
        }

        private async Task SendAsync(EmailJob job)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            switch (job.Kind)
            {
                case EmailKind.Confirmation:
                    await emailService.SendConfirmEmailAsync(job.ToEmail, job.Name, job.Link!);
                    break;
                case EmailKind.PasswordReset:
                    await emailService.SendPasswordResetEmailAsync(job.ToEmail, job.Name, job.Link!);
                    break;
                case EmailKind.TabSwitch:
                    await emailService.SendTabSwitchNotificationAsync(
                        job.ToEmail, job.Name, job.StudentName!, job.ExerciseName!,
                        job.SwitchedAt, job.SwitchCount);
                    break;
            }
        }

        private enum EmailKind { Confirmation, PasswordReset, TabSwitch }

        private sealed class EmailJob
        {
            public EmailJob(EmailKind kind, string toEmail, string name)
            {
                Kind = kind;
                ToEmail = toEmail;
                Name = name;
            }

            public EmailKind Kind { get; }
            public string ToEmail { get; }
            public string Name { get; }
            public string? Link { get; init; }
            public string? StudentName { get; init; }
            public string? ExerciseName { get; init; }
            public DateTime SwitchedAt { get; init; }
            public int SwitchCount { get; init; }
        }
    }
}
