using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.Net;

namespace ELearning_ToanHocHay.Tests.Integration.Infrastructure;

/// <summary>
/// TestServer không gán <c>Connection.RemoteIpAddress</c>; middleware rate-limit coi <c>null</c> là
/// "không phải proxy tin cậy" nên bỏ qua <c>X-Client-Key</c>. Filter này gán loopback trước toàn
/// bộ pipeline để test phân vùng theo <c>X-Client-Key</c> chạy được (IT-F1-26).
/// </summary>
public sealed class ForceRemoteIpStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (ctx, nxt) =>
        {
            ctx.Connection.RemoteIpAddress = IPAddress.Loopback;
            await nxt();
        });
        next(app);
    };
}
