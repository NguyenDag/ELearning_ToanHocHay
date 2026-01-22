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

            CreateMap<Exercise, ExerciseDetailDto>()
        .ForMember(dest => dest.Questions, opt => opt.MapFrom(src =>
            src.ExerciseQuestions.Select(eq => eq.Question)));

            // Thêm Mapping cho nội dung câu hỏi
            CreateMap<Question, QuestionDto>()
                .ForMember(dest => dest.QuestionText, opt => opt.MapFrom(src => src.QuestionText))
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.QuestionOptions));

            // Thêm Mapping cho các phương án trả lời
            CreateMap<QuestionOption, QuestionOptionDto>()
                .ForMember(dest => dest.OptionId, opt => opt.MapFrom(src => src.OptionId))
                .ForMember(dest => dest.OptionText, opt => opt.MapFrom(src => src.OptionText));
        }
    }
}
