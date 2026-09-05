using System.Globalization;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay.Tests.Unit.Infrastructure;

/// <summary>
/// <see cref="IRefundFieldProtector"/> giả — mã hoá = prefix <c>"enc:"</c>. Dùng cho UT-CSV
/// (không cần Data Protection thật). UT-PROT dùng provider thật (<see cref="DataProtection"/>).
/// </summary>
public sealed class FakeRefundFieldProtector : IRefundFieldProtector
{
    public const string Prefix = "enc:";

    /// <summary>Khi bật, <see cref="Unprotect"/> luôn ném — mô phỏng ciphertext hỏng (UT-CSV-10).</summary>
    public bool ThrowOnUnprotect { get; set; }

    public string Protect(string plaintext) => Prefix + (plaintext ?? "");

    public string Unprotect(string ciphertext)
    {
        if (ThrowOnUnprotect)
            throw new InvalidOperationException("FakeRefundFieldProtector: forced decrypt failure.");
        if (ciphertext is null || !ciphertext.StartsWith(Prefix, StringComparison.Ordinal))
            throw new FormatException($"Not a fake ciphertext: '{ciphertext}'");
        return ciphertext[Prefix.Length..];
    }

    public string Last4(string accountNumber)
    {
        var digits = new string((accountNumber ?? "").Where(char.IsDigit).ToArray());
        return digits.Length <= 4 ? digits : digits[^4..];
    }
}

/// <summary>
/// <see cref="TimeProvider"/> có thể tua — inject vào service dùng <c>TimeProvider</c> thay
/// cho <c>DateTime.UtcNow</c> (§2). <c>Advance</c> để mô phỏng "qua 1 ngày" / "quá 30′".
/// </summary>
public sealed class FakeClock : TimeProvider
{
    private DateTimeOffset _now;

    public FakeClock(DateTimeOffset? start = null)
        => _now = start ?? new DateTimeOffset(2026, 9, 6, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
    public void Set(DateTimeOffset to) => _now = to;
}

/// <summary>
/// <see cref="ISystemConfigService"/> giả — đọc từ dict, thiếu key thì trả fallback.
/// Nạp bằng <see cref="Set"/> (nhận string thô như cột SystemConfig.ConfigValue).
/// </summary>
public sealed class FakeSystemConfig : ISystemConfigService
{
    private readonly Dictionary<string, string?> _values = new();

    public FakeSystemConfig Set(string key, string? value)
    {
        _values[key] = value;
        return this;
    }

    public Task<int> GetIntAsync(string key, int fallback)
        => Task.FromResult(_values.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : fallback);

    public Task<decimal> GetDecimalAsync(string key, decimal fallback)
        => Task.FromResult(_values.TryGetValue(key, out var v)
            && decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : fallback);

    public Task<bool> GetBoolAsync(string key, bool fallback)
        => Task.FromResult(_values.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : fallback);

    public Task<string> GetStringAsync(string key, string fallback)
        => Task.FromResult(_values.TryGetValue(key, out var v) && v is not null ? v : fallback);

    public Task<ApiResponse<List<SystemConfigDto>>> GetAllAsync(string? group)
        => Task.FromResult(ApiResponse<List<SystemConfigDto>>.SuccessResponse(
            _values.Select(kv => new SystemConfigDto { ConfigKey = kv.Key, ConfigValue = kv.Value }).ToList()));

    public Task<ApiResponse<SystemConfigDto>> SetAsync(string key, string? value, int updatedBy)
    {
        _values[key] = value;
        return Task.FromResult(ApiResponse<SystemConfigDto>.SuccessResponse(
            new SystemConfigDto { ConfigKey = key, ConfigValue = value }));
    }
}

/// <summary>
/// Ghi lại mọi email được gửi / xếp hàng — assert bằng <see cref="Sent"/> / <see cref="Queued"/>.
/// Cài cả <see cref="IEmailService"/> lẫn <see cref="IBackgroundEmailService"/>.
/// </summary>
public sealed class RecordingEmail : IEmailService, IBackgroundEmailService
{
    public sealed record Mail(string Kind, string ToEmail, string Name, string? Link, object? Extra = null);

    public List<Mail> Sent { get; } = new();
    public List<Mail> Queued { get; } = new();

    // --- IEmailService ---
    public Task SendConfirmEmailAsync(string toEmail, string fullName, string confirmLink)
    {
        Sent.Add(new Mail("confirm", toEmail, fullName, confirmLink));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink)
    {
        Sent.Add(new Mail("reset", toEmail, fullName, resetLink));
        return Task.CompletedTask;
    }

    public Task SendTabSwitchNotificationAsync(string toEmail, string parentName, string studentName,
        string exerciseName, DateTime switchedAt, int switchCount)
    {
        Sent.Add(new Mail("tab-switch", toEmail, parentName, null, new { studentName, exerciseName, switchedAt, switchCount }));
        return Task.CompletedTask;
    }

    // --- IBackgroundEmailService ---
    public void QueueConfirmationEmail(string toEmail, string fullName, string confirmLink)
        => Queued.Add(new Mail("confirm", toEmail, fullName, confirmLink));

    public void QueuePasswordResetEmail(string toEmail, string fullName, string resetLink)
        => Queued.Add(new Mail("reset", toEmail, fullName, resetLink));

    public void QueueTabSwitchEmail(string toEmail, string parentName, string studentName,
        string exerciseName, DateTime switchedAt, int switchCount)
        => Queued.Add(new Mail("tab-switch", toEmail, parentName, null, new { studentName, exerciseName, switchedAt, switchCount }));
}
