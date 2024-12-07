using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;

public class TokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _memoryCache;

    public TokenMiddleware(RequestDelegate next, IMemoryCache memoryCache)
    {
        _next = next;
        _memoryCache = memoryCache;
    }

    public async Task Invoke(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        if (!string.IsNullOrEmpty(token))
        {
            if (_memoryCache.TryGetValue($"blacklist_{token}", out _))
            {
                context.Response.StatusCode = 401; // Token đã logout
                await context.Response.WriteAsync("Token đã logout.");
                return;
            }
        }

        await _next(context);
    }
}
