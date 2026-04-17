using DANGCAPNE.Data;
using DANGCAPNE.Hubs;
using DANGCAPNE.Models.Requests;
using DANGCAPNE.Models.SystemModels;
using DANGCAPNE.Models.Workflow;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DANGCAPNE.Services
{
    public class SlaEscalationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SlaEscalationBackgroundService> _logger;

        public SlaEscalationBackgroundService(IServiceProvider serviceProvider, ILogger<SlaEscalationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingApprovalsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SLA worker loop failed");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task ProcessPendingApprovalsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hub = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
            var email = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();

            var pendingApprovals = await context.RequestApprovals
                .Include(a => a.Approver)
                .Include(a => a.Request)!.ThenInclude(r => r!.Requester)
                .Include(a => a.Request)!.ThenInclude(r => r!.FormTemplate)
                .Where(a => a.Status == "Pending" && a.Request != null)
                .ToListAsync(cancellationToken);

            if (pendingApprovals.Count == 0)
            {
                return;
            }

            var tenantIds = pendingApprovals.Select(a => a.Request!.TenantId).Distinct().ToList();
            var templateIds = pendingApprovals.Select(a => a.Request!.FormTemplateId).Distinct().ToList();

            var slaConfigs = await context.SlaConfigs
                .AsNoTracking()
                .Where(s => tenantIds.Contains(s.TenantId) && (!s.FormTemplateId.HasValue || templateIds.Contains(s.FormTemplateId.Value)))
                .ToListAsync(cancellationToken);

            var escalationRules = await context.EscalationRules
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            foreach (var approval in pendingApprovals)
            {
                var request = approval.Request!;
                var config = ResolveConfig(slaConfigs, request.TenantId, request.FormTemplateId);
                if (config == null)
                {
                    continue;
                }

                var now = DateTime.UtcNow;

                if (config.AutoRemind)
                {
                    var remindAt = request.CreatedAt.AddHours(config.ReminderHours);
                    if (now >= remindAt)
                    {
                        var reminderAction = $"SlaReminderStep{approval.Id}";
                        var reminderExists = await context.RequestAuditLogs
                            .AnyAsync(log => log.RequestId == request.Id && log.Action == reminderAction, cancellationToken);

                        if (!reminderExists)
                        {
                            await SendReminderAsync(context, hub, email, approval, cancellationToken);
                            context.RequestAuditLogs.Add(new RequestAuditLog
                            {
                                RequestId = request.Id,
                                UserId = approval.ApproverId ?? request.RequesterId,
                                Action = reminderAction,
                                NewStatus = request.Status,
                                Details = "SLA reminder sent",
                                CreatedAt = now
                            });
                        }
                    }
                }

                if (config.AutoEscalate)
                {
                    var escalateAt = request.CreatedAt.AddHours(config.EscalationHours);
                    if (now >= escalateAt)
                    {
                        var escalationAction = $"SlaEscalationStep{approval.Id}";
                        var escalationExists = await context.RequestAuditLogs
                            .AnyAsync(log => log.RequestId == request.Id && log.Action == escalationAction, cancellationToken);

                        if (!escalationExists)
                        {
                            var targetUserId = ResolveEscalationTarget(escalationRules, config.Id);
                            if (!targetUserId.HasValue)
                            {
                                targetUserId = await ResolveAdminUserIdAsync(context, request.TenantId, cancellationToken);
                            }

                            if (targetUserId.HasValue)
                            {
                                await SendEscalationAsync(context, hub, email, approval, targetUserId.Value, cancellationToken);

                                context.RequestAuditLogs.Add(new RequestAuditLog
                                {
                                    RequestId = request.Id,
                                    UserId = targetUserId.Value,
                                    Action = escalationAction,
                                    NewStatus = request.Status,
                                    Details = "SLA escalation sent",
                                    CreatedAt = now
                                });
                            }
                        }
                    }
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private static SlaConfig? ResolveConfig(IReadOnlyCollection<SlaConfig> configs, int tenantId, int formTemplateId)
        {
            return configs
                .Where(c => c.TenantId == tenantId && (c.FormTemplateId == null || c.FormTemplateId == formTemplateId))
                .OrderByDescending(c => c.FormTemplateId.HasValue)
                .FirstOrDefault();
        }

        private static int? ResolveEscalationTarget(IReadOnlyCollection<EscalationRule> rules, int slaConfigId)
        {
            return rules
                .Where(r => r.SlaConfigId == slaConfigId)
                .OrderBy(r => r.EscalateAfterHours)
                .Select(r => (int?)r.EscalateToUserId)
                .FirstOrDefault();
        }

        private static async Task<int?> ResolveAdminUserIdAsync(ApplicationDbContext context, int tenantId, CancellationToken cancellationToken)
        {
            var adminRoleId = await context.Roles
                .AsNoTracking()
                .Where(r => r.TenantId == tenantId && r.Name == "Admin")
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!adminRoleId.HasValue)
            {
                return null;
            }

            return await context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.RoleId == adminRoleId.Value)
                .OrderBy(ur => ur.UserId)
                .Select(ur => (int?)ur.UserId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static async Task SendReminderAsync(
            ApplicationDbContext context,
            IHubContext<NotificationHub> hub,
            IEmailNotificationService email,
            RequestApproval approval,
            CancellationToken cancellationToken)
        {
            if (!approval.ApproverId.HasValue)
            {
                return;
            }

            var request = approval.Request!;
            var notification = new Notification
            {
                TenantId = request.TenantId,
                UserId = approval.ApproverId.Value,
                Title = "Nhac nho SLA",
                Message = $"Don {request.RequestCode} da qua han xu ly, vui long kiem tra ngay.",
                Type = "Reminder",
                ActionUrl = $"/Requests/Detail/{request.Id}",
                RelatedRequestId = request.Id,
                CreatedAt = DateTime.UtcNow
            };

            context.Notifications.Add(notification);

            await hub.Clients.Group($"user_{approval.ApproverId.Value}").SendAsync("ReceiveNotification", new
            {
                title = notification.Title,
                message = notification.Message,
                type = notification.Type,
                actionUrl = notification.ActionUrl
            }, cancellationToken);

            if (!string.IsNullOrWhiteSpace(approval.Approver?.Email))
            {
                await email.SendTemplatedEmailAsync(
                    request.TenantId,
                    "Reminder",
                    approval.Approver!.Email,
                    new Dictionary<string, string>
                    {
                        ["ApproverName"] = approval.Approver.FullName,
                        ["RequestCode"] = request.RequestCode,
                        ["ActionUrl"] = $"/Requests/Detail/{request.Id}",
                        ["Hours"] = "SLA"
                    },
                    request.Id,
                    cancellationToken);
            }
        }

        private static async Task SendEscalationAsync(
            ApplicationDbContext context,
            IHubContext<NotificationHub> hub,
            IEmailNotificationService email,
            RequestApproval approval,
            int escalationUserId,
            CancellationToken cancellationToken)
        {
            var request = approval.Request!;

            var escalationUser = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == escalationUserId, cancellationToken);

            var notification = new Notification
            {
                TenantId = request.TenantId,
                UserId = escalationUserId,
                Title = "Escalation SLA",
                Message = $"Don {request.RequestCode} vuot nguong SLA va can xu ly gap.",
                Type = "Escalation",
                ActionUrl = $"/Requests/Detail/{request.Id}",
                RelatedRequestId = request.Id,
                CreatedAt = DateTime.UtcNow
            };

            context.Notifications.Add(notification);

            await hub.Clients.Group($"user_{escalationUserId}").SendAsync("ReceiveNotification", new
            {
                title = notification.Title,
                message = notification.Message,
                type = notification.Type,
                actionUrl = notification.ActionUrl
            }, cancellationToken);

            if (!string.IsNullOrWhiteSpace(escalationUser?.Email))
            {
                await email.SendTemplatedEmailAsync(
                    request.TenantId,
                    "Reminder",
                    escalationUser.Email,
                    new Dictionary<string, string>
                    {
                        ["ApproverName"] = escalationUser.FullName,
                        ["RequestCode"] = request.RequestCode,
                        ["ActionUrl"] = $"/Requests/Detail/{request.Id}",
                        ["Hours"] = "Escalation"
                    },
                    request.Id,
                    cancellationToken);
            }
        }
    }
}
