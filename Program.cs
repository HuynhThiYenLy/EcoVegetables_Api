using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ecovegetables_api.src.Data;
using ecovegetables_api.src.Services;
using ecovegetables_api.src.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Kestrel để lắng nghe tất cả các địa chỉ và cổng 5217
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(System.Net.IPAddress.Any, 5217); // Lắng nghe trên tất cả các địa chỉ và cổng 5217
});

// 2. Cấu hình DbContext với Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// 3. Cấu hình các dịch vụ cho JWT
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])) // Đảm bảo khóa đúng
        };
    });


// 4. Đăng ký các dịch vụ khác
builder.Services.AddScoped<PasswordHasher<User>>();      // Đăng ký PasswordHasher cho User
builder.Services.AddSingleton<EmailService>();           // Đăng ký EmailService

// 5. Đăng ký các dịch vụ mặc định
builder.Services.AddControllers();                      // Thêm các controllers
builder.Services.AddMemoryCache();                      // Thêm caching
builder.Services.AddEndpointsApiExplorer();             // Cho Swagger
builder.Services.AddSwaggerGen();                       // Thêm Swagger

// 6. Cấu hình CORS cho phép mọi nguồn (cross-origin)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()  // Cho phép tất cả các nguồn (Origins)
              .AllowAnyMethod()  // Cho phép tất cả các phương thức (GET, POST, PUT, DELETE, ...)
              .AllowAnyHeader(); // Cho phép tất cả các header
    });
});

// Tạo ứng dụng từ builder
var app = builder.Build();

// 7. Cấu hình CORS phải được đặt trước các middleware khác
app.UseCors("AllowAllOrigins");

// 8. Middleware cho phát triển và bảo mật
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 9. Đảm bảo bạn gọi UseAuthentication và UseAuthorization sau khi cấu hình các middleware khác
app.UseAuthentication(); // Đảm bảo bạn gọi UseAuthentication
app.UseAuthorization();  // Đảm bảo bạn gọi UseAuthorization

// 10. Ánh xạ các route từ controllers
app.MapControllers();

// 11. Chạy ứng dụng
app.Run();
