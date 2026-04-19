using DANGCAPNE.Data;
using DANGCAPNE.Models.Requests;
using DANGCAPNE.Models.SystemModels;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using System.Globalization;

namespace DANGCAPNE.Services
{
    public interface IApprovedRequestPdfService
    {
        Task<RequestAttachment> GenerateApprovedPdfAsync(int requestId, int generatedByUserId, CancellationToken cancellationToken = default);
    }

    public class ApprovedRequestPdfService : IApprovedRequestPdfService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public ApprovedRequestPdfService(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
        }

        public async Task<RequestAttachment> GenerateApprovedPdfAsync(int requestId, int generatedByUserId, CancellationToken cancellationToken = default)
        {
            var request = await _context.Requests
                .Include(r => r.Requester).ThenInclude(u => u!.Department)
                .Include(r => r.FormTemplate)
                .Include(r => r.DataEntries)
                .Include(r => r.Approvals).ThenInclude(a => a.Approver)
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

            if (request == null)
            {
                throw new InvalidOperationException($"Khong tim thay don {requestId} de tao PDF.");
            }

            var outputDirectory = Path.Combine(_environment.WebRootPath, "uploads", request.TenantId.ToString(), "generated");
            Directory.CreateDirectory(outputDirectory);

            var fileName = $"{request.RequestCode}-approved.pdf";
            var relativePath = $"/uploads/{request.TenantId}/generated/{fileName}";
            var physicalPath = Path.Combine(outputDirectory, fileName);

            var existingAttachment = request.Attachments
                .FirstOrDefault(att => string.Equals(att.FilePath, relativePath, StringComparison.OrdinalIgnoreCase));

            var profileUserIds = request.Approvals
                .Where(a => a.ApproverId.HasValue)
                .Select(a => a.ApproverId!.Value)
                .Append(request.RequesterId)
                .Distinct()
                .ToList();

            var profiles = await _context.Set<DigitalSignatureProfile>()
                .AsNoTracking()
                .Where(p => p.IsActive && profileUserIds.Contains(p.UserId))
                .ToListAsync(cancellationToken);

            if (existingAttachment != null && File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }

            var verifyUrl = BuildVerifyUrl(request.RequestCode);
            var qrBytes = GenerateQrPng(verifyUrl);
            GeneratePdfDocument(request, physicalPath, profiles, _environment.WebRootPath, verifyUrl, qrBytes);

            var fileInfo = new FileInfo(physicalPath);
            if (existingAttachment == null)
            {
                existingAttachment = new RequestAttachment
                {
                    RequestId = request.Id,
                    FileName = fileName,
                    UploadedAt = DateTime.Now
                };

                _context.RequestAttachments.Add(existingAttachment);
            }

            existingAttachment.FileName = fileName;
            existingAttachment.FilePath = relativePath;
            existingAttachment.ContentType = "application/pdf";
            existingAttachment.FileSize = fileInfo.Length;
            existingAttachment.UploadedById = generatedByUserId;
            existingAttachment.UploadedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);
            return existingAttachment;
        }

        private static void GeneratePdfDocument(Request request, string outputPath, IReadOnlyCollection<DigitalSignatureProfile> profiles, string webRootPath, string verifyUrl, byte[] qrPng)
        {
            var dataEntries = request.DataEntries
                .OrderBy(entry => entry.FieldKey)
                .ToList();

            var approvals = request.Approvals
                .OrderBy(approval => approval.StepOrder)
                .ToList();

            var signatures = BuildSignatures(request, approvals, profiles);
            var fields = dataEntries.ToDictionary(x => x.FieldKey, x => x.FieldValue ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            var leaveType = fields.TryGetValue("leave_type", out var leaveTypeValue) ? leaveTypeValue : string.Empty;
            var isSickLeave = string.Equals(leaveType, "SL", StringComparison.OrdinalIgnoreCase);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(column =>
                    {
                        column.Spacing(4);
                        column.Item().AlignCenter().Text(isSickLeave ? "DON XIN NGHI OM / GIAY XAC NHAN NGHI BENH" : "PHIEU XAC NHAN DON TU")
                            .FontSize(isSickLeave ? 17 : 20)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);
                        column.Item().AlignCenter().Text("(Ban dien tu co gia tri doi chieu noi bo)").FontSize(9).FontColor(Colors.Grey.Darken1);
                        column.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Spacing(2);
                                col.Item().Text($"So/ma don: {request.RequestCode}").SemiBold();
                                col.Item().Text($"Loai bieu mau: {ResolveLeaveTypeLabel(leaveType, request.FormTemplate?.Name)}");
                                col.Item().Text($"Ngay lap: {request.CreatedAt:dd/MM/yyyy HH:mm}");
                                col.Item().Text($"Link xac minh: {verifyUrl}").FontSize(9).FontColor(Colors.Blue.Darken2);
                            });
                            row.ConstantItem(72).AlignRight().Height(72).Image(qrPng);
                        });
                    });

                    page.Content().Column(column =>
                    {
                        column.Spacing(16);

                        if (isSickLeave)
                        {
                            RenderSickLeaveSection(column, request, fields);
                        }
                        else
                        {
                            RenderDefaultRequestSection(column, request, dataEntries);
                        }

                        column.Item().Text("Xac nhan phe duyet").FontSize(14).Bold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(50);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                            });

                            AddHeaderCell(table, "Buoc");
                            AddHeaderCell(table, "Vai tro");
                            AddHeaderCell(table, "Nguoi xu ly");
                            AddHeaderCell(table, "Thoi gian");
                            AddHeaderCell(table, "Nhan xet");

                            foreach (var approval in approvals)
                            {
                                AddBodyCell(table, approval.StepOrder.ToString());
                                AddBodyCell(table, approval.StepName);
                                AddBodyCell(table, approval.Approver?.FullName ?? "--");
                                AddBodyCell(table, approval.ActionDate?.ToString("dd/MM/yyyy HH:mm") ?? "--");
                                AddBodyCell(table, approval.Comments ?? approval.Status);
                            }
                        });

                        column.Item().Border(1).BorderColor(Colors.Green.Medium).Padding(12).AlignRight().Text(text =>
                        {
                            text.Span("TRANG THAI: ").SemiBold();
                            text.Span("DONE").Bold().FontColor(Colors.Green.Darken2);
                        });

                        column.Item().Text(isSickLeave ? "Xac nhan ky duyet va dong dau" : "Chu ky xac nhan").FontSize(14).Bold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            foreach (var signature in signatures)
                            {
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).MinHeight(120).Column(cell =>
                                {
                                    cell.Spacing(6);
                                    cell.Item().AlignCenter().Text(signature.RoleLabel).SemiBold();
                                    cell.Item().AlignCenter().Text(signature.SignerName);

                                    var imageBytes = TryLoadSignatureImage(signature.SignatureImageUrl, webRootPath);
                                    if (imageBytes != null)
                                    {
                                        cell.Item().PaddingVertical(4).AlignCenter().Height(42).Image(imageBytes);
                                    }
                                    else
                                    {
                                        cell.Item().AlignCenter().Text(signature.SignatureLabel).Italic().FontColor(Colors.Blue.Darken2);
                                    }

                                    cell.Item().AlignCenter().Text(signature.SignedAt.HasValue
                                        ? $"Ky luc: {signature.SignedAt:dd/MM/yyyy HH:mm}"
                                        : "Chua ky");

                                    if (!string.IsNullOrWhiteSpace(signature.IpAddress))
                                    {
                                        cell.Item().AlignCenter().Text($"IP: {signature.IpAddress}").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    }
                                });
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span(isSickLeave ? "Tai lieu dien tu phuc vu doi chieu giay nghi benh/noi bo - " : "File PDF duoc tao tu dong sau khi phe duyet xong - ");
                        text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).SemiBold();
                    });
                });
            }).GeneratePdf(outputPath);
        }

        private static void RenderDefaultRequestSection(ColumnDescriptor column, Request request, IReadOnlyCollection<RequestData> dataEntries)
        {
            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(info =>
            {
                info.Spacing(6);
                info.Item().Text($"Tieu de: {request.Title}").Bold();
                info.Item().Text($"Nguoi gui: {request.Requester?.FullName ?? "Khong xac dinh"}");
                info.Item().Text($"Phong ban: {request.Requester?.Department?.Name ?? "Khong xac dinh"}");
                info.Item().Text($"Thoi gian gui: {request.CreatedAt:dd/MM/yyyy HH:mm}");
                info.Item().Text($"Hoan tat phe duyet: {(request.CompletedAt.HasValue ? request.CompletedAt.Value.ToString("dd/MM/yyyy HH:mm") : "--")}");
                info.Item().Text("Chu ky nguoi gui: xac nhan bang tai khoan dang nhap trong he thong.");
            });

            column.Item().Text("Noi dung don").FontSize(14).Bold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(180);
                    columns.RelativeColumn();
                });

                foreach (var entry in dataEntries)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(6).Text(entry.FieldKey).SemiBold();
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(6).Text(entry.FieldValue ?? "--");
                }
            });
        }

        private static void RenderSickLeaveSection(ColumnDescriptor column, Request request, IReadOnlyDictionary<string, string> fields)
        {
            var startDate = FormatDate(fields.TryGetValue("start_date", out var start) ? start : null);
            var endDate = FormatDate(fields.TryGetValue("end_date", out var end) ? end : null);
            var totalDays = fields.TryGetValue("total_days", out var days) ? days : "--";
            var reason = fields.TryGetValue("reason", out var leaveReason) ? leaveReason : "--";

            column.Item().Border(1).BorderColor(Colors.Grey.Medium).Padding(14).Column(section =>
            {
                section.Spacing(6);
                section.Item().AlignCenter().Text("THONG TIN NGUOI XIN NGHI").FontSize(13).SemiBold();
                section.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Ho va ten: {request.Requester?.FullName ?? "Khong xac dinh"}");
                    row.RelativeItem().Text($"Phong ban: {request.Requester?.Department?.Name ?? "Khong xac dinh"}");
                });
                section.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Ma nhan vien: {request.Requester?.EmployeeCode ?? "--"}");
                    row.RelativeItem().Text($"Ngay lap don: {request.CreatedAt:dd/MM/yyyy}");
                });
            });

            column.Item().Border(1).BorderColor(Colors.Grey.Medium).Padding(14).Column(section =>
            {
                section.Spacing(8);
                section.Item().AlignCenter().Text("NOI DUNG XIN NGHI BENH").FontSize(13).SemiBold();
                section.Item().Text($"Kinh gui: Truong phong / Bo phan Nhan su");
                section.Item().Text($"Toi lam don nay de de nghi nghi om tu ngay {startDate} den ngay {endDate}, tong cong {totalDays} ngay lam viec.");
                section.Item().Text($"Ly do/nguyen nhan: {reason}");
                section.Item().Text("Ho so kem theo: Giay kham benh / giay ra vien / giay chung nhan nghi viec huong BHXH (neu co).")
                    .FontColor(Colors.Grey.Darken1);
                section.Item().Text("Toi cam ket cac thong tin tren la dung su that va chiu trach nhiem truoc cong ty ve noi dung da khai.");
            });

            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(note =>
            {
                note.Spacing(4);
                note.Item().Text("GHI CHU DOI VOI DON NGHI OM").FontSize(12).SemiBold().FontColor(Colors.Blue.Darken2);
                note.Item().Text("- So ngay nghi tren he thong duoc tinh theo ngay lam viec, khong tinh Thu 7, Chu nhat va ngay le.").FontSize(10);
                note.Item().Text("- Truong hop nghi dai ngay hoac nghi om co chung tu, nhan vien can bo sung minh chung dinh kem de doi chieu.").FontSize(10);
            });
        }

        private static string ResolveLeaveTypeLabel(string leaveTypeCode, string? fallback)
        {
            return leaveTypeCode?.ToUpperInvariant() switch
            {
                "SL" => "Nghi om",
                "AL" => "Phep nam",
                "ML" => "Nghi thai san",
                "UL" => "Nghi khong luong",
                _ => fallback ?? "Khong xac dinh"
            };
        }

        private static string FormatDate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "--";
            }

            return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? parsed.ToString("dd/MM/yyyy")
                : raw;
        }

        private string BuildVerifyUrl(string requestCode)
        {
            var baseUrl = Environment.GetEnvironmentVariable("APP_BASE_URL")
                ?? _configuration["App:BaseUrl"]
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = "http://localhost:5268";
            }

            baseUrl = baseUrl.TrimEnd('/');
            return $"{baseUrl}/Requests/Verify/{Uri.EscapeDataString(requestCode)}";
        }

        private static byte[] GenerateQrPng(string payload)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var qr = new PngByteQRCode(data);
            return qr.GetGraphic(10);
        }

        private static void AddHeaderCell(TableDescriptor table, string text)
        {
            table.Cell()
                .Background(Colors.Grey.Lighten3)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Padding(6)
                .Text(text)
                .SemiBold();
        }

        private static void AddBodyCell(TableDescriptor table, string? text)
        {
            table.Cell()
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(6)
                .Text(text ?? "--");
        }

        private static List<PdfSignatureInfo> BuildSignatures(Request request, IReadOnlyCollection<RequestApproval> approvals, IReadOnlyCollection<DigitalSignatureProfile> profiles)
        {
            DigitalSignatureProfile? FindProfile(int userId) => profiles.FirstOrDefault(p => p.UserId == userId);

            var signatures = new List<PdfSignatureInfo>
            {
                CreateSignature(
                    "Nhan vien gui don",
                    request.Requester?.FullName ?? "Khong xac dinh",
                    request.CreatedAt,
                    null,
                    FindProfile(request.RequesterId))
            };

            var managerApproval = approvals.FirstOrDefault(a => a.StepName.Contains("Tr", StringComparison.OrdinalIgnoreCase));
            var hrApproval = approvals.FirstOrDefault(a => a.StepName.Contains("HR", StringComparison.OrdinalIgnoreCase));

            if (managerApproval?.Status == "Approved")
            {
                signatures.Add(CreateSignature(
                    "Truong phong duyet",
                    managerApproval.Approver?.FullName ?? "Chua xac dinh",
                    managerApproval.ActionDate,
                    managerApproval.IpAddress,
                    managerApproval.ApproverId.HasValue ? FindProfile(managerApproval.ApproverId.Value) : null));
            }

            if (hrApproval?.Status == "Approved")
            {
                signatures.Add(CreateSignature(
                    "HR duyet",
                    hrApproval.Approver?.FullName ?? "Chua xac dinh",
                    hrApproval.ActionDate,
                    hrApproval.IpAddress,
                    hrApproval.ApproverId.HasValue ? FindProfile(hrApproval.ApproverId.Value) : null));
            }

            return signatures;
        }

        private static PdfSignatureInfo CreateSignature(string roleLabel, string signerName, DateTime? signedAt, string? ipAddress, DigitalSignatureProfile? profile)
        {
            return new PdfSignatureInfo(
                roleLabel,
                signerName,
                !string.IsNullOrWhiteSpace(profile?.SignatureName) ? profile.SignatureName : $"Ky dien tu boi {signerName}",
                profile?.SignatureImageUrl,
                signedAt,
                ipAddress);
        }

        private static byte[]? TryLoadSignatureImage(string? signatureImageUrl, string webRootPath)
        {
            if (string.IsNullOrWhiteSpace(signatureImageUrl))
            {
                return null;
            }

            var normalizedPath = signatureImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(webRootPath, normalizedPath);
            return File.Exists(physicalPath) ? File.ReadAllBytes(physicalPath) : null;
        }

        private sealed record PdfSignatureInfo(
            string RoleLabel,
            string SignerName,
            string SignatureLabel,
            string? SignatureImageUrl,
            DateTime? SignedAt,
            string? IpAddress);
    }
}
