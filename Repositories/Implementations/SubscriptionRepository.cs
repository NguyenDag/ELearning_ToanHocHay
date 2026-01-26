using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly AppDbContext _context;

        public SubscriptionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Subscription>> GetAllAsync()
            => await _context.Subscriptions
                .Include(x => x.Package)
                .Include(x => x.Student)
                .Include(x => x.Payment)
                .ToListAsync();

        public async Task<Subscription?> GetByIdAsync(int id)
            => await _context.Subscriptions
                .Include(x => x.Package)
                .Include(x => x.Student)
                .Include(x => x.Payment)
                .FirstOrDefaultAsync(x => x.SubscriptionId == id);

        public async Task<Subscription?> GetActiveByStudentAsync(int studentId)
            => await _context.Subscriptions
                .FirstOrDefaultAsync(x =>
                    x.StudentId == studentId &&
                    x.Status == SubscriptionStatus.Active &&
                    x.EndDate >= DateTime.UtcNow);

        public async Task AddAsync(Subscription subscription)
        {
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Subscription subscription)
        {
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }
    }
}
