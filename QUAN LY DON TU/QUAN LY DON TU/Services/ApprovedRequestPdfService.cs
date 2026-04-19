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
            var isLeaveForm = string.Equals(request.FormTemplate?.Category, "Leave", StringComparison.OrdinalIgnoreCase);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(column =>
                    {
                        column.Spacing(6);

                        if (isLeaveForm)
                        {
                            column.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").FontSize(12).Bold();
                            column.Item().AlignCenter().Text("Độc lập - Tự do - Hạnh phúc").FontSize(11).Italic();
                            column.Item().AlignCenter().Text("---------------").FontSize(10);

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().AlignRight().Text($"Ngày {request.CreatedAt:dd} tháng {request.CreatedAt:MM} năm {request.CreatedAt:yyyy}").FontSize(10);
                            });

                            var formTitle = isSickLeave
                                ? "ĐƠN XIN PHÉP NGHỈ ỐM"
                                : $"ĐƠN {request.FormTemplate?.Name?.ToUpperInvariant() ?? "XIN PHÉP"}";

                            column.Item().AlignCenter().Text(formTitle).FontSize(18).Bold();
                            column.Item().AlignCenter().Text("(Bản điện tử – dùng để đối chiếu nội bộ)").FontSize(9).FontColor(Colors.Grey.Darken1);
                        }
                        else
                        {
                            column.Item().AlignCenter().Text("PHIẾU XÁC NHẬN ĐƠN TỪ")
                                .FontSize(20)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);
                            column.Item().AlignCenter().Text("(Bản điện tử – dùng để đối chiếu nội bộ)").FontSize(9).FontColor(Colors.Grey.Darken1);
                        }

                        column.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Spacing(2);
                                col.Item().Text($"Số/Mã đơn: {request.RequestCode}").SemiBold();
                                col.Item().Text($"Loại biểu mẫu: {ResolveLeaveTypeLabel(leaveType, request.FormTemplate?.Name)}");
                                col.Item().Text($"Người gửi: {request.Requester?.FullName ?? "Không xác định"}");
                                col.Item().Text($"Link xác minh: {verifyUrl}").FontSize(9).FontColor(Colors.Blue.Darken2);
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

                        column.Item().Text("Xác nhận phê duyệt").FontSize(14).Bold();
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

                            AddHeaderCell(table, "Bước");
                            AddHeaderCell(table, "Vai trò");
                            AddHeaderCell(table, "Người xử lý");
                            AddHeaderCell(table, "Thời gian");
                            AddHeaderCell(table, "Nhận xét");

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
                            text.Span("TRẠNG THÁI: ").SemiBold();
                            text.Span("ĐÃ DUYỆT").Bold().FontColor(Colors.Green.Darken2);
                        });

                        column.Item().Text("Chữ ký điện tử").FontSize(14).Bold();
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
                                        ? $"Ký lúc: {signature.SignedAt:dd/MM/yyyy HH:mm}"
                                        : "Chưa ký");

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
                        text.Span("File PDF được tạo tự động sau khi phê duyệt – ");
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
                info.Item().Text($"Tiêu đề: {request.Title}").Bold();
                info.Item().Text($"Người gửi: {request.Requester?.FullName ?? "Không xác định"}");
                info.Item().Text($"Phòng ban: {request.Requester?.Department?.Name ?? "Không xác định"}");
                info.Item().Text($"Thời gian gửi: {request.CreatedAt:dd/MM/yyyy HH:mm}");
                info.Item().Text($"Hoàn tất phê duyệt: {(request.CompletedAt.HasValue ? request.CompletedAt.Value.ToString("dd/MM/yyyy HH:mm") : "--")}");
            });

            column.Item().Text("Nội dung đơn").FontSize(14).Bold();
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
                section.Item().AlignCenter().Text("THÔNG TIN NGƯỜI LÀM ĐƠN").FontSize(13).SemiBold();
                section.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Họ và tên: {request.Requester?.FullName ?? "Không xác định"}");
                    row.RelativeItem().Text($"Phòng ban: {request.Requester?.Department?.Name ?? "Không xác định"}");
                });
                section.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Mã nhân viên: {request.Requester?.EmployeeCode ?? "--"}");
                    row.RelativeItem().Text($"Ngày lập đơn: {request.CreatedAt:dd/MM/yyyy}");
                });
            });

            column.Item().Border(1).BorderColor(Colors.Grey.Medium).Padding(14).Column(section =>
            {
                section.Spacing(8);
                section.Item().AlignCenter().Text("NỘI DUNG XIN NGHỈ ỐM").FontSize(13).SemiBold();
                section.Item().Text("Kính gửi: Trưởng phòng / Bộ phận Nhân sự");
                section.Item().Text($"Tôi làm đơn này để xin phép nghỉ ốm từ ngày {startDate} đến ngày {endDate}, tổng cộng {totalDays} ngày làm việc.");
                section.Item().Text($"Lý do: {reason}");
                section.Item().Text("Hồ sơ kèm theo: Giấy khám bệnh / Giấy ra viện / Giấy chứng nhận nghỉ việc hưởng BHXH (nếu có).")
                    .FontColor(Colors.Grey.Darken1);
                section.Item().Text("Tôi cam kết các thông tin trên là đúng sự thật và chịu trách nhiệm trước công ty về nội dung đã khai.");
            });

            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(note =>
            {
                note.Spacing(4);
                note.Item().Text("GHI CHÚ").FontSize(12).SemiBold().FontColor(Colors.Blue.Darken2);
                note.Item().Text("- Số ngày nghỉ được tính theo ngày làm việc (không tính Thứ 7, Chủ nhật và ngày lễ).").FontSize(10);
                note.Item().Text("- Trường hợp nghỉ dài ngày hoặc có chứng từ, vui lòng bổ sung minh chứng đính kèm để đối chiếu.").FontSize(10);
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
                    "Người làm đơn",
                    request.Requester?.FullName ?? "Không xác định",
                    request.CreatedAt,
                    null,
                    FindProfile(request.RequesterId))
            };

            foreach (var approval in approvals.OrderBy(a => a.StepOrder))
            {
                if (!approval.ApproverId.HasValue || approval.Status != "Approved")
                {
                    continue;
                }

                var label = NormalizeSignatureRoleLabel(approval.StepName);
                signatures.Add(CreateSignature(
                    label,
                    approval.Approver?.FullName ?? "Không xác định",
                    approval.ActionDate,
                    approval.IpAddress,
                    FindProfile(approval.ApproverId.Value)));
            }

            return signatures;
        }

        private static string NormalizeSignatureRoleLabel(string stepName)
        {
            if (string.IsNullOrWhiteSpace(stepName))
            {
                return "Người duyệt";
            }

            var name = stepName.Trim();
            if (name.Contains("HR", StringComparison.OrdinalIgnoreCase))
            {
                return "Phòng Nhân sự";
            }
            if (name.Contains("Kế toán", StringComparison.OrdinalIgnoreCase) || name.Contains("Ke toan", StringComparison.OrdinalIgnoreCase))
            {
                return "Phòng Kế toán";
            }
            if (name.Contains("Giám đốc", StringComparison.OrdinalIgnoreCase) || name.Contains("Giam doc", StringComparison.OrdinalIgnoreCase))
            {
                return "Ban Giám đốc";
            }
            if (name.Contains("Trưởng", StringComparison.OrdinalIgnoreCase) || name.Contains("Truong", StringComparison.OrdinalIgnoreCase) || name.Contains("Manager", StringComparison.OrdinalIgnoreCase))
            {
                return "Trưởng đơn vị";
            }

            return name;
        }

        private static PdfSignatureInfo CreateSignature(string roleLabel, string signerName, DateTime? signedAt, string? ipAddress, DigitalSignatureProfile? profile)
        {
            return new PdfSignatureInfo(
                roleLabel,
                signerName,
                !string.IsNullOrWhiteSpace(profile?.SignatureName) ? profile.SignatureName : $"Ký điện tử bởi {signerName}",
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
