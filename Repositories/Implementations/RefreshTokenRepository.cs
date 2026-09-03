using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<RefreshToken?> GetByHashAsync(string tokenHash)
            => _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        public async Task AddAsync(RefreshToken token)
        {
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();
        }

        public async Task<int> RevokeAllForUserAsync(int userId, string? reason = null)
        {
            var now = DateTime.UtcNow;
            var active = await _context.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
                .ToListAsync();

            foreach (var t in active)
                t.RevokedAt = now;

            await _context.SaveChangesAsync();
            return active.Count;
        }

        public Task SaveAsync() => _context.SaveChangesAsync();
    }
}
