using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class StudentParentRepository : IStudentParentRepository
    {
        private readonly AppDbContext _context;

        public StudentParentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(int studentId, int parentId)
        {
            return await _context.StudentParents
                .AnyAsync(sp => sp.StudentId == studentId && sp.ParentId == parentId);
        }

        public async Task<StudentParent> CreateAsync(StudentParent entity)
        {
            _context.StudentParents.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<Parent?> GetParentByConnectionCodeAsync(string code)
        {
            return await _context.Parents
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.ConnectionCode == code);
        }
    }
}
