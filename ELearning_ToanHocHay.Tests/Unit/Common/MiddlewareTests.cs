using System.Text;
using System.Text.Json;
using ELearning_ToanHocHay_Control.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace ELearning_ToanHocHay.Tests.Unit.Common;

/// <summary>§3.37 — UT-MW-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class MiddlewareTests
{
    private const string Header = "X-Correlation-ID";

    // ---- CorrelationIdMiddleware ----

    [Fact] // UT-MW-01
    public async Task Generates_a_correlation_id_when_the_request_has_none()
    {
        var ctx = new DefaultHttpContext();
        await new CorrelationIdMiddleware(_ => Task.CompletedTask).InvokeAsync(ctx);

        var id = ctx.Response.Headers[Header].ToString();
        id.Should().HaveLength(32);
        ctx.Items[Header].Should().Be(id);
    }

    [Fact] // UT-MW-02
    public async Task Echoes_an_inbound_correlation_id()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[Header] = "abc-123";

        await new CorrelationIdMiddleware(_ => Task.CompletedTask).InvokeAsync(ctx);

        ctx.Response.Headers[Header].ToString().Should().Be("abc-123");
    }

    // ---- GlobalExceptionHandler ----

    private static async Task<(int status, string contentType, string body)> Handle(
        Exception ex, string? correlationId = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        if (correlationId != null) ctx.Items[Header] = correlationId;

        await new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance)
            .TryHandleAsync(ctx, ex, CancellationToken.None);

        ctx.Response.Body.Position = 0;
        var body = await new StreamReader(ctx.Response.Body, Encoding.UTF8).ReadToEndAsync();
        return (ctx.Response.StatusCode, ctx.Response.ContentType ?? "", body);
    }

    [Fact] // UT-MW-03
    public async Task Returns_500_envelope_without_leaking_the_exception()
    {
        var (status, _, body) = await Handle(new InvalidOperationException("secret internal detail"));

        status.Should().Be(500);
        body.Should().NotContain("secret internal detail");
        var json = JsonDocument.Parse(body).RootElement;
        json.GetProperty("success").GetBoolean().Should().BeFalse();
        json.GetProperty("message").GetString().Should().Be("Đã xảy ra lỗi máy chủ");
    }

    [Fact] // UT-MW-04
    public async Task Writes_json_content_type()
    {
        var (_, contentType, _) = await Handle(new Exception("x"));
        contentType.Should().Contain("application/json");
    }

    [Fact] // UT-MW-05
    public async Task Surfaces_the_correlation_id_for_tracing()
    {
        var (_, _, body) = await Handle(new Exception("x"), correlationId: "cid-42");
        body.Should().Contain("cid-42");
    }
}
