using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Sepay;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/sepay/[action]")]
    [ApiController]
    public class SepayController : ControllerBase
    {
        private readonly ISePayIpnService _ipnService;
        private readonly ILogger<SepayController> _logger;

        public SepayController(ISePayIpnService ipnService, ILogger<SepayController> logger)
        {
            _ipnService = ipnService;
            _logger = logger;
        }

        /// <summary>
        /// SePay IPN callback. Authenticated by SePay's API key (not JWT). Always returns 200
        /// with a message; the raw payload + outcome are persisted to SePayIpnLog.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [SePayApiKey]
        public async Task<IActionResult> IPN([FromBody] SePayIpnRequest request)
        {
            try
            {
                var result = await _ipnService.ProcessAsync(request);
                return Ok(new { outcome = result.Outcome.ToString(), message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IPN processing failed for referenceCode {Ref}", request.referenceCode);
                // 500 so SePay retries — the log row (if written) records the failure.
                return StatusCode(500, new { outcome = IpnOutcome.Error.ToString(), message = "Processing error" });
            }
        }
    }
}
