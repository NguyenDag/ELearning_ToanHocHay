using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class ExerciseAttemptRepository : IExerciseAttemptRepository
    {
        private readonly AppDbContext _context;

        public ExerciseAttemptRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<ExerciseAttempt> CreateAttemptAsync(ExerciseAttempt attempt)
        {
            _context.ExerciseAttempts.Add(attempt);
            await _context.SaveChangesAsync();
            return attempt;
        }

        public async Task<bool> ExistsByExerciseIdAsync(int exerciseId)
        {
            return await _context.ExerciseAttempts.AnyAsync(a => a.ExerciseId == exerciseId);
        }

        public async Task<ExerciseAttempt> GetAttemptByIdAsync(int attemptId)
        {
            return await _context.ExerciseAttempts
                .Include(a => a.Exercise)
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId);
        }

        public async Task<ExerciseAttempt> GetAttemptWithDetailsAsync(int attemptId)
        {
            return await _context.ExerciseAttempts
                .Include(a => a.Exercise)
                    .ThenInclude(e => e.ExerciseQuestions)
                        .ThenInclude(eq => eq.Question)
                .Include(a => a.StudentAnswers)
                .Include(a => a.Student)
                    .ThenInclude(s => s.User)
                .Include(a => a.AIFeedbacks)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId);
        }

        public async Task<List<ExerciseAttempt>> GetStudentAttemptsAsync(int studentId)
        {
            return await _context.ExerciseAttempts
                .Include(a => a.Exercise)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();
        }

        public async Task<bool> HasActiveAttemptAsync(int studentId, int exerciseId)
        {
            return await _context.ExerciseAttempts
                .AnyAsync(a => a.StudentId == studentId
                    && a.ExerciseId == exerciseId
                    && a.EndTime == null);
        }

        public async Task<ExerciseAttempt> UpdateAttemptAsync(ExerciseAttempt attempt)
        {
            _context.Entry(attempt).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return attempt;
        }
    }
}
