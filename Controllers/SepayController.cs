using System.Text.RegularExpressions;
using System.Web;
using ELearning_ToanHocHay_Control.Models.DTOs.Sepay;
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

        public SepayController(IOptions<SePayOptions> options)
        {
            _options = options.Value;
        }

        [HttpGet]
        public string CreateQr(int subscriptionId, decimal totalAmount)
        {
            var description = $"SUBSCRIPTION_{subscriptionId}";

            var qrUrl =
                $"{_options.BaseUrl}/img" +
                $"?acc={_options.VA}" +
                $"&bank={_options.BankName}" +
                $"&amount={(long)totalAmount}" +
                $"&des={HttpUtility.UrlDecode(description)}";

            return qrUrl;
        }

        [HttpPost]
        public IActionResult IPN(SePayIpnRequest request)
        {
            // subscriptionId
            // action phai in
            if (request.transferType != "in")
            {
                return Ok("request.transferType != \"in\"");
            }

            var subscriptionId = ExtractSubscriptionId(request.content);

            if (subscriptionId == null)
            {
                return Ok("Cannot find subscriptionId in content");
            }

            // lay subscriptionId ...
            return Ok("Sucess");
        }

        private int? ExtractSubscriptionId(string? text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var match = Regex.Match(
                text,
                @"SUBSCRIPTION[\-_]?(\d+)",
                RegexOptions.IgnoreCase
            );

            if (!match.Success) return null;

            return int.Parse(match.Groups[1].Value);
        }
    }
}
