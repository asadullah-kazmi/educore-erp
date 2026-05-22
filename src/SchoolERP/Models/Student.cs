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
        public string Class { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public DateTime? AdmissionDate { get; set; }
    }
}
