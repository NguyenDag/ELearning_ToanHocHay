using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Payment>> GetAllAsync()
            => await _context.Payments
                .Include(x => x.Student)
                .Include(x => x.Subscription)
                .ToListAsync();

        public IQueryable<Payment> Query()
            => _context.Payments.AsNoTracking().Include(x => x.Student);

        public async Task<Payment?> GetByIdAsync(int id)
            => await _context.Payments
                .Include(x => x.Student)
                .Include(x => x.Subscription)
                .FirstOrDefaultAsync(x => x.PaymentId == id);

        public async Task<(List<Payment> Items, int Total)> GetForUserAsync(int userId, int page, int pageSize)
        {
            var q = _context.Payments
                .AsNoTracking()
                .Include(p => p.Student)
                .Where(p => p.PaidByUserId == userId
                            || (p.Student != null && p.Student.UserId == userId));

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(p => p.PaymentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }

        public async Task<Payment> AddAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            return payment;
        }

        public async Task<bool> UpdateAsync(Payment payment)
        {
            _context.Entry(payment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await GetByIdAsync(payment.PaymentId) == null)
                    return false;
                throw;
            }
        }
    }
}
