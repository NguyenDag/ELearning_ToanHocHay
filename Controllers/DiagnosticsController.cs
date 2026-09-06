#if DEBUG
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    /// <summary>
    /// DEBUG-only test hooks. Excluded from Release builds entirely — never reachable in production.
    /// Used by the integration suite to exercise the global exception handler (IT-F12-12).
    /// </summary>
    [Route("api/_diag")]
    [ApiController]
    [AllowAnonymous]
    public class DiagnosticsController : ControllerBase
    {
        [HttpGet("boom")]
        public IActionResult Boom()
            => throw new InvalidOperationException("secret-internal-detail-should-not-leak");
    }
}
#endif
