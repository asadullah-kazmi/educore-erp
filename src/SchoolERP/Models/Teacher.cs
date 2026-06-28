namespace SchoolERP.Models
{
    public class Teacher
    {
        public int TeacherID { get; set; }
        public string Name { get; set; }
        public int? Age { get; set; }
        public string Experience { get; set; }
        public System.DateTime? DOB { get; set; }
        public string ContactNumber { get; set; }
        public System.DateTime? DateOfJoining { get; set; }
        public string Designation { get; set; }
        public decimal Salary { get; set; }
        public string Address { get; set; }
        public string CnicNumber { get; set; }
        public string CnicFrontImagePath { get; set; }
        public string CnicBackImagePath { get; set; }
        public string EducationalDocumentsPath { get; set; }
        public string CertificatesPath { get; set; }
        public int? FingerprintID { get; set; }
    }
}
