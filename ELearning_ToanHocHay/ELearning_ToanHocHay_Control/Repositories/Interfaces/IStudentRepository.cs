using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IStudentRepository
    {
        Task<Student> AddAsync(Student student);
    }
}
