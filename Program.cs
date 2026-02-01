using System.Text;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Sepay;
using ELearning_ToanHocHay_Control.Repositories.Implementations;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace ELearning_ToanHocHay_Control
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Cấu hình Database (Hỗ trợ cả Railway URL và Local Connection String)
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            string connectionString;

            if (!string.IsNullOrEmpty(databaseUrl))
            {
                // Logic cho môi trường Production (Railway)
                connectionString = ConvertRailwayUrlToConnectionString(databaseUrl);
            }
            else
            {
                // Logic cho môi trường Local
                connectionString = builder.Configuration.GetConnectionString("MyCnn")!;
            }

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // 2. Cấu hình App Base URL & Email
            var appBaseUrl = Environment.GetEnvironmentVariable("APP_BASE_URL") ?? "https://localhost:5001";
            builder.Services.Configure<AppSettings>(options => options.BaseUrl = appBaseUrl);
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

            // 3. Đăng ký toàn bộ Repositories và Services
            RegisterAppServices(builder.Services);


            // 4. Cấu hình AutoMapper
            builder.Services.AddAutoMapper(typeof(UserProfile));

            // 5. CẤU HÌNH JWT (Tích hợp logic kiểm tra SecretKey linh hoạt)
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");

            // Ưu tiên lấy từ biến môi trường (Server), nếu không có mới lấy từ appsettings (Local)
            var secretKey = Environment.GetEnvironmentVariable("JwtSettings__SecretKey")
                            ?? jwtSettings["SecretKey"]
                            ?? builder.Configuration["JwtSettings:SecretKey"];


            // Register SePay
            builder.Services.Configure<SePayOptions>(
                builder.Configuration.GetSection("SePay")
            );

            
            // Kiểm tra an toàn: Nếu không tìm thấy SecretKey ở cả 2 nơi thì báo lỗi rõ ràng thay vì ArgumentNullException
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new Exception("CRITICAL ERROR: Không tìm thấy 'SecretKey' trong cấu hình! Hãy kiểm tra appsettings.json hoặc Environment Variables.");
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddAuthorization();

            // 6. Cấu hình Controllers & JSON Options (Giữ nguyên PascalCase cho WebApp dễ đọc)
            // Chỉnh lại trong API
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                // Thay Preserve bằng IgnoreCycles
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            // 7. Cấu hình Swagger & CORS
            builder.Services.AddEndpointsApiExplorer();
            ConfigureSwagger(builder.Services);

            // 7. Cấu hình Swagger & CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowWebApp", policy =>
                {
                    policy.SetIsOriginAllowed(origin => true) // Cho phép tất cả mọi nơi, kể cả local của An
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            var app = builder.Build();

            // 8. Cấu hình Middleware Pipeline theo thứ tự chuẩn
            app.UseSwagger();
            app.UseSwaggerUI();

            // CORS phải đặt TRƯỚC Authentication/Authorization
            app.UseCors("AllowWebApp");
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
            });

            if (!app.Environment.IsProduction())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // Chuyển hướng mặc định về Swagger
            app.MapGet("/", () => Results.Redirect("/swagger"));

            app.Run();
        }

        /// <summary>
        /// Phương thức đăng ký toàn bộ Repositories và Services
        /// </summary>
        private static void RegisterAppServices(IServiceCollection services)
        {
            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<IParentRepository, ParentRepository>();
            services.AddScoped<IExerciseRepository, ExerciseRepository>();
            services.AddScoped<IExerciseAttemptRepository, ExerciseAttemptRepository>();
            services.AddScoped<IStudentAnswerRepository, StudentAnswerRepository>();
            services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
            services.AddScoped<IExerciseQuestionRepository, ExerciseQuestionRepository>();
            services.AddScoped<ICurriculumRepository, CurriculumRepository>();
            services.AddScoped<IChapterRepository, ChapterRepository>();
            services.AddScoped<ITopicRepository, TopicRepository>();
            services.AddScoped<ILessonRepository, LessonRepository>();
            services.AddScoped<ILessonContentRepository, LessonContentRepository>();
            services.AddScoped<IPackageRepository, PackageRepository>();
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IQuestionRepository, QuestionRepository>();
            services.AddScoped<IAIHintRepository, AIHintRepository>();
            services.AddScoped<IAIFeedbackRepository, AIFeedbackRepository>();

            // Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IExerciseService, ExerciseService>();
            services.AddScoped<IExerciseAttemptService, ExerciseAttemptService>();
            services.AddScoped<ICurriculumService, CurriculumService>();
            services.AddScoped<IChapterService, ChapterService>();
            services.AddScoped<ITopicService, TopicService>();
            services.AddScoped<ILessonSevice, LessonService>(); // Đã sửa chính tả
            services.AddScoped<ILessonContentService, LessonContentService>();
            services.AddScoped<IEmailService, SendGridEmailService>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddHttpClient<IAIService, AIService>();
            services.AddScoped<IQuestionService, QuestionService>();
            services.AddScoped<IPackageService, PackageService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ISubscriptionPaymentService, SubscriptionPaymentService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ISePayService, SePayService>();
            services.AddScoped<IAIHintService, AIHintService>();
            services.AddScoped<IAIFeedbackService, AIFeedbackService>();

            // Background Services
            services.AddSingleton<IBackgroundEmailService, BackgroundEmailService>();
            services.AddHostedService<BackgroundEmailService>(provider =>
                (BackgroundEmailService)provider.GetRequiredService<IBackgroundEmailService>());
        }

        private static void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "ELearning API",
                    Version = "v1",
                    Description = "API cho hệ thống E-Learning ToanHocHay"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header sử dụng Bearer scheme. Nhập 'Bearer' [space] và token của bạn",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new string[] {}
                    }
                });
            });
        }

        private static string ConvertRailwayUrlToConnectionString(string databaseUrl)
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            if (userInfo.Length != 2) throw new Exception("DATABASE_URL không hợp lệ");

            return $"Host={uri.Host};" +
                   $"Port={uri.Port};" +
                   $"Database={uri.AbsolutePath.TrimStart('/')};" +
                   $"Username={userInfo[0]};" +
                   $"Password={userInfo[1]};" +
                   $"Ssl Mode=Require;Trust Server Certificate=true;";
        }
    }
}