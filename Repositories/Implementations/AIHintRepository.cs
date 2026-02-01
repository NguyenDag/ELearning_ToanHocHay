using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class AIHintRepository : IAIHintRepository
    {
        private readonly AppDbContext _context;

        public AIHintRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AIHint> CreateAsync(AIHint hint)
        {
            hint.CreatedAt = DateTime.UtcNow;

            _context.AIHints.Add(hint);
            await _context.SaveChangesAsync();

            return hint;
        }

        public async Task<bool> DeleteAsync(int hintId)
        {
            var hint = await _context.AIHints.FindAsync(hintId);
            if (hint == null)
                return false;

            _context.AIHints.Remove(hint);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AIHint?> GetByIdAsync(int hintId)
        {
            return await _context.AIHints
                .FirstOrDefaultAsync(h => h.HintId == hintId);
        }

        public async Task<IEnumerable<AIHint>> GetByAttemptAsync(int attemptId)
        {
            return await _context.AIHints
                .Where(h => h.AttemptId == attemptId)
                .OrderBy(h => h.HintLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<AIHint>> GetByAttemptAndQuestionAsync(int attemptId, int questionId)
        {
            return await _context.AIHints
                .Where(h => h.AttemptId == attemptId && h.QuestionId == questionId)
                .OrderBy(h => h.HintLevel)
                .ToListAsync();
        }

        public async Task<AIHint?> UpdateAsync(AIHint hint)
        {
            var existing = await _context.AIHints
                .FirstOrDefaultAsync(h => h.HintId == hint.HintId);

            if (existing == null)
                return null;

            existing.HintText = hint.HintText;
            existing.HintLevel = hint.HintLevel;

            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
