using System;

namespace SchoolERP.Models
{
    public class TeacherAttendanceRow
    {
        public int TeacherID { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public int? FingerprintID { get; set; }
        public int? AttendanceID { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public DateTime? InTime { get; set; }
    }
}
