using System.Collections.Generic;

namespace SchoolERP.Models
{
    public class AttendanceSummary
    {
        public string Month { get; set; }
        public int Year { get; set; }
        public List<TeacherAttendanceSummary> Rows { get; set; }

        public AttendanceSummary()
        {
            Rows = new List<TeacherAttendanceSummary>();
        }
    }
}
