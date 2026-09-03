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
            Console.WriteLine("📥 Confirmation email queued");
            _emailQueue.Enqueue(new EmailJob(EmailKind.Confirmation, toEmail, fullName, confirmLink));
            _signal.Release();
        }

        public void QueuePasswordResetEmail(string toEmail, string fullName, string resetLink)
        {
            Console.WriteLine("📥 Password-reset email queued");
            _emailQueue.Enqueue(new EmailJob(EmailKind.PasswordReset, toEmail, fullName, resetLink));
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
                            await emailService.SendConfirmEmailAsync(job.ToEmail, job.FullName, job.Link);
                            break;
                        case EmailKind.PasswordReset:
                            await emailService.SendPasswordResetEmailAsync(job.ToEmail, job.FullName, job.Link);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send email: {ex.Message}");
                }
            }
        }

        private enum EmailKind { Confirmation, PasswordReset }

        private sealed record EmailJob(EmailKind Kind, string ToEmail, string FullName, string Link);
    }
}
