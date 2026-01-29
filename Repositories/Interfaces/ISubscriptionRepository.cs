using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface ISubscriptionRepository
    {
        Task<IEnumerable<Subscription>> GetAllAsync();
        Task<Subscription?> GetByIdAsync(int id);
        Task<Subscription?> GetActiveByStudentAsync(int studentId);
        Task AddAsync(Subscription subscription);
        Task UpdateAsync(Subscription subscription);
    }
}
