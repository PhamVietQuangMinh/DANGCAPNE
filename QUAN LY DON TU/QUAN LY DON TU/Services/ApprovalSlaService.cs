using DANGCAPNE.Data;
using DANGCAPNE.Models.Requests;
using DANGCAPNE.Models.Workflow;
using DANGCAPNE.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DANGCAPNE.Services
{
    public interface IApprovalSlaService
    {
        Task<Dictionary<int, ApprovalSlaViewModel>> BuildForRequestAsync(
            Request request,
            IReadOnlyCollection<RequestApproval> approvals,
            IReadOnlyDictionary<string, string> formData,
            CancellationToken cancellationToken = default);

        Task<Dictionary<int, ApprovalSlaViewModel>> BuildForApprovalsAsync(
            IReadOnlyCollection<RequestApproval> approvals,
            CancellationToken cancellationToken = default);
    }

    public class ApprovalSlaService : IApprovalSlaService
    {
        private readonly ApplicationDbContext _context;

        public ApprovalSlaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<int, ApprovalSlaViewModel>> BuildForRequestAsync(
            Request request,
            IReadOnlyCollection<RequestApproval> approvals,
            IReadOnlyDictionary<string, string> formData,
            CancellationToken cancellationToken = default)
        {
            var configs = await LoadConfigsAsync(new[] { request.TenantId }, new[] { request.FormTemplateId }, cancellationToken);
            return approvals.ToDictionary(
                approval => approval.Id,
                approval => BuildSnapshot(request, approval, formData, ResolveConfig(configs, request.TenantId, request.FormTemplateId)));
        }

        public async Task<Dictionary<int, ApprovalSlaViewModel>> BuildForApprovalsAsync(
            IReadOnlyCollection<RequestApproval> approvals,
            CancellationToken cancellationToken = default)
        {
            var effectiveApprovals = approvals
                .Where(a => a.Request != null)
                .ToList();

            if (effectiveApprovals.Count == 0)
            {
                return new Dictionary<int, ApprovalSlaViewModel>();
            }

            var requestIds = effectiveApprovals
                .Select(a => a.RequestId)
                .Distinct()
                .ToList();

            var requestData = await _context.RequestData
                .AsNoTracking()
                .Where(d => requestIds.Contains(d.RequestId))
                .ToListAsync(cancellationToken);

            var dataMap = requestData
                .GroupBy(d => d.RequestId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyDictionary<string, string>)group.ToDictionary(
                        item => item.FieldKey,
                        item => item.FieldValue ?? string.Empty));

            var tenantIds = effectiveApprovals
                .Select(a => a.Request!.TenantId)
                .Distinct()
                .ToList();

            var templateIds = effectiveApprovals
                .Select(a => a.Request!.FormTemplateId)
                .Distinct()
                .ToList();

            var configs = await LoadConfigsAsync(tenantIds, templateIds, cancellationToken);

            return effectiveApprovals.ToDictionary(
                approval => approval.Id,
                approval =>
                {
                    var request = approval.Request!;
                    dataMap.TryGetValue(request.Id, out var formData);
                    return BuildSnapshot(
                        request,
                        approval,
                        formData ?? new Dictionary<string, string>(),
                        ResolveConfig(configs, request.TenantId, request.FormTemplateId));
                });
        }

        private async Task<List<SlaConfig>> LoadConfigsAsync(
            IReadOnlyCollection<int> tenantIds,
            IReadOnlyCollection<int> templateIds,
            CancellationToken cancellationToken)
        {
            return await _context.SlaConfigs
                .AsNoTracking()
                .Where(config => tenantIds.Contains(config.TenantId) &&
                    (!config.FormTemplateId.HasValue || templateIds.Contains(config.FormTemplateId.Value)))
                .ToListAsync(cancellationToken);
        }

        private static SlaConfig? ResolveConfig(IEnumerable<SlaConfig> configs, int tenantId, int formTemplateId)
        {
            return configs
                .Where(config => config.TenantId == tenantId &&
                    (config.FormTemplateId == formTemplateId || config.FormTemplateId == null))
                .OrderByDescending(config => config.FormTemplateId.HasValue)
                .FirstOrDefault();
        }

        private static ApprovalSlaViewModel BuildSnapshot(
            Request request,
            RequestApproval approval,
            IReadOnlyDictionary<string, string> formData,
            SlaConfig? config)
        {
            var dueAt = ResolveDueAt(request, approval, formData, config);
            var referenceTime = approval.Status == "Pending"
                ? DateTime.Now
                : approval.ActionDate ?? request.CompletedAt ?? request.UpdatedAt;

            var breached = dueAt.HasValue && referenceTime > dueAt.Value;
            var statusText = dueAt.HasValue
                ? breached
                    ? approval.Status == "Pending" ? "Qua han SLA" : "Xu ly tre SLA"
                    : approval.Status == "Pending" ? $"Han {dueAt:dd/MM HH:mm}" : "Dung han SLA"
                : "Chua cau hinh SLA";

            return new ApprovalSlaViewModel
            {
                ApprovalId = approval.Id,
                DueAt = dueAt,
                IsOverdue = approval.Status == "Pending" && breached,
                IsBreached = breached,
                StatusText = statusText,
                RemainingText = dueAt.HasValue ? BuildRemainingText(dueAt.Value, approval.Status) : string.Empty
            };
        }

        private static DateTime? ResolveDueAt(
            Request request,
            RequestApproval approval,
            IReadOnlyDictionary<string, string> formData,
            SlaConfig? config)
        {
            if (request.FormTemplateId == 1 ||
                string.Equals(request.FormTemplate?.Category, "Leave", StringComparison.OrdinalIgnoreCase))
            {
                return request.CreatedAt.AddHours(config?.ReminderHours ?? 4);
            }

            if (request.FormTemplate?.RequiresFinancialApproval == true ||
                string.Equals(request.FormTemplate?.Category, "Expense", StringComparison.OrdinalIgnoreCase))
            {
                return request.CreatedAt.AddHours(config?.ReminderHours ?? 24);
            }

            return request.CreatedAt.AddHours(config?.ReminderHours ?? 8);
        }

        private static string BuildRemainingText(DateTime dueAt, string approvalStatus)
        {
            var delta = dueAt - DateTime.Now;
            if (approvalStatus != "Pending")
            {
                return $"Han xu ly: {dueAt:dd/MM HH:mm}";
            }

            if (delta.TotalSeconds <= 0)
            {
                var overdue = delta.Duration();
                return $"Qua han {FormatDuration(overdue)}";
            }

            return $"Con {FormatDuration(delta)}";
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
            {
                return $"{(int)duration.TotalDays} ngay {duration.Hours} gio";
            }

            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours} gio {duration.Minutes} phut";
            }

            return $"{Math.Max(duration.Minutes, 0)} phut";
        }
    }
}
