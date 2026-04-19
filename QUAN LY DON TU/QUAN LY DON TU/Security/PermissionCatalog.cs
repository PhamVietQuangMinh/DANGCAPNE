using DANGCAPNE.Models.Organization;

namespace DANGCAPNE.Security
{
    public static class PermissionCatalog
    {
        public const string RequestsView = "requests.view";
        public const string RequestsCreate = "requests.create";
        public const string ApprovalsView = "approvals.view";
        public const string ApprovalsAct = "approvals.act";
        public const string DelegationManage = "delegation.manage";
        public const string PolicyView = "policy.view";
        public const string PolicyManage = "policy.manage";
        public const string ChecklistManage = "checklist.manage";
        public const string UserManage = "users.manage";
        public const string AuditView = "audit.view";
        public const string AttendanceAdmin = "attendance.admin";
        public const string ModulesView = "modules.view";
        public const string AccessProvision = "access.provision";

        public static IReadOnlyCollection<string> ResolvePermissions(User user, IReadOnlyCollection<string> roles, string primaryRole)
        {
            var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                RequestsView,
                RequestsCreate,
                PolicyView
            };

            if (roles.Contains("Manager") || roles.Contains("ITManager") || roles.Contains("Admin") || roles.Contains("HR"))
            {
                permissions.Add(ApprovalsView);
                permissions.Add(ApprovalsAct);
            }

            if (roles.Contains("Admin"))
            {
                permissions.Add(DelegationManage);
                permissions.Add(PolicyManage);
                permissions.Add(ChecklistManage);
                permissions.Add(UserManage);
                permissions.Add(AuditView);
                permissions.Add(AttendanceAdmin);
                permissions.Add(ModulesView);
                permissions.Add(AccessProvision);
            }

            if (roles.Contains("HR"))
            {
                permissions.Add(ChecklistManage);
                permissions.Add(PolicyManage);
                permissions.Add(AttendanceAdmin);
            }

            if (roles.Contains("IT") || roles.Contains("ITManager"))
            {
                permissions.Add(ModulesView);
            }

            if (roles.Contains("Manager") || roles.Contains("ITManager"))
            {
                permissions.Add(DelegationManage);
            }

            var isChiefAccountant =
                roles.Contains("ChiefAccountant") ||
                string.Equals(user.Position?.Name, "Kế toán trưởng", StringComparison.OrdinalIgnoreCase);

            if (isChiefAccountant)
            {
                permissions.Add(ApprovalsView);
                permissions.Add(ApprovalsAct);
            }

            return permissions.OrderBy(p => p).ToList();
        }
    }
}
