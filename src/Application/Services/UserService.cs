using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ecovegetables_api.src.Application.DTOs.User;
using ecovegetables_api.src.Application.DTOs.User.Request;
using ecovegetables_api.src.Application.DTOs.User.Response;
using ecovegetables_api.src.Application.Interfaces;
using ecovegetables_api.src.Domain.Entities;
using ecovegetables_api.src.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace ecovegetables_api.src.Application.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly EmailService _emailService;
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _configuration;

        public UserService(AppDbContext context, IMemoryCache memoryCache, EmailService emailService,
            ILogger<UserService> logger, IConfiguration configuration)
        {
            _context = context;
            _memoryCache = memoryCache;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }

        #region Tạo OTP
        private string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
        #endregion

        #region JWT Access Token
        public string GenerateJwtToken(User user)
        {
            // Lấy thông tin cấu hình từ appsettings.json
            var secretKey = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"]);

            // Tạo các claim
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Fullname),
            };

            // Khóa bí mật dùng để mã hóa token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Tạo token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(expirationMinutes),
                signingCredentials: creds
            );

            // Trả về token dưới dạng chuỗi
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        #endregion

        #region Tạo Refresh Token
        // Tạo Refresh Token
        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }
        #endregion

        #region Add User
        public async Task<User> AddUserAsync(RegisterRequest registerRequest)
        {
            // Kiểm tra xem email đã tồn tại chưa
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerRequest.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Email đã tồn tại");
                return null;
            }

            // Tạo đối tượng người dùng mới
            var userAccount = new User
            {
                Fullname = registerRequest.Fullname,
                Email = registerRequest.Email,
                Phone = registerRequest.Phone,
                Password = registerRequest.Password,  // Mật khẩu chưa được hash
                IsActive = false,  // Tài khoản chưa được kích hoạt
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Tạo OTP và lưu vào bộ nhớ cache
            var otp = GenerateOtp();
            var otpExpiration = DateTime.UtcNow.AddMinutes(5);
            _memoryCache.Set($"otp_{registerRequest.Email}", otp, otpExpiration);
            _memoryCache.Set($"user_{registerRequest.Email}", userAccount, otpExpiration);

            // Gửi OTP qua email
            var emailSent = await _emailService.SendOtpEmailAsync(registerRequest.Email, otp);

            if (emailSent)
            {
                _logger.LogInformation("Người dùng đã được lưu tạm thời trong cache.\nOTP: {Otp}", otp);
                return userAccount; // Trả về người dùng đã lưu tạm thời trong cache
            }
            else
            {
                _logger.LogError("Gửi OTP thất bại");
                return null;
            }
        }
        #endregion

        #region Verify OTP
        public async Task<bool> VerifyOtpAsync(OtpVerificationRequest otpVerification)
        {
            // Kiểm tra nếu cache chứa OTP và thông tin người dùng
            if (_memoryCache.TryGetValue($"otp_{otpVerification.Email}", out string storedOtp) &&
                storedOtp == otpVerification.Otp &&
                _memoryCache.TryGetValue($"user_{otpVerification.Email}", out User userAccount))
            {
                // Kiểm tra mật khẩu, hash nếu cần
                if (userAccount.Password != null && !userAccount.Password.StartsWith("$2a$"))
                {
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(userAccount.Password);
                    userAccount.Password = hashedPassword;
                }

                // Đánh dấu tài khoản đã được kích hoạt
                userAccount.IsActive = true;
                userAccount.UpdatedAt = DateTime.UtcNow;

                // Lưu vào cơ sở dữ liệu
                await _context.Users.AddAsync(userAccount);  // Thêm người dùng vào cơ sở dữ liệu
                await _context.SaveChangesAsync();

                // Xóa OTP và user tạm thời khỏi cache
                _memoryCache.Remove($"otp_{otpVerification.Email}");
                _memoryCache.Remove($"user_{otpVerification.Email}");

                _logger.LogInformation("Xác thực OTP thành công, tài khoản đã được kích hoạt.");
                return true;
            }
            else
            {
                _logger.LogWarning("Xác thực OTP thất bại");
                return false;
            }
        }
        #endregion

        #region Resend OTP
        public async Task<bool> ResendOtpAsync(ResendOtpRequest resendOtpRequest)
        {
            if (_memoryCache.TryGetValue($"user_{resendOtpRequest.Email}", out User userAccount))
            {
                var otp = GenerateOtp();
                var otpExpiration = DateTime.UtcNow.AddMinutes(5);
                _memoryCache.Set($"otp_{resendOtpRequest.Email}", otp, otpExpiration);

                var emailSent = await _emailService.SendOtpEmailAsync(resendOtpRequest.Email, otp);

                if (emailSent)
                {
                    _logger.LogInformation("Gửi lại OTP thành công. OTP: {Otp}", otp);
                    return true;
                }
                else
                {
                    _logger.LogError("Gửi lại OTP thất bại");
                    return false;
                }
            }

            _logger.LogWarning("Không tìm thấy người dùng trong cache để gửi lại OTP");
            return false;
        }
        #endregion

        #region Login
        public async Task<LoginResponse> LoginAsync(LoginRequest loginRequest)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);
            if (user == null)
            {
                _logger.LogWarning("Email không tồn tại: {Email}", loginRequest.Email);
                return null;
            }

            // Kiểm tra tài khoản đã được kích hoạt chưa
            if (!user.IsActive)
            {
                _logger.LogWarning("Tài khoản chưa được kích hoạt: {Email}", user.Email);
                return null;
            }

            // Kiểm tra mật khẩu
            if (!BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.Password))
            {
                _logger.LogWarning("Sai mật khẩu cho email: {Email}", user.Email);
                return null;
            }

            // Kiểm tra nếu người dùng đã có refreshToken hợp lệ, không cần tạo mới
            if (string.IsNullOrEmpty(user.RefreshToken) || user.RefreshTokenExpires < DateTime.UtcNow)
            {
                // Nếu không có refresh token hợp lệ, tạo mới
                user.RefreshToken = GenerateRefreshToken();

                // Chuyển đổi thời gian UTC thành giờ Việt Nam (UTC+7)
                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Múi giờ Việt Nam
                var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

                user.RefreshTokenExpires = vietnamTime.AddDays(7); // Thời gian hết hạn refresh token sau 7 ngày
            }

            // Tạo Access Token mới
            var accessToken = GenerateJwtToken(user);

            // Cập nhật lại thông tin user trong DB
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Đăng nhập thành công: {Email}", user.Email);

            // Trả về LoginResponse chứa UserResponse và các token
            return new LoginResponse
            {
                UserResponse = new UserResponse  // Chuyển đổi từ User sang UserResponse
                {
                    Id = user.Id,
                    Fullname = user.Fullname,
                    Email = user.Email
                },
                AccessToken = accessToken,  // Trả về AccessToken
                RefreshToken = user.RefreshToken  // Trả về RefreshToken
            };
        }
        #endregion

        #region Xử lý RefreshToken
        // Xử lý Refresh Token
        public async Task<string> RefreshTokenAsync(string refreshToken)
        {
            // Tìm user với refresh token
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
            if (user == null || user.RefreshTokenExpires < DateTime.UtcNow)
            {
                // Nếu không tìm thấy user hoặc refresh token đã hết hạn
                _logger.LogWarning("Refresh Token không hợp lệ hoặc đã hết hạn.");
                return null;
            }

            // Tạo Access Token mới
            var newAccessToken = GenerateJwtToken(user);

            // Trả về Access Token mới
            return newAccessToken;
        }
        #endregion
    }
}
