using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class QuestionBankRepository : IQuestionBankRepository
    {
        private readonly AppDbContext _context;

        public QuestionBankRepository(AppDbContext context)
        {
            _context = context;
        }
        public Task<QuestionBank> CreateQuestionBankAsync(QuestionBank bank)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteQuestionBankAsync(int bankId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<QuestionBank>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<QuestionBank?> GetQuestionBankByIdAsync(int bankId)
        {
            return await _context.QuestionBanks.FindAsync(bankId);
        }

        public Task<QuestionBank?> UpdateQuestionBankAsync(QuestionBank bank)
        {
            throw new NotImplementedException();
        }
    }
}
