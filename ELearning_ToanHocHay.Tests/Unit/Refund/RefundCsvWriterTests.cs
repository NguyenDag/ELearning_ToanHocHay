using System.Text;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Refund;

/// <summary>§3.19 — UT-CSV-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class RefundCsvWriterTests
{
    private const string Header = "STT,SoTaiKhoan,TenNguoiHuong,MaNganHang,SoTien,NoiDung";

    private static readonly FakeRefundFieldProtector Protector = new();

    private static RefundRequest Req(decimal amount = 199000m, Action<RefundRequest>? tweak = null)
        => Entities.NewRefundRequest(paymentId: 1, amount: amount, tweak: r =>
        {
            r.BankAccountNumberProtected = FakeRefundFieldProtector.Prefix + "0071000123456";
            r.BankBin = "970418";
            r.BankAccountHolderName = "Nguyen Van A";
            tweak?.Invoke(r);
        });

    private static string[] Lines(IReadOnlyList<RefundRequest> items, IRefundFieldProtector? p = null)
        => Encoding.UTF8.GetString(RefundCsvWriter.Build(items, p ?? Protector))
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

    [Fact] // UT-CSV-01
    public void Header_and_column_order()
    {
        var lines = Lines(new[] { Req() });

        lines[0].Should().Be(Header);
        lines[1].Split(',').Should().SatisfyRespectively(
            stt => stt.Should().Be("1"),
            acc => acc.Should().Be("0071000123456"),
            name => name.Should().Be("Nguyen Van A"),
            bin => bin.Should().Be("970418"),
            money => money.Should().Be("199000"),
            _ => { });
    }

    [Theory]
    [InlineData(199000.6, "199001")]  // UT-CSV-02
    [InlineData(199000.4, "199000")]  // UT-CSV-03
    public void Amount_is_rounded_away_from_zero_to_integer(double amount, string expected)
        => Lines(new[] { Req((decimal)amount) })[1].Split(',')[4].Should().Be(expected);

    [Fact] // UT-CSV-04
    public void Formula_injection_name_gets_leading_apostrophe()
        => Lines(new[] { Req(tweak: r => r.BankAccountHolderName = "=SUM(A1)") })[1]
            .Should().Contain("'=SUM(A1)");

    [Theory] // UT-CSV-05
    [InlineData("+1")]
    [InlineData("-1")]
    [InlineData("@cmd")]
    public void Dangerous_prefixes_get_leading_apostrophe(string name)
        => Lines(new[] { Req(tweak: r => r.BankAccountHolderName = name) })[1]
            .Should().Contain("'" + name);

    [Fact] // UT-CSV-06
    public void Name_with_comma_is_quoted()
        => Lines(new[] { Req(tweak: r => r.BankAccountHolderName = "Nguyen, Van A") })[1]
            .Should().Contain("\"Nguyen, Van A\"");

    [Fact] // UT-CSV-07
    public void Name_with_quote_is_escaped_inside_quotes()
        => Lines(new[] { Req(tweak: r => r.BankAccountHolderName = "Nguyen \"Tai\" A") })[1]
            .Should().Contain("\"Nguyen \"\"Tai\"\" A\"");

    [Fact] // UT-CSV-08
    public void Vietnamese_name_is_transliterated_to_ascii()
        => Lines(new[] { Req(tweak: r => r.BankAccountHolderName = "Nguyễn Văn Đức") })[1]
            .Should().Contain("Nguyen Van Duc");

    [Fact] // UT-CSV-09
    public void Content_column_format()
    {
        var req = Req(tweak: r => r.ReasonCode = RefundReasonCode.CustomerRequest);
        Lines(new[] { req })[1]
            .Should().Contain($"HOAN TIEN CustomerRequest REF {req.PublicId:N}");
    }

    [Fact] // UT-CSV-10
    public void Decrypt_failure_yields_marker_not_broken_file()
    {
        var lines = Lines(new[] { Req() }, new FakeRefundFieldProtector { ThrowOnUnprotect = true });

        lines.Should().HaveCount(2);
        lines[1].Split(',')[1].Should().Be("DECRYPT_ERROR");
    }

    [Fact] // UT-CSV-11
    public void Sequential_row_numbers()
    {
        var lines = Lines(new[] { Req(), Req(), Req() });

        lines[1].Split(',')[0].Should().Be("1");
        lines[2].Split(',')[0].Should().Be("2");
        lines[3].Split(',')[0].Should().Be("3");
    }

    [Fact] // UT-CSV-12
    public void Every_line_ends_with_crlf()
    {
        var text = Encoding.UTF8.GetString(RefundCsvWriter.Build(new[] { Req(), Req() }, Protector));

        text.Should().EndWith("\r\n");
        System.Text.RegularExpressions.Regex.Matches(text, "\r\n").Count.Should().Be(3); // header + 2 rows
    }

    [Fact] // UT-CSV-13
    public void Encoded_as_utf8_without_bom()
    {
        var bytes = RefundCsvWriter.Build(new[] { Req() }, Protector);
        bytes[0].Should().NotBe(0xEF);
    }

    [Fact] // UT-CSV-14
    public void Empty_input_is_header_only()
    {
        var text = Encoding.UTF8.GetString(RefundCsvWriter.Build(Array.Empty<RefundRequest>(), Protector));
        text.Should().Be(Header + "\r\n");
    }
}
