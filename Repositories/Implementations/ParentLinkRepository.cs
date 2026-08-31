using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class ParentLinkRepository : IParentLinkRepository
    {
        private readonly AppDbContext _context;

        public ParentLinkRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ParentLink?> GetByIdAsync(int parentLinkId) =>
            await _context.ParentLinks
                .Include(l => l.Student).ThenInclude(s => s!.User)
                .Include(l => l.Parent).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(l => l.ParentLinkId == parentLinkId);

        public async Task<ParentLink?> GetAsync(int parentId, int studentId) =>
            await _context.ParentLinks
                .FirstOrDefaultAsync(l => l.ParentId == parentId && l.StudentId == studentId);

        public async Task<List<ParentLink>> GetByParentAsync(int parentId, bool activeOnly = false) =>
            await _context.ParentLinks
                .Include(l => l.Student).ThenInclude(s => s!.User)
                .Where(l => l.ParentId == parentId && (!activeOnly || l.Status == LinkStatus.Active))
                .ToListAsync();

        public async Task<List<ParentLink>> GetByStudentAsync(int studentId, bool activeOnly = false) =>
            await _context.ParentLinks
                .Include(l => l.Parent).ThenInclude(p => p!.User)
                .Where(l => l.StudentId == studentId && (!activeOnly || l.Status == LinkStatus.Active))
                .ToListAsync();

        public async Task<bool> ExistsActiveAsync(int studentId, int parentId) =>
            await _context.ParentLinks.AnyAsync(l =>
                l.StudentId == studentId && l.ParentId == parentId && l.Status == LinkStatus.Active);

        public async Task<ParentLink> AddAsync(ParentLink link)
        {
            _context.ParentLinks.Add(link);
            await _context.SaveChangesAsync();
            return link;
        }

        public async Task UpdateAsync(ParentLink link)
        {
            _context.ParentLinks.Update(link);
            await _context.SaveChangesAsync();
        }
    }
}
