using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class DailyProgressJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyProgressJob> _logger;

        public DailyProgressJob(IServiceProvider serviceProvider, ILogger<DailyProgressJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Initial delay to align to 2:00 AM
            var now = DateTime.Now;
            var nextRun = now.Date.AddDays(1).AddHours(2);
            if (now.Hour < 2)
            {
                nextRun = now.Date.AddHours(2);
            }

            var initialDelay = nextRun - now;
            _logger.LogInformation("DailyProgressJob will start in {InitialDelay}", initialDelay);

            // Wait until 2:00 AM for the first run, or just run immediately in dev environment if configured (but sticking to real delay here)
            // To prevent blocking if a short app lifecycle, we'll loop
            try
            {
                await Task.Delay(initialDelay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting DailyProgressJob execution");

                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
                    await CalculateDailyRatioAsync(dbContext, yesterday, stoppingToken);

                    // We can also perform SkillProgress calculation here for newly created skills or regular updates

                    _logger.LogInformation("Completed DailyProgressJob execution");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing DailyProgressJob");
                }

                // Wait 24 hours
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task CalculateDailyRatioAsync(AppDbContext dbContext, DateOnly date, CancellationToken ct)
        {
            var dateStart = date.ToDateTime(TimeOnly.MinValue);
            var dateEnd = dateStart.AddDays(1);

            // Calculate ratios from attempts for yesterday
            var attemptsByStudent = await dbContext.ExerciseAttempts
                .Where(a => a.Status != AttemptStatus.InProgress && a.MaxScore > 0
                            && a.StartTime >= dateStart && a.StartTime < dateEnd)
                .GroupBy(a => a.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    AverageRatio = g.Average(a => a.TotalScore / a.MaxScore)
                })
                .ToListAsync(ct);

            foreach (var record in attemptsByStudent)
            {
                var snapshot = await dbContext.DailyActivitySnapshots
                    .FirstOrDefaultAsync(s => s.StudentId == record.StudentId && s.Date == date, ct);

                if (snapshot != null)
                {
                    // Update average ratio (Phase 3 requirement)
                    snapshot.AverageRatio = Math.Round((decimal)record.AverageRatio, 4);
                }
            }

            await dbContext.SaveChangesAsync(ct);
        }
    }
}
