using Microsoft.EntityFrameworkCore;
using ecovegetables_api.src.Models;

namespace ecovegetables_api.src.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Fullname).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(100);

                // Mật khẩu không bắt buộc với Google login
                entity.Property(u => u.Password).IsRequired(false);

                entity.Property(u => u.Avatar).IsRequired(false); 

                // Cấu hình Google login
                entity.Property(u => u.GoogleId).IsRequired(false); 

                entity.Property(u => u.IsActive).HasDefaultValue(true);

                entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(u => u.UpdatedAt)
                    .HasDefaultValueSql("GETDATE()")
                    .ValueGeneratedOnAddOrUpdate();

                // Tạo index duy nhất cho cột Email
                entity.HasIndex(u => u.Email).IsUnique();

                // Tạo index duy nhất cho cột GoogleId (chỉ khi có giá trị)
                entity.HasIndex(u => u.GoogleId).IsUnique().HasFilter("[GoogleId] IS NOT NULL");

                entity.Property(u => u.Address).IsRequired(false);
            });
        }
    }
}
