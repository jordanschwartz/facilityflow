using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FacilityFlow.Infrastructure.Services;

public class WorkOrderPdfService : IWorkOrderPdfService
{
    private readonly IServiceRequestRepository _serviceRequests;
    private readonly IRepository<VendorInvite> _vendorInvites;

    // Brand colors
    private const string PrimaryHex = "#E8511A";
    private const string DarkTextHex = "#1F2937";
    private const string GrayTextHex = "#6B7280";
    private const string LightBgHex = "#FFF3EE";

    private static byte[]? _logoBytes;

    public WorkOrderPdfService(IServiceRequestRepository serviceRequests, IRepository<VendorInvite> vendorInvites)
    {
        _serviceRequests = serviceRequests;
        _vendorInvites = vendorInvites;
        LoadLogo();
    }

    private static void LoadLogo()
    {
        if (_logoBytes != null) return;
        var logoPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "images", "logo-full.png");
        if (File.Exists(logoPath))
            _logoBytes = File.ReadAllBytes(logoPath);
    }

    public async Task<byte[]> GeneratePdfAsync(Guid serviceRequestId, Guid vendorInviteId)
    {
        var sr = await _serviceRequests.Query()
            .Include(s => s.Client)
            .Include(s => s.CreatedBy)
            .FirstOrDefaultAsync(s => s.Id == serviceRequestId)
            ?? throw new NotFoundException("Service request not found.");

        var invite = await _vendorInvites.Query()
            .Include(vi => vi.Vendor)
            .FirstOrDefaultAsync(vi => vi.Id == vendorInviteId)
            ?? throw new NotFoundException("Vendor invite not found.");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginHorizontal(50);
                page.MarginVertical(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(DarkTextHex));

                page.Header().Element(c => ComposeHeader(c, sr));
                page.Content().Element(c => ComposeContent(c, sr, invite));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, ServiceRequest sr)
    {
        var workOrderNumber = !string.IsNullOrWhiteSpace(sr.WorkOrderNumber)
            ? sr.WorkOrderNumber
            : $"WO-{sr.Id.ToString("N")[..8].ToUpper()}";
        var date = DateTime.UtcNow;

        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    if (_logoBytes != null)
                    {
                        left.Item().Width(200).Image(_logoBytes);
                    }
                    else
                    {
                        left.Item().Text("On-Call")
                            .Bold().FontSize(22).FontColor(PrimaryHex);
                        left.Item().Text("Facilities & Maintenance")
                            .FontSize(10).FontColor(GrayTextHex);
                    }

                    left.Item().PaddingTop(4).Text("123 Main Street, Suite 100, Miami, FL 33101")
                        .FontSize(8).FontColor(GrayTextHex);
                    left.Item().Text("(305) 555-0100  |  dispatch@oncallfm.com")
                        .FontSize(8).FontColor(GrayTextHex);
                });

                row.RelativeItem().AlignRight().Column(right =>
                {
                    right.Item().Text("WORK ORDER")
                        .Bold().FontSize(28).FontColor(PrimaryHex);
                    right.Item().PaddingTop(4).Text(workOrderNumber)
                        .FontSize(11).FontColor(GrayTextHex);
                    right.Item().Text(date.ToString("MMMM d, yyyy"))
                        .FontSize(10).FontColor(GrayTextHex);
                });
            });

            col.Item().PaddingVertical(12)
                .LineHorizontal(2).LineColor(PrimaryHex);
        });
    }

    private static void ComposeContent(IContainer container, ServiceRequest sr, VendorInvite invite)
    {
        var client = sr.Client;
        var contact = sr.CreatedBy;

        container.PaddingTop(4).Column(col =>
        {
            col.Spacing(16);

            // Work Order Info
            col.Item().Row(row =>
            {
                row.RelativeItem().Component(new SectionBox("Work Order Info", c =>
                {
                    var woNum = !string.IsNullOrWhiteSpace(sr.WorkOrderNumber)
                        ? sr.WorkOrderNumber
                        : $"WO-{sr.Id.ToString("N")[..8].ToUpper()}";
                    c.Item().Text($"Work Order #: {woNum}").Bold().FontSize(11);
                    c.Item().PaddingTop(2).Text($"Date: {DateTime.UtcNow:MMMM d, yyyy}").FontSize(10);
                    c.Item().Text($"Priority: {sr.Priority}").FontSize(10);
                    c.Item().Text($"Category: {sr.Category}").FontSize(10);
                }));

                row.ConstantItem(20);

                row.RelativeItem().Component(new SectionBox("Vendor", c =>
                {
                    c.Item().Text(invite.Vendor.CompanyName).Bold().FontSize(11);
                    if (!string.IsNullOrWhiteSpace(invite.Vendor.PrimaryContactName))
                        c.Item().PaddingTop(2).Text(invite.Vendor.PrimaryContactName).FontSize(10);
                    if (!string.IsNullOrWhiteSpace(invite.Vendor.Email))
                        c.Item().Text(invite.Vendor.Email).FontSize(9).FontColor(GrayTextHex);
                    if (!string.IsNullOrWhiteSpace(invite.Vendor.Phone))
                        c.Item().Text(invite.Vendor.Phone).FontSize(9).FontColor(GrayTextHex);
                }));
            });

            // Job Information
            col.Item().Component(new SectionBox("Job Information", c =>
            {
                c.Item().Text($"Client: {client.CompanyName}").Bold().FontSize(11);
                c.Item().PaddingTop(2).Text($"Service Location: {sr.Location}").FontSize(10);
                c.Item().PaddingTop(4).Text("Description:").Bold().FontSize(10);
                c.Item().Text(sr.Description).FontSize(10).LineHeight(1.5f);
                if (!string.IsNullOrWhiteSpace(sr.Title))
                {
                    c.Item().PaddingTop(4).Text("Scope of Work:").Bold().FontSize(10);
                    c.Item().Text(sr.Title).FontSize(10).LineHeight(1.5f);
                }
            }));

            // Scheduling
            col.Item().Component(new SectionBox("Scheduling", c =>
            {
                c.Item().Text($"Requested Date: {sr.CreatedAt:MMMM d, yyyy}").FontSize(10);
                if (sr.ScheduledDate.HasValue)
                    c.Item().Text($"Scheduled Date: {sr.ScheduledDate.Value:MMMM d, yyyy}").FontSize(10);
                else
                    c.Item().Text("Scheduled Date: TBD").FontSize(10).FontColor(GrayTextHex);
            }));

            // Contact Section
            col.Item().Component(new SectionBox("Point of Contact", c =>
            {
                c.Item().Text($"{contact.FirstName} {contact.LastName}").Bold().FontSize(11);
                c.Item().PaddingTop(2).Text($"Email: {contact.Email}").FontSize(10);
            }));

            // Terms & Conditions
            col.Item().Column(terms =>
            {
                terms.Item().PaddingBottom(6).Text("Terms & Conditions")
                    .Bold().FontSize(14).FontColor(PrimaryHex);

                terms.Item().Background(LightBgHex).Padding(12).Column(tc =>
                {
                    var termsText = new[]
                    {
                        ("1. NOT-TO-EXCEED (NTE) ENFORCEMENT", "All work must be performed within the approved Not-To-Exceed (NTE) amount. Any costs exceeding the NTE require prior written approval from On-Call Facilities & Maintenance LLC. Unauthorized overages will not be reimbursed."),
                        ("2. PAYMENT TERMS", "Payment will be issued within forty-five (45) days of receipt of a properly submitted invoice, provided all required documentation has been received and the work has been verified as complete and satisfactory."),
                        ("3. APPROVAL REQUIREMENTS", "No additional work, change orders, or scope modifications shall be performed without prior written authorization from On-Call Facilities & Maintenance LLC. Unauthorized work will not be compensated."),
                        ("4. COMMUNICATION REQUIREMENTS", "The vendor must provide status updates as requested and promptly communicate any delays, issues, or changes in scheduling. Failure to maintain communication may result in removal from active jobs."),
                        ("5. DOCUMENTATION REQUIREMENTS", "The vendor must provide before-and-after photos of all completed work, along with any relevant documentation (permits, inspection reports, material receipts, etc.) prior to invoicing."),
                        ("6. INVOICE REQUIREMENTS", "Invoices must include: Work Order number, detailed description of work performed, itemized costs, date(s) of service, and supporting documentation. Incomplete invoices will be returned and may delay payment."),
                        ("7. NON-PAYMENT CLAUSES", "On-Call reserves the right to withhold payment if: (a) work is incomplete or deficient, (b) required documentation is not provided, (c) work was performed outside the approved scope, or (d) the vendor fails to comply with any terms of this work order."),
                        ("8. INSURANCE", "Vendor must maintain general liability insurance of at least $1,000,000 per occurrence and workers' compensation coverage as required by law. Proof of insurance must be provided upon request."),
                        ("9. SUBCONTRACTING", "The vendor shall not subcontract any portion of the work without prior written consent from On-Call Facilities & Maintenance LLC."),
                        ("10. INDEMNIFICATION", "Vendor agrees to indemnify and hold harmless On-Call Facilities & Maintenance LLC, its clients, and their respective agents from any claims, damages, or liabilities arising from the vendor's performance of work."),
                        ("11. LIEN WAIVER", "Upon receipt of payment, vendor agrees to provide a full and unconditional lien waiver for all work performed and materials supplied."),
                        ("12. GOVERNING LAW", "This work order shall be governed by the laws of the State of Florida."),
                        ("13. ACCEPTANCE", "By performing work under this work order, the vendor acknowledges and agrees to all terms and conditions stated herein."),
                        ("14. WARRANTY OF WORK", "Vendor warrants all work performed for a minimum period of ninety (90) days from completion. Defective work must be corrected at vendor's expense."),
                        ("15. SAFETY COMPLIANCE", "Vendor must comply with all applicable OSHA regulations and safety standards. Vendor is responsible for ensuring all personnel follow proper safety protocols."),
                        ("16. RIGHT TO WITHHOLD PAYMENT", "On-Call reserves the right to withhold payment for disputed work until resolution is reached."),
                        ("17. TERMINATION", "On-Call reserves the right to terminate this work order at any time for cause or convenience with written notice.")
                    };

                    foreach (var (title, body) in termsText)
                    {
                        tc.Item().PaddingBottom(6).Column(item =>
                        {
                            item.Item().Text(title).Bold().FontSize(8).FontColor(DarkTextHex);
                            item.Item().Text(body).FontSize(8).FontColor(GrayTextHex).LineHeight(1.4f);
                        });
                    }
                });
            });

            // Client Sign-Off Section — starts on its own page
            col.Item().PageBreak();
            col.Item().Column(signoff =>
            {
                signoff.Item().PaddingBottom(6).Text("Client Sign-Off")
                    .Bold().FontSize(14).FontColor(PrimaryHex);

                signoff.Item().Background(LightBgHex).Padding(12).Column(c =>
                {
                    // Checkboxes
                    var checkboxLabels = new[] { "Follow-up Service Required", "Work Not Completed to Satisfaction", "Work Completed to Satisfaction" };
                    foreach (var label in checkboxLabels)
                    {
                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.ConstantItem(14).Height(14).Border(1).BorderColor(DarkTextHex);
                            r.ConstantItem(8);
                            r.RelativeItem().AlignMiddle().Text(label).FontSize(10);
                        });
                    }

                    // Comments
                    c.Item().PaddingTop(16).Text("Comments:").Bold().FontSize(10);
                    c.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(GrayTextHex);
                    c.Item().PaddingTop(16).LineHorizontal(0.5f).LineColor(GrayTextHex);
                    c.Item().PaddingTop(16).LineHorizontal(0.5f).LineColor(GrayTextHex);

                    // Signature
                    c.Item().PaddingTop(24).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().LineHorizontal(1).LineColor(GrayTextHex);
                            left.Item().PaddingTop(4).Text("Signature")
                                .FontSize(9).FontColor(GrayTextHex);
                        });

                        row.ConstantItem(40);

                        row.RelativeItem().Column(right =>
                        {
                            right.Item().LineHorizontal(1).LineColor(GrayTextHex);
                            right.Item().PaddingTop(4).Text("Date")
                                .FontSize(9).FontColor(GrayTextHex);
                        });
                    });
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(8).FontColor(GrayTextHex));
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
        });
    }

    private class SectionBox : IComponent
    {
        private readonly string _title;
        private readonly Action<ColumnDescriptor> _content;

        public SectionBox(string title, Action<ColumnDescriptor> content)
        {
            _title = title;
            _content = content;
        }

        public void Compose(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().PaddingBottom(6).Text(_title)
                    .Bold().FontSize(14).FontColor(PrimaryHex);

                col.Item().Background(LightBgHex).Padding(12).Column(_content);
            });
        }
    }
}
