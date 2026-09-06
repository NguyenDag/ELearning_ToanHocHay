using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Implementations;

namespace ELearning_ToanHocHay.Tests.Integration.Infrastructure;

/// <summary>
/// Builder theo luồng — mỗi hàm <c>Guid</c>-hoá tên/email, KHÔNG đụng golden dataset.
/// Bổ sung dần theo từng flow F1…F12.
/// </summary>
public sealed class FlowSeed(ApiFactory app)
{
    private static string Rand() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>Người dùng đã xác nhận email, đăng nhập được ngay bằng <see cref="SeedData.Password"/>.</summary>
    public async Task<(int userId, string email, string password)> NewConfirmedUserAsync(UserType type)
    {
        var email = $"{type.ToString().ToLowerInvariant()}.{Rand()}@flow.test";
        var userId = await app.Db(async db =>
        {
            var u = new User
            {
                Email = email,
                PasswordHash = new PasswordHasher().HashPassword(SeedData.Password),
                FullName = $"Flow {Rand()}",
                UserType = type,
                IsEmailConfirmed = true,
                EmailConfirmedAt = DateTime.UtcNow,
                IsActive = true,
            };
            db.Users.Add(u);
            await db.SaveChangesAsync();
            await AttachRoleRowAsync(db, u);
            return u.UserId;
        });
        return (userId, email, SeedData.Password);
    }

    /// <summary>Người dùng CHƯA xác nhận + token xác nhận thô (để gọi <c>confirm-email</c>).</summary>
    public async Task<(int userId, string email, string token)> NewUnconfirmedUserAsync(UserType type)
    {
        var email = $"{type.ToString().ToLowerInvariant()}.{Rand()}@flow.test";
        var raw = Guid.NewGuid().ToString("N");
        var userId = await app.Db(async db =>
        {
            var u = new User
            {
                Email = email,
                PasswordHash = new PasswordHasher().HashPassword(SeedData.Password),
                FullName = $"Flow {Rand()}",
                UserType = type,
                IsEmailConfirmed = false,
                IsActive = true,
            };
            db.Users.Add(u);
            await db.SaveChangesAsync();
            await AttachRoleRowAsync(db, u);

            db.EmailVerificationTokens.Add(new EmailVerificationToken
            {
                UserId = u.UserId,
                Token = raw,
                ExpiredAt = DateTime.UtcNow.AddHours(24),
                IsUsed = false,
            });
            await db.SaveChangesAsync();
            return u.UserId;
        });
        return (userId, email, raw);
    }

    private static async Task AttachRoleRowAsync(ELearning_ToanHocHay_Control.Data.AppDbContext db, User u)
    {
        switch (u.UserType)
        {
            case UserType.Student:
                db.Students.Add(new Student { UserId = u.UserId, CurrentGradeLevelId = 1 });
                break;
            case UserType.Parent:
                db.Parents.Add(new Parent { UserId = u.UserId, ConnectionCode = Guid.NewGuid().ToString("N")[..8].ToUpper() });
                break;
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Payment đã Completed, không gắn subscription — dùng cho luồng hoàn tiền F8.</summary>
    public async Task<int> SeedRefundablePaymentAsync(int payerUserId, decimal amount = 199_000m, DateTime? paidAt = null)
        => await app.Db(async db =>
        {
            var studentId = db.Students.Where(s => s.UserId == payerUserId).Select(s => (int?)s.StudentId).FirstOrDefault();
            var p = new Payment
            {
                PaidByUserId = payerUserId,
                StudentId = studentId,
                Amount = amount,
                PaymentMethod = PaymentMethod.BankTransfer,
                Status = PaymentStatus.Completed,
                TransactionId = "FLOW-" + Rand(),
                PaymentDate = paidAt ?? DateTime.UtcNow.AddDays(-1),
            };
            db.Payments.Add(p);
            await db.SaveChangesAsync();
            return p.PaymentId;
        });
}
