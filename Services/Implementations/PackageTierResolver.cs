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

            // Tier persist dưới dạng string (HasConversion<string>) nên không thể ORDER BY ở DB —
            // nạp các tier đang hiệu lực rồi lấy max theo thứ tự enum (Free < Standard < Premium < Yearly).
            var tiers = await _context.Subscriptions
                .Where(s => s.StudentId == studentId && s.Status == SubscriptionStatus.Active && s.EndDate > now)
                .Select(s => s.Package!.Tier)
                .ToListAsync();

            return tiers.Count == 0 ? PackageTier.Free : tiers.Max();
        }
    }
}
