using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class CourseRepository : ICourseRepository
    {
        private readonly AppDbContext _context;

        public CourseRepository(AppDbContext context)
        {
            _context = context;
        }

        // ---------------- Course ----------------
        public async Task<List<Course>> GetCoursesAsync(int? subjectId, int? gradeLevelId, CourseStatus? status)
        {
            var q = _context.Courses
                .AsNoTracking()
                .Include(c => c.Subject)
                .Include(c => c.GradeLevel)
                .Include(c => c.Framework)
                .Include(c => c.Versions)
                .AsQueryable();

            if (subjectId.HasValue) q = q.Where(c => c.SubjectId == subjectId);
            if (gradeLevelId.HasValue) q = q.Where(c => c.GradeLevelId == gradeLevelId);
            if (status.HasValue) q = q.Where(c => c.Status == status);

            return await q.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Title).ToListAsync();
        }

        public async Task<Course?> GetCourseAsync(int courseId, bool withVersions = false)
        {
            var q = _context.Courses
                .Include(c => c.Subject)
                .Include(c => c.GradeLevel)
                .Include(c => c.Framework)
                .AsQueryable();

            if (withVersions) q = q.Include(c => c.Versions);

            return await q.FirstOrDefaultAsync(c => c.CourseId == courseId);
        }

        public Task<Course?> GetCourseBySlugAsync(string slug)
            => _context.Courses
                .AsNoTracking()
                .Include(c => c.Subject)
                .Include(c => c.GradeLevel)
                .Include(c => c.Framework)
                .Include(c => c.Versions)
                .FirstOrDefaultAsync(c => c.Slug == slug);

        public Task<bool> SlugExistsAsync(string slug, int? exceptId = null)
            => _context.Courses.AnyAsync(c =>
                c.Slug.ToLower() == slug.ToLower() && (exceptId == null || c.CourseId != exceptId));

        public Task<bool> SubjectGradeFrameworkExistsAsync(int subjectId, int gradeLevelId, int? frameworkId, int? exceptId = null)
            => _context.Courses.AnyAsync(c =>
                c.SubjectId == subjectId &&
                c.GradeLevelId == gradeLevelId &&
                c.FrameworkId == frameworkId &&
                (exceptId == null || c.CourseId != exceptId));

        public async Task<Course> AddCourseAsync(Course course)
        {
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            return course;
        }

        public Task UpdateCourseAsync(Course course)
        {
            _context.Courses.Update(course);
            return _context.SaveChangesAsync();
        }

        // ---------------- CourseVersion ----------------
        public Task<CourseVersion?> GetVersionAsync(int versionId)
            => _context.CourseVersions
                .Include(v => v.Course)
                .FirstOrDefaultAsync(v => v.CourseVersionId == versionId);

        public async Task<List<CourseVersion>> GetVersionsAsync(int courseId)
            => await _context.CourseVersions
                .AsNoTracking()
                .Where(v => v.CourseId == courseId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();

        public async Task<int> NextVersionNumberAsync(int courseId)
        {
            var max = await _context.CourseVersions
                .Where(v => v.CourseId == courseId)
                .Select(v => (int?)v.VersionNumber)
                .MaxAsync();
            return (max ?? 0) + 1;
        }

        public Task<CourseVersion?> GetPublishedVersionAsync(int courseId)
            => _context.CourseVersions
                .FirstOrDefaultAsync(v => v.CourseId == courseId && v.State == VersionState.Published);

        public async Task<CourseVersion> AddVersionAsync(CourseVersion version)
        {
            _context.CourseVersions.Add(version);
            await _context.SaveChangesAsync();
            return version;
        }

        public Task UpdateVersionAsync(CourseVersion version)
        {
            _context.CourseVersions.Update(version);
            return _context.SaveChangesAsync();
        }

        public Task SaveAsync() => _context.SaveChangesAsync();

        // ---------------- Clone ----------------
        public async Task CloneContentTreeAsync(int sourceVersionId, int targetVersionId, int userId)
        {
            var nodes = await _context.ContentNodes
                .Where(n => n.CourseVersionId == sourceVersionId)
                .OrderBy(n => n.Depth).ThenBy(n => n.OrderIndex)
                .ToListAsync();

            if (nodes.Count == 0) return;

            var sourceIds = nodes.Select(n => n.NodeId).ToList();

            var blocks = await _context.ContentBlocks
                .Where(b => sourceIds.Contains(b.NodeId)).ToListAsync();
            var resources = await _context.LessonResources
                .Where(r => sourceIds.Contains(r.NodeId)).ToListAsync();
            var decks = await _context.FlashcardDecks
                .Where(d => sourceIds.Contains(d.NodeId)).ToListAsync();
            var deckIds = decks.Select(d => d.DeckId).ToList();
            var cards = await _context.Flashcards
                .Where(c => deckIds.Contains(c.DeckId)).ToListAsync();

            var now = DateTime.UtcNow;
            var idMap = new Dictionary<int, ContentNode>();

            // Depth order guarantees the parent is created before its children.
            foreach (var src in nodes)
            {
                var clone = new ContentNode
                {
                    CourseVersionId = targetVersionId,
                    ParentNodeId = null, // fixed up below
                    NodeType = src.NodeType,
                    Title = src.Title,
                    Slug = src.Slug,
                    OrderIndex = src.OrderIndex,
                    Depth = src.Depth,
                    MaterializedPath = "/",
                    IsFree = src.IsFree,
                    IsHidden = src.IsHidden,
                    CreatedBy = userId,
                    CreatedAt = now
                };
                _context.ContentNodes.Add(clone);
                idMap[src.NodeId] = clone;
            }
            await _context.SaveChangesAsync(); // assign NodeIds

            foreach (var src in nodes)
            {
                var clone = idMap[src.NodeId];
                clone.ParentNodeId = src.ParentNodeId.HasValue && idMap.TryGetValue(src.ParentNodeId.Value, out var p)
                    ? p.NodeId
                    : null;
                clone.MaterializedPath = clone.ParentNodeId.HasValue
                    ? idMap[src.ParentNodeId!.Value].MaterializedPath + clone.NodeId + "/"
                    : "/" + clone.NodeId + "/";
            }

            foreach (var b in blocks)
            {
                _context.ContentBlocks.Add(new ContentBlock
                {
                    NodeId = idMap[b.NodeId].NodeId,
                    BlockType = b.BlockType,
                    ContentText = b.ContentText,
                    ContentUrl = b.ContentUrl,
                    MetadataJson = b.MetadataJson,
                    OrderIndex = b.OrderIndex
                });
            }

            foreach (var r in resources)
            {
                _context.LessonResources.Add(new LessonResource
                {
                    NodeId = idMap[r.NodeId].NodeId,
                    Title = r.Title,
                    ResourceType = r.ResourceType,
                    MediaAssetId = r.MediaAssetId,
                    ExternalUrl = r.ExternalUrl,
                    IsDownloadable = r.IsDownloadable,
                    OrderIndex = r.OrderIndex
                });
            }

            var deckMap = new Dictionary<int, FlashcardDeck>();
            foreach (var d in decks)
            {
                var clone = new FlashcardDeck
                {
                    NodeId = idMap[d.NodeId].NodeId,
                    Title = d.Title,
                    CreatedAt = now
                };
                _context.FlashcardDecks.Add(clone);
                deckMap[d.DeckId] = clone;
            }
            await _context.SaveChangesAsync();

            foreach (var c in cards)
            {
                _context.Flashcards.Add(new Flashcard
                {
                    DeckId = deckMap[c.DeckId].DeckId,
                    FrontText = c.FrontText,
                    BackText = c.BackText,
                    FrontImageUrl = c.FrontImageUrl,
                    BackImageUrl = c.BackImageUrl,
                    Hint = c.Hint,
                    OrderIndex = c.OrderIndex
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}
