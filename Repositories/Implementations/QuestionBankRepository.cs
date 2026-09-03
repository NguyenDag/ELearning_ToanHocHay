using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class QuestionBankRepository : IQuestionBankRepository
    {
        private readonly AppDbContext _context;

        public QuestionBankRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<QuestionBank>> GetAllAsync()
            => await _context.QuestionBanks.AsNoTracking().ToListAsync();

        public async Task<List<QuestionBank>> GetAllAsync(int? subjectId, int? gradeLevelId, bool includeInactive)
        {
            var q = _context.QuestionBanks
                .AsNoTracking()
                .Include(b => b.Subject)
                .Include(b => b.GradeLevel)
                .AsQueryable();

            if (!includeInactive) q = q.Where(b => b.IsActive);
            if (subjectId.HasValue) q = q.Where(b => b.SubjectId == subjectId);
            if (gradeLevelId.HasValue) q = q.Where(b => b.GradeLevelId == gradeLevelId);

            return await q.OrderBy(b => b.BankName).ToListAsync();
        }

        public async Task<QuestionBank?> GetQuestionBankByIdAsync(int bankId)
            => await _context.QuestionBanks
                .Include(b => b.Subject)
                .Include(b => b.GradeLevel)
                .FirstOrDefaultAsync(b => b.BankId == bankId);

        public async Task<QuestionBank> CreateQuestionBankAsync(QuestionBank bank)
        {
            _context.QuestionBanks.Add(bank);
            await _context.SaveChangesAsync();
            return bank;
        }

        public async Task<QuestionBank?> UpdateQuestionBankAsync(QuestionBank bank)
        {
            _context.QuestionBanks.Update(bank);
            await _context.SaveChangesAsync();
            return bank;
        }

        public async Task<bool> DeleteQuestionBankAsync(int bankId)
        {
            var bank = await _context.QuestionBanks.FindAsync(bankId);
            if (bank == null) return false;
            _context.QuestionBanks.Remove(bank);
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<int> QuestionCountAsync(int bankId)
            => _context.Questions.CountAsync(q => q.BankId == bankId);
    }
}
