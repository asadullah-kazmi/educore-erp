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
                Section = student.Section,
                Address = student.Address,
                Phone = student.Phone,
                StudentFormBOrCnicNumber = student.StudentFormBOrCnicNumber,
                StudentFormBOrCnicPicturePath = student.StudentFormBOrCnicPicturePath,
                StudentFormBOrCnicFrontPicturePath = student.StudentFormBOrCnicFrontPicturePath,
                StudentFormBOrCnicBackPicturePath = student.StudentFormBOrCnicBackPicturePath,
                GuardianCnicNumber = student.GuardianCnicNumber,
                GuardianCnicPicturePath = student.GuardianCnicPicturePath,
                GuardianCnicFrontPicturePath = student.GuardianCnicFrontPicturePath,
                GuardianCnicBackPicturePath = student.GuardianCnicBackPicturePath,
                GuardianPhone = student.GuardianPhone,
                EmergencyContactNumber = student.EmergencyContactNumber,
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
                Section = Section,
                Address = Address,
                Phone = Phone,
                StudentFormBOrCnicNumber = StudentFormBOrCnicNumber,
                StudentFormBOrCnicPicturePath = StudentFormBOrCnicPicturePath,
                StudentFormBOrCnicFrontPicturePath = StudentFormBOrCnicFrontPicturePath,
                StudentFormBOrCnicBackPicturePath = StudentFormBOrCnicBackPicturePath,
                GuardianCnicNumber = GuardianCnicNumber,
                GuardianCnicPicturePath = GuardianCnicPicturePath,
                GuardianCnicFrontPicturePath = GuardianCnicFrontPicturePath,
                GuardianCnicBackPicturePath = GuardianCnicBackPicturePath,
                GuardianPhone = GuardianPhone,
                EmergencyContactNumber = EmergencyContactNumber,
                AdmissionDate = AdmissionDate,
                MonthlyFee = MonthlyFee
            };
        }
    }
}
