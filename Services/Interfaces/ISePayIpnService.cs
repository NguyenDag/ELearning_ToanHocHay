using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.Sepay;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public record IpnResult(IpnOutcome Outcome, string Message);

    /// <summary>P5 (A2-10) — processes a SePay IPN callback: async, transactional, idempotent.</summary>
    public interface ISePayIpnService
    {
        Task<IpnResult> ProcessAsync(SePayIpnRequest request);
    }
}
