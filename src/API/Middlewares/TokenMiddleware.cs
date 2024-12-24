using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;

namespace ecovegetables_api.src.API.Middlewares
{
    public class TokenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _memoryCache;
        private readonly IConfiguration _configuration;

        public TokenMiddleware(RequestDelegate next, IMemoryCache memoryCache, IConfiguration configuration)
        {
            _next = next;
            _memoryCache = memoryCache;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<TokenMiddleware>>();

            // Kiểm tra URL có phải là các endpoint không yêu cầu token
            var excludedPaths = new HashSet<string>
            {
                "/user/verifyOtp", "/user/login", "/user/add"
            }; 

            if (excludedPaths.Contains(context.Request.Path.Value))
            {
                await _next(context);  // Bỏ qua middleware kiểm tra token nếu là các endpoint không yêu cầu
                return;
            }

            // Kiểm tra Authorization header để lấy token
            var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            // Nếu không có token trong header, trả về 401 Unauthorized
            if (string.IsNullOrEmpty(token))
            {
                logger.LogWarning("Token không được cung cấp.");
                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsync("Token không được cung cấp.");
                return;
            }

            // Kiểm tra nếu token đã bị blacklist (giả định sử dụng cache)
            if (_memoryCache.TryGetValue($"blacklist_{token}", out _))
            {
                logger.LogWarning("Token đã bị blacklist.");
                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsync("Token đã bị blacklist.");
                return;
            }

            // Kiểm tra tính hợp lệ của token
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                }, out SecurityToken validatedToken);
            }
            catch (SecurityTokenException)
            {
                logger.LogWarning("Token không hợp lệ.");
                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsync("Token không hợp lệ.");
                return;
            }

            // Nếu token hợp lệ, tiếp tục xử lý yêu cầu
            await _next(context);
        }
    }
}
