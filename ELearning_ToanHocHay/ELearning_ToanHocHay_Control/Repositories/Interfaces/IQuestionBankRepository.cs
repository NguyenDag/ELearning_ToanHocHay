using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IQuestionBankRepository
    {
        Task<IEnumerable<QuestionBank>> GetAllAsync();
        Task<QuestionBank?> GetQuestionBankByIdAsync(int bankId);
        Task<QuestionBank> CreateQuestionBankAsync(QuestionBank bank);
        Task<QuestionBank?> UpdateQuestionBankAsync(QuestionBank bank);
        Task<bool> DeleteQuestionBankAsync(int bankId);
    }
}
