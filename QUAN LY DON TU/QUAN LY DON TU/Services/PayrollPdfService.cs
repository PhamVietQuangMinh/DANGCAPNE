using DANGCAPNE.Data;
using DANGCAPNE.Models.Finance;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DANGCAPNE.Services
{
    public interface IPayrollPdfService
    {
        Task<string> GeneratePayrollSlipPdfAsync(int payrollSlipId, CancellationToken cancellationToken = default);
    }

    public class PayrollPdfService : IPayrollPdfService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PayrollPdfService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<string> GeneratePayrollSlipPdfAsync(int payrollSlipId, CancellationToken cancellationToken = default)
        {
            var slip = await _context.PayrollSlips
                .Include(s => s.User).ThenInclude(u => u!.Position)
                .Include(s => s.PayrollClosure).ThenInclude(c => c!.ClosedByUser)
                .FirstOrDefaultAsync(s => s.Id == payrollSlipId, cancellationToken);

            if (slip == null)
            {
                throw new InvalidOperationException($"Khong tim thay phieu luong {payrollSlipId}.");
            }

            var tenant = await _context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == slip.TenantId, cancellationToken);

            var outputDirectory = Path.Combine(_environment.WebRootPath, "uploads", "payroll", slip.TenantId.ToString(), slip.PayrollMonth);
            Directory.CreateDirectory(outputDirectory);

            var safeEmployeeCode = string.IsNullOrWhiteSpace(slip.User?.EmployeeCode) ? $"U{slip.UserId}" : slip.User!.EmployeeCode;
            var fileName = $"PAYROLL-{slip.PayrollMonth}-{safeEmployeeCode}.pdf";
            var physicalPath = Path.Combine(outputDirectory, fileName);
            var relativePath = $"/uploads/payroll/{slip.TenantId}/{slip.PayrollMonth}/{fileName}";

            GeneratePdfDocument(slip, tenant?.CompanyName ?? "Cong ty", physicalPath);

            slip.PdfPath = relativePath;
            await _context.SaveChangesAsync(cancellationToken);
            return relativePath;
        }

        private static void GeneratePdfDocument(PayrollSlip slip, string companyName, string outputPath)
        {
            var user = slip.User;
            var totalBeforeDeductions = slip.MainSalary + slip.OvertimeSalary + slip.FixedAllowance + slip.OtherIncome;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(22);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Column(column =>
                    {
                        column.Item().Text(companyName).FontSize(12);
                        column.Item().AlignRight().Text("PHIEU LUONG").FontSize(22).Bold().FontColor(Colors.Red.Medium);
                        column.Item().AlignRight().Text($"Ky luong: {slip.PayrollMonth}").SemiBold();
                    });

                    page.Content().PaddingTop(14).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.3f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(1.8f);
                        });

                        AddCell(table, "Ho ten", user?.FullName?.ToUpperInvariant() ?? "KHONG XAC DINH", true);
                        AddCell(table, "Chuc danh", user?.Position?.Name ?? "--", false);
                        AddCell(table, "Ma NV", user?.EmployeeCode ?? "--", false);

                        AddCell(table, "Nhan viec", user?.HireDate.ToString("dd/MM/yyyy") ?? "--", false);
                        AddCell(table, "He so luong", slip.SalaryCoefficient.ToString("N2"), false);
                        AddCell(table, "Nguoi chot", slip.PayrollClosure?.ClosedByUser?.FullName ?? "--", false);

                        AddCell(table, "Cong chinh huong P/C", slip.StandardWorkDays.ToString(), false);
                        AddCell(table, "Ngay cong thuc te", slip.ActualWorkingDays.ToString(), true);
                        AddCell(table, "Phut di tre", slip.LateMinutes.ToString(), false);

                        AddCell(table, "Cong chinh", slip.ActualWorkHours.ToString("N2"), false);
                        AddCell(table, "Tien luong / gio", slip.HourlyRate.ToString("N0"), true);
                        AddCell(table, "Luong co ban", (slip.BaseSalary * slip.SalaryCoefficient).ToString("N0"), false);

                        AddCell(table, "Luong chinh", slip.MainSalary.ToString("N0"), false);
                        AddCell(table, "Cong lam them", slip.OvertimeHours.ToString("N2"), true);
                        AddCell(table, "Luong lam them", slip.OvertimeSalary.ToString("N0"), false);

                        AddCell(table, "Phu cap co dinh", slip.FixedAllowance.ToString("N0"), false);
                        AddCell(table, "Thu nhap khac", slip.OtherIncome.ToString("N0"), false);
                        AddCell(table, "Tam ung luong", slip.AdvanceDeduction.ToString("N0"), false);

                        AddCell(table, "Phat di tre", slip.LatePenalty.ToString("N0"), false);
                        AddCell(table, "Thuc linh", slip.NetSalary.ToString("N0"), true, Colors.Blue.Medium);
                        AddCell(table, "Tong cong truoc tru", totalBeforeDeductions.ToString("N0"), true, Colors.Blue.Medium);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Phieu luong duoc tao tu dong boi he thong - ");
                        text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).SemiBold();
                    });
                });
            }).GeneratePdf(outputPath);
        }

        private static void AddCell(TableDescriptor table, string label, string value, bool emphasize, string? valueColor = null)
        {
            table.Cell().Border(1).BorderColor(Colors.Blue.Medium).Padding(8).Text(label);
            table.Cell().Border(1).BorderColor(Colors.Blue.Medium).Padding(8).Text(text =>
            {
                var span = text.Span(value);
                if (emphasize)
                {
                    span.Bold();
                }

                if (!string.IsNullOrWhiteSpace(valueColor))
                {
                    span.FontColor(valueColor);
                }
            });
        }
    }
}
