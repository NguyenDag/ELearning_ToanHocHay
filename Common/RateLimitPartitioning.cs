using System.Net;

namespace ELearning_ToanHocHay_Control.Common
{
    /// <summary>
    /// Chọn khoá phân vùng cho rate limiter.
    ///
    /// Vấn đề: khi request đi qua WebApp (proxy first-party), <c>Connection.RemoteIpAddress</c>
    /// là IP của WebApp — mọi người dùng dùng chung một "xô" cho login/quên mật khẩu.
    ///
    /// Cách xử lý: nếu request đến từ một proxy tin cậy (cấu hình
    /// <c>RateLimiting:TrustedProxies</c>, mặc định chỉ loopback) và có header
    /// <c>X-Client-Key</c> thì dùng giá trị đó; ngược lại dùng IP thật của kết nối.
    /// </summary>
    public static class RateLimitPartitioning
    {
        public const string ClientKeyHeader = "X-Client-Key";
        private const int MaxKeyLength = 100;

        public static string ResolveClientKey(HttpContext ctx, IConfiguration config)
        {
            var remote = ctx.Connection.RemoteIpAddress;

            if (remote != null && IsTrustedProxy(remote, config)
                && ctx.Request.Headers.TryGetValue(ClientKeyHeader, out var forwarded))
            {
                var key = forwarded.ToString().Trim();
                if (!string.IsNullOrEmpty(key))
                    return "ck:" + (key.Length > MaxKeyLength ? key[..MaxKeyLength] : key);
            }

            return "ip:" + (remote?.ToString() ?? "unknown");
        }

        private static bool IsTrustedProxy(IPAddress remote, IConfiguration config)
        {
            var configured = config.GetSection("RateLimiting:TrustedProxies").Get<string[]>();
            var list = (configured is { Length: > 0 })
                ? configured
                : new[] { "127.0.0.1", "::1", "::ffff:127.0.0.1" };

            var normalized = remote.IsIPv4MappedToIPv6 ? remote.MapToIPv4() : remote;

            foreach (var entry in list)
            {
                if (!IPAddress.TryParse(entry.Trim(), out var allowed)) continue;
                var allowedNorm = allowed.IsIPv4MappedToIPv6 ? allowed.MapToIPv4() : allowed;
                if (allowedNorm.Equals(normalized)) return true;
            }
            return IPAddress.IsLoopback(normalized);
        }
    }
}
