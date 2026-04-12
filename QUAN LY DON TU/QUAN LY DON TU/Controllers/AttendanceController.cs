using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;
using DANGCAPNE.Models.Timekeeping;
using System.Net;

namespace DANGCAPNE.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId);
            ViewBag.HasFaceRegistered = !string.IsNullOrEmpty(user?.FaceDescriptorFront);
            ViewBag.FaceDescriptorFront = user?.FaceDescriptorFront;
            ViewBag.AvatarUrl = user?.AvatarUrl;

            var today = DateTime.Today;
            var timesheet = await _context.Timesheets
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Date == today);

            ViewBag.IsTodayCompleted = timesheet?.CheckOut != null;
            ViewBag.IsCheckInDone = timesheet?.CheckIn != null && timesheet?.CheckOut == null;
            ViewBag.CheckInTime = timesheet?.CheckIn?.ToString("HH:mm:ss");
            ViewBag.IsIntranet = IsInternalNetwork(HttpContext.Connection.RemoteIpAddress);
            ViewBag.ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            return View();
        }

        public async Task<IActionResult> Timeline()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var history = await _context.Timesheets
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .Take(30)
                .ToListAsync();

            return View(history);
        }

        public IActionResult RegisterFace()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            return View();
        }

        public async Task<IActionResult> AdminReport(DateTime? fromDate, DateTime? toDate, int? deptId, int? searchUserId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            bool isAdmin = roles.Contains("Admin");
            bool isHR = roles.Contains("HR");
            if (!isAdmin && !isHR) return RedirectToAction("Index");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var from = fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var to = toDate ?? DateTime.Today;

            var query = _context.Timesheets
                .Include(t => t.User).ThenInclude(u => u!.Department)
                .Where(t => t.User!.TenantId == tenantId && t.Date >= from && t.Date <= to);

            if (deptId.HasValue)
                query = query.Where(t => t.User!.DepartmentId == deptId);

            if (searchUserId.HasValue)
                query = query.Where(t => t.UserId == searchUserId);

            var timesheets = await query
                .OrderByDescending(t => t.Date)
                .ThenBy(t => t.User!.FullName)
                .ToListAsync();

            var departments = await _context.Departments
                .Where(d => d.TenantId == tenantId)
                .ToListAsync();

            var employees = await _context.Users
                .Where(u => u.TenantId == tenantId && u.Status == "Active")
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.Timesheets = timesheets;
            ViewBag.Departments = departments;
            ViewBag.Employees = employees;
            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");
            ViewBag.SelectedDeptId = deptId;
            ViewBag.SelectedUserId = searchUserId;
            ViewBag.TotalPresent = timesheets.Count;
            ViewBag.TotalWorkHours = timesheets.Sum(t => t.WorkHours);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveFaceDescriptorFront(string descriptor)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Phien het han" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Json(new { success = false, message = "User khong ton tai" });

            user.FaceDescriptorFront = descriptor;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Dang ky khuon mat thanh cong" });
        }

        [HttpPost]
        public async Task<IActionResult> CheckIn(double? lat, double? lon, string? wifiName, string? wifiBssid, string? qrCode, string? photoBase64, bool? faceMatched)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Phien dang nhap het han" });

            if (!IsInternalNetwork(HttpContext.Connection.RemoteIpAddress))
                return Json(new { success = false, message = "Ban dang o ngoai mang noi bo, khong the cham cong." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Json(new { success = false, message = "Khong tim thay nguoi dung" });

            if (!string.IsNullOrEmpty(user.FaceDescriptorFront) && faceMatched != true)
                return Json(new { success = false, message = "Xac thuc khuon mat that bai." });

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var config = await _context.AttendanceLocationConfigs
                .FirstOrDefaultAsync(c => c.BranchId == user.BranchId && c.IsActive);

            string source = "FaceRecognition";

            if (config != null && !string.IsNullOrEmpty(config.QrCodeKey))
            {
                if (qrCode != config.QrCodeKey)
                    return Json(new { success = false, message = "Ma QR khong hop le" });
                source = "QRCode";
            }
            if (config != null && !string.IsNullOrEmpty(config.WifiBssid))
            {
                if (wifiBssid != config.WifiBssid)
                    return Json(new { success = false, message = "Vui long ket noi dung Wifi van phong" });
                source = "Wifi";
            }
            if (config != null && config.AllowedLatitude.HasValue && lat.HasValue)
            {
                double distance = CalculateDistance(lat.Value, lon!.Value, config.AllowedLatitude.Value, config.AllowedLongitude!.Value);
                if (distance > config.AllowedRadiusMeters)
                    return Json(new { success = false, message = $"Ban dang o qua xa van phong ({Math.Round(distance)}m)" });
                source = "GPS";
            }

            var today = DateTime.Today;
            var timesheet = await _context.Timesheets
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Date == today);

            if (timesheet == null)
            {
                timesheet = new Timesheet
                {
                    TenantId = tenantId,
                    UserId = userId.Value,
                    Date = today,
                    CheckIn = DateTime.UtcNow,
                    Source = source,
                    GpsLatitude = lat,
                    GpsLongitude = lon,
                    WifiName = wifiName,
                    WifiBssid = wifiBssid,
                    QrCodeKey = qrCode,
                    PhotoUrl = photoBase64,
                    Status = "Present"
                };
                _context.Timesheets.Add(timesheet);
                await _context.SaveChangesAsync();
                return Json(new { success = true, type = "checkin", message = "Vao thanh cong", time = DateTime.UtcNow.ToString("HH:mm:ss") });
            }

            if (timesheet.CheckOut == null)
            {
                timesheet.CheckOut = DateTime.UtcNow;
                var workHours = (timesheet.CheckOut.Value - timesheet.CheckIn!.Value).TotalHours;
                timesheet.WorkHours = Math.Round(Math.Max(workHours, 0), 2);

                var now = DateTime.UtcNow;
                var targetCheckOut = new DateTime(now.Year, now.Month, now.Day, 18, 0, 0, DateTimeKind.Utc);

                if (now < targetCheckOut)
                {
                    timesheet.Status = "EarlyLeave";
                }
                else if (timesheet.Status != "Late")
                {
                    timesheet.Status = "Present";
                }

                timesheet.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true, type = "checkout", message = "Ra thanh cong", time = DateTime.UtcNow.ToString("HH:mm:ss") });
            }

            return Json(new { success = false, message = "Ban da hoan thanh du 2 lan cham cong hom nay." });
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371e3;
            var p1 = lat1 * Math.PI / 180;
            var p2 = lat2 * Math.PI / 180;
            var dp = (lat2 - lat1) * Math.PI / 180;
            var dl = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(dp / 2) * Math.Sin(dp / 2) +
                    Math.Cos(p1) * Math.Cos(p2) *
                    Math.Sin(dl / 2) * Math.Sin(dl / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private static bool IsInternalNetwork(IPAddress? ip)
        {
            if (ip == null)
                return false;

            if (IPAddress.IsLoopback(ip))
                return true;

            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                if (bytes.Length == 4)
                {
                    if (bytes[0] == 10)
                        return true;

                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                        return true;

                    if (bytes[0] == 192 && bytes[1] == 168)
                        return true;
                }
            }

            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                var text = ip.ToString();
                if (text.StartsWith("fc", StringComparison.OrdinalIgnoreCase) || text.StartsWith("fd", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
