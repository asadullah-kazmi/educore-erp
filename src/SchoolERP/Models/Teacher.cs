namespace SchoolERP.Models
{
    public class Teacher
    {
        public int TeacherID { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public decimal Salary { get; set; }
        public int? FingerprintID { get; set; }
    }
}
