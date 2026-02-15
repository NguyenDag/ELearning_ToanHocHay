using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class ChapterRepository : IChapterRepository
    {
        private readonly AppDbContext _context;

        public ChapterRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Chapter> CreateAsync(Chapter chapter)
        {
            chapter.CreatedAt = DateTime.UtcNow;
            chapter.IsActive = true;

            _context.Chapters.Add(chapter);
            await _context.SaveChangesAsync();

            return chapter;
        }

        public async Task<bool> DeleteAsync(int chapterId)
        {
            var chapter = await _context.Chapters.FindAsync(chapterId);
            if (chapter == null)
                return false;

            _context.Chapters.Remove(chapter);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            return entity != null;
        }

        public async Task<IEnumerable<Chapter>> GetByCurriculumIdAsync(int curriculumId)
        {
            return await _context.Chapters
            .Where(c => c.CurriculumId == curriculumId)
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();
        }

        public async Task<Chapter?> GetByIdAsync(int chapterId)
        {
            return await _context.Chapters
            .FirstOrDefaultAsync(c => c.ChapterId == chapterId);
        }

        public async Task<Chapter?> UpdateAsync(Chapter chapter)
        {
            var existing = await _context.Chapters
            .FirstOrDefaultAsync(c => c.ChapterId == chapter.ChapterId);

            if (existing == null)
                return null;

            existing.ChapterName = chapter.ChapterName;
            existing.OrderIndex = chapter.OrderIndex;
            existing.Description = chapter.Description;
            existing.IsActive = chapter.IsActive;

            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
