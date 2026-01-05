using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class StudentAnswerRepository : IStudentAnswerRepository
    {
        private readonly AppDbContext _context;

        public StudentAnswerRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<StudentAnswer> CreateAnswerAsync(StudentAnswer answer)
        {
            _context.StudentAnswers.Add(answer);
            await _context.SaveChangesAsync();
            return answer;
        }

        public async Task<StudentAnswer> GetAnswerAsync(int attemptId, int questionId)
        {
            return await _context.StudentAnswers
                .Include(a => a.Question)
                    .ThenInclude(q => q.QuestionBank)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId
                    && a.QuestionId == questionId);
        }

        public async Task<List<StudentAnswer>> GetAttemptAnswersAsync(int attemptId)
        {
            return await _context.StudentAnswers
                .Include(a => a.Question)
                    .ThenInclude(q => q.QuestionBank)
                .Where(a => a.AttemptId == attemptId)
                .ToListAsync();
        }

        public async Task<StudentAnswer> UpdateAnswerAsync(StudentAnswer answer)
        {
            _context.Update(answer);
            await _context.SaveChangesAsync();
            return answer;
        }
    }
}
