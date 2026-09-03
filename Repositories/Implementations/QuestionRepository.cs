using ELearning_ToanHocHay_Control.Data; // <--- DÒNG NÀY SỬA LỖI ApplicationDbContext
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly AppDbContext _context;

        public QuestionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Question?> GetQuestionByIdAsync(int id)
        {
            return await _context.Questions
                .Include(q => q.QuestionOptions)
                .FirstOrDefaultAsync(q => q.QuestionId == id);
        }

        public async Task<Question> CreateAsync(Question question)
        {
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return question;
        }

        public async Task<List<Question>> CreateMultipleAsync(List<Question> questions)
        {
            _context.Questions.AddRange(questions);
            await _context.SaveChangesAsync();
            return questions;
        }

        // ---------------- A3/P2 ----------------
        public async Task<(List<Question> Items, int Total)> GetByBankAsync(
            int bankId, QuestionStatus? status, string? search, int page, int pageSize)
        {
            var q = _context.Questions
                .Include(x => x.QuestionOptions)
                .Where(x => x.BankId == bankId);

            if (status.HasValue) q = q.Where(x => x.Status == status);
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(x => x.QuestionText.ToLower().Contains(search.ToLower()));

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }

        public Task SaveAsync() => _context.SaveChangesAsync();

        public Task DeleteAsync(Question question)
        {
            _context.Questions.Remove(question);
            return _context.SaveChangesAsync();
        }

        public Task<bool> IsUsedInAttemptsAsync(int questionId)
            => _context.StudentAnswers.AnyAsync(a => a.QuestionId == questionId);
    }
}