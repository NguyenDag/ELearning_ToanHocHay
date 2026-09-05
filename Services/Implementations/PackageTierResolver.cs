using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class PackageTierResolver : IPackageTierResolver
    {
        private readonly AppDbContext _context;
        private readonly TimeProvider _clock;

        public PackageTierResolver(AppDbContext context, TimeProvider clock)
        {
            _context = context;
            _clock = clock;
        }

        public async Task<PackageTier> ResolveAsync(int studentId)
        {
            var now = _clock.GetUtcNow().UtcDateTime;

            var tier = await _context.Subscriptions
                .Where(s => s.StudentId == studentId && s.Status == SubscriptionStatus.Active && s.EndDate > now)
                .Include(s => s.Package)
                .OrderByDescending(s => s.Package!.Tier)
                .ThenByDescending(s => s.EndDate)
                .Select(s => (PackageTier?)s.Package!.Tier)
                .FirstOrDefaultAsync();

            return tier ?? PackageTier.Free;
        }
    }
}
