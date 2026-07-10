using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Stycue.Api.Data;
using Stycue.Api.Options;
using Stycue.Api.Services;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Entities;
using Stycue.Api.Middlewares;
using Stycue.Api.DTOs.Comm;

namespace Stycue.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            const long MaxRequestBodySize = 20 * 1024 * 1024;

            // Kestrel request body limit
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = MaxRequestBodySize;
            });

            // Add services to the container.
            
            // Add Controller
            builder.Services.AddControllers();

            // Set Multipart form upload limit
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = MaxRequestBodySize;
            });

            // Add Options
            builder.Services.Configure<JwtOptions>(
                builder.Configuration.GetSection("Jwt"));
            builder.Services.Configure<GoogleAuthOptions>(
                builder.Configuration.GetSection("GoogleAuth"));
            builder.Services.Configure<BlobStorageOptions>(
                builder.Configuration.GetSection("BlobStorage"));
            builder.Services.Configure<PointsOptions>(
                builder.Configuration.GetSection("Points"));

            // Database Connection String
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            // Cors 跨域設定
            var corsPolicy = builder.Configuration["Cors:Policy"] ?? "Frontend";

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

                    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
                });
                options.AddPolicy("All", policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });

            // JWT 驗證
            var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();

            var jwtSecretKeys = builder.Configuration["Jwt:SecretKey"];

            if(jwtOptions == null)
            {
                throw new InvalidOperationException("Jwt Options is missing.");
            }
            if (String.IsNullOrWhiteSpace(jwtSecretKeys))
            {
                throw new InvalidOperationException("Jwt:SecretKey is missing");
            }

            // JWT Authentication setting
            // AddAuthentication => 註冊使用JWT認證機制驗證，驗證沒有通過時，也使用JWT Bearer 的方式回應 401 Unauthorized
            // AddJwtBearer => 設定 JWT Bearer 的細節
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = true;
                    options.SaveToken = false;

                    // 收到 JWT 後，要檢查哪些東西才算合法
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions!.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero, //不給 token 額外寬限時間

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSecretKeys))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = async context =>
                        {
                            // 呼叫HandleResponse原因 => 這次 401 response 我自己處理，框架不要再寫預設 challenge response。
                            context.HandleResponse();

                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json; charset=utf-8";

                            var response = ApiResponse<object>.FailResult("未登入或登入資訊無效", "UNAUTHORIZED");

                            await context.Response.WriteAsJsonAsync(response);
                        },
                        OnForbidden = async context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json; charset=utf-8";

                            var response = ApiResponse<object>.FailResult("沒有權限執行此操作", "FORBIDDEN");

                            await context.Response.WriteAsJsonAsync(response);
                        }
                    };
                });


            // Authorization
            builder.Services.AddAuthorization();

            // AutoMapper


            // Application Services

            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

            // DI 建立 PasswordService 時，需要知道 IPasswordHasher<User> 要用哪個實作類別
            builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            builder.Services.AddScoped<IPasswordService, PasswordService>();
            builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
            builder.Services.AddScoped<IImageService, ImageService>();
            builder.Services.AddScoped<ITagService, TagService>();

            // Open Api
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            var docs = builder.Configuration.GetSection(ApiDocOptions.SectionName)
                .Get<ApiDocOptions>() ?? new();

            // 啟用 OpenAPI 文件，設定文件標題/版本，並告訴 Scalar 這份 API 支援 JWT Bearer 認證
            if (docs.Enabled)
            {
                builder.Services.AddOpenApi(docs.Version, options =>
                {
                    options.AddDocumentTransformer((document, _, _) =>
                    {
                        document.Info = new OpenApiInfo
                        {
                            Title = docs.Title,
                            Version = docs.Version
                        };

                        document.Components ??= new OpenApiComponents();
                        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.Http,
                            Scheme = "bearer",
                            BearerFormat = "JWT"
                        };

                        return Task.CompletedTask;
                    });
                });
            }



            var app = builder.Build();

            // Configure the HTTP request pipeline.

            // Development-only API docs
            if (docs.Enabled && (app.Environment.IsDevelopment() || app.Environment.IsStaging()))
            {
                app.MapOpenApi();
                app.MapScalarApiReference(docs.Route, options =>
                {
                    options.WithTitle(docs.Title);
                    options.AddPreferredSecuritySchemes("Bearer");
                    options.DisableAgent();
                });
            }

            // Middleware pipeline

            // exception middleware
            app.UseMiddleware<GlobalExceptionMiddleware>();

            app.UseHttpsRedirection();

            app.UseCors(corsPolicy);

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
