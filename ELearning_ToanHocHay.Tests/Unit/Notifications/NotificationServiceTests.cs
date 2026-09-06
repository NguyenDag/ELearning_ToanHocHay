using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Notifications;

/// <summary>§3.34 — UT-NOTIF-*. `NotificationRules` (U1) + `NotificationService` (U2, SQLite).</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U2")]
public class NotificationServiceTests : IDisposable
{
    private readonly SqliteDb _sql = SqliteDb.New();
    private readonly int _userId;
    private readonly int _otherUserId;

    public NotificationServiceTests()
    {
        _userId = Seed.User(_sql.Db, UserType.Student).UserId;
        _otherUserId = Seed.User(_sql.Db, UserType.Student).UserId;
    }

    public void Dispose() => _sql.Dispose();

    private NotificationService Svc() => new(_sql.NewContext());

    private Notification Add(
        int userId, bool read = false, DateTime? createdAt = null)
    {
        var n = new Notification
        {
            UserId = userId,
            Audience = NotifyAudience.Student,
            Title = "t",
            Message = "m",
            NotificationType = NotificationType.Info,
            IsRead = read,
            ReadAt = read ? DateTime.UtcNow.AddDays(-1) : null,
            CreatedAt = createdAt ?? DateTime.UtcNow,
        };
        _sql.Db.Notifications.Add(n);
        _sql.Db.SaveChanges();
        return n;
    }

    [Fact] // UT-NOTIF-01
    public void Rules_All_has_the_three_rule_keys()
        => NotificationRules.All.Should().BeEquivalentTo(new[] { "tab-switch", "low-score", "inactivity" });

    [Fact] // UT-NOTIF-02
    public async Task GetMine_only_returns_the_users_own()
    {
        Add(_userId);
        Add(_otherUserId);

        var res = await Svc().GetMineAsync(_userId, unreadOnly: false, page: 1, pageSize: 20);

        res.Data.Items.Should().HaveCount(1);
        res.Data.Total.Should().Be(1);
    }

    [Fact] // UT-NOTIF-03
    public async Task GetMine_unread_only()
    {
        Add(_userId, read: true);
        Add(_userId, read: false);

        var res = await Svc().GetMineAsync(_userId, unreadOnly: true, page: 1, pageSize: 20);

        res.Data.Items.Should().OnlyContain(n => !n.IsRead);
        res.Data.Total.Should().Be(1);
    }

    [Fact] // UT-NOTIF-04
    public async Task GetMine_is_newest_first()
    {
        Add(_userId, createdAt: DateTime.UtcNow.AddHours(-2));
        Add(_userId, createdAt: DateTime.UtcNow.AddHours(-1));

        var res = await Svc().GetMineAsync(_userId, unreadOnly: false, page: 1, pageSize: 20);

        res.Data.Items.Should().BeInDescendingOrder(n => n.CreatedAt);
    }

    [Fact] // UT-NOTIF-05
    public async Task GetMine_clamps_paging_and_reports_total()
    {
        for (var i = 0; i < 5; i++) Add(_userId);

        var res = await Svc().GetMineAsync(_userId, unreadOnly: false, page: 0, pageSize: 500);

        res.Data.Page.Should().Be(1);
        res.Data.PageSize.Should().Be(100);
        res.Data.Total.Should().Be(5);
    }

    [Fact] // UT-NOTIF-06
    public async Task GetUnreadCount()
    {
        Add(_userId, read: false);
        Add(_userId, read: false);
        Add(_userId, read: true);
        Add(_otherUserId, read: false);

        (await Svc().GetUnreadCountAsync(_userId)).Data.Should().Be(2);
    }

    [Fact] // UT-NOTIF-07
    public async Task MarkRead_foreign_notification_is_not_found()
    {
        var other = Add(_otherUserId);
        (await Svc().MarkReadAsync(_userId, other.NotificationId)).Message.Should().Be("Không tìm thấy thông báo");
    }

    [Fact] // UT-NOTIF-08
    public async Task MarkRead_sets_read_and_timestamp()
    {
        var n = Add(_userId, read: false);

        var res = await Svc().MarkReadAsync(_userId, n.NotificationId);

        res.Success.Should().BeTrue();
        var saved = _sql.NewContext().Notifications.Single(x => x.NotificationId == n.NotificationId);
        saved.IsRead.Should().BeTrue();
        saved.ReadAt.Should().NotBeNull();
    }

    [Fact] // UT-NOTIF-09
    public async Task MarkRead_already_read_keeps_timestamp()
    {
        var n = Add(_userId, read: true);
        var originalReadAt = n.ReadAt;

        var res = await Svc().MarkReadAsync(_userId, n.NotificationId);

        res.Success.Should().BeTrue();
        _sql.NewContext().Notifications.Single(x => x.NotificationId == n.NotificationId)
            .ReadAt.Should().BeCloseTo(originalReadAt!.Value, TimeSpan.FromSeconds(1));
    }

    [Fact] // UT-NOTIF-10
    public async Task MarkAllRead_marks_every_unread_and_reports_count()
    {
        Add(_userId, read: false);
        Add(_userId, read: false);
        Add(_userId, read: true);

        var res = await Svc().MarkAllReadAsync(_userId);

        res.Message.Should().Contain("2");
        _sql.NewContext().Notifications.Where(n => n.UserId == _userId)
            .Should().OnlyContain(n => n.IsRead);
    }
}
