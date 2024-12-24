using ecovegetables_api.src.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ecovegetables_api.src.Infrastructure.Data
{
      public class AppDbContext : DbContext
      {
            public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

            // MARK: DbSet
            public DbSet<User> Users { get; set; }

            // MARK: Entity Configuration
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                  base.OnModelCreating(modelBuilder);

                  // Đăng ký tất cả cấu hình Entity
                  modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            }
      }
}
