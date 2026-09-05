using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Common;

/// <summary>§3.27 — UT-PAGE-*. Chuẩn hoá trang thuần (U1).</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class PagingTests
{
    [Theory]
    [InlineData(0, 1)]    // UT-PAGE-01
    [InlineData(-3, 1)]   // UT-PAGE-02
    [InlineData(5, 5)]    // UT-PAGE-03
    public void NormPage(int page, int expected)
        => new PagedRequest { Page = page }.NormPage.Should().Be(expected);

    [Theory]
    [InlineData(500, 100)]  // UT-PAGE-04
    [InlineData(0, 1)]      // UT-PAGE-05
    [InlineData(20, 20)]    // UT-PAGE-06
    public void NormPageSize(int size, int expected)
        => new PagedRequest { PageSize = size }.NormPageSize.Should().Be(expected);

    [Fact] // UT-PAGE-07
    public void Map_transforms_items_and_keeps_metadata()
    {
        var src = new PagedResult<int> { Items = new() { 1, 2, 3 }, Total = 42, Page = 2, PageSize = 3 };

        var mapped = src.Map(x => x.ToString());

        mapped.Items.Should().Equal("1", "2", "3");
        mapped.Total.Should().Be(42);
        mapped.Page.Should().Be(2);
        mapped.PageSize.Should().Be(3);
    }
}

/// <summary>§3.27 — UT-PAGE-08/09. <c>ToPagedResultAsync</c> cần EF provider (SQLite).</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U2")]
public class PagingQueryTests
{
    private static SqliteDb SeededUsers(int count)
    {
        var sql = SqliteDb.New();
        for (var i = 0; i < count; i++)
            sql.Db.Users.Add(Entities.NewUser());
        sql.Db.SaveChanges();
        return sql;
    }

    [Fact] // UT-PAGE-08
    public async Task ToPagedResultAsync_page_2()
    {
        using var sql = SeededUsers(25);

        var result = await sql.Db.Users.OrderBy(u => u.UserId)
            .ToPagedResultAsync(new PagedRequest { Page = 2, PageSize = 10 });

        result.Items.Should().HaveCount(10);
        result.Total.Should().Be(25);
        result.Page.Should().Be(2);
    }

    [Fact] // UT-PAGE-09
    public async Task ToPagedResultAsync_last_partial_page()
    {
        using var sql = SeededUsers(25);

        var result = await sql.Db.Users.OrderBy(u => u.UserId)
            .ToPagedResultAsync(new PagedRequest { Page = 3, PageSize = 10 });

        result.Items.Should().HaveCount(5);
    }
}
