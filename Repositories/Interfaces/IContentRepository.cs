using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    /// <summary>A3/P2 — content tree (ContentNode + Block + Resource + FlashcardDeck/Flashcard).</summary>
    public interface IContentRepository
    {
        // ----- nodes -----
        Task<List<ContentNode>> GetNodesByVersionAsync(int courseVersionId);
        Task<ContentNode?> GetNodeAsync(int nodeId);
        Task<ContentNode?> GetNodeWithDetailAsync(int nodeId);

        /// <summary>Node + details + CourseVersion + Course — for the consumption gate.</summary>
        Task<ContentNode?> GetNodeForConsumptionAsync(int nodeId);
        Task<List<ContentNode>> GetChildrenAsync(int courseVersionId, int? parentNodeId);
        Task<bool> HasChildrenAsync(int nodeId);
        Task<ContentNode> AddNodeAsync(ContentNode node);
        Task SaveAsync();
        Task RemoveNodeAsync(ContentNode node);

        Task<List<ContentNode>> GetSubtreeAsync(int courseVersionId, string materializedPathPrefix);

        // ----- node type rules -----
        Task<bool> NodeTypeAllowedAsync(int? subjectId, NodeType? parentType, NodeType childType);

        // ----- revisions -----
        Task<List<NodeRevision>> GetRevisionsAsync(int nodeId);
        Task<NodeRevision?> GetRevisionAsync(int nodeId, int revisionNumber);
        Task<int> NextRevisionNumberAsync(int nodeId);
        Task AddRevisionAsync(NodeRevision revision);

        // ----- blocks -----
        Task<List<ContentBlock>> GetBlocksAsync(int nodeId);
        Task<ContentBlock?> GetBlockAsync(int blockId);
        Task<int> MaxBlockOrderAsync(int nodeId);
        Task<ContentBlock> AddBlockAsync(ContentBlock block);
        Task RemoveBlockAsync(ContentBlock block);

        // ----- resources -----
        Task<List<LessonResource>> GetResourcesAsync(int nodeId);
        Task<LessonResource?> GetResourceAsync(int resourceId);
        Task<int> MaxResourceOrderAsync(int nodeId);
        Task<LessonResource> AddResourceAsync(LessonResource resource);
        Task RemoveResourceAsync(LessonResource resource);

        // ----- flashcards -----
        Task<List<FlashcardDeck>> GetDecksAsync(int nodeId);
        Task<FlashcardDeck?> GetDeckAsync(int deckId);
        Task<FlashcardDeck?> GetDeckWithCardsAsync(int deckId);
        Task<FlashcardDeck> AddDeckAsync(FlashcardDeck deck);
        Task RemoveDeckAsync(FlashcardDeck deck);
        Task<Flashcard?> GetCardAsync(int cardId);
        Task<int> MaxCardOrderAsync(int deckId);
        Task<Flashcard> AddCardAsync(Flashcard card);
        Task RemoveCardAsync(Flashcard card);
    }
}
