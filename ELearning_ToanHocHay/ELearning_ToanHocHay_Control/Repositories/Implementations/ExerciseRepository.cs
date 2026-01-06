using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class ExerciseRepository : IExerciseRepository
    {
        private readonly AppDbContext _context;
        public ExerciseRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Exercise> CreateExerciseAsync(Exercise exercise)
        {
            exercise.IsFree = false;
            exercise.CreatedAt = DateTime.Now;
            exercise.IsActive = true;
            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();
            return exercise;
        }

        public async Task<bool> DeleteExerciseAsync(int exerciseId)
        {
            var exercise = await GetExerciseByIdAsync(exerciseId);
            if (exercise == null)
            {
                return false;
            }
            _context.Exercises.Remove(exercise);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Exercise>> GetAllAsync()
        {
            return await _context.Exercises.ToListAsync();
        }

        public async Task<Exercise?> GetExerciseByIdAsync(int exerciseId)
        {
            return await _context.Exercises
                .FirstOrDefaultAsync(e => e.ExerciseId == exerciseId);
        }

        public async Task<Exercise> GetExerciseWithQuestionsAsync(int exerciseId)
        {
            return await _context.Exercises
                .Include(e => e.ExerciseQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(qb => qb.QuestionOptions)
                .FirstOrDefaultAsync(e => e.ExerciseId == exerciseId);
        }

        public async Task<List<Question>> GetRandomQuestionsAsync(int? questionBankId, int count)
        {
            var query = _context.Questions
                .Include(qb => qb.QuestionOptions)
                .Where(q => q.IsActive);

            if (questionBankId.HasValue)
            {
                query = query.Where(q => q.BankId == questionBankId.Value);
            }

            var totalQuestions = await query.CountAsync();

            if (totalQuestions <= count)
            {
                return await query.ToListAsync();
            }

            // Random selection
            var random = new Random();
            var skipCount = random.Next(0, Math.Max(1, totalQuestions - count));

            return await query
                .OrderBy(q => Guid.NewGuid()) // Random order
                .Take(count)
                .ToListAsync();
        }

        public async Task<Exercise?> UpdateExerciseAsync(Exercise exercise)
        {
            _context.Entry(exercise).State = EntityState.Modified;
            try
            {
                _context.Update(exercise);
                await _context.SaveChangesAsync();
                return exercise;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await GetExerciseByIdAsync(exercise.ExerciseId) == null)
                    return null;
                throw;
            }
        }
    }
}
