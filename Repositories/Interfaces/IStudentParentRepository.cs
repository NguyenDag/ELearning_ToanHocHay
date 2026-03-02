using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IStudentParentRepository
    {
        Task<bool> ExistsAsync(int studentId, int parentId);
        Task<StudentParent> CreateAsync(StudentParent entity);
        Task<Parent?> GetParentByConnectionCodeAsync(string code);
    }
}
