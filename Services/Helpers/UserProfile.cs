using AutoMapper;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            // Entity -> DTO
            CreateMap<User, UserDto>();
            CreateMap<Exercise, ExerciseDto>();

            // DTO -> Entity
            CreateMap<UserDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastLogin, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            CreateMap<CreateUserDto, User>();
            CreateMap<UpdateUserDto, User>();

            CreateMap<ExerciseDto, Exercise>();
            CreateMap<ExerciseAttemptDto, Exercise?>();
        }
    }
}
