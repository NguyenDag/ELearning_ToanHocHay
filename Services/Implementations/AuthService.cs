using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IParentRepository _parentRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly AppSettings _appSettings;
        private readonly IBackgroundEmailService _backgroundEmailService;
        private readonly IConfiguration _configuration;

        // P1 — login throttling
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan BaseLockout = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan MaxLockout = TimeSpan.FromMinutes(30);

        public AuthService(
            AppDbContext context,
            IUserRepository userRepository,
            IStudentRepository studentRepository,
            IParentRepository parentRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IJwtService jwtService,
            IPasswordHasher passwordHasher,
            IEmailService emailService,
            IOptions<AppSettings> appSettings,
            IBackgroundEmailService backgroundEmailService,
            IConfiguration configuration)
        {
            _context = context;
            _userRepository = userRepository;
            _studentRepository = studentRepository;
            _parentRepository = parentRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _appSettings = appSettings.Value;
            _backgroundEmailService = backgroundEmailService;
            _configuration = configuration;
        }

        // ==================================================================
        // Login
        // ==================================================================
        public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request, string? ip = null)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                    return ApiResponse<LoginResponseDto>.ErrorResponse("Email hoặc mật khẩu không đúng");

                if (user.LockoutEndsAt.HasValue && user.LockoutEndsAt.Value > DateTime.UtcNow)
                {
                    var mins = Math.Ceiling((user.LockoutEndsAt.Value - DateTime.UtcNow).TotalMinutes);
                    return ApiResponse<LoginResponseDto>.ErrorResponse(
                        $"Tài khoản tạm khoá do đăng nhập sai nhiều lần. Thử lại sau {mins} phút.");
                }

                if (!user.IsEmailConfirmed)
                    return ApiResponse<LoginResponseDto>.ErrorResponse("Vui lòng xác nhận email trước khi đăng nhập");

                if (!user.IsActive || user.LockedAt.HasValue)
                    return ApiResponse<LoginResponseDto>.ErrorResponse("Tài khoản đã bị vô hiệu hóa");

                if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                {
                    await RegisterFailedLoginAsync(user);
                    return ApiResponse<LoginResponseDto>.ErrorResponse("Email hoặc mật khẩu không đúng");
                }

                // success -> clear the failure counter
                if (user.FailedLoginCount != 0 || user.LockoutEndsAt != null)
                {
                    user.FailedLoginCount = 0;
                    user.LockoutEndsAt = null;
                }

                int? studentId = null;
                int? parentId = null;
                var packageTier = PackageTier.Free;

                if (user.UserType == UserType.Student)
                {
                    var student = await _studentRepository.GetByUserIdAsync(user.UserId);
                    if (student == null)
                        return ApiResponse<LoginResponseDto>.ErrorResponse("Không tìm thấy thông tin học sinh");
                    studentId = student.StudentId;
                    packageTier = await ResolvePackageTierAsync(student.StudentId);
                }
                else if (user.UserType == UserType.Parent)
                {
                    var parent = await _parentRepository.GetByUserIdAsync(user.UserId);
                    if (parent == null)
                        return ApiResponse<LoginResponseDto>.ErrorResponse("Không tìm thấy thông tin phụ huynh");
                    parentId = parent.ParentId;
                }

                await _userRepository.UpdateLastLoginAsync(user.UserId);

                var pair = await IssueTokenPairAsync(user, studentId, parentId, ip);

                return ApiResponse<LoginResponseDto>.SuccessResponse(new LoginResponseDto
                {
                    UserId = user.UserId,
                    StudentId = studentId,
                    ParentId = parentId,
                    Email = user.Email,
                    FullName = user.FullName,
                    UserType = user.UserType,
                    Token = pair.Token,
                    TokenExpiration = pair.TokenExpiration,
                    RefreshToken = pair.RefreshToken,
                    RefreshTokenExpiration = pair.RefreshTokenExpiration,
                    AvatarUrl = user.AvatarUrl,
                    PackageTier = packageTier
                }, "Đăng nhập thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<LoginResponseDto>.ErrorResponse("Đã xảy ra lỗi", new List<string> { ex.Message });
            }
        }

        private async Task RegisterFailedLoginAsync(User user)
        {
            user.FailedLoginCount += 1;
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                // escalating lockout: 1, 2, 4, 8 … minutes, capped at 30
                var over = user.FailedLoginCount - MaxFailedAttempts;
                var minutes = Math.Min(MaxLockout.TotalMinutes, BaseLockout.TotalMinutes * Math.Pow(2, over));
                user.LockoutEndsAt = DateTime.UtcNow.AddMinutes(minutes);
            }
            await _userRepository.UpdateUserAsync(user);
        }

        private async Task<PackageTier> ResolvePackageTierAsync(int studentId)
        {
            var now = DateTime.UtcNow;
            var tier = await _context.Subscriptions
                .Where(s => s.StudentId == studentId && s.Status == SubscriptionStatus.Active && s.EndDate > now)
                .Include(s => s.Package)
                .OrderByDescending(s => s.Package!.Tier)
                .ThenByDescending(s => s.EndDate)
                .Select(s => (PackageTier?)s.Package!.Tier)
                .FirstOrDefaultAsync();

            return tier ?? PackageTier.Free;
        }

        // ==================================================================
        // Token issuance / refresh (P1 — real rotation)
        // ==================================================================
        private async Task<TokenPairDto> IssueTokenPairAsync(User user, int? studentId, int? parentId, string? ip)
        {
            var accessMinutes = int.TryParse(_configuration["JwtSettings:ExpirationMinutes"], out var m) ? m : 30;
            var refreshDays = int.TryParse(_configuration["JwtSettings:RefreshTokenDays"], out var d) ? d : 30;

            var access = _jwtService.GenerateToken(user, studentId, parentId);

            var raw = SecureTokens.NewToken();
            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = SecureTokens.Hash(raw),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
                CreatedByIp = ip
            });

            return new TokenPairDto
            {
                Token = access,
                TokenExpiration = DateTime.UtcNow.AddMinutes(accessMinutes),
                RefreshToken = raw,
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(refreshDays)
            };
        }

        public async Task<ApiResponse<TokenPairDto>> RefreshTokenAsync(string refreshToken, string? ip = null)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return ApiResponse<TokenPairDto>.ErrorResponse("Refresh token không hợp lệ");

            var hash = SecureTokens.Hash(refreshToken);
            var stored = await _refreshTokenRepository.GetByHashAsync(hash);

            if (stored == null)
                return ApiResponse<TokenPairDto>.ErrorResponse("Refresh token không hợp lệ");

            if (!stored.IsActive)
            {
                // A revoked token was presented again — treat as reuse and cut every session.
                if (stored.RevokedAt != null)
                    await _refreshTokenRepository.RevokeAllForUserAsync(stored.UserId);
                return ApiResponse<TokenPairDto>.ErrorResponse("Refresh token đã hết hạn hoặc bị thu hồi");
            }

            var user = await _userRepository.GetByIdAsync(stored.UserId);
            if (user == null || !user.IsActive || user.LockedAt.HasValue)
                return ApiResponse<TokenPairDto>.ErrorResponse("Tài khoản không tồn tại hoặc bị khóa");

            int? studentId = user.UserType == UserType.Student
                ? (await _studentRepository.GetByUserIdAsync(user.UserId))?.StudentId
                : null;
            int? parentId = user.UserType == UserType.Parent
                ? (await _parentRepository.GetByUserIdAsync(user.UserId))?.ParentId
                : null;

            var pair = await IssueTokenPairAsync(user, studentId, parentId, ip);

            // rotate: revoke the presented token, point it at its replacement
            stored.RevokedAt = DateTime.UtcNow;
            stored.ReplacedByTokenHash = SecureTokens.Hash(pair.RefreshToken);
            await _refreshTokenRepository.SaveAsync();

            return ApiResponse<TokenPairDto>.SuccessResponse(pair, "Làm mới token thành công");
        }

        // ==================================================================
        // Logout / change password
        // ==================================================================
        public async Task<ApiResponse<bool>> LogoutAsync(int userId, string? refreshToken = null)
        {
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var stored = await _refreshTokenRepository.GetByHashAsync(SecureTokens.Hash(refreshToken));
                if (stored != null && stored.UserId == userId && stored.RevokedAt == null)
                {
                    stored.RevokedAt = DateTime.UtcNow;
                    await _refreshTokenRepository.SaveAsync();
                }
            }
            else
            {
                await _refreshTokenRepository.RevokeAllForUserAsync(userId);
            }

            return ApiResponse<bool>.SuccessResponse(true, "Đăng xuất thành công");
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordDto request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return ApiResponse<bool>.ErrorResponse("User không tồn tại");

            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                return ApiResponse<bool>.ErrorResponse("Mật khẩu hiện tại không đúng");

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            await _userRepository.UpdateUserAsync(user);

            // P1 — invalidate every existing session
            await _refreshTokenRepository.RevokeAllForUserAsync(userId);

            return ApiResponse<bool>.SuccessResponse(true, "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.");
        }

        // ==================================================================
        // Email confirmation
        // ==================================================================
        public async Task<ApiResponse<bool>> ConfirmEmailAsync(string token)
        {
            var emailToken = await _context.EmailVerificationTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == token && !x.IsUsed && x.ExpiredAt > DateTime.UtcNow);

            if (emailToken == null)
                return ApiResponse<bool>.ErrorResponse("Token không hợp lệ");

            emailToken.User.IsEmailConfirmed = true;
            emailToken.User.EmailConfirmedAt = DateTime.UtcNow;
            emailToken.IsUsed = true;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Xác nhận email thành công");
        }

        public async Task<ApiResponse<bool>> ResendConfirmationEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return ApiResponse<bool>.SuccessResponse(true, "Nếu email tồn tại, hệ thống đã gửi lại xác nhận.");

            if (user.IsEmailConfirmed)
                return ApiResponse<bool>.ErrorResponse("Tài khoản này đã được xác nhận trước đó");

            var oldTokens = await _context.EmailVerificationTokens
                .Where(t => t.UserId == user.UserId && !t.IsUsed)
                .ToListAsync();
            foreach (var t in oldTokens) t.IsUsed = true;

            var tokenValue = Guid.NewGuid().ToString("N");
            await _context.EmailVerificationTokens.AddAsync(new EmailVerificationToken
            {
                UserId = user.UserId,
                Token = tokenValue,
                ExpiredAt = DateTime.UtcNow.AddHours(24),
                IsUsed = false
            });
            await _context.SaveChangesAsync();

            _backgroundEmailService.QueueConfirmationEmail(user.Email, user.FullName, ConfirmLink(tokenValue));
            return ApiResponse<bool>.SuccessResponse(true, "Email xác nhận mới đã được gửi");
        }

        // ==================================================================
        // Forgot / reset password (P1)
        // ==================================================================
        public async Task<ApiResponse<bool>> ForgotPasswordAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);

            // Always report success so the endpoint cannot be used to enumerate accounts.
            if (user == null || !user.IsEmailConfirmed || !user.IsActive)
                return ApiResponse<bool>.SuccessResponse(true, "Nếu email tồn tại, hệ thống đã gửi liên kết đặt lại mật khẩu.");

            var open = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.UserId && !t.IsUsed)
                .ToListAsync();
            foreach (var t in open) t.IsUsed = true;

            var raw = SecureTokens.NewToken();
            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.UserId,
                Token = raw,
                ExpiredAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            });
            await _context.SaveChangesAsync();

            var link = $"{_appSettings.BaseUrl.TrimEnd('/')}/reset-password?token={raw}";
            _backgroundEmailService.QueuePasswordResetEmail(user.Email, user.FullName, link);

            return ApiResponse<bool>.SuccessResponse(true, "Nếu email tồn tại, hệ thống đã gửi liên kết đặt lại mật khẩu.");
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(string token, string newPassword)
        {
            var reset = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed && t.ExpiredAt > DateTime.UtcNow);

            if (reset == null)
                return ApiResponse<bool>.ErrorResponse("Token không hợp lệ hoặc đã hết hạn");

            var user = await _userRepository.GetByIdAsync(reset.UserId);
            if (user == null)
                return ApiResponse<bool>.ErrorResponse("User không tồn tại");

            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            user.FailedLoginCount = 0;
            user.LockoutEndsAt = null;
            reset.IsUsed = true;
            await _userRepository.UpdateUserAsync(user);
            await _context.SaveChangesAsync();

            // P1 — a password reset kills every existing session
            await _refreshTokenRepository.RevokeAllForUserAsync(user.UserId);

            return ApiResponse<bool>.SuccessResponse(true, "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.");
        }

        // ==================================================================
        // Register
        // ==================================================================
        public async Task<ApiResponse<bool>> RegisterAsync(RegisterRequestDto request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingUser = await _userRepository.GetByEmailAsync(request.Email);

                if (existingUser != null && !existingUser.IsEmailConfirmed)
                    return ApiResponse<bool>.ErrorResponse("Email đã được đăng ký và chờ xác nhận");
                if (existingUser != null && existingUser.IsEmailConfirmed)
                    return ApiResponse<bool>.ErrorResponse("Email đã được đăng ký");

                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = _passwordHasher.HashPassword(request.Password),
                    FullName = request.FullName,
                    Phone = request.Phone,
                    Dob = request.Dob,
                    UserType = request.UserType,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _userRepository.CreateUserAsync(user);
                await _context.SaveChangesAsync();

                switch (request.UserType)
                {
                    case UserType.Student:
                        await _studentRepository.AddAsync(new Student
                        {
                            UserId = user.UserId,
                            CurrentGradeLevelId = request.GradeLevelId,
                            SchoolName = request.SchoolName
                        });
                        break;

                    case UserType.Parent:
                        await _parentRepository.AddAsync(new Parent
                        {
                            UserId = user.UserId,
                            Job = request.Job,
                            ConnectionCode = Guid.NewGuid().ToString("N")[..8].ToUpper()
                        });
                        break;

                    default:
                        return ApiResponse<bool>.ErrorResponse("Không cho phép đăng ký role này");
                }

                var tokenValue = Guid.NewGuid().ToString("N");
                await _context.EmailVerificationTokens.AddAsync(new EmailVerificationToken
                {
                    UserId = user.UserId,
                    Token = tokenValue,
                    ExpiredAt = DateTime.UtcNow.AddHours(24),
                    IsUsed = false
                });
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _backgroundEmailService.QueueConfirmationEmail(user.Email, user.FullName, ConfirmLink(tokenValue));

                return ApiResponse<bool>.SuccessResponse(true,
                    "Đăng ký thành công. Vui lòng kiểm tra email để xác nhận tài khoản");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                // A2-15 — don't leak internals to the client; log server-side instead.
                return ApiResponse<bool>.ErrorResponse("Đăng ký thất bại, vui lòng thử lại sau");
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var principal = _jwtService.ValidateToken(token);
                if (principal == null) return false;

                var userId = _jwtService.GetUserIdFromToken(token);
                if (!userId.HasValue) return false;

                var user = await _userRepository.GetByIdAsync(userId.Value);
                return user != null && user.IsActive && !user.LockedAt.HasValue;
            }
            catch
            {
                return false;
            }
        }

        // A2-12 — one consistent confirmation route (the API endpoint).
        private string ConfirmLink(string token)
            => $"{_appSettings.BaseUrl.TrimEnd('/')}/api/auth/confirm-email?token={token}";
    }
}
