using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ecovegetables_api.src.Models;
using ecovegetables_api.src.Services;
using Microsoft.Extensions.Caching.Memory;
using ecovegetables_api.src.Data;
using ecovegetables_api.src.Request;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using EcoVegetables_Api.src.Request.User;

namespace ecovegetables_api.src.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _configuration;  // truy cập vào nhiều file khác như pakage.json
        private readonly EmailService _emailService;
        private readonly ILogger<UserController> _logger; 
        private readonly IMemoryCache _memoryCache; 
        private readonly AppDbContext _context;

        public UserController(IConfiguration configuration, EmailService emailService, ILogger<UserController> logger, IMemoryCache memoryCache, AppDbContext context)
        {
            _configuration = configuration;  
            _emailService = emailService;
            _logger = logger;
            _memoryCache = memoryCache;
            _context = context;
        }

        #region tạo OTP
        // (6 chữ số)
        private string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
        #endregion

        #region add
        [HttpPost("add")]
        public async Task<IActionResult> AddUserAsync([FromBody] RegisterRequest registerRequest)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerRequest.Email);
            if (existingUser != null)
            {
                return BadRequest(new
                {
                    code = "EMAIL_EXISTS",
                    message = "Email đã tồn tại."
                });
            }

            var otp = GenerateOtp(); 

            var userAccount = new User
            {
                Fullname = registerRequest.Fullname,
                Email = registerRequest.Email,
                Phone = registerRequest.Phone,
                Password = registerRequest.Password, // Lưu mật khẩu gốc chưa băm
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var otpExpiration = DateTime.UtcNow.AddMinutes(5);
            _memoryCache.Set($"otp_{registerRequest.Email}", otp, otpExpiration);

            _memoryCache.Set($"user_{registerRequest.Email}", userAccount, otpExpiration);

            var emailSent = await _emailService.SendOtpEmailAsync(registerRequest.Email, otp);

            if (emailSent)
            {
                _logger.LogInformation($"Saved user: {userAccount.ToString()}");
                _logger.LogInformation($"Password: {registerRequest.Password}");
                _logger.LogInformation($"OTP generated: {otp}");
                _logger.LogInformation($"OTP sent successfully to {registerRequest.Email}");

                return Ok(new
                {
                    code = "USER_CREATED",
                    message = "Người dùng được tạo thành công. OTP đã được gửi!",
                    user = new
                    {
                        userAccount.Fullname,
                        userAccount.Email,
                        userAccount.Phone,
                        IsActive = userAccount.IsActive
                    }
                });
            }
            else
            {
                _logger.LogError($"Failed to send OTP to {registerRequest.Email}");
                return BadRequest(new
                {
                    code = "OTP_SEND_FAILED",
                    message = "Gửi OTP thất bại."
                });
            }
        }
        #endregion

        #region verifyOtp
        [HttpPost("verifyOtp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpVerificationRequest otpVerification)
        {
            if (string.IsNullOrEmpty(otpVerification.Email) || string.IsNullOrEmpty(otpVerification.Otp))
            {
                return BadRequest("Email hoặc OTP không hợp lệ.");
            }

            // Lấy OTP từ bộ nhớ cache
            if (_memoryCache.TryGetValue($"otp_{otpVerification.Email}", out string storedOtp))
            {
                // Kiểm tra OTP hợp lệ
                if (storedOtp == otpVerification.Otp)
                {
                    // Lấy người dùng từ bộ nhớ tạm (Cache)
                    if (_memoryCache.TryGetValue($"user_{otpVerification.Email}", out User userAccount))
                    {
                        // Băm mật khẩu ngay khi xác minh OTP thành công (tránh băm lại khi đăng ký)
                        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(userAccount.Password);

                        // Cập nhật mật khẩu đã băm vào đối tượng người dùng
                        userAccount.Password = hashedPassword;

                        // Cập nhật người dùng trong bộ nhớ tạm thành 'Active'
                        userAccount.IsActive = true;
                        userAccount.UpdatedAt = DateTime.UtcNow;

                        // Lưu người dùng vào cơ sở dữ liệu
                        _context.Users.Add(userAccount);
                        await _context.SaveChangesAsync();

                        // Log thông tin người dùng sau khi xác minh OTP thành công và lưu vào cơ sở dữ liệu
                        _logger.LogInformation($"User account successfully verified and saved: {userAccount.ToString()}");

                        // Xóa OTP và thông tin người dùng khỏi bộ nhớ tạm
                        _memoryCache.Remove($"otp_{otpVerification.Email}");
                        _memoryCache.Remove($"user_{otpVerification.Email}");

                        _logger.LogInformation("OTP verified and user saved successfully.");
                        return Ok("OTP verified successfully and user saved.");
                    }
                    else
                    {
                        _logger.LogWarning("User not found in temporary cache.");
                        return BadRequest("User not found.");
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid OTP provided.");
                    return BadRequest("Invalid OTP.");
                }
            }
            else
            {
                _logger.LogWarning("OTP has expired or not found.");
                return BadRequest("OTP has expired or was not sent.");
            }
        }
        #endregion

        #region resendOtp
        [HttpPost("resendOtp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest resendOtpRequest)
        {
            if (string.IsNullOrEmpty(resendOtpRequest.Email))
            {
                return BadRequest(new { message = "Email không được để trống." });
            }

            // Lấy người dùng từ bộ nhớ cache tạm thời
            if (_memoryCache.TryGetValue($"user_{resendOtpRequest.Email}", out User users))
            {
                var otp = GenerateOtp(); // Tạo OTP mới
                var otpExpiration = DateTime.UtcNow.AddMinutes(5); // Thời hạn 5 phút

                // Cập nhật OTP mới vào bộ nhớ cache
                _memoryCache.Set($"otp_{resendOtpRequest.Email}", otp, otpExpiration);

                // Gửi OTP qua email
                var emailSent = await _emailService.SendOtpEmailAsync(resendOtpRequest.Email, otp);

                if (emailSent)
                {
                    _logger.LogInformation($"OTP mới: {otp} đã được gửi tới {resendOtpRequest.Email}");
                    return Ok(new
                    {
                        message = "OTP đã được gửi lại thành công.",
                        email = resendOtpRequest.Email
                    });
                }
                else
                {
                    _logger.LogError($"Gửi OTP mới thất bại tới {resendOtpRequest.Email}");
                    return StatusCode(500, new { message = "Không thể gửi OTP." });
                }
            }
            else
            {
                _logger.LogWarning($"Không tìm thấy người dùng với email {resendOtpRequest.Email} trong bộ nhớ tạm.");
                return BadRequest(new { message = "Không tìm thấy người dùng hoặc OTP đã hết hạn." });
            }
        }
        #endregion

        #region login
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest loginRequest)
        {
            try
            {
                // Kiểm tra input
                if (string.IsNullOrEmpty(loginRequest.Email) || string.IsNullOrEmpty(loginRequest.Password))
                {
                    return BadRequest(new
                    {
                        status = 400,
                        error = "Thông tin không hợp lệ",
                        details = "Email và mật khẩu không được để trống."
                    });
                }

                // Truy vấn người dùng từ database
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);
                if (user == null)
                {
                    _logger.LogWarning($"Đăng nhập thất bại: Email {loginRequest.Email} không tồn tại.");
                    return BadRequest(new
                    {
                        status = 400,
                        error = "Thông tin không hợp lệ",
                        details = "Tài khoản không tồn tại."
                    });
                }

                // Kiểm tra mật khẩu
                if (string.IsNullOrEmpty(user.Password) || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.Password))
                {
                    _logger.LogWarning($"Đăng nhập thất bại: Sai mật khẩu cho email {loginRequest.Email}.");
                    return BadRequest(new
                    {
                        status = 400,
                        error = "Thông tin không hợp lệ",
                        details = "Mật khẩu sai."
                    });
                }

                // Kiểm tra trạng thái tài khoản
                if (!user.IsActive)
                {
                    _logger.LogWarning($"Tài khoản bị khóa: Email {user.Email}");
                    return BadRequest(new
                    {
                        status = 400,
                        error = "Tài khoản bị khóa",
                        details = "Tài khoản của bạn đã bị khóa."
                    });
                }

                // Kiểm tra cấu hình JWT
                if (string.IsNullOrEmpty(_configuration["Jwt:Key"]) ||
                    string.IsNullOrEmpty(_configuration["Jwt:Issuer"]) ||
                    string.IsNullOrEmpty(_configuration["Jwt:Audience"]))
                {
                    _logger.LogError("Cấu hình JWT bị thiếu.");
                    return StatusCode(500, new
                    {
                        status = 500,
                        error = "Lỗi cấu hình",
                        details = "Cấu hình JWT không đầy đủ."
                    });
                }

                // Tạo JWT
                var claims = new[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email)
        };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddHours(1),
                    signingCredentials: creds
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                var response = new
                {
                    message = "Login successful",
                    token = tokenString,
                    user = new
                    {
                        user.Id,
                        user.Fullname,
                        user.Email,
                        user.Phone
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi trong login: {ex.Message} - StackTrace: {ex.StackTrace}");
                return StatusCode(500, new
                {
                    status = 500,
                    error = "Lỗi hệ thống",
                    details = "Đã xảy ra lỗi trong quá trình xử lý."
                });
            }
        }
        #endregion

        #region logout
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token không hợp lệ.");
            }

            _memoryCache.Set($"blacklist_{token}", true, TimeSpan.FromMinutes(30)); // Lưu token logout trong 30 phút

            return Ok(new { message = "Logout successful." });
        }
        #endregion


        #region getAll
        [HttpGet("getAll")]
        public async Task<IActionResult> GetAllUsersAsync()
        {
            try
            {
                // Lấy tất cả người dùng từ cơ sở dữ liệu
                var users = await _context.Users.ToListAsync();

                if (users == null || users.Count == 0)
                {
                    return NotFound("Không có người dùng nào.");
                }

                // Trả về danh sách người dùng
                return Ok(new
                {
                    message = "Lấy danh sách người dùng thành công",
                    users = users.Select(user => new
                    {
                        user.Id,
                        user.Fullname,
                        user.Email,
                        user.Phone,
                        user.IsActive,
                        user.CreatedAt,
                        user.UpdatedAt
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi lấy danh sách người dùng: {ex.Message}");
                return StatusCode(500, "Đã xảy ra lỗi trong quá trình xử lý.");
            }
        }
        #endregion

        #region getProfile
        [HttpGet("profile")]
        [Authorize]  // Phải đăng nhập để xem thông tin profile
        public async Task<IActionResult> GetProfileAsync()
        {
            // Lấy thông tin người dùng từ JWT token (dùng claim)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (user == null)
            {
                return BadRequest("User not found.");
            }

            var userProfile = new
            {
                user.Id,
                user.Fullname,
                user.Email,
                user.Phone,
                user.CreatedAt,
                user.UpdatedAt,
                user.GoogleId
            };

            return Ok(userProfile);
        }
        #endregion

        #region update
        [Authorize]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateRequest updateRequest)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new { message = "Người dùng không tồn tại" });
                }

                if (!string.IsNullOrEmpty(updateRequest.Fullname))
                {
                    user.Fullname = updateRequest.Fullname;
                }

                if (!string.IsNullOrEmpty(updateRequest.Phone))
                {
                    user.Phone = updateRequest.Phone;
                }

                if (!string.IsNullOrEmpty(updateRequest.Password))
                {
                    // Hash mật khẩu nếu được gửi
                    user.Password = BCrypt.Net.BCrypt.HashPassword(updateRequest.Password);
                }

                if (!string.IsNullOrEmpty(updateRequest.Avatar))
                {
                    user.Avatar = updateRequest.Avatar;
                }

                if (!string.IsNullOrEmpty(updateRequest.Address))
                {
                    user.Address = updateRequest.Address;
                }

                _context.Users.Update(user); // Lưu thông tin
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Cập nhật thông tin người dùng thành công",
                    data = user
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Có lỗi xảy ra trong quá trình cập nhật thông tin",
                    error = ex.Message
                });
            }
        }
        #endregion

        #region toggle lock/unlock
        [Authorize]
        [HttpPut("toggle/{id}")]
        public async Task<IActionResult> ToggleUserAccount(int id)
        {
            try
            {
                // Tìm người dùng theo ID
                var user = await _context.Users.FindAsync(id);

                // Kiểm tra xem người dùng có tồn tại không
                if (user == null)
                {
                    _logger.LogWarning($"Người dùng với ID {id} không tồn tại.");
                    return NotFound(new { message = "Người dùng không tồn tại." });
                }

                if (user.IsActive)
                {
                    // Khóa tài khoản
                    user.IsActive = false;
                    _logger.LogInformation($"Khóa tài khoản ID {id}.");

                    try
                    {
                        var emailSent = await _emailService.SendLockNotificationEmailAsync(user.Email, user.Fullname);
                        if (emailSent)
                        {
                            _logger.LogInformation($"Đã gửi email thông báo khóa tài khoản tới {user.Email}.");
                        }
                        else
                        {
                            _logger.LogWarning($"Gửi email khóa thất bại tới {user.Email}.");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError($"Lỗi gửi email khóa: {emailEx.Message}");
                    }
                }
                else
                {
                    // Mở khóa tài khoản
                    user.IsActive = true;
                    _logger.LogInformation($"Mở khóa tài khoản ID {id}.");

                    try
                    {
                        var emailSent = await _emailService.SendLockNotificationEmailAsync(user.Email, user.Fullname);
                        if (emailSent)
                        {
                            _logger.LogInformation($"Đã gửi email thông báo mở khóa tài khoản tới {user.Email}.");
                        }
                        else
                        {
                            _logger.LogWarning($"Gửi email mở khóa thất bại tới {user.Email}.");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError($"Lỗi gửi email mở khóa: {emailEx.Message}");
                    }
                }

                // Cập nhật thông tin vào DB
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Cập nhật trạng thái thành công cho ID {id}. Trạng thái mới: {(user.IsActive ? "Đang hoạt động" : "Đã khóa")}");
                return Ok(new { message = $"Tài khoản đã {(user.IsActive ? "mở khóa" : "khóa")} thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi thực hiện khóa/mở khóa tài khoản: {ex.Message}");
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình khóa hoặc mở khóa tài khoản." });
            }
        }
        #endregion
    }
}