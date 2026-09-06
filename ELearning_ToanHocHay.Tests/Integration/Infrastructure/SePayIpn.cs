namespace ELearning_ToanHocHay.Tests.Integration.Infrastructure;

/// <summary>Payload IPN của SePay cho test F7.</summary>
public static class SePayIpn
{
    public static object In(int subId, long amount, string reference) => new
    {
        id = Random.Shared.Next(1, int.MaxValue),
        content = $"TKPTTS SUBSCRIPTION_{subId}",
        transferType = "in",
        transferAmount = amount,
        referenceCode = reference,
    };

    public static object Out(long amount, string reference) => new
    {
        id = Random.Shared.Next(1, int.MaxValue),
        content = "TKPTTS RUT TIEN",
        transferType = "out",
        transferAmount = amount,
        referenceCode = reference,
    };

    public static object BadContent(long amount, string reference) => new
    {
        id = Random.Shared.Next(1, int.MaxValue),
        content = "CHUYEN TIEN HOC PHI",
        transferType = "in",
        transferAmount = amount,
        referenceCode = reference,
    };

    public static HttpClient Client(ApiFactory f, string key = "test-sepay-key")
    {
        var c = f.CreateClient();
        c.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Apikey {key}");
        return c;
    }
}
