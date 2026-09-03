using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    /// <summary>P5 — finance operations: subscription lifecycle sweep + reconciliation.</summary>
    [Route("api/finance")]
    [ApiController]
    [AuthorizeUserType(UserType.FinanceManager, UserType.SystemAdmin)]
    public class FinanceController : ControllerBase
    {
        private readonly ISubscriptionLifecycleService _lifecycle;

        public FinanceController(ISubscriptionLifecycleService lifecycle)
        {
            _lifecycle = lifecycle;
        }

        [HttpPost("subscriptions/run-lifecycle")]
        public async Task<IActionResult> RunLifecycle()
        {
            var result = await _lifecycle.RunSweepAsync();
            return Ok(new { expiredActive = result.ExpiredActive, releasedPending = result.ReleasedPending });
        }

        [HttpGet("subscriptions/reconciliation")]
        public async Task<IActionResult> Reconciliation()
            => Ok(await _lifecycle.BuildReconciliationAsync());
    }
}
