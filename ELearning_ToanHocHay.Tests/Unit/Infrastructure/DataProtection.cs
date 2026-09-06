using Microsoft.AspNetCore.DataProtection;

namespace ELearning_ToanHocHay.Tests.Unit.Infrastructure;

/// <summary>
/// <see cref="IDataProtectionProvider"/> phù du (in-memory, không key ring trên đĩa/DB) cho
/// <c>UT-PROT</c> — round-trip <c>Protect</c>/<c>Unprotect</c> thật của
/// <see cref="ELearning_ToanHocHay_Control.Services.Helpers.RefundFieldProtector"/>.
/// </summary>
public static class DataProtection
{
    /// <summary>Provider mới, độc lập — hai lần gọi cho hai key ring khác nhau (UT-PROT-08).</summary>
    public static IDataProtectionProvider Ephemeral() => new EphemeralDataProtectionProvider();
}
