using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.Infrastructure;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<TelemetryReading> TelemetryReadings { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<AlertRule> AlertRules { get; set; }
    
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
                .HasMaxLength(32)
                .IsUnicode(false)
                .IsRequired();
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(e => e.IsActive)
                .IsRequired();
            entity.Property(e => e.TelemetryIntervalSeconds)
                .IsRequired();
        });

        modelBuilder.Entity<TelemetryReading>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HardwareId)
                .HasMaxLength(32)
                .IsUnicode(false)
                .IsRequired();
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime2(0)")
                .IsRequired();
            entity.Property(e => e.Key)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();
            entity.Property(e => e.Value)
                .HasColumnType("real")
                .IsRequired();
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired(false);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.HasMany(r => r.Devices)
                .WithOne(d => d.Room)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AlertRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(e => e.TelemetryKey)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();
            entity.Property(e => e.ThresholdValue)
                .HasColumnType("real")
                .IsRequired();
            entity.Property(e => e.Operator)
                .IsRequired();
            
            entity.HasOne(e => e.Room)
                .WithMany()
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Device)
                .WithMany()
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
