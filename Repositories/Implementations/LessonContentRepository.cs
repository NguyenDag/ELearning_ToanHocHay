using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class LessonContentRepository : ILessonContentRepository
    {
        private readonly AppDbContext _context;

        public LessonContentRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<LessonContent> AddAsync(LessonContent content)
        {
            _context.LessonContents.Add(content);
            await _context.SaveChangesAsync();
            return content;
        }

        public async Task<bool> DeleteAsync(int contentId)
        {
            var content = await _context.LessonContents
                .FirstOrDefaultAsync(x => x.ContentId == contentId);
             
            if (content == null)
                return false;

            _context.LessonContents.Remove(content);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<LessonContent?> GetByIdAsync(int contentId)
        {
            return await _context.LessonContents
                .FirstOrDefaultAsync(x => x.ContentId == contentId);
        }

        public async Task<IEnumerable<LessonContent>> GetByLessonAsync(int lessonId)
        {
            return await _context.LessonContents
                .Where(x => x.LessonId == lessonId)
                .OrderBy(x => x.OrderIndex)
                .ToListAsync();
        }

        public async Task<LessonContent?> UpdateAsync(LessonContent content)
        {
            var existing = await _context.LessonContents
                .FirstOrDefaultAsync(x => x.ContentId == content.ContentId);

            if (existing == null)
                return null;

            existing.LessonId = content.LessonId;
            existing.BlockType = content.BlockType;
            existing.ContentText = content.ContentText;
            existing.ContentUrl = content.ContentUrl;
            existing.OrderIndex = content.OrderIndex;

            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
