using AutoMapper;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Parent;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class ParentService : IParentService
    {
        private readonly IParentRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public ParentService(
            IParentRepository repository,
            IUserRepository userRepository,
            IMapper mapper)
        {
            _repository = repository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<ParentDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return ApiResponse<ParentDto>.ErrorResponse("Not found", null);

            return ApiResponse<ParentDto>.SuccessResponse(
                _mapper.Map<ParentDto>(entity));
        }

        public async Task<ApiResponse<ParentDto>> UpdateAsync(int id, UpdateParentDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return ApiResponse<ParentDto>.ErrorResponse("Not found", null);

            entity.Job = dto.Job;

            await _repository.UpdateAsync(entity);

            return ApiResponse<ParentDto>.SuccessResponse(
                _mapper.Map<ParentDto>(entity),
                "Updated successfully");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var deleted = await _repository.DeleteAsync(id);
            return ApiResponse<bool>.SuccessResponse(deleted);
        }
    }
}
