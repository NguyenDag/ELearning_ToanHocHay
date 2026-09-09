using ELearning_ToanHocHay_Control.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Data.Seed
{
    /// <summary>
    /// Đảm bảo hệ thống luôn có bộ gói cước chuẩn (Free / Standard / Premium) với giá đã cấu hình
    /// sẵn, để trang bảng giá và luồng thanh toán SePay hoạt động ngay sau khi deploy.
    ///
    /// Chạy mỗi lần khởi động sau <c>db.Database.Migrate()</c> và <see cref="DefaultAdminSeeder"/>.
    /// Idempotent: khớp gói theo <see cref="Package.Tier"/> — thiếu gói nào thì tạo gói đó (kèm
    /// quyền truy cập toàn bộ nội dung cho gói trả phí). Gói đã tồn tại thì KHÔNG bị ghi đè giá /
    /// tính năng (admin toàn quyền chỉnh qua API sau này) — chỉ log lại giá hiện tại.
    ///
    /// Giá mặc định có thể override qua cấu hình <c>Seed:Packages:{Standard|Premium}:Price</c>.
    /// </summary>
    public sealed class DefaultPackageSeeder(
        AppDbContext db,
        IConfiguration configuration,
        ILogger<DefaultPackageSeeder> logger)
    {
        private sealed record PackageSpec(
            PackageTier Tier,
            string Name,
            string Description,
            decimal DefaultPrice,
            int DurationDays,
            int? AiHintLimitDaily,
            bool UnlimitedAiHint,
            bool PersonalizedPath,
            bool MistakeRetry,
            bool SmartReminder,
            bool PrioritySupport,
            bool GrantAllContent);

        private static readonly PackageSpec[] Specs =
        {
            new(PackageTier.Free, "Free", "Gói miễn phí — học nội dung mở, giới hạn gợi ý AI mỗi ngày.",
                DefaultPrice: 0m, DurationDays: 0, AiHintLimitDaily: 3,
                UnlimitedAiHint: false, PersonalizedPath: false, MistakeRetry: false,
                SmartReminder: false, PrioritySupport: false, GrantAllContent: false),

            new(PackageTier.Standard, "Standard", "Gói Tiêu chuẩn — mở toàn bộ khóa học, gợi ý AI không giới hạn, luyện lại câu sai, nhắc học thông minh.",
                DefaultPrice: 199_000m, DurationDays: 30, AiHintLimitDaily: null,
                UnlimitedAiHint: true, PersonalizedPath: false, MistakeRetry: true,
                SmartReminder: true, PrioritySupport: false, GrantAllContent: true),

            new(PackageTier.Premium, "Premium", "Gói Cao cấp — mọi quyền lợi của Standard, thêm lộ trình học cá nhân hóa, phân tích AI và hỗ trợ ưu tiên.",
                DefaultPrice: 299_000m, DurationDays: 30, AiHintLimitDaily: null,
                UnlimitedAiHint: true, PersonalizedPath: true, MistakeRetry: true,
                SmartReminder: true, PrioritySupport: true, GrantAllContent: true),
        };

        public async Task SeedAsync(CancellationToken ct = default)
        {
            var ownerId = await db.Users.AsNoTracking()
                .Where(u => u.UserType == UserType.SystemAdmin && u.IsActive)
                .Select(u => (int?)u.UserId)
                .FirstOrDefaultAsync(ct);

            ownerId ??= await db.Users.AsNoTracking()
                .OrderBy(u => u.UserId)
                .Select(u => (int?)u.UserId)
                .FirstOrDefaultAsync(ct);

            if (ownerId is null)
            {
                logger.LogWarning(
                    "DefaultPackageSeeder: chưa có người dùng nào để gán quyền sở hữu gói — bỏ qua.");
                return;
            }

            var existing = await db.Packages.ToListAsync(ct);
            var created = 0;

            foreach (var spec in Specs)
            {
                var match = existing.FirstOrDefault(p => p.Tier == spec.Tier);
                if (match is not null)
                {
                    logger.LogInformation(
                        "DefaultPackageSeeder: gói {Tier} đã tồn tại (\"{Name}\", giá {Price:N0}đ) — không thay đổi.",
                        spec.Tier, match.PackageName, match.Price);
                    continue;
                }

                var price = configuration.GetValue<decimal?>($"Seed:Packages:{spec.Tier}:Price")
                            ?? spec.DefaultPrice;

                var now = DateTime.UtcNow;
                var package = new Package
                {
                    UserId = ownerId.Value,
                    PackageName = spec.Name,
                    Description = spec.Description,
                    Tier = spec.Tier,
                    Price = price,
                    DurationDays = spec.DurationDays,
                    AiHintLimitDaily = spec.AiHintLimitDaily,
                    UnlimitedAiHint = spec.UnlimitedAiHint,
                    PersonalizedPath = spec.PersonalizedPath,
                    MistakeRetry = spec.MistakeRetry,
                    SmartReminder = spec.SmartReminder,
                    PrioritySupport = spec.PrioritySupport,
                    IsActive = true,
                    CreatedAt = now,
                };

                if (spec.GrantAllContent)
                {
                    package.Entitlements = new List<PackageEntitlement>
                    {
                        new() { ScopeType = EntitlementScope.AllContent }
                    };
                }

                db.Packages.Add(package);
                created++;

                logger.LogInformation(
                    "DefaultPackageSeeder: tạo gói {Tier} \"{Name}\" — giá {Price:N0}đ, {Days} ngày.",
                    spec.Tier, spec.Name, price, spec.DurationDays);
            }

            if (created > 0)
                await db.SaveChangesAsync(ct);
        }
    }
}
