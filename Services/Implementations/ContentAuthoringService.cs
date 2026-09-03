using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Content;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class ContentAuthoringService : IContentAuthoringService
    {
        private readonly IContentRepository _repo;
        private readonly ICourseRepository _courseRepo;

        public ContentAuthoringService(IContentRepository repo, ICourseRepository courseRepo)
        {
            _repo = repo;
            _courseRepo = courseRepo;
        }

        // ================= tree / nodes =================
        public async Task<ApiResponse<List<ContentNodeDto>>> GetVersionTreeAsync(int courseVersionId)
        {
            if (await _courseRepo.GetVersionAsync(courseVersionId) == null)
                return ApiResponse<List<ContentNodeDto>>.ErrorResponse("Version not found");

            var nodes = await _repo.GetNodesByVersionAsync(courseVersionId);
            return ApiResponse<List<ContentNodeDto>>.SuccessResponse(
                ContentMapping.BuildTree(nodes, includeHidden: true));
        }

        public async Task<ApiResponse<ContentNodeDetailDto>> GetNodeAsync(int nodeId)
        {
            var node = await _repo.GetNodeWithDetailAsync(nodeId);
            return node == null
                ? ApiResponse<ContentNodeDetailDto>.ErrorResponse("Node not found")
                : ApiResponse<ContentNodeDetailDto>.SuccessResponse(ContentMapping.MapNodeDetail(node));
        }

        public async Task<ApiResponse<ContentNodeDto>> CreateNodeAsync(int courseVersionId, CreateContentNodeDto dto, int userId)
        {
            var version = await _courseRepo.GetVersionAsync(courseVersionId);
            if (version == null) return ApiResponse<ContentNodeDto>.ErrorResponse("Version not found");
            var guard = EnsureDraft(version);
            if (guard != null) return ApiResponse<ContentNodeDto>.ErrorResponse(guard);

            ContentNode? parent = null;
            if (dto.ParentNodeId.HasValue)
            {
                parent = await _repo.GetNodeAsync(dto.ParentNodeId.Value);
                if (parent == null || parent.CourseVersionId != courseVersionId)
                    return ApiResponse<ContentNodeDto>.ErrorResponse("Parent node does not belong to this version");
            }

            var subjectId = version.Course?.SubjectId;
            if (!await _repo.NodeTypeAllowedAsync(subjectId, parent?.NodeType, dto.NodeType))
                return ApiResponse<ContentNodeDto>.ErrorResponse(
                    $"A {dto.NodeType} node is not allowed under {(parent == null ? "the root" : parent.NodeType.ToString())}");

            var siblings = await _repo.GetChildrenAsync(courseVersionId, dto.ParentNodeId);
            var orderIndex = dto.OrderIndex ?? (siblings.Count == 0 ? 0 : siblings.Max(s => s.OrderIndex) + 1);

            var node = new ContentNode
            {
                CourseVersionId = courseVersionId,
                ParentNodeId = dto.ParentNodeId,
                NodeType = dto.NodeType,
                Title = dto.Title.Trim(),
                Slug = string.IsNullOrWhiteSpace(dto.Slug) ? null : dto.Slug.Trim(),
                OrderIndex = orderIndex,
                Depth = parent == null ? 0 : parent.Depth + 1,
                MaterializedPath = "/",
                IsFree = dto.IsFree,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _repo.AddNodeAsync(node);

            node.MaterializedPath = (parent?.MaterializedPath ?? "/") + node.NodeId + "/";
            if (dto.NodeType == NodeType.Lesson && dto.DurationMinutes.HasValue)
                node.LessonDetail = new LessonDetail { NodeId = node.NodeId, DurationMinutes = dto.DurationMinutes };
            await _repo.SaveAsync();

            return ApiResponse<ContentNodeDto>.SuccessResponse(ContentMapping.MapNode(node), "Node created");
        }

        public async Task<ApiResponse<ContentNodeDto>> UpdateNodeAsync(int nodeId, UpdateContentNodeDto dto, int userId)
        {
            var node = await _repo.GetNodeAsync(nodeId);
            if (node == null) return ApiResponse<ContentNodeDto>.ErrorResponse("Node not found");
            var guard = EnsureDraft(node.CourseVersion);
            if (guard != null) return ApiResponse<ContentNodeDto>.ErrorResponse(guard);

            await SnapshotAsync(node, userId); // P2 — NodeRevision before the edit

            if (!string.IsNullOrWhiteSpace(dto.Title)) node.Title = dto.Title.Trim();
            if (dto.Slug != null) node.Slug = string.IsNullOrWhiteSpace(dto.Slug) ? null : dto.Slug.Trim();
            if (dto.IsFree.HasValue) node.IsFree = dto.IsFree.Value;
            if (dto.IsHidden.HasValue) node.IsHidden = dto.IsHidden.Value;
            node.UpdatedBy = userId;
            node.UpdatedAt = DateTime.UtcNow;

            if (dto.DurationMinutes.HasValue)
            {
                if (node.LessonDetail == null)
                    node.LessonDetail = new LessonDetail { NodeId = node.NodeId, DurationMinutes = dto.DurationMinutes };
                else
                    node.LessonDetail.DurationMinutes = dto.DurationMinutes;
            }

            await _repo.SaveAsync();
            return ApiResponse<ContentNodeDto>.SuccessResponse(ContentMapping.MapNode(node), "Node updated");
        }

        public async Task<ApiResponse<bool>> DeleteNodeAsync(int nodeId)
        {
            var node = await _repo.GetNodeAsync(nodeId);
            if (node == null) return ApiResponse<bool>.ErrorResponse("Node not found");
            var guard = EnsureDraft(node.CourseVersion);
            if (guard != null) return ApiResponse<bool>.ErrorResponse(guard);

            if (await _repo.HasChildrenAsync(nodeId))
                return ApiResponse<bool>.ErrorResponse("Delete or move the child nodes first");

            await _repo.RemoveNodeAsync(node);
            return ApiResponse<bool>.SuccessResponse(true, "Node deleted");
        }

        public async Task<ApiResponse<bool>> ReorderChildrenAsync(int courseVersionId, int? parentNodeId, ReorderNodesDto dto)
        {
            var version = await _courseRepo.GetVersionAsync(courseVersionId);
            if (version == null) return ApiResponse<bool>.ErrorResponse("Version not found");
            var guard = EnsureDraft(version);
            if (guard != null) return ApiResponse<bool>.ErrorResponse(guard);

            var siblings = await _repo.GetChildrenAsync(courseVersionId, parentNodeId);
            var ids = siblings.Select(s => s.NodeId).ToHashSet();
            if (dto.OrderedNodeIds.Count != siblings.Count || !dto.OrderedNodeIds.All(ids.Contains))
                return ApiResponse<bool>.ErrorResponse("The id list must be exactly the children of this parent");

            for (var i = 0; i < dto.OrderedNodeIds.Count; i++)
                siblings.First(s => s.NodeId == dto.OrderedNodeIds[i]).OrderIndex = i;

            await _repo.SaveAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Reordered");
        }

        public async Task<ApiResponse<ContentNodeDto>> MoveNodeAsync(int nodeId, MoveNodeDto dto, int userId)
        {
            var node = await _repo.GetNodeAsync(nodeId);
            if (node == null) return ApiResponse<ContentNodeDto>.ErrorResponse("Node not found");
            var version = node.CourseVersion;
            var guard = EnsureDraft(version);
            if (guard != null) return ApiResponse<ContentNodeDto>.ErrorResponse(guard);

            ContentNode? newParent = null;
            if (dto.NewParentNodeId.HasValue)
            {
                if (dto.NewParentNodeId.Value == nodeId)
                    return ApiResponse<ContentNodeDto>.ErrorResponse("A node cannot be its own parent");
                newParent = await _repo.GetNodeAsync(dto.NewParentNodeId.Value);
                if (newParent == null || newParent.CourseVersionId != node.CourseVersionId)
                    return ApiResponse<ContentNodeDto>.ErrorResponse("New parent does not belong to this version");
                if (newParent.MaterializedPath.StartsWith(node.MaterializedPath))
                    return ApiResponse<ContentNodeDto>.ErrorResponse("Cannot move a node into its own subtree");
            }

            var courseSubjectId = (await _courseRepo.GetVersionAsync(node.CourseVersionId))?.Course?.SubjectId;
            if (!await _repo.NodeTypeAllowedAsync(courseSubjectId, newParent?.NodeType, node.NodeType))
                return ApiResponse<ContentNodeDto>.ErrorResponse(
                    $"A {node.NodeType} node is not allowed under {(newParent == null ? "the root" : newParent.NodeType.ToString())}");

            var oldPrefix = node.MaterializedPath;
            var subtree = await _repo.GetSubtreeAsync(node.CourseVersionId, oldPrefix);

            var siblings = await _repo.GetChildrenAsync(node.CourseVersionId, dto.NewParentNodeId);
            node.ParentNodeId = dto.NewParentNodeId;
            node.Depth = newParent == null ? 0 : newParent.Depth + 1;
            node.OrderIndex = dto.OrderIndex
                ?? (siblings.Where(s => s.NodeId != nodeId).Select(s => (int?)s.OrderIndex).DefaultIfEmpty(-1).Max()!.Value + 1);
            var newPrefix = (newParent?.MaterializedPath ?? "/") + node.NodeId + "/";

            foreach (var d in subtree)
            {
                d.MaterializedPath = newPrefix + d.MaterializedPath[oldPrefix.Length..];
                d.Depth = node.Depth + (d.MaterializedPath.Count(c => c == '/') - newPrefix.Count(c => c == '/'));
            }
            node.MaterializedPath = newPrefix;
            node.UpdatedBy = userId;
            node.UpdatedAt = DateTime.UtcNow;

            await _repo.SaveAsync();
            return ApiResponse<ContentNodeDto>.SuccessResponse(ContentMapping.MapNode(node), "Node moved");
        }

        // ================= revisions =================
        public async Task<ApiResponse<List<NodeRevisionDto>>> GetRevisionsAsync(int nodeId)
        {
            if (await _repo.GetNodeAsync(nodeId) == null)
                return ApiResponse<List<NodeRevisionDto>>.ErrorResponse("Node not found");

            var revs = await _repo.GetRevisionsAsync(nodeId);
            return ApiResponse<List<NodeRevisionDto>>.SuccessResponse(revs.Select(r => new NodeRevisionDto
            {
                RevisionId = r.RevisionId,
                NodeId = r.NodeId,
                RevisionNumber = r.RevisionNumber,
                Snapshot = r.Snapshot,
                EditedBy = r.EditedBy,
                EditedAt = r.EditedAt
            }).ToList());
        }

        public async Task<ApiResponse<ContentNodeDto>> RestoreRevisionAsync(int nodeId, int revisionNumber, int userId)
        {
            var node = await _repo.GetNodeAsync(nodeId);
            if (node == null) return ApiResponse<ContentNodeDto>.ErrorResponse("Node not found");
            var guard = EnsureDraft(node.CourseVersion);
            if (guard != null) return ApiResponse<ContentNodeDto>.ErrorResponse(guard);

            var rev = await _repo.GetRevisionAsync(nodeId, revisionNumber);
            if (rev?.Snapshot == null) return ApiResponse<ContentNodeDto>.ErrorResponse("Revision not found");

            var snap = System.Text.Json.JsonSerializer.Deserialize<NodeSnapshot>(rev.Snapshot);
            if (snap == null) return ApiResponse<ContentNodeDto>.ErrorResponse("Revision snapshot is corrupt");

            await SnapshotAsync(node, userId); // snapshot current state before restoring

            node.Title = snap.Title;
            node.Slug = snap.Slug;
            node.IsFree = snap.IsFree;
            node.IsHidden = snap.IsHidden;
            if (node.LessonDetail != null) node.LessonDetail.DurationMinutes = snap.DurationMinutes;
            node.UpdatedBy = userId;
            node.UpdatedAt = DateTime.UtcNow;
            await _repo.SaveAsync();

            return ApiResponse<ContentNodeDto>.SuccessResponse(ContentMapping.MapNode(node),
                $"Restored to revision {revisionNumber}");
        }

        private record NodeSnapshot(string Title, string? Slug, bool IsFree, bool IsHidden, int? DurationMinutes);

        private async Task SnapshotAsync(ContentNode node, int userId)
        {
            var snapshot = System.Text.Json.JsonSerializer.Serialize(new NodeSnapshot(
                node.Title, node.Slug, node.IsFree, node.IsHidden, node.LessonDetail?.DurationMinutes));

            await _repo.AddRevisionAsync(new NodeRevision
            {
                NodeId = node.NodeId,
                RevisionNumber = await _repo.NextRevisionNumberAsync(node.NodeId),
                Snapshot = snapshot,
                EditedBy = userId,
                EditedAt = DateTime.UtcNow
            });
        }

        // ================= blocks =================
        public async Task<ApiResponse<ContentBlockDto>> AddBlockAsync(int nodeId, ContentBlockRequestDto dto)
        {
            var node = await _repo.GetNodeAsync(nodeId);
            if (node == null) return ApiResponse<ContentBlockDto>.ErrorResponse("Node not found");
            var guard = EnsureDraft(node.CourseVersion);
            if (guard != null) return ApiResponse<ContentBlockDto>.ErrorResponse(guard);

            var block = new ContentBlock
            {
                NodeId = nodeId,
                BlockType = dto.BlockType,
                ContentText = dto.ContentText,
                ContentUrl = dto.ContentUrl,
                MetadataJson = dto.MetadataJson,
                OrderIndex = dto.OrderIndex ?? await _repo.MaxBlockOrderAsync(nodeId) + 1
            };
            await _repo.AddBlockAsync(block);
            return ApiResponse<ContentBlockDto>.SuccessResponse(ContentMapping.MapBlock(block), "Block added");
        }

        public async Task<ApiResponse<ContentBlockDto>> UpdateBlockAsync(int blockId, ContentBlockRequestDto dto)
        {
            var block = await _repo.GetBlockAsync(blockId);
            if (block == null) return ApiResponse<ContentBlockDto>.ErrorResponse("Block not found");
            var guard = EnsureDraft(block.Node?.CourseVersion);
            if (guard != null) return ApiResponse<ContentBlockDto>.ErrorResponse(guard);

            block.BlockType = dto.BlockType;
            block.ContentText = dto.ContentText;
            block.ContentUrl = dto.ContentUrl;
            block.MetadataJson = dto.MetadataJson;
            if (dto.OrderIndex.HasValue) block.OrderIndex = dto.OrderIndex.Value;
            await _repo.SaveAsync();
            return ApiResponse<ContentBlockDto>.SuccessResponse(ContentMapping.MapBlock(block), "Block updated");
        }

        public async Task<ApiResponse<bool>> DeleteBlockAsync(int blockId)
        {
            var block = await _repo.GetBlockAsync(blockId);
            if (block == null) return ApiResponse<bool>.ErrorResponse("Block not found");
            var guard = EnsureDraft(block.Node?.CourseVersion);
            if (guard != null) return ApiResponse<bool>.ErrorResponse(guard);

            await _repo.RemoveBlockAsync(block);
            return ApiResponse<bool>.SuccessResponse(true, "Block deleted");
        }

        // ================= resources =================
        public async Task<ApiResponse<LessonResourceDto>> AddResourceAsync(int nodeId, LessonResourceRequestDto dto)
        {
            var node = await _repo.GetNodeAsync(nodeId);
            if (node == null) return ApiResponse<LessonResourceDto>.ErrorResponse("Node not found");
            var guard = EnsureDraft(node.CourseVersion);
            if (guard != null) return ApiResponse<LessonResourceDto>.ErrorResponse(guard);

            var resource = new LessonResource
            {
                NodeId = nodeId,
                Title = dto.Title.Trim(),
                ResourceType = dto.ResourceType,
                MediaAssetId = dto.MediaAssetId,
                ExternalUrl = dto.ExternalUrl,
                IsDownloadable = dto.IsDownloadable,
                OrderIndex = dto.OrderIndex ?? await _repo.MaxResourceOrderAsync(nodeId) + 1
            };
            await _repo.AddResourceAsync(resource);
            return ApiResponse<LessonResourceDto>.SuccessResponse(ContentMapping.MapResource(resource), "Resource added");
        }

        public async Task<ApiResponse<LessonResourceDto>> UpdateResourceAsync(int resourceId, LessonResourceRequestDto dto)
        {
            var resource = await _repo.GetResourceAsync(resourceId);
            if (resource == null) return ApiResponse<LessonResourceDto>.ErrorResponse("Resource not found");
            var guard = EnsureDraft(resource.Node?.CourseVersion);
            if (guard != null) return ApiResponse<LessonResourceDto>.ErrorResponse(guard);

            resource.Title = dto.Title.Trim();
            resource.ResourceType = dto.ResourceType;
            resource.MediaAssetId = dto.MediaAssetId;
            resource.ExternalUrl = dto.ExternalUrl;
            resource.IsDownloadable = dto.IsDownloadable;
            if (dto.OrderIndex.HasValue) resource.OrderIndex = dto.OrderIndex.Value;
            await _repo.SaveAsync();
            return ApiResponse<LessonResourceDto>.SuccessResponse(ContentMapping.MapResource(resource), "Resource updated");
        }

        public async Task<ApiResponse<bool>> DeleteResourceAsync(int resourceId)
        {
            var resource = await _repo.GetResourceAsync(resourceId);
            if (resource == null) return ApiResponse<bool>.ErrorResponse("Resource not found");
            var guard = EnsureDraft(resource.Node?.CourseVersion);
            if (guard != null) return ApiResponse<bool>.ErrorResponse(guard);

            await _repo.RemoveResourceAsync(resource);
            return ApiResponse<bool>.SuccessResponse(true, "Resource deleted");
        }

        // ================= flashcards =================
        public async Task<ApiResponse<FlashcardDeckDto>> AddDeckAsync(int nodeId, FlashcardDeckRequestDto dto)
        {
            var node = await _repo.GetNodeAsync(nodeId);
            if (node == null) return ApiResponse<FlashcardDeckDto>.ErrorResponse("Node not found");
            var guard = EnsureDraft(node.CourseVersion);
            if (guard != null) return ApiResponse<FlashcardDeckDto>.ErrorResponse(guard);

            var deck = new FlashcardDeck { NodeId = nodeId, Title = dto.Title.Trim(), CreatedAt = DateTime.UtcNow };
            await _repo.AddDeckAsync(deck);
            return ApiResponse<FlashcardDeckDto>.SuccessResponse(ContentMapping.MapDeck(deck), "Deck added");
        }

        public async Task<ApiResponse<bool>> DeleteDeckAsync(int deckId)
        {
            var deck = await _repo.GetDeckAsync(deckId);
            if (deck == null) return ApiResponse<bool>.ErrorResponse("Deck not found");
            var guard = EnsureDraft(deck.Node?.CourseVersion);
            if (guard != null) return ApiResponse<bool>.ErrorResponse(guard);

            await _repo.RemoveDeckAsync(deck);
            return ApiResponse<bool>.SuccessResponse(true, "Deck deleted");
        }

        public async Task<ApiResponse<FlashcardDto>> AddCardAsync(int deckId, FlashcardRequestDto dto)
        {
            var deck = await _repo.GetDeckAsync(deckId);
            if (deck == null) return ApiResponse<FlashcardDto>.ErrorResponse("Deck not found");
            var guard = EnsureDraft(deck.Node?.CourseVersion);
            if (guard != null) return ApiResponse<FlashcardDto>.ErrorResponse(guard);

            var card = new Flashcard
            {
                DeckId = deckId,
                FrontText = dto.FrontText.Trim(),
                BackText = dto.BackText.Trim(),
                FrontImageUrl = dto.FrontImageUrl,
                BackImageUrl = dto.BackImageUrl,
                Hint = dto.Hint,
                OrderIndex = dto.OrderIndex ?? await _repo.MaxCardOrderAsync(deckId) + 1
            };
            await _repo.AddCardAsync(card);
            return ApiResponse<FlashcardDto>.SuccessResponse(ContentMapping.MapCard(card), "Card added");
        }

        public async Task<ApiResponse<FlashcardDto>> UpdateCardAsync(int cardId, FlashcardRequestDto dto)
        {
            var card = await _repo.GetCardAsync(cardId);
            if (card == null) return ApiResponse<FlashcardDto>.ErrorResponse("Card not found");
            var guard = EnsureDraft(card.Deck?.Node?.CourseVersion);
            if (guard != null) return ApiResponse<FlashcardDto>.ErrorResponse(guard);

            card.FrontText = dto.FrontText.Trim();
            card.BackText = dto.BackText.Trim();
            card.FrontImageUrl = dto.FrontImageUrl;
            card.BackImageUrl = dto.BackImageUrl;
            card.Hint = dto.Hint;
            if (dto.OrderIndex.HasValue) card.OrderIndex = dto.OrderIndex.Value;
            await _repo.SaveAsync();
            return ApiResponse<FlashcardDto>.SuccessResponse(ContentMapping.MapCard(card), "Card updated");
        }

        public async Task<ApiResponse<bool>> DeleteCardAsync(int cardId)
        {
            var card = await _repo.GetCardAsync(cardId);
            if (card == null) return ApiResponse<bool>.ErrorResponse("Card not found");
            var guard = EnsureDraft(card.Deck?.Node?.CourseVersion);
            if (guard != null) return ApiResponse<bool>.ErrorResponse(guard);

            await _repo.RemoveCardAsync(card);
            return ApiResponse<bool>.SuccessResponse(true, "Card deleted");
        }

        // ================= helpers =================
        private static string? EnsureDraft(CourseVersion? version)
        {
            if (version == null) return "Owning course version not found";
            return version.State == VersionState.Draft
                ? null
                : $"Content can only be edited while the version is in Draft (current: {version.State})";
        }
    }
}
