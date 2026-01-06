using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User> CreateUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }
            user.UpdatedAt = DateTime.Now;
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsByEmail(string email)
        {
            var user = await GetByEmailAsync(email);
            if (user == null) return false;
            return true;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            return await _context.Users
                .FindAsync(userId);
        }

        public async Task<User?> UpdateUserAsync(User user)
        {
            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return user;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await GetByIdAsync(user.UserId) == null)
                    return null;
                throw;
            }
        }

        public async Task<bool> UpdateLastLoginAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
                return false;

            user.LastLogin = DateTime.Now;
            await UpdateUserAsync(user);
            return true;
        }

        public async Task<User?> GetUserByStudentIdAsync(int studentId)
        {
            return await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(s => s.Student.StudentId == studentId);
        }

        public async Task<User?> GetUserByParentIdAsync(int parentId)
        {
            return await _context.Users
                .Include(u => u.Parent)
                .FirstOrDefaultAsync(s => s.Parent.ParentId == parentId);
        }
    }
}
