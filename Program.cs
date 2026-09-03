using System.Text;
using System.Threading.RateLimiting;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Sepay;
using ELearning_ToanHocHay_Control.Repositories.Implementations;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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

            // 1. Database config (supports both a Railway URL and a local connection string)
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            string connectionString;

            if (!string.IsNullOrEmpty(databaseUrl))
            {
                // Production (Railway)
                connectionString = ConvertRailwayUrlToConnectionString(databaseUrl);
            }
            else
            {
                // Local
                connectionString = builder.Configuration.GetConnectionString("MyCnn")!;
            }

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // 2. App base URL & email config
            var appBaseUrl = Environment.GetEnvironmentVariable("APP_BASE_URL") ?? "https://localhost:5001";
            builder.Services.Configure<AppSettings>(options => options.BaseUrl = appBaseUrl);
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

            // 3. Register all repositories and services
            RegisterAppServices(builder.Services);



            // 4. AutoMapper
            builder.Services.AddAutoMapper(typeof(UserProfile));

            // 5. JWT config (flexible SecretKey resolution)
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");

            // Prefer an environment variable (server), fall back to appsettings (local)
            var secretKey = Environment.GetEnvironmentVariable("JwtSettings__SecretKey")
                            ?? jwtSettings["SecretKey"]
                            ?? builder.Configuration["JwtSettings:SecretKey"];


            // Register SePay
            builder.Services.Configure<SePayOptions>(
                builder.Configuration.GetSection("SePay")
            );

            
            // Fail fast with a clear message instead of an ArgumentNullException if the key is missing
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new Exception("CRITICAL ERROR: 'SecretKey' not found in configuration! Check appsettings.json or environment variables.");
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
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // Default: EVERY endpoint requires authentication. Public endpoints must be marked [AllowAnonymous].
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            // Rate limiting
            var authPermitLimit = int.TryParse(
                builder.Configuration["RateLimiting:AuthPermitLimit"], out var apl) ? apl : 5;

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // N requests / minute / IP for sensitive endpoints (login, password reset).
                options.AddPolicy("auth", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = authPermitLimit,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                // 20 requests / minute / user for AI endpoints.
                options.AddPolicy("ai", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.User.GetUserId()?.ToString()
                                      ?? context.Connection.RemoteIpAddress?.ToString()
                                      ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 20,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });

            // 6. Controllers & JSON options (keep PascalCase to match the WebApp DTOs)
            builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 1. Keep property names as-is (PascalCase) to match the WebApp DTOs
        options.JsonSerializerOptions.PropertyNamingPolicy = null;

        // 2. Use IgnoreCycles instead of Preserve: still guards against infinite
        //    loops but returns clean array-shaped JSON
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

        // 3. (Optional) Drop null fields to make the JSON smaller
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

            // 7. Swagger & CORS
            builder.Services.AddEndpointsApiExplorer();
            ConfigureSwagger(builder.Services);
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowWebApp", policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();
                    }
                });
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }
                
            // 8. Middleware pipeline, in the standard order
            app.UseSwagger();
            app.UseSwaggerUI();

            // CORS must come BEFORE Authentication/Authorization
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
            app.UseRateLimiter();

            app.MapControllers();

            // Default redirect to Swagger
            app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();

            // Health check (A3)
            app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

            app.Run();
        }

        /// <summary>
        /// Registers all repositories and services.
        /// </summary>
        private static void RegisterAppServices(IServiceCollection services)
        {
            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<IParentRepository, ParentRepository>();
            services.AddScoped<IExerciseRepository, ExerciseRepository>();
            services.AddScoped<IExerciseAttemptRepository, ExerciseAttemptRepository>();
            services.AddScoped<IStudentAnswerRepository, StudentAnswerRepository>();
            services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
            services.AddScoped<IExerciseQuestionRepository, ExerciseQuestionRepository>();
            services.AddScoped<IPackageRepository, PackageRepository>();
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IQuestionRepository, QuestionRepository>();
            services.AddScoped<IAIHintRepository, AIHintRepository>();
            services.AddScoped<IAIFeedbackRepository, AIFeedbackRepository>();
            services.AddScoped<IDashboardRepository, DashboardRepository>();
            services.AddScoped<IParentLinkRepository, ParentLinkRepository>();
            services.AddScoped<IParentRepository, ParentRepository>();

            // A3/P2 — content layer
            services.AddScoped<ICatalogRepository, CatalogRepository>();
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<IContentRepository, ContentRepository>();
            services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

            // Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IExerciseService, ExerciseService>();
            services.AddScoped<IExerciseAttemptService, ExerciseAttemptService>();
            services.AddScoped<IEmailService, SendGridEmailService>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddHttpClient<IAIService, AIService>();
            services.AddScoped<IQuestionService, QuestionService>();
            services.AddScoped<IPackageService, PackageService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<SubscriptionInfoHelper>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ISubscriptionPaymentService, SubscriptionPaymentService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ISePayService, SePayService>();
            services.AddScoped<ISePayIpnService, SePayIpnService>();
            services.AddScoped<ISubscriptionLifecycleService, SubscriptionLifecycleService>();
            services.AddHostedService<SubscriptionLifecycleHostedService>();
            services.AddScoped<IAIHintService, AIHintService>();
            services.AddScoped<IAIFeedbackService, AIFeedbackService>();
            services.AddScoped<IAiQuotaService, AiQuotaService>();
            services.AddScoped<ICoreDashboardService, CoreDashboardService>();
            services.AddScoped<IParentService, ParentService>();
            services.AddScoped<IParentLinkService, ParentLinkService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<INotificationRuleEngine, NotificationRuleEngine>();
            services.AddScoped<IResourceAccessService, ResourceAccessService>();

            // A3/P2 — content layer
            services.AddScoped<ICatalogService, CatalogService>();
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<IContentAuthoringService, ContentAuthoringService>();
            services.AddScoped<IContentAccessService, ContentAccessService>();
            services.AddScoped<IEnrollmentService, EnrollmentService>();
            services.AddScoped<ILearnService, LearnService>();
            services.AddScoped<IQuestionBankService, QuestionBankService>();
            services.AddScoped<IAdminUserService, AdminUserService>();
            services.AddScoped<IProgressProjectionService, ProgressProjectionService>();

            // Background Services
            services.AddSingleton<IBackgroundEmailService, BackgroundEmailService>();
            services.AddHostedService<BackgroundEmailService>(provider =>
                (BackgroundEmailService)provider.GetRequiredService<IBackgroundEmailService>());

            services.AddSingleton<IAiFeedbackQueue, AiFeedbackBackgroundService>();
            services.AddHostedService<AiFeedbackBackgroundService>(provider =>
                (AiFeedbackBackgroundService)provider.GetRequiredService<IAiFeedbackQueue>());
        }

        private static void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "ELearning API",
                    Version = "v1",
                    Description = "API for the ToanHocHay e-learning platform"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] then your token",
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
            if (userInfo.Length != 2) throw new Exception("Invalid DATABASE_URL");

            return $"Host={uri.Host};" +
                   $"Port={uri.Port};" +
                   $"Database={uri.AbsolutePath.TrimStart('/')};" +
                   $"Username={userInfo[0]};" +
                   $"Password={userInfo[1]};" +
                   $"Ssl Mode=Require;Trust Server Certificate=true;";
        }
    }
}
