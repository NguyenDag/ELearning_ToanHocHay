using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    /// <summary>
    /// A3/P2 — student/guest reading of published content. The tree returned is the full one
    /// for entitled students (enrolment or covering subscription) and free-nodes-only otherwise.
    /// </summary>
    [Route("api/learn")]
    [ApiController]
    public class LearnController : ControllerBase
    {
        private readonly ILearnService _learn;

        public LearnController(ILearnService learn)
        {
            _learn = learn;
        }

        [HttpGet("courses/{courseId:int}/content")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCourseContent(int courseId)
        {
            var r = await _learn.GetCourseContentAsync(User, courseId);
            return r.ToActionResult();
        }

        [HttpGet("nodes/{nodeId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNode(int nodeId)
        {
            return (await _learn.GetNodeAsync(User, nodeId)).ToActionResult();
        }
    }
}
