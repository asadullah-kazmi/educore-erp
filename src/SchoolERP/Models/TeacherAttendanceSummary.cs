namespace SchoolERP.Models
{
    public class TeacherAttendanceSummary
    {
        public int TeacherID { get; set; }
        public string Name { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int NotMarkedDays { get; set; }
        public int TotalWorkingDays { get; set; }
    }
}
