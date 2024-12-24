using ecovegetables_api.src.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ecovegetables_api.src.Infrastructure.Data.Configurations
{
       public class UserEntity : IEntityTypeConfiguration<User>
       {
              public void Configure(EntityTypeBuilder<User> builder)
              {
                     builder.HasKey(u => u.Id);

                     builder.Property(u => u.Fullname)
                            .IsRequired()
                            .HasMaxLength(100);

                     builder.Property(u => u.Email)
                            .IsRequired()
                            .HasMaxLength(100);

                     builder.HasIndex(u => u.Email)
                            .IsUnique();

                     builder.Property(u => u.Phone)
                            .IsRequired()
                            .HasMaxLength(10);

                     builder.Property(u => u.Password)
                            .IsRequired(false);

                     builder.Property(u => u.IsActive)
                            .HasDefaultValue(true);

                     builder.Property(u => u.Avatar)
                            .IsRequired();

                     builder.Property(u => u.Address)
                            .IsRequired(false);

                     builder.Property(u => u.GoogleId)
                            .IsRequired(false);

                     builder.HasIndex(u => u.GoogleId)
                            .IsUnique()
                            .HasFilter("[GoogleId] IS NOT NULL");

                     builder.Property(u => u.CreatedAt)
                            .HasDefaultValueSql("GETDATE()");

                     builder.Property(u => u.UpdatedAt)
                            .HasDefaultValueSql("GETDATE()")
                            .ValueGeneratedOnAddOrUpdate();

                     builder.Property(u => u.RefreshToken)
                            .IsRequired(false);

                     builder.Property(u => u.RefreshTokenExpires)
                            .IsRequired(false);
              }
       }
}
