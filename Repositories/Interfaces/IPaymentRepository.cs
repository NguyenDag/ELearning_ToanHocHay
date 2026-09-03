using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<Payment>> GetAllAsync();
        IQueryable<Payment> Query();
        Task<Payment?> GetByIdAsync(int id);
        Task<Payment> AddAsync(Payment payment);
        Task<bool> UpdateAsync(Payment payment);

        /// <summary>Payments this user made (payer) or benefits from (their own student record).</summary>
        Task<(List<Payment> Items, int Total)> GetForUserAsync(int userId, int page, int pageSize);
    }
}
