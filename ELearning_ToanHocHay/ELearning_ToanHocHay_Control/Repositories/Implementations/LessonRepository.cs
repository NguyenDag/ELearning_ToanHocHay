using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class LessonRepository : ILessonRepository
    {
        private readonly AppDbContext _context;

        public LessonRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Lesson> CreateAsync(Lesson lesson)
        {
            lesson.Status = LessonStatus.Draft;
            lesson.IsActive = false;
            lesson.CreatedAt = DateTime.UtcNow;

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return lesson;
        }

        public async Task<Lesson?> GetByIdAsync(int lessonId)
        {
            return await _context.Lessons
            .FirstOrDefaultAsync(l => l.LessonId == lessonId);
        }

        public async Task<IEnumerable<Lesson>> GetByTopicIdAsync(int topicId)
        {
            return await _context.Lessons
            .Where(l => l.TopicId == topicId)
            .OrderBy(l => l.OrderIndex)
            .ToListAsync();
        }

        public async Task<Lesson?> UpdateAsync(Lesson lesson)
        {
            var existing = await _context.Lessons
            .FirstOrDefaultAsync(l => l.LessonId == lesson.LessonId);

            if (existing == null)
                return null;

            existing.LessonName = lesson.LessonName;
            existing.Description = lesson.Description;
            existing.DurationMinutes = lesson.DurationMinutes;
            existing.OrderIndex = lesson.OrderIndex;
            existing.IsFree = lesson.IsFree;
            existing.IsActive = lesson.IsActive;
            existing.Status = lesson.Status;
            existing.ReviewedBy = lesson.ReviewedBy;
            existing.ReviewedAt = lesson.ReviewedAt;
            existing.RejectReason = lesson.RejectReason;
            existing.PublishedAt = lesson.PublishedAt;

            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
