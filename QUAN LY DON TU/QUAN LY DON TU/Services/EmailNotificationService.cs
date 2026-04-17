using System.Net;
using System.Net.Mail;
using DANGCAPNE.Data;
using DANGCAPNE.Models.SystemModels;
using Microsoft.EntityFrameworkCore;

namespace DANGCAPNE.Services
{
    public sealed class EmailSettings
    {
        public bool Enabled { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string FromEmail { get; set; } = "no-reply@dangcapne.local";
        public string FromName { get; set; } = "DANGCAPNE";
    }

    public interface IEmailNotificationService
    {
        Task SendTemplatedEmailAsync(
            int tenantId,
            string templateName,
            string toEmail,
            IReadOnlyDictionary<string, string> placeholders,
            int? relatedRequestId = null,
            CancellationToken cancellationToken = default);
    }

    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<EmailNotificationService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendTemplatedEmailAsync(
            int tenantId,
            string templateName,
            string toEmail,
            IReadOnlyDictionary<string, string> placeholders,
            int? relatedRequestId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return;
            }

            var settings = _configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
            var template = await _context.EmailTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Name == templateName && t.IsActive, cancellationToken);

            var tenant = await _context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CompanyName"] = tenant?.CompanyName ?? "DANGCAPNE"
            };

            foreach (var pair in placeholders)
            {
                merged[pair.Key] = pair.Value;
            }

            var subject = ApplyPlaceholders(template?.Subject ?? $"[{merged["CompanyName"]}] Notification", merged);
            var body = ApplyPlaceholders(template?.BodyHtml ?? "<p>{{Message}}</p>", merged);

            if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.Host))
            {
                await SaveEmailLogAsync(tenantId, toEmail, subject, "Queued", "SMTP not configured", relatedRequestId, cancellationToken);
                return;
            }

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(settings.FromEmail, settings.FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(toEmail);

                using var smtp = new SmtpClient(settings.Host, settings.Port)
                {
                    EnableSsl = settings.UseSsl,
                    Credentials = string.IsNullOrWhiteSpace(settings.Username)
                        ? CredentialCache.DefaultNetworkCredentials
                        : new NetworkCredential(settings.Username, settings.Password)
                };

                await smtp.SendMailAsync(message);
                await SaveEmailLogAsync(tenantId, toEmail, subject, "Sent", null, relatedRequestId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Email send failed to {ToEmail}", toEmail);
                await SaveEmailLogAsync(tenantId, toEmail, subject, "Failed", ex.Message, relatedRequestId, cancellationToken);
            }
        }

        private async Task SaveEmailLogAsync(
            int tenantId,
            string toEmail,
            string subject,
            string status,
            string? error,
            int? relatedRequestId,
            CancellationToken cancellationToken)
        {
            _context.EmailLogs.Add(new EmailLog
            {
                TenantId = tenantId,
                ToEmail = toEmail,
                Subject = subject,
                Status = status,
                ErrorMessage = error,
                RelatedRequestId = relatedRequestId,
                SentAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static string ApplyPlaceholders(string template, IReadOnlyDictionary<string, string> values)
        {
            var output = template;
            foreach (var pair in values)
            {
                output = output.Replace($"{{{{{pair.Key}}}}}", pair.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return output;
        }
    }
}
