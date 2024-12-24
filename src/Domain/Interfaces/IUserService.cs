using ecovegetables_api.src.Application.DTOs.User;
using ecovegetables_api.src.Application.DTOs.User.Request;
using ecovegetables_api.src.Application.DTOs.User.Response;
using ecovegetables_api.src.Domain.Entities;

namespace ecovegetables_api.src.Application.Interfaces
{
    public interface IUserService
    {
        Task<User> AddUserAsync(RegisterRequest registerRequest);
        Task<bool> VerifyOtpAsync(OtpVerificationRequest otpVerification);
        Task<bool> ResendOtpAsync(ResendOtpRequest resendOtpRequest);

        Task<LoginResponse> LoginAsync(LoginRequest model);
        string GenerateJwtToken(User user);
        string GenerateRefreshToken();
        Task<string> RefreshTokenAsync(string refreshToken);
    }
}
