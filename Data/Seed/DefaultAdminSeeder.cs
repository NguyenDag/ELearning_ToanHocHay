using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Data.Seed
{
    /// <summary>
    /// Ensures a single <see cref="UserType.SystemAdmin"/> account exists so the platform can be
    /// administered right after the first deploy. Runs on every startup after
    /// <c>db.Database.Migrate()</c> and is idempotent — once an account with the configured email
    /// exists it does nothing (it never resets the password or re-activates a locked admin).
    ///
    /// Credentials MUST be supplied via configuration / environment variables — there is no
    /// hard-coded fallback. If either value is missing the seeder logs a warning and does nothing.
    ///   DefaultAdmin__Email     (required)
    ///   DefaultAdmin__Password  (required)
    ///   DefaultAdmin__FullName  (optional, defaults to "Quản trị hệ thống")
    /// </summary>
    public sealed class DefaultAdminSeeder(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<DefaultAdminSeeder> logger)
    {
        private const string DefaultFullName = "Quản trị hệ thống";

        public async Task SeedAsync(CancellationToken ct = default)
        {
            var email = configuration["DefaultAdmin:Email"]?.Trim();
            var password = configuration["DefaultAdmin:Password"];
            var fullName = configuration["DefaultAdmin:FullName"]?.Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning(
                    "DefaultAdminSeeder: chưa cấu hình DefaultAdmin__Email / DefaultAdmin__Password — " +
                    "bỏ qua việc tạo tài khoản admin.");
                return;
            }

            if (await db.Users.AsNoTracking().AnyAsync(u => u.Email == email, ct))
            {
                logger.LogInformation(
                    "DefaultAdminSeeder: tài khoản admin \"{Email}\" đã tồn tại — không thay đổi.", email);
                return;
            }

            var now = DateTime.UtcNow;
            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = passwordHasher.HashPassword(password),
                FullName = string.IsNullOrWhiteSpace(fullName) ? DefaultFullName : fullName,
                UserType = UserType.SystemAdmin,
                IsEmailConfirmed = true,
                EmailConfirmedAt = now,
                IsActive = true,
                CreatedAt = now,
            });
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "DefaultAdminSeeder: đã tạo tài khoản admin \"{Email}\" (vai trò SystemAdmin).", email);
        }
    }
}
