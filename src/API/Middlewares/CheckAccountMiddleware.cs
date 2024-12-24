using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ecovegetables_api.src.Infrastructure.Data;

namespace ecovegetables_api.src.API.Middlewares
{
    public class CheckAccountMiddleware
    {
        private readonly RequestDelegate _next;

        public CheckAccountMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            try
            {
                // Kiểm tra xem người dùng đã xác thực
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                    if (userId != null)
                    {
                        // Kiểm tra thông tin người dùng trong database
                        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);

                        if (user == null)
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsync("Tài khoản không tồn tại.");
                            return;
                        }

                        if (!user.IsActive)
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            await context.Response.WriteAsync("Tài khoản của bạn đã bị khóa.");
                            return;
                        }
                    } 
                }

                // Tiếp tục request
                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Lỗi trong middleware: " + ex.Message);
            }
        }
    }
}
