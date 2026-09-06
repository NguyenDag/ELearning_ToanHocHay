using ELearning_ToanHocHay_Control.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay.Tests.Unit.Infrastructure;

/// <summary>
/// Hạ tầng tầng <b>U2</b> (§1.2 / §1.3): mở một <see cref="AppDbContext"/> chạy trên
/// SQLite <c>:memory:</c>. Connection được giữ mở suốt vòng đời handle — đóng connection
/// là mất sạch schema + dữ liệu. Mỗi test tạo một instance mới:
/// <code>
/// using var sql = SqliteDb.New();
/// var db = sql.Db;
/// </code>
/// </summary>
public sealed class SqliteDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public AppDbContext Db { get; }

    private SqliteDb(SqliteConnection connection, AppDbContext db)
    {
        _connection = connection;
        Db = db;
    }

    /// <summary>Mở connection mới, dựng schema qua <c>EnsureCreated()</c>, trả context sẵn dùng.</summary>
    public static SqliteDb New()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();

        return new SqliteDb(connection, db);
    }

    /// <summary>Context mới trên <b>cùng</b> connection — đọc lại state sau khi ghi, không dính change-tracker cũ.</summary>
    public AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;
        return new AppDbContext(options);
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
