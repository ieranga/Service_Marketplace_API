using Microsoft.EntityFrameworkCore;
using Service_Marketplace_API.Entities;

namespace Service_Marketplace_API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<ProviderServiceProfile> ProviderServiceProfiles => Set<ProviderServiceProfile>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ServiceEntity> Services => Set<ServiceEntity>();
    public DbSet<ServiceVariant> ServiceVariants => Set<ServiceVariant>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ServiceVariantTag> ServiceVariantTags => Set<ServiceVariantTag>();
    public DbSet<ProviderService> ProviderServices => Set<ProviderService>();
    public DbSet<ProviderServiceArea> ProviderServiceAreas => Set<ProviderServiceArea>();
    public DbSet<ProviderServiceTag> ProviderServiceTags => Set<ProviderServiceTag>();
    public DbSet<UserJob> UserJobs => Set<UserJob>();
    public DbSet<UserJobArea> UserJobAreas => Set<UserJobArea>();
    public DbSet<UserJobTag> UserJobTags => Set<UserJobTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            entity.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(30);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.NIC).HasMaxLength(30);
            entity.Property(u => u.Role).HasConversion<int>();
            entity.Property(u => u.Status).HasConversion<int>();

            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.PhoneNumber);

            entity.HasOne(u => u.Profile)
                  .WithOne(p => p.User)
                  .HasForeignKey<UserProfile>(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(u => u.ServiceProfile)
                  .WithOne(p => p.User)
                  .HasForeignKey<ProviderServiceProfile>(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.Jobs)
                  .WithOne(j => j.User)
                  .HasForeignKey(j => j.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserProfile Configuration
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.City).HasMaxLength(100);
            entity.Property(p => p.Address).HasMaxLength(250);
            entity.Property(p => p.Bio).HasMaxLength(500);
        });

        // ProviderServiceProfile Configuration
        modelBuilder.Entity<ProviderServiceProfile>(entity =>
        {
            entity.ToTable("ProviderServiceProfiles");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.BusinessName).HasMaxLength(150);
            entity.Property(p => p.BusinessRegistrationNumber).HasMaxLength(50);
            entity.Property(p => p.Bio).HasMaxLength(1000);
            entity.Property(p => p.VerificationStatus).HasMaxLength(30);
        });

        // Category Configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Slug).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Description).HasMaxLength(500);
            entity.Property(c => c.Icon).HasMaxLength(50);

            entity.HasIndex(c => c.Slug).IsUnique();

            entity.HasMany(c => c.Services)
                  .WithOne(s => s.Category)
                  .HasForeignKey(s => s.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ServiceEntity Configuration
        modelBuilder.Entity<ServiceEntity>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Slug).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Description).HasMaxLength(500);

            entity.HasIndex(s => new { s.CategoryId, s.Slug }).IsUnique();

            entity.HasMany(s => s.Variants)
                  .WithOne(v => v.Service)
                  .HasForeignKey(v => v.ServiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ServiceVariant Configuration
        modelBuilder.Entity<ServiceVariant>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Name).IsRequired().HasMaxLength(100);
            entity.Property(v => v.Slug).IsRequired().HasMaxLength(100);
            entity.Property(v => v.Description).HasMaxLength(500);
            entity.Property(v => v.SuggestedStartingPrice).HasPrecision(18, 2);

            entity.HasIndex(v => new { v.ServiceId, v.Slug }).IsUnique();
        });

        // Tag Configuration
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(80);
            entity.Property(t => t.Slug).IsRequired().HasMaxLength(80);
            entity.Property(t => t.TagGroup).HasMaxLength(50);

            entity.HasIndex(t => t.Slug).IsUnique();
        });

        // ServiceVariantTag (Many-to-Many junction)
        modelBuilder.Entity<ServiceVariantTag>(entity =>
        {
            entity.HasKey(vt => new { vt.ServiceVariantId, vt.TagId });

            entity.HasOne(vt => vt.ServiceVariant)
                  .WithMany(v => v.VariantTags)
                  .HasForeignKey(vt => vt.ServiceVariantId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(vt => vt.Tag)
                  .WithMany(t => t.VariantTags)
                  .HasForeignKey(vt => vt.TagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ProviderService Configuration
        modelBuilder.Entity<ProviderService>(entity =>
        {
            entity.ToTable("ProviderServices");
            entity.HasKey(ps => ps.Id);
            entity.Property(ps => ps.StartingPrice).HasPrecision(18, 2);
            entity.Property(ps => ps.PriceUnit).HasMaxLength(30);
            entity.Property(ps => ps.Description).HasMaxLength(1000);

            entity.HasOne(ps => ps.ProviderServiceProfile)
                  .WithMany(p => p.Services)
                  .HasForeignKey(ps => ps.ProviderServiceProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ps => ps.ServiceVariant)
                  .WithMany(v => v.ProviderServices)
                  .HasForeignKey(ps => ps.ServiceVariantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ProviderServiceArea Configuration
        modelBuilder.Entity<ProviderServiceArea>(entity =>
        {
            entity.ToTable("ProviderServiceAreas");
            entity.HasKey(pa => pa.Id);
            entity.Property(pa => pa.CityName).IsRequired().HasMaxLength(100);

            entity.HasOne(pa => pa.ProviderServiceProfile)
                  .WithMany(p => p.ServiceAreas)
                  .HasForeignKey(pa => pa.ProviderServiceProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ProviderServiceTag (Many-to-Many junction)
        modelBuilder.Entity<ProviderServiceTag>(entity =>
        {
            entity.HasKey(pst => new { pst.ProviderServiceId, pst.TagId });

            entity.HasOne(pst => pst.ProviderService)
                  .WithMany(ps => ps.ProviderServiceTags)
                  .HasForeignKey(pst => pst.ProviderServiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pst => pst.Tag)
                  .WithMany(t => t.ProviderServiceTags)
                  .HasForeignKey(pst => pst.TagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserJob Configuration
        modelBuilder.Entity<UserJob>(entity =>
        {
            entity.ToTable("UserJobs");
            entity.HasKey(j => j.Id);
            entity.Property(j => j.Title).IsRequired().HasMaxLength(200);
            entity.Property(j => j.Description).HasMaxLength(2000);
            entity.Property(j => j.Budget).HasPrecision(18, 2);
            entity.Property(j => j.PaymentMethod).HasMaxLength(50);
            entity.Property(j => j.Status).HasMaxLength(30);
            entity.Property(j => j.UrgencyOrPreferredDate).HasMaxLength(100);

            entity.HasOne(j => j.User)
                  .WithMany(u => u.Jobs)
                  .HasForeignKey(j => j.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(j => j.ServiceVariant)
                  .WithMany(v => v.UserJobs)
                  .HasForeignKey(j => j.ServiceVariantId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // UserJobArea Configuration
        modelBuilder.Entity<UserJobArea>(entity =>
        {
            entity.ToTable("UserJobAreas");
            entity.HasKey(ja => ja.Id);
            entity.Property(ja => ja.CityName).IsRequired().HasMaxLength(100);
            entity.Property(ja => ja.Address).HasMaxLength(250);

            entity.HasOne(ja => ja.UserJob)
                  .WithMany(j => j.JobAreas)
                  .HasForeignKey(ja => ja.UserJobId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserJobTag (Many-to-Many junction)
        modelBuilder.Entity<UserJobTag>(entity =>
        {
            entity.ToTable("UserJobTags");
            entity.HasKey(jt => new { jt.UserJobId, jt.TagId });

            entity.HasOne(jt => jt.UserJob)
                  .WithMany(j => j.JobTags)
                  .HasForeignKey(jt => jt.UserJobId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(jt => jt.Tag)
                  .WithMany(t => t.UserJobTags)
                  .HasForeignKey(jt => jt.TagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
