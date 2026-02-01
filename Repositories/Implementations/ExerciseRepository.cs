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

        public async Task<bool> AddQuestionsToExerciseAsync(int exerciseId, List<int> questionIds, double scorePerQuestion)
        {
            var exercise = await _context.Exercises
                .Include(e => e.ExerciseQuestions)
                .FirstOrDefaultAsync(e => e.ExerciseId == exerciseId);

            if (exercise == null) return false;

            var currentMaxOrder = exercise.ExerciseQuestions.Any()
                ? exercise.ExerciseQuestions.Max(eq => eq.OrderIndex)
                : 0;

            var existingQuestionIds = exercise.ExerciseQuestions
                .Select(eq => eq.QuestionId)
                .ToHashSet();

            var newExerciseQuestions = new List<ExerciseQuestion>();

            foreach (var questionId in questionIds)
            {
                if (existingQuestionIds.Contains(questionId))
                    continue;

                newExerciseQuestions.Add(new ExerciseQuestion
                {
                    ExerciseId = exerciseId,
                    QuestionId = questionId,
                    Score = scorePerQuestion,
                    OrderIndex = ++currentMaxOrder
                });
            }

            if (!newExerciseQuestions.Any())
                return true;

            _context.ExerciseQuestions.AddRange(newExerciseQuestions);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Exercise> CreateExerciseAsync(Exercise exercise)
        {
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
            return await _context.Exercises
                .Include(e => e.ExerciseQuestions) // Nạp để có thể tính TotalQuestions
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Exercise>> GetByChapterIdAsync(int chapterId)
        {
            return await _context.Exercises
                .Where(e => e.ChapterId == chapterId)
                .ToListAsync();
        }

        // Không có
        public Task<IEnumerable<Exercise>> GetByLessonIdAsync(int lessonId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Exercise>> GetByTopicIdAsync(int topicId)
        {
            return await _context.Exercises
                .Where(e => e.TopicId == topicId)
                .ToListAsync();
        }

        public async Task<Exercise?> GetExerciseByIdAsync(int exerciseId)
        {
            return await _context.Exercises
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ExerciseId == exerciseId);
        }

        public async Task<List<ExerciseQuestion>> GetExerciseQuestionsAsync(int exerciseId)
        {
            return await _context.ExerciseQuestions
                .Include(eq => eq.Question)
                    .ThenInclude(q => q.QuestionOptions)
                .Where(eq => eq.ExerciseId == exerciseId)
                .OrderBy(eq => eq.OrderIndex)
                .ToListAsync();
        }

        public async Task<Exercise?> GetExerciseWithQuestionsAsync(int exerciseId)
        {
            return await _context.Exercises
                .Include(e => e.ExerciseQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.QuestionOptions) // Bắt buộc nạp Options để không bị NullReference ở Service
                .AsNoTracking() // Tăng tốc độ đọc dữ liệu
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

        public async Task<bool> RemoveQuestionFromExerciseAsync(int exerciseId, int questionId)
        {
            var eq = await _context.ExerciseQuestions
                .FirstOrDefaultAsync(x =>
                x.ExerciseId == exerciseId &&
                x.QuestionId == questionId);

            if (eq == null) return false;

            _context.ExerciseQuestions.Remove(eq);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Exercise?> UpdateExerciseAsync(Exercise exercise)
        {
            _context.Entry(exercise).State = EntityState.Modified;
            try
            {
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

        public async Task<bool> UpdateExerciseQuestionScoreAsync(int exerciseId, int questionId, double score)
        {
            var eq = await _context.ExerciseQuestions
                .FirstOrDefaultAsync(x =>
                x.ExerciseId == exerciseId &&
                x.QuestionId == questionId);

            if (eq == null) return false;

            eq.Score = score;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
