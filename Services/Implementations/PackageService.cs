using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
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

        public async Task<ApiResponse<IEnumerable<PackageDto>>> GetAllAsync()
        {
            var packages = await _repository.GetAllAsync();

            var data = packages.Select(x => new PackageDto
            {
                PackageId = x.PackageId,
                PackageName = x.PackageName,
                Description = x.Description,
                Price = x.Price,
                DurationDays = x.DurationDays,
                UnlimitedAiHint = x.UnlimitedAiHint,
                PersonalizedPath = x.PersonalizedPath,
                MistakeRetry = x.MistakeRetry,
                SmartReminder = x.SmartReminder,
                PrioritySupport = x.PrioritySupport,
                IsActive = x.IsActive
            });

            return ApiResponse<IEnumerable<PackageDto>>
                .SuccessResponse(data, "Lấy danh sách gói thành công");
        }

        public async Task<ApiResponse<PackageDto>> GetByIdAsync(int packageId)
        {
            var package = await _repository.GetByIdAsync(packageId);
            if (package == null)
                return ApiResponse<PackageDto>
                    .ErrorResponse("Không tìm thấy gói");

            var dto = new PackageDto
            {
                PackageId = package.PackageId,
                PackageName = package.PackageName,
                Description = package.Description,
                Price = package.Price,
                DurationDays = package.DurationDays,
                UnlimitedAiHint = package.UnlimitedAiHint,
                PersonalizedPath = package.PersonalizedPath,
                MistakeRetry = package.MistakeRetry,
                SmartReminder = package.SmartReminder,
                PrioritySupport = package.PrioritySupport,
                IsActive = package.IsActive
            };

            return ApiResponse<PackageDto>
                .SuccessResponse(dto, "Lấy thông tin gói thành công");
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
                .SuccessResponse(null, "Tạo gói thành công");
        }

        public async Task<ApiResponse<PackageDto>> UpdateAsync(int packageId, CreateOrUpdatePackageDto dto)
        {
            var package = await _repository.GetByIdAsync(packageId);
            if (package == null)
                return ApiResponse<PackageDto>
                    .ErrorResponse("Không tìm thấy gói");

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
                .SuccessResponse(null, "Cập nhật gói thành công");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int packageId)
        {
            var package = await _repository.GetByIdAsync(packageId);
            if (package == null)
                return ApiResponse<bool>
                    .ErrorResponse("Không tìm thấy gói");

            await _repository.DeleteAsync(package);

            return ApiResponse<bool>
                .SuccessResponse(true, "Xóa gói thành công");
        }
    }
}
