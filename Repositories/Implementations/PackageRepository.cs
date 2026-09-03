using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class PackageRepository : IPackageRepository
    {
        private readonly AppDbContext _context;

        public PackageRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<Package>> GetAllAsync()
        {
            return await _context.Packages
                                 .Where(x => x.IsActive)
                                 .OrderBy(x => x.Price)
                                 .ToListAsync();
        }

        public async Task<Package?> GetByIdAsync(int id)
        {
            return await _context.Packages.FirstOrDefaultAsync(x => x.PackageId == id);
        }

        public async Task AddAsync(Package package)
        {
            _context.Packages.Add(package);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Package package)
        {
            _context.Packages.Update(package);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Package package)
        {
            package.IsActive = false;
            await _context.SaveChangesAsync();
        }

        public async Task<Subscription?> GetActivePackageAsync(int studentId)
        {
            // A2-11 — consistent tie-break: highest tier, then latest expiry.
            return await _context.Subscriptions
            .AsNoTracking()
            .Include(s => s.Package)
            .Where(s => s.StudentId == studentId &&
                       s.Status == SubscriptionStatus.Active &&
                       s.EndDate > DateTime.UtcNow)
            .OrderByDescending(s => s.Package!.Tier)
            .ThenByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();
        }

        public async Task<PackageTier?> GetActivePackageTierAsync(int studentId)
        {
            var today = DateTime.UtcNow;

            // A2-05 — tier from Package.Tier, not from the PackageId (row key).
            return await _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.StudentId == studentId &&
                            s.Status == SubscriptionStatus.Active &&
                            s.StartDate <= today &&
                            s.EndDate >= today)
                .OrderByDescending(s => s.Package!.Tier)
                .Select(s => (PackageTier?)s.Package!.Tier)
                .FirstOrDefaultAsync();
        }
    }
}
