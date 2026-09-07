using AutoMapper;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Exercise;
using ELearning_ToanHocHay_Control.Models.DTOs.ExerciseAttempt;
using ELearning_ToanHocHay_Control.Models.DTOs.Payment;
using ELearning_ToanHocHay_Control.Models.DTOs.Question;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            // DateOnly <-> DateTime — AutoMapper 12.0.0 built-in bị lỗi ở lần map đầu tiên
            // (AutoMapperMappingException "Missing type map"), nên khai báo tường minh.
            CreateMap<DateOnly, DateTime>().ConvertUsing(d => d.ToDateTime(TimeOnly.MinValue));
            CreateMap<DateTime, DateOnly>().ConvertUsing(d => DateOnly.FromDateTime(d));
            CreateMap<DateOnly?, DateTime?>()
                .ConvertUsing(d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : null);
            CreateMap<DateTime?, DateOnly?>()
                .ConvertUsing(d => d.HasValue ? DateOnly.FromDateTime(d.Value) : null);

            // Entity -> DTO
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Dob, opt => opt.MapFrom(src =>
                    src.Dob.HasValue ? src.Dob.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null));
            CreateMap<Exercise, ExerciseDto>()
                .ForMember(dest => dest.TotalQuestions, opt => opt.MapFrom(src =>
                    src.ExerciseQuestions != null ? src.ExerciseQuestions.Count : src.TotalQuestions));
            CreateMap<Payment, PaymentDto>();

            // DTO -> Entity
            CreateMap<UserDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastLogin, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Dob, opt => opt.MapFrom(src =>
                    src.Dob.HasValue ? DateOnly.FromDateTime(src.Dob.Value) : (DateOnly?)null));

            CreateMap<CreateUserDto, User>();

            CreateMap<ExerciseDto, Exercise>();
            CreateMap<ExerciseAttemptDto, Exercise?>();

            CreateMap<Exercise, ExerciseDetailDto>()
                .ForMember(dest => dest.Questions, opt => opt.MapFrom(src =>
                    src.ExerciseQuestions.Select(eq => eq.Question)));

            CreateMap<Question, QuestionDto>()
                .ForMember(dest => dest.QuestionText, opt => opt.MapFrom(src => src.QuestionText))
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.QuestionOptions));

            CreateMap<QuestionOption, QuestionOptionDto>()
                .ForMember(dest => dest.OptionId, opt => opt.MapFrom(src => src.OptionId))
                .ForMember(dest => dest.OptionText, opt => opt.MapFrom(src => src.OptionText));
        }
    }
}
