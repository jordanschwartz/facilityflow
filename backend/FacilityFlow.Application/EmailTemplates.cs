using System.Net;

namespace FacilityFlow.Application;

public static class EmailTemplates
{
    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string Layout(string content)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
</head>
<body style=""margin:0;padding:0;background-color:#f4f4f5;font-family:Arial,Helvetica,sans-serif;"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f4f5;padding:24px 0;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff;border-radius:8px;overflow:hidden;max-width:600px;width:100%;"">
                    <!-- Header -->
                    <tr>
                        <td style=""background-color:#1a1a2e;padding:24px 32px;"">
                            <span style=""color:#ffffff;font-size:20px;font-weight:bold;"">On-Call Facilities &amp; Maintenance</span>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style=""padding:32px;"">
                            {content}
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color:#f9fafb;padding:20px 32px;border-top:1px solid #e5e7eb;"">
                            <p style=""margin:0 0 8px;font-size:12px;color:#9ca3af;text-align:center;"">
                                On-Call Facilities &amp; Maintenance LLC | 123 Main Street, Suite 100, Miami, FL 33101
                            </p>
                            <p style=""margin:0;font-size:11px;color:#b0b0b0;text-align:center;"">
                                This is an automated message. Please do not reply directly to this email.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private static string SummaryRow(string label, string value)
    {
        return $@"<tr>
    <td style=""padding:4px 0;font-size:14px;color:#6b7280;width:120px;"">{Encode(label)}</td>
    <td style=""padding:4px 0;font-size:14px;color:#111827;font-weight:bold;"">{Encode(value)}</td>
</tr>";
    }

    private static string SummaryBox(params (string Label, string Value)[] rows)
    {
        var rowsHtml = string.Join("\n", rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Value))
            .Select(r => SummaryRow(r.Label, r.Value)));

        return $@"<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;margin-bottom:24px;"">
    <tr>
        <td style=""padding:20px;"">
            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                {rowsHtml}
            </table>
        </td>
    </tr>
</table>";
    }

    private static string PrimaryButton(string text, string url)
    {
        return $@"<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
    <tr>
        <td align=""center"" style=""padding-bottom:12px;"">
            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"">
                <tr>
                    <td style=""background-color:#E8511A;border-radius:6px;"">
                        <a href=""{Encode(url)}"" target=""_blank"" style=""display:inline-block;padding:12px 32px;color:#ffffff;text-decoration:none;font-size:16px;font-weight:bold;"">{Encode(text)}</a>
                    </td>
                </tr>
            </table>
        </td>
    </tr>
</table>";
    }

    private static string SecondaryButton(string text, string url)
    {
        return $@"<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
    <tr>
        <td align=""center"" style=""padding-bottom:12px;"">
            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"">
                <tr>
                    <td style=""border:2px solid #E8511A;border-radius:6px;"">
                        <a href=""{Encode(url)}"" target=""_blank"" style=""display:inline-block;padding:10px 30px;color:#E8511A;text-decoration:none;font-size:14px;font-weight:bold;"">{Encode(text)}</a>
                    </td>
                </tr>
            </table>
        </td>
    </tr>
</table>";
    }

    private static string Greeting(string name) =>
        $@"<p style=""margin:0 0 16px;font-size:16px;color:#333333;"">Hi {Encode(name)},</p>";

    private static string Paragraph(string text) =>
        $@"<p style=""margin:0 0 24px;font-size:16px;color:#333333;"">{Encode(text)}</p>";

    private static string Note(string text) =>
        $@"<p style=""margin:0 0 24px;font-size:14px;color:#6b7280;text-align:center;"">{Encode(text)}</p>";

    // ──────────────────────────────────────────────
    // Template methods — each returns (Subject, HtmlBody)
    // ──────────────────────────────────────────────

    public static (string Subject, string HtmlBody) WorkOrderDispatch(
        string vendorName, string workOrderNumber, string jobTitle,
        string location, string priority, string? description,
        string viewWorkOrderUrl, string submitQuoteUrl)
    {
        var subject = $"[{workOrderNumber}] New Work Order \u2013 {jobTitle}";

        var content = Greeting(vendorName)
            + Paragraph("You have received a new work order from On-Call Facilities & Maintenance LLC.")
            + SummaryBox(
                ("Work Order #", workOrderNumber),
                ("Job Title", jobTitle),
                ("Location", location),
                ("Priority", priority),
                ("Description", description ?? ""))
            + Note("The full work order PDF is attached to this email.")
            + PrimaryButton("Submit Quote", submitQuoteUrl)
            + SecondaryButton("View Work Order", viewWorkOrderUrl);

        return (subject, Layout(content));
    }

    public static (string Subject, string HtmlBody) ProposalSent(
        string clientName, string workOrderNumber, string jobTitle,
        string proposalTotal, string viewProposalUrl)
    {
        var subject = $"[{workOrderNumber}] Proposal Ready for Review \u2013 {jobTitle}";

        var content = Greeting(clientName)
            + Paragraph($"A proposal has been submitted for your review on work order {workOrderNumber}.")
            + SummaryBox(
                ("Work Order #", workOrderNumber),
                ("Job Title", jobTitle),
                ("Proposal Total", proposalTotal))
            + PrimaryButton("View Proposal", viewProposalUrl);

        return (subject, Layout(content));
    }

    public static (string Subject, string HtmlBody) InvoiceSent(
        string clientName, string invoiceNumber, string workOrderNumber,
        string amountDue, string viewInvoiceUrl, string payInvoiceUrl)
    {
        var subject = $"[{workOrderNumber}] Invoice #{invoiceNumber} \u2013 Payment Requested";

        var content = Greeting(clientName)
            + Paragraph($"An invoice has been issued for work order {workOrderNumber}.")
            + SummaryBox(
                ("Invoice #", invoiceNumber),
                ("Work Order #", workOrderNumber),
                ("Amount Due", amountDue))
            + PrimaryButton("Pay Invoice", payInvoiceUrl)
            + SecondaryButton("View Invoice", viewInvoiceUrl);

        return (subject, Layout(content));
    }

    public static (string Subject, string HtmlBody) QuoteRequest(
        string vendorName, string workOrderNumber, string jobTitle,
        string location, string description, string submitQuoteUrl)
    {
        var subject = $"[{workOrderNumber}] Quote Requested \u2013 {jobTitle}";

        var content = Greeting(vendorName)
            + Paragraph("We'd like to request a quote for the following job.")
            + SummaryBox(
                ("Work Order #", workOrderNumber),
                ("Job Title", jobTitle),
                ("Location", location),
                ("Description", description))
            + PrimaryButton("Submit Quote", submitQuoteUrl);

        return (subject, Layout(content));
    }

    public static (string Subject, string HtmlBody) Reminder(
        string recipientName, string workOrderNumber, string subject,
        string message, string ctaText, string ctaUrl)
    {
        var emailSubject = $"[{workOrderNumber}] Reminder \u2013 {subject}";

        var content = Greeting(recipientName)
            + Paragraph(message)
            + SummaryBox(("Work Order #", workOrderNumber))
            + PrimaryButton(ctaText, ctaUrl);

        return (emailSubject, Layout(content));
    }

    public static (string Subject, string HtmlBody) StatusUpdate(
        string recipientName, string workOrderNumber, string jobTitle,
        string statusMessage, string? ctaText, string? ctaUrl)
    {
        var subject = $"[{workOrderNumber}] Status Update \u2013 {jobTitle}";

        var content = Greeting(recipientName)
            + Paragraph(statusMessage)
            + SummaryBox(
                ("Work Order #", workOrderNumber),
                ("Job Title", jobTitle));

        if (!string.IsNullOrWhiteSpace(ctaText) && !string.IsNullOrWhiteSpace(ctaUrl))
            content += PrimaryButton(ctaText, ctaUrl);

        return (subject, Layout(content));
    }
}
