using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Stycue.Api.Data;
using Stycue.Api.Options;
using Stycue.Api.Services;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Entities;

namespace Stycue.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            // Add Options
            builder.Services.Configure<JwtOptions>(
                builder.Configuration.GetSection("Jwt"));
            builder.Services.Configure<GoogleAuthOptions>(
                builder.Configuration.GetSection("GoogleAuth"));

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
            app.UseHttpsRedirection();

            app.UseCors(corsPolicy);

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
