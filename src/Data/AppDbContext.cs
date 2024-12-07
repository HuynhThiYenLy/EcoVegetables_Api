using Microsoft.EntityFrameworkCore;
using ecovegetables_api.src.Models;
using EcoVegetables_Api.src.Models;

namespace ecovegetables_api.src.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }

        #region OnModel
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình User
            #region User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Fullname).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Password).IsRequired(false);
                entity.Property(u => u.Avatar).IsRequired(false);
                entity.Property(u => u.GoogleId).IsRequired(false);
                entity.Property(u => u.IsActive).HasDefaultValue(true);
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(u => u.UpdatedAt)
                    .HasDefaultValueSql("GETDATE()")
                    .ValueGeneratedOnAddOrUpdate();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.GoogleId).IsUnique().HasFilter("[GoogleId] IS NOT NULL");
                entity.Property(u => u.Address).IsRequired(false);
            });
            #endregion

            // Cấu hình Category
            #region Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(c => c.ParentId)
                    .IsRequired(false);

                // Cấu hình self-referencing relationship
                entity.HasOne(c => c.Parent)
                    .WithMany(c => c.Children)
                    .HasForeignKey(c => c.ParentId)
                    .OnDelete(DeleteBehavior.Restrict); // Ngăn xoá parent nếu còn child
            });
            #endregion
        }
        #endregion
    }
}
