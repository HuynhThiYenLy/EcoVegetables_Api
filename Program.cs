using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ecovegetables_api.src.Data;
using ecovegetables_api.src.Services;
using ecovegetables_api.src.Models;
using Microsoft.AspNetCore.Identity;
using ecovegetables_api.src.Middleware;

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
builder.Services.AddSingleton<EmailService>();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 5. CORS Middleware
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 6. Build app
var app = builder.Build();

// 7. Middleware CORS
app.UseCors("AllowAllOrigins");

// 8. Swagger Dev Only
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 9. Authentication + Middleware TokenBlacklist
app.UseMiddleware<TokenMiddleware>(); // Token blacklist middleware
app.UseMiddleware<CheckAccountMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// 10. Map Route
app.MapControllers();

// 11. Run application
app.Run();
