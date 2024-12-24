using System.Text;
using ecovegetables_api.src.Application.Interfaces;
using ecovegetables_api.src.Application.Services;
using ecovegetables_api.src.Domain.Entities;
using ecovegetables_api.src.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Kestrel lắng nghe trên cổng 5217
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(System.Net.IPAddress.Any, 5217);
});

// 2. Cấu hình DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// 3. Cấu hình JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        };
    });

// 4. Cấu hình các dịch vụ thêm
builder.Services.AddScoped<PasswordHasher<User>>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 5. CORS Middleware
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://example.com")  // Đặt các origin cụ thể
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 6. Cấu hình Cookie (refresh token)
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true; // Kiểm tra consent nếu cần
    options.MinimumSameSitePolicy = SameSiteMode.Lax; // Cấu hình SameSite cho cookie
    options.Secure = CookieSecurePolicy.Always; // Cookie chỉ gửi qua HTTPS
});

// 7. Build app
var app = builder.Build();

// 8. Middleware CORS
app.UseCors("AllowSpecificOrigins");

// 9. Swagger Dev Only
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 10. Authentication + Middleware TokenBlacklist
// app.UseMiddleware<TokenMiddleware>();
// app.UseMiddleware<CheckAccountMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// 11. Cấu hình cookie policy (cho refresh token)
app.UseCookiePolicy();  // Đảm bảo cookies được xử lý

// 12. Map Route
app.MapControllers();

// 13. Run application
app.Run();
