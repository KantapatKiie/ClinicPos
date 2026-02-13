using ClinicPos.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClinicPos.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(x => x.Id);
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("branches");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            entity.HasOne(x => x.Tenant).WithMany(x => x.Branches).HasForeignKey(x => x.TenantId);
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ApiToken).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            entity.Property(x => x.Role).HasConversion<string>();
        });

        modelBuilder.Entity<UserBranch>(entity =>
        {
            entity.ToTable("user_branches");
            entity.HasKey(x => new { x.UserAccountId, x.BranchId });
            entity.HasOne(x => x.UserAccount).WithMany(x => x.UserBranches).HasForeignKey(x => x.UserAccountId);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("patients");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.PhoneNumber }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.PrimaryBranchId, x.CreatedAt });
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("appointments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.PatientId, x.BranchId, x.StartAt }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.BranchId, x.StartAt });
        });
    }
}
