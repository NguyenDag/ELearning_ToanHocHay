using ELearning_ToanHocHay_Control.Models.DTOs.Sepay;
using ELearning_ToanHocHay_Control.Services.Implementations;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ELearning_ToanHocHay.Tests.Unit.Sepay;

/// <summary>§3.17 — UT-SEPAY-SVC-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class SePayServiceTests
{
    private const string ApiKey = "sepay-secret-key";

    private static SePayService Svc(string? apiKeyValidator = ApiKey)
    {
        var options = Options.Create(new SePayOptions
        {
            BaseUrl = "https://qr.sepay.vn",
            VA = "VA123456",
            BankName = "MBBank",
            ApiKeyValidator = apiKeyValidator!,
        });
        return new SePayService(options, NullLogger<SePayService>.Instance);
    }

    [Fact] // UT-SEPAY-SVC-01
    public void GenerateQrUrl_carries_account_bank_amount()
    {
        var url = Svc().GenerateQrUrl(12, 199000m);
        url.Should().Contain("acc=VA123456").And.Contain("bank=MBBank").And.Contain("amount=199000");
    }

    [Fact] // UT-SEPAY-SVC-02
    public void GenerateQrUrl_description_is_url_escaped()
        => Svc().GenerateQrUrl(12, 199000m)
            .Should().Contain("des=" + Uri.EscapeDataString("TKPTTS SUBSCRIPTION_12"));

    [Fact] // UT-SEPAY-SVC-03
    public void GenerateQrUrl_amount_is_truncated_to_long()
        => Svc().GenerateQrUrl(1, 199000.7m).Should().Contain("amount=199000").And.NotContain("199000.7");

    [Fact] // UT-SEPAY-SVC-04
    public void GenerateQrUrl_prefix()
        => Svc().GenerateQrUrl(1, 199000m).Should().StartWith("https://qr.sepay.vn/img?");

    [Theory] // UT-SEPAY-SVC-05 / 06 / 07
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sai-key")]
    public void ValidateApiKey_rejects(string? key)
        => Svc().ValidateApiKey(key).Should().BeFalse();

    [Fact] // UT-SEPAY-SVC-08
    public void ValidateApiKey_accepts_configured_key()
        => Svc().ValidateApiKey(ApiKey).Should().BeTrue();

    [Theory] // UT-SEPAY-SVC-09 — fail-closed khi không cấu hình
    [InlineData(null)]
    [InlineData("")]
    public void ValidateApiKey_fails_closed_when_validator_unset(string? validator)
        => Svc(apiKeyValidator: validator).ValidateApiKey("anything").Should().BeFalse();
}
