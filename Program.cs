
using System.Text;
using ELearning_ToanHocHay_Control.Data;
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

            /*builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));*/

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("MyCnn")));

            // Register Repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IStudentRepository, StudentRepository>();
            builder.Services.AddScoped<IParentRepository, ParentRepository>();
            builder.Services.AddScoped<IExerciseRepository, ExerciseRepository>();
            builder.Services.AddScoped<IExerciseAttemptRepository, ExerciseAttemptRepository>();
            builder.Services.AddScoped<IStudentAnswerRepository, StudentAnswerRepository>();
            builder.Services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
            builder.Services.AddScoped<IExerciseQuestionRepository, ExerciseQuestionRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ICurriculumRepository, CurriculumRepository>();
            builder.Services.AddScoped<IChapterRepository, ChapterRepository>();

            // Register Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IExerciseService, ExerciseService>();
            builder.Services.AddScoped<IExerciseAttemptService, ExerciseAttemptService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ICurriculumService, CurriculumService>();
            builder.Services.AddScoped<IChapterService, ChapterService>();
            builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
            builder.Services.AddHttpClient<IAIService, AIService>();


            //Register AutoMapper
            builder.Services.AddAutoMapper(typeof(UserProfile));
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
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Services.AddControllers();

            var app = builder.Build();



            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}

/*
 class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Password Hash Generator ===");
            Console.WriteLine("Nhập mật khẩu cần hash (hoặc nhấn Enter để hash 'password123'):");
            
            string password = Console.ReadLine();
            if (string.IsNullOrEmpty(password))
            {
                password = "password123";
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            
            Console.WriteLine($"\nMật khẩu gốc: {password}");
            Console.WriteLine($"Password Hash: {hashedPassword}");
            Console.WriteLine("\nCopy hash này vào file SampleUsers.sql để thay thế cho '$2a$11$XYZ...'");
            
            Console.WriteLine("\n=== Test Verify ===");
            bool isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            Console.WriteLine($"Verify kết quả: {isValid}");
            
            Console.WriteLine("\nNhấn Enter để thoát...");
            Console.ReadLine();
        }
    }
 */
