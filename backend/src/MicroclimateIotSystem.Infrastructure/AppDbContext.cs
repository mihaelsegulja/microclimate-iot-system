using MicroclimateIotSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<TelemetryReading> TelemetryReadings { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50);
            entity.HasIndex(e => e.Username)
                .IsUnique();
            entity.Property(e => e.PasswordHash)
                .IsRequired();
            entity.Property(e => e.Role)
                .IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(256);
            entity.HasIndex(e => e.Token)
                .IsUnique();

            entity.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HardwareId)
                .HasColumnType("varchar(32)")
                .IsRequired();
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(255)")
                .IsRequired();
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .IsRequired();
            entity.Property(e => e.TelemetryIntervalSeconds)
                .HasColumnType("int")
                .IsRequired();
        });

        modelBuilder.Entity<TelemetryReading>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HardwareId)
                .HasColumnType("varchar(32)")
                .IsRequired();
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime2(0)")
                .IsRequired();
            entity.Property(e => e.Key)
                .HasColumnType("varchar(50)")
                .IsRequired();
            entity.Property(e => e.Value)
                .HasColumnType("real")
                .IsRequired();
            entity.Property(e => e.Unit)
                .HasColumnType("varchar(50)")
                .IsRequired(false);
        });
    }
}
