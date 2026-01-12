using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class ExerciseQuestionRepository : IExerciseQuestionRepository
    {
        private readonly AppDbContext _context;

        public ExerciseQuestionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ExerciseQuestion exerciseQuestion)
        {
            _context.ExerciseQuestions.Add(exerciseQuestion);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<Data.Entities.ExerciseQuestion> exerciseQuestions)
        {
            await _context.ExerciseQuestions.AddRangeAsync(exerciseQuestions);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int exerciseId, int questionId)
        {
            return await _context.ExerciseQuestions
                .AnyAsync(eq =>
                    eq.ExerciseId == exerciseId &&
                    eq.QuestionId == questionId);
        }

        public async Task<ExerciseQuestion?> GetAsync(int exerciseId, int questionId)
        {
            return await _context.ExerciseQuestions
                .FirstOrDefaultAsync(eq =>
                    eq.ExerciseId == exerciseId &&
                    eq.QuestionId == questionId);
        }

        public async Task<List<ExerciseQuestion>> GetByExerciseIdAsync(int exerciseId)
        {
            return await _context.ExerciseQuestions
                .Where(eq => eq.ExerciseId == exerciseId)
                .OrderBy(eq => eq.OrderIndex)
                .ToListAsync();
        }

        public async Task RemoveAsync(int exerciseId, int questionId)
        {
            var eq = await GetAsync(exerciseId, questionId);
            if (eq != null)
            {
                _context.ExerciseQuestions.Remove(eq);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveByExerciseIdAsync(int exerciseId)
        {
            var questions = await _context.ExerciseQuestions
                .Where(eq => eq.ExerciseId == exerciseId)
                .ToListAsync();

            _context.ExerciseQuestions.RemoveRange(questions);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ExerciseQuestion exerciseQuestion)
        {
            _context.ExerciseQuestions.Update(exerciseQuestion);
            await _context.SaveChangesAsync();
        }
    }
}
