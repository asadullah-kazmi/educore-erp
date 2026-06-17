using System;
using SchoolERP.Models;

namespace SchoolERP.ViewModels
{
    public class StudentViewModel : ViewModelBase
    {
        public int StudentID { get; set; }
        public string RegistrationNo { get; set; }
        public string Name { get; set; }
        public string FatherName { get; set; }
        public DateTime? DOB { get; set; }
        public int? ClassID { get; set; }
        public string ClassName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public decimal MonthlyFee { get; set; }

        public string AdmissionDateDisplay =>
            AdmissionDate.HasValue ? AdmissionDate.Value.ToString("dd MMM yyyy") : string.Empty;

        public static StudentViewModel FromModel(Student student)
        {
            if (student == null)
            {
                return null;
            }

            return new StudentViewModel
            {
                StudentID = student.StudentID,
                RegistrationNo = student.RegistrationNo,
                Name = student.Name,
                FatherName = student.FatherName,
                DOB = student.DOB,
                ClassID = student.ClassID,
                ClassName = student.ClassName,
                Address = student.Address,
                Phone = student.Phone,
                AdmissionDate = student.AdmissionDate,
                MonthlyFee = student.MonthlyFee
            };
        }

        public Student ToModel()
        {
            return new Student
            {
                StudentID = StudentID,
                RegistrationNo = RegistrationNo,
                Name = Name,
                FatherName = FatherName,
                DOB = DOB,
                ClassID = ClassID,
                ClassName = ClassName,
                Address = Address,
                Phone = Phone,
                AdmissionDate = AdmissionDate,
                MonthlyFee = MonthlyFee
            };
        }
    }
}
