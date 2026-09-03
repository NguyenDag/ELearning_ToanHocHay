using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Models.DTOs.Content;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    /// <summary>
    /// A3/P2 — authoring the content tree. Content-management roles only; every mutation
    /// requires the owning CourseVersion to be in Draft.
    /// </summary>
    [Route("api/content")]
    [ApiController]
    [AuthorizeContentRole]
    public class ContentAuthoringController : ControllerBase
    {
        private readonly IContentAuthoringService _content;

        public ContentAuthoringController(IContentAuthoringService content)
        {
            _content = content;
        }

        private int Uid => User.GetUserId()!.Value;

        // ---------------- tree / nodes ----------------
        [HttpGet("versions/{versionId:int}/tree")]
        public async Task<IActionResult> GetTree(int versionId)
        {
            var r = await _content.GetVersionTreeAsync(versionId);
            return r.ToActionResult();
        }

        [HttpGet("nodes/{nodeId:int}")]
        public async Task<IActionResult> GetNode(int nodeId)
        {
            var r = await _content.GetNodeAsync(nodeId);
            return r.ToActionResult();
        }

        [HttpPost("versions/{versionId:int}/nodes")]
        public async Task<IActionResult> CreateNode(int versionId, [FromBody] CreateContentNodeDto dto)
        {
            var r = await _content.CreateNodeAsync(versionId, dto, Uid);
            return r.ToActionResult();
        }

        [HttpPut("nodes/{nodeId:int}")]
        public async Task<IActionResult> UpdateNode(int nodeId, [FromBody] UpdateContentNodeDto dto)
        {
            var r = await _content.UpdateNodeAsync(nodeId, dto, Uid);
            return r.ToActionResult();
        }

        [HttpDelete("nodes/{nodeId:int}")]
        public async Task<IActionResult> DeleteNode(int nodeId)
        {
            var r = await _content.DeleteNodeAsync(nodeId);
            return r.ToActionResult();
        }

        [HttpPost("versions/{versionId:int}/nodes/reorder")]
        public async Task<IActionResult> Reorder(int versionId, [FromQuery] int? parentNodeId, [FromBody] ReorderNodesDto dto)
        {
            var r = await _content.ReorderChildrenAsync(versionId, parentNodeId, dto);
            return r.ToActionResult();
        }

        [HttpPatch("nodes/{nodeId:int}/move")]
        public async Task<IActionResult> MoveNode(int nodeId, [FromBody] MoveNodeDto dto)
        {
            var r = await _content.MoveNodeAsync(nodeId, dto, Uid);
            return r.ToActionResult();
        }

        [HttpGet("nodes/{nodeId:int}/revisions")]
        public async Task<IActionResult> GetRevisions(int nodeId)
        {
            var r = await _content.GetRevisionsAsync(nodeId);
            return r.ToActionResult();
        }

        [HttpPost("nodes/{nodeId:int}/revisions/{revisionNumber:int}/restore")]
        public async Task<IActionResult> RestoreRevision(int nodeId, int revisionNumber)
        {
            var r = await _content.RestoreRevisionAsync(nodeId, revisionNumber, Uid);
            return r.ToActionResult();
        }

        // ---------------- blocks ----------------
        [HttpPost("nodes/{nodeId:int}/blocks")]
        public async Task<IActionResult> AddBlock(int nodeId, [FromBody] ContentBlockRequestDto dto)
        {
            var r = await _content.AddBlockAsync(nodeId, dto);
            return r.ToActionResult();
        }

        [HttpPut("blocks/{blockId:int}")]
        public async Task<IActionResult> UpdateBlock(int blockId, [FromBody] ContentBlockRequestDto dto)
        {
            var r = await _content.UpdateBlockAsync(blockId, dto);
            return r.ToActionResult();
        }

        [HttpDelete("blocks/{blockId:int}")]
        public async Task<IActionResult> DeleteBlock(int blockId)
        {
            var r = await _content.DeleteBlockAsync(blockId);
            return r.ToActionResult();
        }

        // ---------------- resources ----------------
        [HttpPost("nodes/{nodeId:int}/resources")]
        public async Task<IActionResult> AddResource(int nodeId, [FromBody] LessonResourceRequestDto dto)
        {
            var r = await _content.AddResourceAsync(nodeId, dto);
            return r.ToActionResult();
        }

        [HttpPut("resources/{resourceId:int}")]
        public async Task<IActionResult> UpdateResource(int resourceId, [FromBody] LessonResourceRequestDto dto)
        {
            var r = await _content.UpdateResourceAsync(resourceId, dto);
            return r.ToActionResult();
        }

        [HttpDelete("resources/{resourceId:int}")]
        public async Task<IActionResult> DeleteResource(int resourceId)
        {
            var r = await _content.DeleteResourceAsync(resourceId);
            return r.ToActionResult();
        }

        // ---------------- flashcards ----------------
        [HttpPost("nodes/{nodeId:int}/flashcard-decks")]
        public async Task<IActionResult> AddDeck(int nodeId, [FromBody] FlashcardDeckRequestDto dto)
        {
            var r = await _content.AddDeckAsync(nodeId, dto);
            return r.ToActionResult();
        }

        [HttpDelete("flashcard-decks/{deckId:int}")]
        public async Task<IActionResult> DeleteDeck(int deckId)
        {
            var r = await _content.DeleteDeckAsync(deckId);
            return r.ToActionResult();
        }

        [HttpPost("flashcard-decks/{deckId:int}/cards")]
        public async Task<IActionResult> AddCard(int deckId, [FromBody] FlashcardRequestDto dto)
        {
            var r = await _content.AddCardAsync(deckId, dto);
            return r.ToActionResult();
        }

        [HttpPut("flashcards/{cardId:int}")]
        public async Task<IActionResult> UpdateCard(int cardId, [FromBody] FlashcardRequestDto dto)
        {
            var r = await _content.UpdateCardAsync(cardId, dto);
            return r.ToActionResult();
        }

        [HttpDelete("flashcards/{cardId:int}")]
        public async Task<IActionResult> DeleteCard(int cardId)
        {
            var r = await _content.DeleteCardAsync(cardId);
            return r.ToActionResult();
        }
    }
}
