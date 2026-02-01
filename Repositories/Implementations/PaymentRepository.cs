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

        public async Task<Payment?> GetByIdAsync(int id)
            => await _context.Payments
                .Include(x => x.Student)
                .Include(x => x.Subscription)
                .FirstOrDefaultAsync(x => x.PaymentId == id);

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
