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
            _emailQueue.Enqueue(new EmailJob
            {
                ToEmail = toEmail,
                FullName = fullName,
                ConfirmLink = confirmLink
            });
            _signal.Release();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _signal.WaitAsync(stoppingToken);

                if (_emailQueue.TryDequeue(out var job))
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        await emailService.SendConfirmEmailAsync(
                            job.ToEmail,
                            job.FullName,
                            job.ConfirmLink
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send email: {ex.Message}");
                        // Có thể thêm retry logic ở đây
                    }
                }
            }
        }
        private class EmailJob
        {
            public string ToEmail { get; set; }
            public string FullName { get; set; }
            public string ConfirmLink { get; set; }
        }
    }
}


