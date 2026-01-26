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
    }
}
