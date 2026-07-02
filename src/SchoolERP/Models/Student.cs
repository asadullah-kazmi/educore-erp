using System;

namespace SchoolERP.Models
{
    public class Student
    {
        public int StudentID { get; set; }
        public string RegistrationNo { get; set; }
        public string Name { get; set; }
        public string FatherName { get; set; }
        public DateTime? DOB { get; set; }
        public int? ClassID { get; set; }
        public string ClassName { get; set; }
        public string Section { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string StudentFormBOrCnicNumber { get; set; }
        public string StudentFormBOrCnicPicturePath { get; set; }
        public string StudentFormBOrCnicFrontPicturePath { get; set; }
        public string StudentFormBOrCnicBackPicturePath { get; set; }
        public string GuardianCnicNumber { get; set; }
        public string GuardianCnicPicturePath { get; set; }
        public string GuardianCnicFrontPicturePath { get; set; }
        public string GuardianCnicBackPicturePath { get; set; }
        public string GuardianPhone { get; set; }
        public string EmergencyContactNumber { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public decimal MonthlyFee { get; set; }
    }
}
