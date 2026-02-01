using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class AIFeedbackRepository : IAIFeedbackRepository
    {
        private readonly AppDbContext _context;

        public AIFeedbackRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AIFeedback> CreateAsync(AIFeedback feedback)
        {
            feedback.CreatedAt = DateTime.UtcNow;

            _context.AIFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return feedback;
        }

        public async Task<bool> DeleteAsync(int feedbackId)
        {
            var feedback = await _context.AIFeedbacks.FindAsync(feedbackId);
            if (feedback == null)
                return false;

            _context.AIFeedbacks.Remove(feedback);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AIFeedback?> GetByIdAsync(int feedbackId)
        {
            return await _context.AIFeedbacks
                .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId);
        }

        public async Task<IEnumerable<AIFeedback>> GetByAttemptAsync(int attemptId)
        {
            return await _context.AIFeedbacks
                .Where(f => f.AttemptId == attemptId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<AIFeedback?> UpdateAsync(AIFeedback feedback)
        {
            var existing = await _context.AIFeedbacks
                .FirstOrDefaultAsync(f => f.FeedbackId == feedback.FeedbackId);

            if (existing == null)
                return null;

            existing.FullSolution = feedback.FullSolution;
            existing.MistakeAnalysis = feedback.MistakeAnalysis;
            existing.ImprovementAdvice = feedback.ImprovementAdvice;

            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
