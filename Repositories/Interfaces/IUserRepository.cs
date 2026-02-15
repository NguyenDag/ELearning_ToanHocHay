using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int userId);
        Task<User?> GetUserByStudentIdAsync(int studentId);
        Task<User?> GetUserByParentIdAsync(int parentId);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> CreateUserAsync(User user);
        Task<bool> UpdateLastLoginAsync(int userId);
        Task<User?> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int userId);

        Task<bool> ExistsByEmail(string email);
        Task<bool> ExistsAsync(int id);
    }
}
