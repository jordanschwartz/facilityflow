using System.Globalization;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using FacilityFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FacilityFlow.Infrastructure.Services;

public class ProposalPdfService : IProposalPdfService
{
    private readonly IProposalRepository _proposals;

    // Brand colors
    private const string PrimaryHex = "#E8511A";
    private const string DarkTextHex = "#1F2937";
    private const string GrayTextHex = "#6B7280";
    private const string LightBgHex = "#FFF3EE";

    private static byte[]? _logoBytes;

    public ProposalPdfService(IProposalRepository proposals)
    {
        _proposals = proposals;
        LoadLogo();
    }

    private static void LoadLogo()
    {
        if (_logoBytes != null) return;
        var logoPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "images", "logo-full.png");
        if (File.Exists(logoPath))
            _logoBytes = File.ReadAllBytes(logoPath);
    }

    public async Task<byte[]> GenerateAsync(Guid proposalId)
    {
        var proposal = await _proposals.Query()
            .Include(p => p.ServiceRequest).ThenInclude(sr => sr.Client)
            .Include(p => p.Quote).ThenInclude(q => q.LineItems)
            .Include(p => p.LineItems)
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new NotFoundException("Proposal not found.");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginHorizontal(50);
                page.MarginVertical(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(DarkTextHex));

                page.Header().Element(c => ComposeHeader(c, proposal));
                page.Content().Element(c => ComposeContent(c, proposal));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, Proposal proposal)
    {
        var proposalNumber = !string.IsNullOrWhiteSpace(proposal.ProposalNumber)
            ? proposal.ProposalNumber
            : $"PROP-{proposal.Id.ToString("N")[..8].ToUpper()}";
        var date = proposal.SentAt ?? DateTime.UtcNow;

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
                });

                row.RelativeItem().AlignRight().Column(right =>
                {
                    right.Item().Text("PROPOSAL")
                        .Bold().FontSize(28).FontColor(PrimaryHex);
                    right.Item().PaddingTop(4).Text(proposalNumber)
                        .FontSize(11).FontColor(GrayTextHex);
                    right.Item().Text(date.ToString("MMMM d, yyyy"))
                        .FontSize(10).FontColor(GrayTextHex);
                });
            });

            col.Item().PaddingVertical(12)
                .LineHorizontal(2).LineColor(PrimaryHex);
        });
    }

    private static void ComposeContent(IContainer container, Proposal proposal)
    {
        var sr = proposal.ServiceRequest;
        var client = sr.Client;

        // Use proposal's own line items if present, otherwise fall back to quote line items
        var hasProposalLineItems = proposal.LineItems.Count > 0;
        var lineItems = hasProposalLineItems
            ? proposal.LineItems.OrderBy(li => li.SortOrder)
                .Select(li => new { li.Description, li.Quantity, li.UnitPrice })
                .ToList()
            : proposal.Quote.LineItems
                .Select(li => new { li.Description, li.Quantity, li.UnitPrice })
                .ToList();

        container.PaddingTop(4).Column(col =>
        {
            col.Spacing(16);

            // Client & Service Details side by side
            col.Item().Row(row =>
            {
                row.RelativeItem().Component(new SectionBox("Prepared For", c =>
                {
                    c.Item().Text(client.CompanyName).Bold().FontSize(12);
                    c.Item().PaddingTop(2).Text(client.ContactName).FontSize(10);
                    if (!string.IsNullOrWhiteSpace(client.Address))
                        c.Item().Text(client.Address).FontSize(9).FontColor(GrayTextHex);
                    if (!string.IsNullOrWhiteSpace(client.Email))
                        c.Item().Text(client.Email).FontSize(9).FontColor(GrayTextHex);
                    if (!string.IsNullOrWhiteSpace(client.Phone))
                        c.Item().Text(client.Phone).FontSize(9).FontColor(GrayTextHex);
                }));

                row.ConstantItem(20);

                row.RelativeItem().Component(new SectionBox("Service Details", c =>
                {
                    c.Item().Text(sr.Title).Bold().FontSize(12);
                    c.Item().PaddingTop(2).Text($"Location: {sr.Location}").FontSize(10);
                    c.Item().Text($"Category: {sr.Category}").FontSize(10);
                }));
            });

            // Scope of Work
            if (!string.IsNullOrWhiteSpace(proposal.ScopeOfWork))
            {
                col.Item().Component(new SectionBox("Scope of Work", c =>
                {
                    c.Item().Text(proposal.ScopeOfWork).FontSize(10).LineHeight(1.5f);
                }));
            }

            // Summary
            if (!string.IsNullOrWhiteSpace(proposal.Summary))
            {
                col.Item().Component(new SectionBox("Summary", c =>
                {
                    c.Item().Text(proposal.Summary).FontSize(10).LineHeight(1.5f);
                }));
            }

            // Pricing
            col.Item().Column(pricing =>
            {
                pricing.Item().PaddingBottom(6).Text("Pricing")
                    .Bold().FontSize(14).FontColor(PrimaryHex);

                if (lineItems.Count > 0)
                {
                    pricing.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);   // #
                            columns.RelativeColumn(3);    // Description
                            columns.ConstantColumn(50);   // Qty
                            columns.ConstantColumn(90);   // Unit Price
                            columns.ConstantColumn(90);   // Total
                        });

                        // Header row
                        table.Header(header =>
                        {
                            header.Cell().Background(PrimaryHex).Padding(6)
                                .Text("#").Bold().FontSize(9).FontColor(Colors.White);
                            header.Cell().Background(PrimaryHex).Padding(6)
                                .Text("Description").Bold().FontSize(9).FontColor(Colors.White);
                            header.Cell().Background(PrimaryHex).Padding(6).AlignRight()
                                .Text("Qty").Bold().FontSize(9).FontColor(Colors.White);
                            header.Cell().Background(PrimaryHex).Padding(6).AlignRight()
                                .Text("Unit Price").Bold().FontSize(9).FontColor(Colors.White);
                            header.Cell().Background(PrimaryHex).Padding(6).AlignRight()
                                .Text("Total").Bold().FontSize(9).FontColor(Colors.White);
                        });

                        for (var i = 0; i < lineItems.Count; i++)
                        {
                            var item = lineItems[i];
                            var lineTotal = item.Quantity * item.UnitPrice;
                            var bg = i % 2 == 0 ? Colors.White : Color.FromHex(LightBgHex);

                            table.Cell().Background(bg).Padding(6)
                                .Text((i + 1).ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(6)
                                .Text(item.Description).FontSize(9);
                            table.Cell().Background(bg).Padding(6).AlignRight()
                                .Text(item.Quantity.ToString("G")).FontSize(9);
                            table.Cell().Background(bg).Padding(6).AlignRight()
                                .Text(item.UnitPrice.ToString("C", CultureInfo.GetCultureInfo("en-US"))).FontSize(9);
                            table.Cell().Background(bg).Padding(6).AlignRight()
                                .Text(lineTotal.ToString("C", CultureInfo.GetCultureInfo("en-US"))).FontSize(9);
                        }
                    });

                    // Subtotal
                    var subtotal = lineItems.Sum(li => li.Quantity * li.UnitPrice);
                    pricing.Item().PaddingTop(4).AlignRight().Row(row =>
                    {
                        row.ConstantItem(90).AlignRight().Text("Subtotal:").FontSize(10).FontColor(GrayTextHex);
                        row.ConstantItem(100).AlignRight().Text(subtotal.ToString("C", CultureInfo.GetCultureInfo("en-US"))).FontSize(10);
                    });
                }

                // NTE
                if (proposal.UseNtePricing && proposal.NotToExceedPrice.HasValue)
                {
                    pricing.Item().PaddingTop(2).AlignRight().Row(row =>
                    {
                        row.ConstantItem(120).AlignRight().Text("Not to Exceed:")
                            .FontSize(10).FontColor(GrayTextHex);
                        row.ConstantItem(100).AlignRight()
                            .Text(proposal.NotToExceedPrice.Value.ToString("C", CultureInfo.GetCultureInfo("en-US"))).FontSize(10);
                    });
                }

                // Total
                pricing.Item().PaddingTop(8).AlignRight()
                    .Background(LightBgHex).Padding(10).Row(row =>
                    {
                        row.ConstantItem(80).AlignRight().Text("Total:")
                            .Bold().FontSize(14).FontColor(PrimaryHex);
                        row.ConstantItem(120).AlignRight()
                            .Text(proposal.Price.ToString("C", CultureInfo.GetCultureInfo("en-US")))
                            .Bold().FontSize(14).FontColor(PrimaryHex);
                    });
            });

            // Timeline
            if (proposal.ProposedStartDate.HasValue || !string.IsNullOrWhiteSpace(proposal.EstimatedDuration))
            {
                col.Item().Component(new SectionBox("Timeline", c =>
                {
                    if (proposal.ProposedStartDate.HasValue)
                        c.Item().Text($"Proposed Start Date: {proposal.ProposedStartDate.Value:MMMM d, yyyy}")
                            .FontSize(10);
                    if (!string.IsNullOrWhiteSpace(proposal.EstimatedDuration))
                        c.Item().Text($"Estimated Duration: {proposal.EstimatedDuration}")
                            .FontSize(10);
                }));
            }

            // Terms & Conditions
            if (!string.IsNullOrWhiteSpace(proposal.TermsAndConditions))
            {
                col.Item().Component(new SectionBox("Terms & Conditions", c =>
                {
                    c.Item().Text(proposal.TermsAndConditions).FontSize(9).LineHeight(1.5f).FontColor(GrayTextHex);
                }));
            }

            // Validity & Signature
            col.Item().PaddingTop(12).Column(sig =>
            {
                sig.Item().Text("This proposal is valid for 30 days from the date above.")
                    .Italic().FontSize(9).FontColor(GrayTextHex);

                sig.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().LineHorizontal(1).LineColor(GrayTextHex);
                        left.Item().PaddingTop(4).Text("Client Signature")
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

    /// <summary>
    /// Reusable component for a labeled section with a header and content area.
    /// </summary>
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
