using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class TopicRepository : ITopicRepository
    {
        private readonly AppDbContext _context;

        public TopicRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Topic> CreateAsync(Topic topic)
        {
            topic.CreatedAt = DateTime.UtcNow;
            topic.IsActive = true;

            _context.Topics.Add(topic);
            await _context.SaveChangesAsync();

            return topic;
        }

        public async Task<bool> DeleteAsync(int topicId)
        {
            var topic = await _context.Topics.FindAsync(topicId);
            if (topic == null)
                return false;

            _context.Topics.Remove(topic);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            return entity != null;
        }

        public async Task<IEnumerable<Topic>> GetByChapterIdAsync(int chapterId)
        {
            return await _context.Topics
                .Where(t => t.ChapterId == chapterId)
                .OrderBy(t => t.OrderIndex)
                .ToListAsync();
        }

        public async Task<Topic?> GetByIdAsync(int topicId)
        {
            return await _context.Topics
            .FirstOrDefaultAsync(t => t.TopicId == topicId);
        }

        public async Task<Topic?> UpdateAsync(Topic topic)
        {
            var existing = await _context.Topics
            .FirstOrDefaultAsync(t => t.TopicId == topic.TopicId);

            if (existing == null)
                return null;

            existing.TopicName = topic.TopicName;
            existing.OrderIndex = topic.OrderIndex;
            existing.Description = topic.Description;
            existing.IsFree = topic.IsFree;
            existing.IsActive = topic.IsActive;

            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
