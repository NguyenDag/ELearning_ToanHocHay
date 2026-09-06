using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Integration.Infrastructure;

/// <summary>Assertion tiện dụng trên <see cref="HttpResponseMessage"/> theo vỏ <c>ApiResponse</c> (PascalCase).</summary>
public static class Envelope
{
    public static async Task<JsonElement> RootAsync(this HttpResponseMessage res)
    {
        var text = await res.Content.ReadAsStringAsync();
        text.Should().NotBeNullOrWhiteSpace($"phản hồi rỗng (status {(int)res.StatusCode})");
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    public static async Task<JsonElement> DataAsync(this HttpResponseMessage res)
        => (await res.RootAsync()).GetProperty("Data");

    public static async Task ShouldBeOk(this HttpResponseMessage res)
    {
        res.StatusCode.Should().Be(HttpStatusCode.OK, await SafeBody(res));
        (await res.RootAsync()).TryGetProperty("Data", out _).Should().BeTrue();
    }

    public static async Task ShouldBeCreated(this HttpResponseMessage res)
        => res.StatusCode.Should().Be(HttpStatusCode.Created, await SafeBody(res));

    public static async Task ShouldBeError(this HttpResponseMessage res, HttpStatusCode code, string? messageContains = null)
    {
        res.StatusCode.Should().Be(code, await SafeBody(res));
        if (messageContains != null)
        {
            var root = await res.RootAsync();
            root.GetProperty("Message").GetString().Should().Contain(messageContains);
        }
    }

    public static async Task<(int Total, int Page, int PageSize, JsonElement Items)> ShouldBePaged(this HttpResponseMessage res)
    {
        await res.ShouldBeOk();
        var data = await res.DataAsync();
        return (
            data.GetProperty("Total").GetInt32(),
            data.GetProperty("Page").GetInt32(),
            data.GetProperty("PageSize").GetInt32(),
            data.GetProperty("Items"));
    }

    private static async Task<string> SafeBody(HttpResponseMessage res)
    {
        try { return "Body: " + await res.Content.ReadAsStringAsync(); }
        catch { return "Body: <unreadable>"; }
    }
}
