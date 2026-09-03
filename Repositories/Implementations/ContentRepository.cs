using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class ContentRepository : IContentRepository
    {
        private readonly AppDbContext _context;

        public ContentRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task SaveAsync() => _context.SaveChangesAsync();

        // ---------------- nodes ----------------
        public async Task<List<ContentNode>> GetNodesByVersionAsync(int courseVersionId)
            => await _context.ContentNodes
                .AsNoTracking()
                .Include(n => n.LessonDetail)
                .Where(n => n.CourseVersionId == courseVersionId)
                .OrderBy(n => n.Depth).ThenBy(n => n.OrderIndex)
                .ToListAsync();

        public Task<ContentNode?> GetNodeAsync(int nodeId)
            => _context.ContentNodes
                .Include(n => n.CourseVersion)
                .Include(n => n.LessonDetail)
                .FirstOrDefaultAsync(n => n.NodeId == nodeId);

        public Task<ContentNode?> GetNodeWithDetailAsync(int nodeId)
            => _context.ContentNodes
                .AsNoTracking()
                .Include(n => n.LessonDetail)
                .Include(n => n.Blocks)
                .Include(n => n.Resources)
                .Include(n => n.FlashcardDecks).ThenInclude(d => d.Cards)
                .FirstOrDefaultAsync(n => n.NodeId == nodeId);

        public Task<ContentNode?> GetNodeForConsumptionAsync(int nodeId)
            => _context.ContentNodes
                .AsNoTracking()
                .Include(n => n.LessonDetail)
                .Include(n => n.Blocks)
                .Include(n => n.Resources)
                .Include(n => n.FlashcardDecks).ThenInclude(d => d.Cards)
                .Include(n => n.CourseVersion).ThenInclude(v => v!.Course)
                .FirstOrDefaultAsync(n => n.NodeId == nodeId);

        public async Task<List<ContentNode>> GetChildrenAsync(int courseVersionId, int? parentNodeId)
            => await _context.ContentNodes
                .Where(n => n.CourseVersionId == courseVersionId && n.ParentNodeId == parentNodeId)
                .OrderBy(n => n.OrderIndex)
                .ToListAsync();

        public Task<bool> HasChildrenAsync(int nodeId)
            => _context.ContentNodes.AnyAsync(n => n.ParentNodeId == nodeId);

        public async Task<ContentNode> AddNodeAsync(ContentNode node)
        {
            _context.ContentNodes.Add(node);
            await _context.SaveChangesAsync();
            return node;
        }

        public Task RemoveNodeAsync(ContentNode node)
        {
            _context.ContentNodes.Remove(node);
            return _context.SaveChangesAsync();
        }

        public async Task<List<ContentNode>> GetSubtreeAsync(int courseVersionId, string materializedPathPrefix)
            => await _context.ContentNodes
                .Where(n => n.CourseVersionId == courseVersionId
                            && n.MaterializedPath.StartsWith(materializedPathPrefix))
                .ToListAsync();

        // ---------------- revisions ----------------
        public async Task<List<NodeRevision>> GetRevisionsAsync(int nodeId)
            => await _context.NodeRevisions.AsNoTracking()
                .Where(r => r.NodeId == nodeId)
                .OrderByDescending(r => r.RevisionNumber)
                .ToListAsync();

        public Task<NodeRevision?> GetRevisionAsync(int nodeId, int revisionNumber)
            => _context.NodeRevisions.AsNoTracking()
                .FirstOrDefaultAsync(r => r.NodeId == nodeId && r.RevisionNumber == revisionNumber);

        public async Task<int> NextRevisionNumberAsync(int nodeId)
            => ((await _context.NodeRevisions.Where(r => r.NodeId == nodeId)
                .Select(r => (int?)r.RevisionNumber).MaxAsync()) ?? 0) + 1;

        public async Task AddRevisionAsync(NodeRevision revision)
        {
            _context.NodeRevisions.Add(revision);
            await _context.SaveChangesAsync();
        }

        // ---------------- node type rules ----------------
        public async Task<bool> NodeTypeAllowedAsync(int? subjectId, NodeType? parentType, NodeType childType)
        {
            // Subject-specific rules override the defaults (SubjectId == null).
            var rules = await _context.NodeTypeRules
                .Where(r => r.SubjectId == null || r.SubjectId == subjectId)
                .ToListAsync();

            if (subjectId.HasValue && rules.Any(r => r.SubjectId == subjectId))
                rules = rules.Where(r => r.SubjectId == subjectId).ToList();

            return rules.Any(r => r.ParentType == parentType && r.ChildType == childType);
        }

        // ---------------- blocks ----------------
        public async Task<List<ContentBlock>> GetBlocksAsync(int nodeId)
            => await _context.ContentBlocks
                .Where(b => b.NodeId == nodeId)
                .OrderBy(b => b.OrderIndex)
                .ToListAsync();

        public Task<ContentBlock?> GetBlockAsync(int blockId)
            => _context.ContentBlocks
                .Include(b => b.Node).ThenInclude(n => n!.CourseVersion)
                .FirstOrDefaultAsync(b => b.BlockId == blockId);

        public async Task<int> MaxBlockOrderAsync(int nodeId)
            => (await _context.ContentBlocks.Where(b => b.NodeId == nodeId)
                .Select(b => (int?)b.OrderIndex).MaxAsync()) ?? 0;

        public async Task<ContentBlock> AddBlockAsync(ContentBlock block)
        {
            _context.ContentBlocks.Add(block);
            await _context.SaveChangesAsync();
            return block;
        }

        public Task RemoveBlockAsync(ContentBlock block)
        {
            _context.ContentBlocks.Remove(block);
            return _context.SaveChangesAsync();
        }

        // ---------------- resources ----------------
        public async Task<List<LessonResource>> GetResourcesAsync(int nodeId)
            => await _context.LessonResources
                .Where(r => r.NodeId == nodeId)
                .OrderBy(r => r.OrderIndex)
                .ToListAsync();

        public Task<LessonResource?> GetResourceAsync(int resourceId)
            => _context.LessonResources
                .Include(r => r.Node).ThenInclude(n => n!.CourseVersion)
                .FirstOrDefaultAsync(r => r.ResourceId == resourceId);

        public async Task<int> MaxResourceOrderAsync(int nodeId)
            => (await _context.LessonResources.Where(r => r.NodeId == nodeId)
                .Select(r => (int?)r.OrderIndex).MaxAsync()) ?? 0;

        public async Task<LessonResource> AddResourceAsync(LessonResource resource)
        {
            _context.LessonResources.Add(resource);
            await _context.SaveChangesAsync();
            return resource;
        }

        public Task RemoveResourceAsync(LessonResource resource)
        {
            _context.LessonResources.Remove(resource);
            return _context.SaveChangesAsync();
        }

        // ---------------- flashcards ----------------
        public async Task<List<FlashcardDeck>> GetDecksAsync(int nodeId)
            => await _context.FlashcardDecks
                .Include(d => d.Cards)
                .Where(d => d.NodeId == nodeId)
                .ToListAsync();

        public Task<FlashcardDeck?> GetDeckAsync(int deckId)
            => _context.FlashcardDecks
                .Include(d => d.Node).ThenInclude(n => n!.CourseVersion)
                .FirstOrDefaultAsync(d => d.DeckId == deckId);

        public Task<FlashcardDeck?> GetDeckWithCardsAsync(int deckId)
            => _context.FlashcardDecks
                .Include(d => d.Cards)
                .Include(d => d.Node).ThenInclude(n => n!.CourseVersion)
                .FirstOrDefaultAsync(d => d.DeckId == deckId);

        public async Task<FlashcardDeck> AddDeckAsync(FlashcardDeck deck)
        {
            _context.FlashcardDecks.Add(deck);
            await _context.SaveChangesAsync();
            return deck;
        }

        public Task RemoveDeckAsync(FlashcardDeck deck)
        {
            _context.FlashcardDecks.Remove(deck);
            return _context.SaveChangesAsync();
        }

        public Task<Flashcard?> GetCardAsync(int cardId)
            => _context.Flashcards
                .Include(c => c.Deck).ThenInclude(d => d!.Node).ThenInclude(n => n!.CourseVersion)
                .FirstOrDefaultAsync(c => c.CardId == cardId);

        public async Task<int> MaxCardOrderAsync(int deckId)
            => (await _context.Flashcards.Where(c => c.DeckId == deckId)
                .Select(c => (int?)c.OrderIndex).MaxAsync()) ?? 0;

        public async Task<Flashcard> AddCardAsync(Flashcard card)
        {
            _context.Flashcards.Add(card);
            await _context.SaveChangesAsync();
            return card;
        }

        public Task RemoveCardAsync(Flashcard card)
        {
            _context.Flashcards.Remove(card);
            return _context.SaveChangesAsync();
        }
    }
}
