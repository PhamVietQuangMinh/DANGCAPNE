using System.Collections.Generic;
using DANGCAPNE.Models.Security;
using DANGCAPNE.Models.Organization;
using DANGCAPNE.Models.AdminOps; // Assume AssetIncident here

namespace DANGCAPNE.ViewModels
{
    public class ITDashboardViewModel
    {
        public List<AuthAuditLog> RecentAuthLogs { get; set; } = new();
        public List<object> OnlineSessions { get; set; } = new(); // Simplified
        public List<User> PendingWhitelist { get; set; } = new();
        public List<object> AssetIncidents { get; set; } = new();
    }
}


