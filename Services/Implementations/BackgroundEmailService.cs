using System.Collections.Concurrent;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class BackgroundEmailService : BackgroundService, IBackgroundEmailService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentQueue<EmailJob> _emailQueue = new();
        private readonly SemaphoreSlim _signal = new(0);

        public BackgroundEmailService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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

                try
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
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send email: {ex.Message}");
                }
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
