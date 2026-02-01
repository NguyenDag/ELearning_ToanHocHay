
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

            // ===== Database configuration (Railway + Local) =====
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            /*builder.Services.Configure<AppSettings>(options =>
            {
                options.BaseUrl = baseUrl;
            });*/

            string connectionString;

            if (!string.IsNullOrEmpty(databaseUrl))
            {
                // Railway (Production)
                var uri = new Uri(databaseUrl);
                var userInfo = uri.UserInfo.Split(':');

                if (userInfo.Length != 2)
                    throw new Exception("DATABASE_URL không hợp lệ");

                connectionString =
                    $"Host={uri.Host};" +
                    $"Port={uri.Port};" +
                    $"Database={uri.AbsolutePath.TrimStart('/')};" +
                    $"Username={userInfo[0]};" +
                    $"Password={userInfo[1]};" +
                    $"Ssl Mode=Require;Trust Server Certificate=true;";
            }
            else
            {
                // Local
                connectionString = builder.Configuration.GetConnectionString("MyCnn")!;
            }

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // ===== APP BASE URL =====
            var appBaseUrl =
                Environment.GetEnvironmentVariable("APP_BASE_URL")
                //?? throw new Exception("APP_BASE_URL chưa được cấu hình");
                ?? "https://localhost:5001";

            builder.Services.Configure<AppSettings>(options =>
            {
                options.BaseUrl = appBaseUrl;
            });

            builder.Services.Configure<EmailSettings>(
                builder.Configuration.GetSection("EmailSettings")
                );

            // Register Repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IStudentRepository, StudentRepository>();
            builder.Services.AddScoped<IParentRepository, ParentRepository>();
            builder.Services.AddScoped<IExerciseRepository, ExerciseRepository>();
            builder.Services.AddScoped<IExerciseAttemptRepository, ExerciseAttemptRepository>();
            builder.Services.AddScoped<IStudentAnswerRepository, StudentAnswerRepository>();
            builder.Services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
            builder.Services.AddScoped<IExerciseQuestionRepository, ExerciseQuestionRepository>();
            builder.Services.AddScoped<ICurriculumRepository, CurriculumRepository>();
            builder.Services.AddScoped<IChapterRepository, ChapterRepository>();
            builder.Services.AddScoped<ITopicRepository, TopicRepository>();
            builder.Services.AddScoped<ILessonRepository, LessonRepository>();
            builder.Services.AddScoped<ILessonContentRepository, LessonContentRepository>();
            builder.Services.AddScoped<IPackageRepository, PackageRepository>();
            builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

            // Register Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IExerciseService, ExerciseService>();
            builder.Services.AddScoped<IExerciseAttemptService, ExerciseAttemptService>();
            builder.Services.AddScoped<ICurriculumService, CurriculumService>();
            builder.Services.AddScoped<IChapterService, ChapterService>();
            builder.Services.AddScoped<ITopicService, TopicService>();
            builder.Services.AddScoped<ILessonSevice, LessonService>();
            builder.Services.AddScoped<ILessonContentService, LessonContentService>();
            //builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IEmailService, SendGridEmailService>();
            builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
            builder.Services.AddHttpClient<IAIService, AIService>();
            builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
            builder.Services.AddScoped<IQuestionService, QuestionService>();
            builder.Services.AddScoped<IPackageService, PackageService>();
            builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<ISubscriptionPaymentService, SubscriptionPaymentService>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ISePayService, SePayService>();

            builder.Services.AddSingleton<IBackgroundEmailService, BackgroundEmailService>();
            builder.Services.AddHostedService<BackgroundEmailService>(provider =>
                (BackgroundEmailService)provider.GetRequiredService<IBackgroundEmailService>());

            //Register AutoMapper
            builder.Services.AddAutoMapper(typeof(UserProfile));

            // Register SePay
            builder.Services.Configure<SePayOptions>(
                builder.Configuration.GetSection("SePay")
            );

            // Configure JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

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

            // Configure Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "ELearning API",
                    Version = "v1",
                    Description = "API cho hệ thống E-Learning ToanHocHay"
                });

                // Configure JWT trong Swagger
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
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowWebApp", policy =>
                {
                    policy.WithOrigins("https://localhost:7299") // Cổng WebApp của bạn
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Giữ nguyên tên thuộc tính (Success, Data, Message) thay vì biến thành camelCase
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                    // Xử lý lỗi vòng lặp nếu có
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });

            var app = builder.Build();

            // 2. Cấu hình Middleware theo đúng thứ tự
            app.UseSwagger();
            app.UseSwaggerUI();

            // Kích hoạt CORS ngay sau Swagger và PHẢI TRƯỚC Authentication/Authorization
            app.UseCors("AllowWebApp");

            if (!app.Environment.IsProduction())
            {
                app.UseHttpsRedirection();
            }

            // Thứ tự này rất quan trọng: Auth -> Auth
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // Chuyển hướng trang chủ về Swagger cho tiện debug
            app.MapGet("/", () => Results.Redirect("/swagger"));

            app.Run();
        }
    }
}
