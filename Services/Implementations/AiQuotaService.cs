using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class AiQuotaService : IAiQuotaService
    {
        private readonly AppDbContext _context;
        private readonly IPackageRepository _packageRepo;
        private readonly ISystemConfigService _config;
        private readonly int _freeDailyHintFallback;

        public AiQuotaService(
            AppDbContext context, IPackageRepository packageRepo, ISystemConfigService config, IConfiguration appConfig)
        {
            _context = context;
            _packageRepo = packageRepo;
            _config = config;
            _freeDailyHintFallback = int.TryParse(appConfig["AI:FreeDailyHintLimit"], out var n) ? n : 3;
        }

        public async Task<QuotaCheck> PeekHintAsync(int studentId)
        {
            var (limit, unlimited) = await ResolveHintLimitAsync(studentId);
            var used = await TodayHintCountAsync(studentId);
            return new QuotaCheck(unlimited || used < limit, used, limit, unlimited);
        }

        public async Task<QuotaCheck> TryConsumeHintAsync(int studentId)
        {
            var (limit, unlimited) = await ResolveHintLimitAsync(studentId);
            var row = await GetOrCreateTodayAsync(studentId);

            if (!unlimited && row.HintCount >= limit)
                return new QuotaCheck(false, row.HintCount, limit, false);

            row.HintCount += 1;
            await _context.SaveChangesAsync();
            return new QuotaCheck(true, row.HintCount, limit, unlimited);
        }

        public async Task RecordFeedbackAsync(int studentId)
        {
            var row = await GetOrCreateTodayAsync(studentId);
            row.FeedbackCount += 1;
            await _context.SaveChangesAsync();
        }

        public async Task RecordChatAsync(int studentId)
        {
            var row = await GetOrCreateTodayAsync(studentId);
            row.ChatCount += 1;
            await _context.SaveChangesAsync();
        }

        // ---- helpers ----
        private async Task<(int Limit, bool Unlimited)> ResolveHintLimitAsync(int studentId)
        {
            var sub = await _packageRepo.GetActivePackageAsync(studentId);
            var package = sub?.Package;

            if (package == null)
            {
                var freeLimit = await _config.GetIntAsync("ai.hint.dailyLimitFreeTier", _freeDailyHintFallback);
                return (freeLimit, false); // Free tier
            }

            if (package.UnlimitedAiHint)
                return (int.MaxValue, true);

            return (package.AiHintLimitDaily ?? 0, false);
        }

        private Task<int> TodayHintCountAsync(int studentId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return _context.AiUsageDailies
                .Where(u => u.StudentId == studentId && u.Date == today)
                .Select(u => u.HintCount)
                .FirstOrDefaultAsync();
        }

        private async Task<AiUsageDaily> GetOrCreateTodayAsync(int studentId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var row = await _context.AiUsageDailies
                .FirstOrDefaultAsync(u => u.StudentId == studentId && u.Date == today);

            if (row == null)
            {
                row = new AiUsageDaily { StudentId = studentId, Date = today };
                _context.AiUsageDailies.Add(row);
            }
            return row;
        }
    }
}
