using System;

namespace SchoolERP.Models
{
    public class AttendanceSummaryRow
    {
        public string Name { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int TotalDays { get; set; }
        public decimal AttendancePercent => TotalDays == 0 ? 0 : Math.Round((decimal)PresentDays / TotalDays * 100, 1);
        public string AttendanceColor => AttendancePercent >= 75 ? "#10B981" : "#EF4444";
    }
}
