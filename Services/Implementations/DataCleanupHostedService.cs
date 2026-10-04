using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class DataCleanupHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataCleanupHostedService> _logger;

        public DataCleanupHostedService(IServiceProvider serviceProvider, ILogger<DataCleanupHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Delay start to allow app initialization
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting scheduled data cleanup...");

                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var now = DateTime.UtcNow;

                    // Delete attempt cache > 48h
                    var cutoffAttempts = now.AddHours(-48);

                    var oldAttempts = await dbContext.ExerciseAttempts
                        .Where(a => a.Status == AttemptStatus.InProgress && a.StartTime < cutoffAttempts)
                        .ToListAsync(stoppingToken);

                    if (oldAttempts.Count > 0)
                    {
                        // Delete related answers first (Cascade delete might not be enabled or handles this, but let's just delete the attempts)
                        // If DB handles cascading delete, this is fine.
                        dbContext.ExerciseAttempts.RemoveRange(oldAttempts);
                    }

                    // Delete password reset tokens > 2h
                    var cutoffTokens = now.AddHours(-2);
                    var oldTokens = await dbContext.PasswordResetTokens
                        .Where(t => t.ExpiredAt < cutoffTokens)
                        .ToListAsync(stoppingToken);

                    if (oldTokens.Count > 0)
                    {
                        dbContext.PasswordResetTokens.RemoveRange(oldTokens);
                    }

                    int deleted = await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"Data cleanup completed. Deleted {deleted} records.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurring during data cleanup.");
                }

                // Run once a day
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}
