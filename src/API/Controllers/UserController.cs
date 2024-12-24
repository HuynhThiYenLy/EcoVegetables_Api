using Microsoft.AspNetCore.Mvc;
using ecovegetables_api.src.Application.DTOs.User;
using ecovegetables_api.src.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ecovegetables_api.src.Application.Interfaces;
using ecovegetables_api.src.Application.DTOs.User.Request;

namespace ecovegetables_api.src.API.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, AppDbContext context, ILogger<UserController> logger)
        {
            _userService = userService;
            _context = context;
            _logger = logger;
        }

        #region Add User
        [HttpPost("add")]
        public async Task<IActionResult> AddUserAsync([FromBody] RegisterRequest registerRequest)
        {
            if (registerRequest == null || string.IsNullOrEmpty(registerRequest.Email))
            {
                return BadRequest(new { code = "INVALID_REQUEST", message = "Dữ liệu không hợp lệ." });
            }

            var userAccount = await _userService.AddUserAsync(registerRequest);
            if (userAccount == null)
            {
                return BadRequest(new { code = "EMAIL_EXISTS", message = "Email đã tồn tại." });
            }

            return Ok(new
            {
                code = "USER_CREATED",
                message = "Đăng ký thành công. OTP đã được gửi!",
                user = new
                {
                    userAccount.Fullname,
                    userAccount.Email,
                    userAccount.Phone,
                    IsActive = userAccount.IsActive
                }
            });
        }
        #endregion

        #region Verify OTP
        [HttpPost("verifyOtp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpVerificationRequest otpVerification)
        {
            if (otpVerification == null || string.IsNullOrEmpty(otpVerification.Email) || string.IsNullOrEmpty(otpVerification.Otp))
            {
                return BadRequest(new { code = "INVALID_REQUEST", message = "Dữ liệu không hợp lệ." });
            }

            bool result = await _userService.VerifyOtpAsync(otpVerification);
            if (result)
            {
                return Ok(new
                {
                    code = "OTP_VERIFIED",
                    message = "OTP xác thực thành công và người dùng đã được lưu.",
                    email = otpVerification.Email
                });
            }

            return BadRequest(new { code = "INVALID_OTP", message = "OTP không hợp lệ." });
        }
        #endregion

        #region Resend OTP
        [HttpPost("resendOtp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest resendOtpRequest)
        {
            if (resendOtpRequest == null || string.IsNullOrEmpty(resendOtpRequest.Email))
            {
                return BadRequest(new { code = "INVALID_REQUEST", message = "Dữ liệu không hợp lệ." });
            }

            bool result = await _userService.ResendOtpAsync(resendOtpRequest);
            if (result)
            {
                return Ok(new { code = "OTP_RESENT", message = "OTP đã được gửi lại.", email = resendOtpRequest.Email });
            }

            return BadRequest(new { code = "RESEND_FAILED", message = "Gửi lại OTP thất bại." });
        }
        #endregion

        #region login
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Email) || string.IsNullOrEmpty(loginRequest.Password))
            {
                return BadRequest(new { code = "INVALID_REQUEST", message = "Dữ liệu không hợp lệ." });
            }

            var loginResponse = await _userService.LoginAsync(loginRequest);
            if (loginResponse == null)
            {
                return BadRequest(new { code = "LOGIN_FAILED", message = "Đăng nhập thất bại. Kiểm tra email hoặc mật khẩu." });
            }

            return Ok(new
            {
                code = "LOGIN_SUCCESS",
                message = "Đăng nhập thành công.",
                user = loginResponse.UserResponse,  // Trả về thông tin người dùng
                accessToken = loginResponse.AccessToken,  // Trả về access token
                refreshToken = loginResponse.RefreshToken  // Trả về refresh token
            });
        }
        #endregion

        #region Refresh Token
        [HttpPost("refreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest refreshTokenRequest)
        {
            if (refreshTokenRequest == null || string.IsNullOrEmpty(refreshTokenRequest.RefreshToken))
            {
                return BadRequest(new { code = "INVALID_REQUEST", message = "Dữ liệu không hợp lệ." });
            }

            var newAccessToken = await _userService.RefreshTokenAsync(refreshTokenRequest.RefreshToken);

            if (string.IsNullOrEmpty(newAccessToken))
            {
                return BadRequest(new { code = "INVALID_REFRESH_TOKEN", message = "Refresh token không hợp lệ hoặc đã hết hạn." });
            }

            return Ok(new
            {
                code = "TOKEN_REFRESHED",
                message = "Refresh token thành công.",
                accessToken = newAccessToken
            });
        }
        #endregion
    }
}
