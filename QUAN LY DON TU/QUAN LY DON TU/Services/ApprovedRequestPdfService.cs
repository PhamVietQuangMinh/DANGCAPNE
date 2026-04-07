using DANGCAPNE.Data;
using DANGCAPNE.Models.Requests;
using DANGCAPNE.Models.SystemModels;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

        public ApprovedRequestPdfService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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

            GeneratePdfDocument(request, physicalPath, profiles, _environment.WebRootPath);

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

        private static void GeneratePdfDocument(Request request, string outputPath, IReadOnlyCollection<DigitalSignatureProfile> profiles, string webRootPath)
        {
            var dataEntries = request.DataEntries
                .OrderBy(entry => entry.FieldKey)
                .ToList();

            var approvals = request.Approvals
                .OrderBy(approval => approval.StepOrder)
                .ToList();

            var signatures = BuildSignatures(request, approvals, profiles);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(column =>
                    {
                        column.Item().Text("PHIEU XAC NHAN DON TU").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                        column.Item().Text($"Ma don: {request.RequestCode}").SemiBold();
                        column.Item().Text($"Loai don: {request.FormTemplate?.Name ?? "Khong xac dinh"}");
                    });

                    page.Content().Column(column =>
                    {
                        column.Spacing(16);

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

                        column.Item().Text("Chu ky xac nhan").FontSize(14).Bold();
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
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(cell =>
                                {
                                    cell.Spacing(6);
                                    cell.Item().Text(signature.RoleLabel).SemiBold();
                                    cell.Item().Text(signature.SignerName);

                                    var imageBytes = TryLoadSignatureImage(signature.SignatureImageUrl, webRootPath);
                                    if (imageBytes != null)
                                    {
                                        cell.Item().Height(50).Image(imageBytes);
                                    }
                                    else
                                    {
                                        cell.Item().Text(signature.SignatureLabel).Italic().FontColor(Colors.Blue.Darken2);
                                    }

                                    cell.Item().Text(signature.SignedAt.HasValue
                                        ? $"Ky luc: {signature.SignedAt:dd/MM/yyyy HH:mm}"
                                        : "Chua ky");

                                    if (!string.IsNullOrWhiteSpace(signature.IpAddress))
                                    {
                                        cell.Item().Text($"IP: {signature.IpAddress}").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    }
                                });
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("File PDF duoc tao tu dong sau khi HR duyet xong - ");
                        text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).SemiBold();
                    });
                });
            }).GeneratePdf(outputPath);
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
