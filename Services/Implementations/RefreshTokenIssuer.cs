using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class RefreshTokenIssuer : IRefreshTokenIssuer
    {
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IConfiguration _configuration;
        private readonly TimeProvider _clock;

        public RefreshTokenIssuer(
            IJwtService jwtService,
            IRefreshTokenRepository refreshTokenRepository,
            IConfiguration configuration,
            TimeProvider clock)
        {
            _jwtService = jwtService;
            _refreshTokenRepository = refreshTokenRepository;
            _configuration = configuration;
            _clock = clock;
        }

        public async Task<TokenPairDto> IssueAsync(User user, int? studentId, int? parentId, string? ip)
        {
            var accessMinutes = int.TryParse(_configuration["JwtSettings:ExpirationMinutes"], out var m) ? m : 30;
            var refreshDays = int.TryParse(_configuration["JwtSettings:RefreshTokenDays"], out var d) ? d : 30;
            var now = _clock.GetUtcNow().UtcDateTime;

            var access = _jwtService.GenerateToken(user, studentId, parentId);

            var raw = SecureTokens.NewToken();
            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = SecureTokens.Hash(raw),
                CreatedAt = now,
                ExpiresAt = now.AddDays(refreshDays),
                CreatedByIp = ip
            });

            return new TokenPairDto
            {
                Token = access,
                TokenExpiration = now.AddMinutes(accessMinutes),
                RefreshToken = raw,
                RefreshTokenExpiration = now.AddDays(refreshDays)
            };
        }
    }
}
