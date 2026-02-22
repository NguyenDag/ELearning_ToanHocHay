using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Sepay;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SepayController : ControllerBase
    {
        private readonly SePayOptions _options;
        private readonly ISePayService _sePayService;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly AppDbContext _context;

        public SepayController(IOptions<SePayOptions> options, ISePayService sePayService, ISubscriptionRepository subscriptionRepository, AppDbContext context)
        {
            _options = options.Value;
            _sePayService = sePayService;
            _subscriptionRepository = subscriptionRepository;
            _context = context;
        }

        [HttpPost]
        [SePayApiKey] // Attribute tự động validate API Key
        public async Task<IActionResult> IPN(
            [FromBody] SePayIpnRequest request
        )
        {
            // 1. Chỉ xử lý tiền vào
            if (request.transferType != "in")
                return Ok("Ignore out transaction");

            // 2. Parse subscriptionId
            var subscriptionId = _sePayService.ExtractSubscriptionId(request.content);
            if (subscriptionId == null)
                return Ok("Invalid content");

            var subscription = await _subscriptionRepository.GetByIdAsync((int)subscriptionId);

            if (subscription == null)
                return Ok("Subscription not found");

            // 3. Chống IPN trùng
            if (subscription.Status == SubscriptionStatus.Active)
                return Ok("Already processed");

            // 4. Check amount
            if (!_sePayService.IsValidAmount(
                request.transferAmount,
                subscription.AmountPaid))
            {
                return Ok("Amount mismatch");
            }

            // 5. Update Payment
            subscription.Payment.Status = PaymentStatus.Completed;
            subscription.Payment.TransactionId = request.referenceCode;
            subscription.Payment.PaymentDate = DateTime.UtcNow;

            // 6. Active Subscription
            subscription.Status = SubscriptionStatus.Active;
            subscription.StartDate = DateTime.UtcNow;
            subscription.EndDate = DateTime.UtcNow.AddMonths(1);

            _context.SaveChanges();

            return Ok("Success");
        }
    }
}
