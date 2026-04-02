using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<VendorNote> VendorNotes => Set<VendorNote>();
    public DbSet<VendorPayment> VendorPayments => Set<VendorPayment>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<VendorInvite> VendorInvites => Set<VendorInvite>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enum → string conversions
        modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>();
        modelBuilder.Entity<ServiceRequest>().Property(s => s.Priority).HasConversion<string>();
        modelBuilder.Entity<ServiceRequest>().Property(s => s.Status).HasConversion<string>();
        modelBuilder.Entity<VendorInvite>().Property(v => v.Status).HasConversion<string>();
        modelBuilder.Entity<Quote>().Property(q => q.Status).HasConversion<string>();
        modelBuilder.Entity<Proposal>().Property(p => p.Status).HasConversion<string>();
        modelBuilder.Entity<WorkOrder>().Property(w => w.Status).HasConversion<string>();
        modelBuilder.Entity<VendorPayment>().Property(vp => vp.Status).HasConversion<string>();

        // JSON columns for Vendor arrays
        modelBuilder.Entity<Vendor>().Property(v => v.Trades).HasColumnType("jsonb");
        modelBuilder.Entity<Vendor>().Property(v => v.ZipCodes).HasColumnType("jsonb");

        // Unique indexes
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Quote>().HasIndex(q => q.PublicToken).IsUnique();
        modelBuilder.Entity<Proposal>().HasIndex(p => p.PublicToken).IsUnique();
        modelBuilder.Entity<Proposal>().HasIndex(p => p.ServiceRequestId).IsUnique();
        modelBuilder.Entity<WorkOrder>().HasIndex(wo => wo.ServiceRequestId).IsUnique();
        modelBuilder.Entity<VendorInvite>().HasIndex(vi => new { vi.ServiceRequestId, vi.VendorId }).IsUnique();

        // Performance indexes
        modelBuilder.Entity<ServiceRequest>().HasIndex(sr => sr.Status);
        modelBuilder.Entity<ServiceRequest>().HasIndex(sr => sr.ClientId);
        modelBuilder.Entity<Notification>().HasIndex(n => new { n.UserId, n.Read });
        modelBuilder.Entity<VendorNote>().HasIndex(vn => vn.VendorId);
        modelBuilder.Entity<VendorNote>().HasIndex(vn => vn.CreatedAt);
        modelBuilder.Entity<VendorPayment>().HasIndex(vp => vp.VendorId);

        // Relationships
        modelBuilder.Entity<ServiceRequest>()
            .HasOne(sr => sr.Client).WithMany(c => c.ServiceRequests).HasForeignKey(sr => sr.ClientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ServiceRequest>()
            .HasOne(sr => sr.CreatedBy).WithMany().HasForeignKey(sr => sr.CreatedById).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Author).WithMany(u => u.Comments).HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User).WithMany(u => u.Notifications).HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VendorNote>()
            .HasOne(vn => vn.Vendor).WithMany(v => v.Notes).HasForeignKey(vn => vn.VendorId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<VendorNote>()
            .HasOne(vn => vn.CreatedBy).WithMany().HasForeignKey(vn => vn.CreatedById).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VendorPayment>()
            .HasOne(vp => vp.Vendor).WithMany(v => v.Payments).HasForeignKey(vp => vp.VendorId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<VendorPayment>()
            .HasOne(vp => vp.WorkOrder).WithMany().HasForeignKey(vp => vp.WorkOrderId).OnDelete(DeleteBehavior.SetNull);

        // Vendor.UserId is now optional (email-first vendors don't need user accounts)
        modelBuilder.Entity<Vendor>()
            .HasOne(v => v.User).WithOne(u => u.Vendor).HasForeignKey<Vendor>(v => v.UserId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        // Decimal precision
        modelBuilder.Entity<Quote>().Property(q => q.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Proposal>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Vendor>().Property(v => v.Rating).HasPrecision(3, 2);
        modelBuilder.Entity<VendorPayment>().Property(vp => vp.Amount).HasPrecision(18, 2);
    }
}
