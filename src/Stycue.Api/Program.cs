
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Stycue.Api.Data;
using Stycue.Api.Options;
using Stycue.Api.Services;
using Stycue.Api.Services.Interfaces;

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


            // Database Connection String
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            // Cors 跨域設定
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

                    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
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
            

            // Open Api
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();


            var app = builder.Build();

            // Configure the HTTP request pipeline.

            // Development-only API docs
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            // Middleware pipeline
            app.UseHttpsRedirection();

            app.UseCors("Frontend");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
