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
    public DbSet<QuoteLineItem> QuoteLineItems => Set<QuoteLineItem>();
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<ProposalAttachment> ProposalAttachments => Set<ProposalAttachment>();
    public DbSet<ProposalLineItem> ProposalLineItems => Set<ProposalLineItem>();
    public DbSet<ProposalVersion> ProposalVersions => Set<ProposalVersion>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<WorkOrderDocument> WorkOrderDocuments => Set<WorkOrderDocument>();
    public DbSet<InboundEmail> InboundEmails => Set<InboundEmail>();
    public DbSet<InboundEmailAttachment> InboundEmailAttachments => Set<InboundEmailAttachment>();
    public DbSet<OutboundEmail> OutboundEmails => Set<OutboundEmail>();
    public DbSet<OutboundEmailAttachment> OutboundEmailAttachments => Set<OutboundEmailAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enum → string conversions
        modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>();
        modelBuilder.Entity<User>().Property(u => u.Status).HasConversion<string>();
        modelBuilder.Entity<User>().Ignore(u => u.Name);
        modelBuilder.Entity<ServiceRequest>().Property(s => s.Priority).HasConversion<string>();
        modelBuilder.Entity<ServiceRequest>().Property(s => s.Status).HasConversion<string>();
        modelBuilder.Entity<VendorInvite>().Property(v => v.Status).HasConversion<string>();
        modelBuilder.Entity<Quote>().Property(q => q.Status).HasConversion<string>();
        modelBuilder.Entity<Proposal>().Property(p => p.Status).HasConversion<string>();
        modelBuilder.Entity<WorkOrder>().Property(w => w.Status).HasConversion<string>();
        modelBuilder.Entity<VendorPayment>().Property(vp => vp.Status).HasConversion<string>();
        modelBuilder.Entity<Invoice>().Property(i => i.Status).HasConversion<string>();
        modelBuilder.Entity<ActivityLog>().Property(a => a.Category).HasConversion<string>();
        modelBuilder.Entity<Vendor>().Property(v => v.Status).HasConversion<string>();

        // JSON columns for Vendor arrays
        modelBuilder.Entity<Vendor>().Property(v => v.Trades).HasColumnType("jsonb");
        modelBuilder.Entity<Vendor>().Property(v => v.ZipCodes).HasColumnType("jsonb");

        // Unique indexes
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Quote>().HasIndex(q => q.PublicToken).IsUnique();
        modelBuilder.Entity<Proposal>().HasIndex(p => p.PublicToken).IsUnique();
        modelBuilder.Entity<Proposal>().HasIndex(p => p.ServiceRequestId).IsUnique();
        modelBuilder.Entity<WorkOrder>().HasIndex(wo => wo.ServiceRequestId).IsUnique();
        modelBuilder.Entity<VendorInvite>().HasIndex(vi => vi.PublicToken).IsUnique();
        modelBuilder.Entity<VendorInvite>().HasIndex(vi => new { vi.ServiceRequestId, vi.VendorId }).IsUnique();
        modelBuilder.Entity<Invoice>().HasIndex(i => i.WorkOrderId).IsUnique();
        modelBuilder.Entity<Invoice>().HasIndex(i => i.PublicToken).IsUnique();

        // Performance indexes
        modelBuilder.Entity<ServiceRequest>().HasIndex(sr => sr.Status);
        modelBuilder.Entity<ServiceRequest>().HasIndex(sr => sr.ClientId);
        modelBuilder.Entity<Notification>().HasIndex(n => new { n.UserId, n.Read });
        modelBuilder.Entity<VendorNote>().HasIndex(vn => vn.VendorId);
        modelBuilder.Entity<VendorNote>().HasIndex(vn => vn.CreatedAt);
        modelBuilder.Entity<VendorPayment>().HasIndex(vp => vp.VendorId);
        modelBuilder.Entity<Invoice>().HasIndex(i => i.ClientId);
        modelBuilder.Entity<Invoice>().HasIndex(i => i.Status);

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

        // Client.UserId is now optional (clients are contact records, not necessarily system users)
        modelBuilder.Entity<Client>()
            .HasOne(c => c.User).WithOne(u => u.Client).HasForeignKey<Client>(c => c.UserId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        // Vendor.UserId is now optional (email-first vendors don't need user accounts)
        modelBuilder.Entity<Vendor>()
            .HasOne(v => v.User).WithOne(u => u.Vendor).HasForeignKey<Vendor>(v => v.UserId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        // Decimal precision
        modelBuilder.Entity<Quote>().Property(q => q.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Quote>().Property(q => q.NotToExceedPrice).HasPrecision(18, 2);
        modelBuilder.Entity<QuoteLineItem>().Property(li => li.Quantity).HasPrecision(18, 4);
        modelBuilder.Entity<QuoteLineItem>().Property(li => li.UnitPrice).HasPrecision(18, 2);

        // QuoteLineItem relationships
        modelBuilder.Entity<QuoteLineItem>()
            .HasOne(li => li.Quote).WithMany(q => q.LineItems).HasForeignKey(li => li.QuoteId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Proposal>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Proposal>().Property(p => p.VendorCost).HasPrecision(18, 2);
        modelBuilder.Entity<Proposal>().Property(p => p.MarginPercentage).HasPrecision(5, 2);
        modelBuilder.Entity<Proposal>().Property(p => p.NotToExceedPrice).HasPrecision(18, 2);

        // ProposalAttachment relationships
        modelBuilder.Entity<ProposalAttachment>()
            .HasOne(pa => pa.Proposal).WithMany(p => p.Attachments).HasForeignKey(pa => pa.ProposalId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProposalAttachment>()
            .HasOne(pa => pa.Attachment).WithMany().HasForeignKey(pa => pa.AttachmentId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProposalAttachment>()
            .HasIndex(pa => new { pa.ProposalId, pa.AttachmentId }).IsUnique();

        // ProposalVersion relationships
        modelBuilder.Entity<ProposalVersion>()
            .HasOne(pv => pv.Proposal).WithMany(p => p.Versions).HasForeignKey(pv => pv.ProposalId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProposalVersion>().Property(pv => pv.Price).HasPrecision(18, 2);
        modelBuilder.Entity<ProposalVersion>().Property(pv => pv.VendorCost).HasPrecision(18, 2);
        modelBuilder.Entity<ProposalVersion>().Property(pv => pv.MarginPercentage).HasPrecision(5, 2);
        modelBuilder.Entity<ProposalVersion>().Property(pv => pv.NotToExceedPrice).HasPrecision(18, 2);

        // ProposalLineItem relationships
        modelBuilder.Entity<ProposalLineItem>()
            .HasOne(li => li.Proposal).WithMany(p => p.LineItems).HasForeignKey(li => li.ProposalId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProposalLineItem>().Property(li => li.Quantity).HasPrecision(18, 4);
        modelBuilder.Entity<ProposalLineItem>().Property(li => li.UnitPrice).HasPrecision(18, 2);

        modelBuilder.Entity<Vendor>().Property(v => v.Rating).HasPrecision(3, 2);
        modelBuilder.Entity<VendorPayment>().Property(vp => vp.Amount).HasPrecision(18, 2);

        // Invoice
        modelBuilder.Entity<Invoice>().Property(i => i.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>().HasOne(i => i.WorkOrder).WithMany().HasForeignKey(i => i.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Invoice>().HasOne(i => i.Client).WithMany().HasForeignKey(i => i.ClientId).OnDelete(DeleteBehavior.Restrict);

        // WorkOrderDocument
        modelBuilder.Entity<WorkOrderDocument>()
            .HasOne(d => d.ServiceRequest).WithMany().HasForeignKey(d => d.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WorkOrderDocument>()
            .HasOne(d => d.VendorInvite).WithMany().HasForeignKey(d => d.VendorInviteId).OnDelete(DeleteBehavior.Cascade);

        // InboundEmail
        modelBuilder.Entity<InboundEmail>().HasIndex(ie => ie.MessageId).IsUnique();
        modelBuilder.Entity<InboundEmail>().HasIndex(ie => ie.ServiceRequestId);
        modelBuilder.Entity<InboundEmail>().HasIndex(ie => ie.ReceivedAt);
        modelBuilder.Entity<InboundEmail>().HasIndex(ie => ie.ConversationId);
        modelBuilder.Entity<InboundEmail>().HasIndex(ie => ie.InReplyToMessageId);
        modelBuilder.Entity<InboundEmail>()
            .HasOne(ie => ie.ServiceRequest).WithMany().HasForeignKey(ie => ie.ServiceRequestId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<InboundEmailAttachment>()
            .HasOne(a => a.InboundEmail).WithMany(ie => ie.Attachments).HasForeignKey(a => a.InboundEmailId).OnDelete(DeleteBehavior.Cascade);

        // OutboundEmail
        modelBuilder.Entity<OutboundEmail>().Property(oe => oe.EmailType).HasConversion<string>();
        modelBuilder.Entity<OutboundEmail>().HasIndex(oe => oe.ServiceRequestId);
        modelBuilder.Entity<OutboundEmail>().HasIndex(oe => oe.SentAt);
        modelBuilder.Entity<OutboundEmail>().HasIndex(oe => oe.ConversationId);
        modelBuilder.Entity<OutboundEmail>()
            .HasOne(oe => oe.ServiceRequest).WithMany().HasForeignKey(oe => oe.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OutboundEmail>()
            .HasOne(oe => oe.SentBy).WithMany().HasForeignKey(oe => oe.SentById).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OutboundEmailAttachment>()
            .HasOne(a => a.OutboundEmail).WithMany(oe => oe.Attachments).HasForeignKey(a => a.OutboundEmailId).OnDelete(DeleteBehavior.Cascade);

        // ActivityLog
        modelBuilder.Entity<ActivityLog>().Property(a => a.Action).IsRequired();
        modelBuilder.Entity<ActivityLog>().Property(a => a.ActorName).IsRequired();
        modelBuilder.Entity<ActivityLog>().HasIndex(a => a.ServiceRequestId);
        modelBuilder.Entity<ActivityLog>().HasIndex(a => a.WorkOrderId);
        modelBuilder.Entity<ActivityLog>()
            .HasOne(a => a.ServiceRequest).WithMany().HasForeignKey(a => a.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ActivityLog>()
            .HasOne(a => a.WorkOrder).WithMany().HasForeignKey(a => a.WorkOrderId).OnDelete(DeleteBehavior.SetNull);
    }
}
