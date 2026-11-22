using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Entities;

namespace SoftwareServices.Infrastructure.Services;

public class PdfQuotationGenerator : IPdfQuotationGenerator
{
    private readonly IConfiguration _configuration;

    public PdfQuotationGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateQuotationPdf(Quotation quotation, Customer customer, IEnumerable<QuotationItem> items, Dictionary<string, string>? companySettings = null)
    {
        var businessName = companySettings?.GetValueOrDefault("Company:Name")
            ?? _configuration["Company:Name"]
            ?? "AffordStack";

        var businessEmail = companySettings?.GetValueOrDefault("Company:Email")
            ?? _configuration["Company:Email"]
            ?? "manishkiran8464@gmail.com";

        var businessPhone = companySettings?.GetValueOrDefault("Company:Phone")
            ?? _configuration["Company:Phone"]
            ?? "+91 8464992957";

        var businessAddress = companySettings?.GetValueOrDefault("Company:Address")
            ?? _configuration["Company:Address"]
            ?? "Rc puram Hyd";

        var currencySetting = companySettings?.GetValueOrDefault("Finance:Currency")
            ?? _configuration["Finance:Currency"]
            ?? "INR (₹)";

        var currencyCode = currencySetting.Contains("(")
            ? currencySetting.Split('(')[0].Trim()
            : currencySetting;

        var currencySymbol = currencySetting.Contains("(") && currencySetting.Contains(")")
            ? currencySetting.Split('(')[1].Replace(")", "").Trim()
            : currencyCode;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                // Header
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(businessName).FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
                            c.Item().Text("Low-Cost Custom Software & Technical Consulting").FontSize(10).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{businessAddress} | {businessEmail} | {businessPhone}").FontSize(8).FontColor(Colors.Grey.Medium);
                        });

                        row.ConstantItem(180).Column(c =>
                        {
                            c.Item().AlignRight().Text("OFFICIAL QUOTATION").FontSize(16).Bold().FontColor(Colors.Grey.Darken4);
                            c.Item().AlignRight().Text($"Quote #: {quotation.QuotationNumber}").Bold();
                            c.Item().AlignRight().Text($"Date: {quotation.CreatedAt:dd MMM yyyy}");
                            c.Item().AlignRight().Text($"Valid Until: {quotation.ValidUntil:dd MMM yyyy}").FontColor(Colors.Red.Medium);
                        });
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // Content
                page.Content().PaddingVertical(10).Column(col =>
                {
                    // Customer & Project Info Cards
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                        {
                            c.Item().Text("PREPARED FOR:").FontSize(9).Bold().FontColor(Colors.Blue.Darken2);
                            c.Item().Text(customer.FullName).Bold();
                            if (!string.IsNullOrWhiteSpace(customer.CompanyName))
                                c.Item().Text(customer.CompanyName);
                            c.Item().Text(customer.Email);
                            c.Item().Text(customer.PhoneNumber);
                        });

                        row.ConstantItem(16);

                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                        {
                            c.Item().Text("PROJECT DETAILS:").FontSize(9).Bold().FontColor(Colors.Blue.Darken2);
                            c.Item().Text(quotation.ServiceDescription).Bold();
                            if (quotation.Enquiry != null)
                            {
                                c.Item().Text($"Ref: #{quotation.Enquiry.ReferenceNumber}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                if (!string.IsNullOrWhiteSpace(quotation.Enquiry.RequiredTechnology))
                                    c.Item().Text($"Tech: {quotation.Enquiry.RequiredTechnology}").FontSize(8).FontColor(Colors.Grey.Darken1);
                            }
                            c.Item().Text($"Expected Delivery: {quotation.ExpectedDeliveryDate:dd MMM yyyy}");
                            c.Item().Text($"Status: {quotation.Status}");
                        });
                    });

                    col.Item().PaddingVertical(10);

                    // Deliverables
                    if (!string.IsNullOrWhiteSpace(quotation.Deliverables))
                    {
                        col.Item().Column(c =>
                        {
                            c.Item().Text("Scope of Work & Deliverables").FontSize(12).Bold().FontColor(Colors.Grey.Darken4);
                            c.Item().PaddingTop(4).Text(quotation.Deliverables).FontSize(9);
                        });
                        col.Item().PaddingVertical(8);
                    }

                    // Items Table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4); // Item & Description
                            columns.RelativeColumn(1); // Qty
                            columns.RelativeColumn(2); // Unit Price
                            columns.RelativeColumn(2); // Total
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("Deliverable / Module").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignCenter().Text("Qty").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text($"Unit Price ({currencySymbol})").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text($"Total ({currencySymbol})").Bold();
                        });

                        var itemList = items.ToList();
                        if (itemList.Count == 0)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                .Text(quotation.ServiceDescription);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignCenter().Text("1");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{currencyCode} {quotation.DevelopmentCost:N2}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{currencyCode} {quotation.DevelopmentCost:N2}");
                        }
                        else
                        {
                            foreach (var item in itemList)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(ic =>
                                {
                                    ic.Item().Text(item.Title).Bold();
                                    if (!string.IsNullOrWhiteSpace(item.Description))
                                        ic.Item().Text(item.Description).FontSize(8).FontColor(Colors.Grey.Darken1);
                                });
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignCenter().Text(item.Quantity.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{currencyCode} {item.UnitPrice:N2}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"{currencyCode} {item.TotalPrice:N2}");
                            }
                        }
                    });

                    // Summary Rows
                    col.Item().PaddingTop(10).AlignRight().Width(240).Column(sc =>
                    {
                        sc.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Development Cost:");
                            r.RelativeItem().AlignRight().Text($"{currencyCode} {quotation.DevelopmentCost:N2}");
                        });
                        if (quotation.AdditionalCost > 0)
                        {
                            sc.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Additional Cost:");
                                r.RelativeItem().AlignRight().Text($"{currencyCode} {quotation.AdditionalCost:N2}");
                            });
                        }
                        if (quotation.TaxAmount > 0)
                        {
                            sc.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"Tax ({quotation.TaxRate}%):");
                                r.RelativeItem().AlignRight().Text($"{currencyCode} {quotation.TaxAmount:N2}");
                            });
                        }
                        sc.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                        sc.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Total Amount:").Bold().FontSize(11);
                            r.RelativeItem().AlignRight().Text($"{currencyCode} {quotation.TotalAmount:N2}").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
                        });
                        sc.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Advance Required:").Bold();
                            r.RelativeItem().AlignRight().Text($"{currencyCode} {quotation.AdvancePaymentAmount:N2}").Bold();
                        });
                        sc.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Remaining Balance:");
                            r.RelativeItem().AlignRight().Text($"{currencyCode} {quotation.RemainingPaymentAmount:N2}");
                        });
                    });

                    // Terms and Conditions
                    if (!string.IsNullOrWhiteSpace(quotation.TermsAndConditions))
                    {
                        col.Item().PaddingTop(16).Column(tc =>
                        {
                            tc.Item().Text("Terms & Conditions").FontSize(10).Bold().FontColor(Colors.Grey.Darken4);
                            tc.Item().PaddingTop(4).Text(quotation.TermsAndConditions).FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    }
                });

                // Footer
                page.Footer().Column(fc =>
                {
                    fc.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    fc.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text("Thank you for choosing our technical services!").FontSize(8).Italic();
                        r.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
