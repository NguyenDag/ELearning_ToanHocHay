using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class ParentRepository : IParentRepository
    {
        private readonly AppDbContext _context;

        public ParentRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Parent> AddAsync(Parent parent)
        {
            _context.Parents.Add(parent);
            await _context.SaveChangesAsync();
            return parent;
        }

        public async Task<Parent?> GetByIdAsync(int parentId)
        {
            return await _context.Parents
                .FirstOrDefaultAsync(u => u.ParentId == parentId);
        }

        public async Task<Parent?> GetByUserIdAsync(int userId)
        {
            return await _context.Parents
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }
    }
}
