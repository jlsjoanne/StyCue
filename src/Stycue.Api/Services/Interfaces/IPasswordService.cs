using Microsoft.AspNetCore.Identity;
using Stycue.Api.Entities;

namespace Stycue.Api.Services.Interfaces
{
    public interface IPasswordService
    {
        string HashPassword(User user, string password);
        PasswordVerificationResult VerifyPassword(User user, string hashedPassword, string providedPassword);
    }
}
