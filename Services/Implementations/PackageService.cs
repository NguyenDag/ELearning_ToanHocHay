using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Package;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class PackageService : IPackageService
    {
        private readonly IPackageRepository _repository;

        public PackageService(IPackageRepository repository)
        {
            _repository = repository;
        }

        private static PackageDto ToDto(Package x, IReadOnlyDictionary<int, int>? subscriberCounts = null) => new()
        {
            PackageId = x.PackageId,
            PackageName = x.PackageName,
            Description = x.Description,
            Tier = x.Tier,
            Price = x.Price,
            DurationDays = x.DurationDays,
            MaxMembers = x.MaxMembers,
            AiHintLimitDaily = x.AiHintLimitDaily,
            UnlimitedAiHint = x.UnlimitedAiHint,
            PersonalizedPath = x.PersonalizedPath,
            MistakeRetry = x.MistakeRetry,
            SmartReminder = x.SmartReminder,
            PrioritySupport = x.PrioritySupport,
            IsActive = x.IsActive,
            CreatedAt = x.CreatedAt,
            LastUpdated = x.LastUpdated,
            ActiveSubscriberCount = subscriberCounts != null && subscriberCounts.TryGetValue(x.PackageId, out var c) ? c : 0
        };

        public async Task<ApiResponse<IEnumerable<PackageDto>>> GetAllAsync()
        {
            var packages = await _repository.GetAllAsync();

            return ApiResponse<IEnumerable<PackageDto>>
                .SuccessResponse(packages.Select(x => ToDto(x)), "Lấy danh sách gói thành công");
        }

        public async Task<ApiResponse<IEnumerable<PackageDto>>> GetAllForManagementAsync()
        {
            var packages = await _repository.GetAllIncludingInactiveAsync();
            var counts = await _repository.GetActiveSubscriberCountsAsync();

            return ApiResponse<IEnumerable<PackageDto>>
                .SuccessResponse(packages.Select(x => ToDto(x, counts)), "Lấy danh sách gói thành công");
        }

        public async Task<ApiResponse<PackageDto>> GetByIdAsync(int packageId)
        {
            var package = await _repository.GetByIdAsync(packageId);
            if (package == null)
                return ApiResponse<PackageDto>
                    .NotFound("Không tìm thấy gói");

            var counts = await _repository.GetActiveSubscriberCountsAsync();

            return ApiResponse<PackageDto>
                .SuccessResponse(ToDto(package, counts), "Lấy thông tin gói thành công");
        }

        public async Task<ApiResponse<PackageDto>> CreateAsync(int userId, CreateOrUpdatePackageDto dto)
        {
            var package = new Package
            {
                UserId = userId,
                PackageName = dto.PackageName,
                Description = dto.Description,
                Price = dto.Price,
                DurationDays = dto.DurationDays,
                AiHintLimitDaily = dto.AiHintLimitDaily,
                UnlimitedAiHint = dto.UnlimitedAiHint,
                PersonalizedPath = dto.PersonalizedPath,
                MistakeRetry = dto.MistakeRetry,
                SmartReminder = dto.SmartReminder,
                PrioritySupport = dto.PrioritySupport,
                IsActive = dto.IsActive
            };

            await _repository.AddAsync(package);

            return ApiResponse<PackageDto>
                .SuccessResponse(ToDto(package), "Tạo gói thành công");
        }

        public async Task<ApiResponse<PackageDto>> UpdateAsync(int packageId, CreateOrUpdatePackageDto dto)
        {
            var package = await _repository.GetByIdAsync(packageId);
            if (package == null)
                return ApiResponse<PackageDto>
                    .NotFound("Không tìm thấy gói");

            // Tier cố định — không cho đổi bậc gói ở màn hình quản lý giá.
            package.PackageName = dto.PackageName;
            package.Description = dto.Description;
            package.Price = dto.Price;
            package.DurationDays = dto.DurationDays;
            package.AiHintLimitDaily = dto.AiHintLimitDaily;
            package.UnlimitedAiHint = dto.UnlimitedAiHint;
            package.PersonalizedPath = dto.PersonalizedPath;
            package.MistakeRetry = dto.MistakeRetry;
            package.SmartReminder = dto.SmartReminder;
            package.PrioritySupport = dto.PrioritySupport;
            package.IsActive = dto.IsActive;
            package.LastUpdated = DateTime.UtcNow;

            await _repository.UpdateAsync(package);

            return ApiResponse<PackageDto>
                .SuccessResponse(ToDto(package), "Cập nhật gói thành công");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int packageId)
        {
            var package = await _repository.GetByIdAsync(packageId);
            if (package == null)
                return ApiResponse<bool>
                    .NotFound("Không tìm thấy gói");

            if (await _repository.HasAnySubscriptionAsync(packageId))
                return ApiResponse<bool>
                    .Conflict("Gói đã có người đăng ký — hãy tắt hoạt động thay vì xoá");

            await _repository.DeleteAsync(package);

            return ApiResponse<bool>
                .SuccessResponse(true, "Xóa gói thành công");
        }
    }
}
