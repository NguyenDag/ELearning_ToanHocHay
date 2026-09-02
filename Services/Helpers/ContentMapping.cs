using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Content;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>Shared entity → DTO mapping for the content tree.</summary>
    public static class ContentMapping
    {
        public static ContentNodeDto MapNode(ContentNode n) => new()
        {
            NodeId = n.NodeId,
            CourseVersionId = n.CourseVersionId,
            ParentNodeId = n.ParentNodeId,
            NodeType = n.NodeType,
            Title = n.Title,
            Slug = n.Slug,
            OrderIndex = n.OrderIndex,
            Depth = n.Depth,
            MaterializedPath = n.MaterializedPath,
            IsFree = n.IsFree,
            IsHidden = n.IsHidden,
            DurationMinutes = n.LessonDetail?.DurationMinutes
        };

        /// <summary>Builds a nested tree from a flat node list. Optionally drops hidden nodes.</summary>
        public static List<ContentNodeDto> BuildTree(IEnumerable<ContentNode> nodes, bool includeHidden)
        {
            var dtos = nodes
                .Where(n => includeHidden || !n.IsHidden)
                .Select(MapNode)
                .ToList();

            var byId = dtos.ToDictionary(d => d.NodeId);
            var roots = new List<ContentNodeDto>();

            foreach (var d in dtos)
            {
                if (d.ParentNodeId is int pid && byId.TryGetValue(pid, out var parent))
                    parent.Children.Add(d);
                else
                    roots.Add(d);
            }

            void Sort(List<ContentNodeDto> list)
            {
                list.Sort((a, b) => a.OrderIndex.CompareTo(b.OrderIndex));
                foreach (var c in list) Sort(c.Children);
            }
            Sort(roots);
            return roots;
        }

        public static ContentBlockDto MapBlock(ContentBlock b) => new()
        {
            BlockId = b.BlockId,
            NodeId = b.NodeId,
            BlockType = b.BlockType,
            ContentText = b.ContentText,
            ContentUrl = b.ContentUrl,
            MetadataJson = b.MetadataJson,
            OrderIndex = b.OrderIndex
        };

        public static LessonResourceDto MapResource(LessonResource r) => new()
        {
            ResourceId = r.ResourceId,
            NodeId = r.NodeId,
            Title = r.Title,
            ResourceType = r.ResourceType,
            MediaAssetId = r.MediaAssetId,
            ExternalUrl = r.ExternalUrl,
            IsDownloadable = r.IsDownloadable,
            OrderIndex = r.OrderIndex
        };

        public static FlashcardDto MapCard(Flashcard c) => new()
        {
            CardId = c.CardId,
            DeckId = c.DeckId,
            FrontText = c.FrontText,
            BackText = c.BackText,
            FrontImageUrl = c.FrontImageUrl,
            BackImageUrl = c.BackImageUrl,
            Hint = c.Hint,
            OrderIndex = c.OrderIndex
        };

        public static FlashcardDeckDto MapDeck(FlashcardDeck d) => new()
        {
            DeckId = d.DeckId,
            NodeId = d.NodeId,
            Title = d.Title,
            Cards = (d.Cards ?? new List<Flashcard>())
                .OrderBy(c => c.OrderIndex)
                .Select(MapCard)
                .ToList()
        };

        public static ContentNodeDetailDto MapNodeDetail(ContentNode n)
        {
            var baseDto = MapNode(n);
            return new ContentNodeDetailDto
            {
                NodeId = baseDto.NodeId,
                CourseVersionId = baseDto.CourseVersionId,
                ParentNodeId = baseDto.ParentNodeId,
                NodeType = baseDto.NodeType,
                Title = baseDto.Title,
                Slug = baseDto.Slug,
                OrderIndex = baseDto.OrderIndex,
                Depth = baseDto.Depth,
                MaterializedPath = baseDto.MaterializedPath,
                IsFree = baseDto.IsFree,
                IsHidden = baseDto.IsHidden,
                DurationMinutes = baseDto.DurationMinutes,
                Blocks = (n.Blocks ?? new List<ContentBlock>())
                    .OrderBy(b => b.OrderIndex).Select(MapBlock).ToList(),
                Resources = (n.Resources ?? new List<LessonResource>())
                    .OrderBy(r => r.OrderIndex).Select(MapResource).ToList(),
                FlashcardDecks = (n.FlashcardDecks ?? new List<FlashcardDeck>())
                    .Select(MapDeck).ToList()
            };
        }
    }
}
