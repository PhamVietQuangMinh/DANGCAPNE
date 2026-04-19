namespace DANGCAPNE.ViewModels;

public class ShiftCalendarDayViewModel
{
    public DateTime Date { get; set; }
    public bool IsToday { get; set; }
    public string? ShiftName { get; set; }
    public TimeSpan? ShiftStart { get; set; }
    public TimeSpan? ShiftEnd { get; set; }
    public string? AttendanceStatus { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
}

public class ShiftCalendarViewModel
{
    public string UserFullName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }

    public DateTime MonthStart { get; set; }
    public List<ShiftCalendarDayViewModel> Days { get; set; } = new();

    public int WorkingDays { get; set; }
    public double OtHours { get; set; }

    public int PrevYear { get; set; }
    public int PrevMonth { get; set; }
    public int NextYear { get; set; }
    public int NextMonth { get; set; }
}

