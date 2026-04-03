namespace FacilityFlow.Application;

public static class EmailTemplates
{
    public static string WorkOrderEmail(
        string vendorName, string workOrderNumber, string title,
        string location, string priority, string quoteUrl)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>New Work Order</title>
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
                            <p style=""margin:0 0 16px;font-size:16px;color:#333333;"">
                                Hi {System.Net.WebUtility.HtmlEncode(vendorName)},
                            </p>
                            <p style=""margin:0 0 24px;font-size:16px;color:#333333;"">
                                You have received a new work order from On-Call Facilities &amp; Maintenance LLC.
                            </p>
                            <!-- Summary -->
                            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;margin-bottom:24px;"">
                                <tr>
                                    <td style=""padding:20px;"">
                                        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                            <tr>
                                                <td style=""padding:4px 0;font-size:14px;color:#6b7280;width:120px;"">Work Order #</td>
                                                <td style=""padding:4px 0;font-size:14px;color:#111827;font-weight:bold;"">{System.Net.WebUtility.HtmlEncode(workOrderNumber)}</td>
                                            </tr>
                                            <tr>
                                                <td style=""padding:4px 0;font-size:14px;color:#6b7280;"">Job Title</td>
                                                <td style=""padding:4px 0;font-size:14px;color:#111827;font-weight:bold;"">{System.Net.WebUtility.HtmlEncode(title)}</td>
                                            </tr>
                                            <tr>
                                                <td style=""padding:4px 0;font-size:14px;color:#6b7280;"">Location</td>
                                                <td style=""padding:4px 0;font-size:14px;color:#111827;"">{System.Net.WebUtility.HtmlEncode(location)}</td>
                                            </tr>
                                            <tr>
                                                <td style=""padding:4px 0;font-size:14px;color:#6b7280;"">Priority</td>
                                                <td style=""padding:4px 0;font-size:14px;color:#111827;"">{System.Net.WebUtility.HtmlEncode(priority)}</td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                            <!-- PDF Note -->
                            <p style=""margin:0 0 24px;font-size:14px;color:#6b7280;text-align:center;"">
                                The full work order PDF is attached to this email.
                            </p>
                            <!-- CTA Button -->
                            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                <tr>
                                    <td align=""center"" style=""padding-bottom:12px;"">
                                        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"">
                                            <tr>
                                                <td style=""background-color:#E8511A;border-radius:6px;"">
                                                    <a href=""{System.Net.WebUtility.HtmlEncode(quoteUrl)}"" target=""_blank"" style=""display:inline-block;padding:12px 32px;color:#ffffff;text-decoration:none;font-size:16px;font-weight:bold;"">Submit Quote</a>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color:#f9fafb;padding:20px 32px;border-top:1px solid #e5e7eb;"">
                            <p style=""margin:0;font-size:12px;color:#9ca3af;text-align:center;"">
                                On-Call Facilities &amp; Maintenance LLC | 123 Main Street, Suite 100, Miami, FL 33101
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
}
