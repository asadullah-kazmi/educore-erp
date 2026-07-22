using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        public byte[] StudentFormBOrCnicFrontPictureData { get; set; }
        public string StudentFormBOrCnicFrontPictureFileName { get; set; }
        public string StudentFormBOrCnicBackPicturePath { get; set; }
        public byte[] StudentFormBOrCnicBackPictureData { get; set; }
        public string StudentFormBOrCnicBackPictureFileName { get; set; }
        public string GuardianCnicNumber { get; set; }
        public string GuardianCnicPicturePath { get; set; }
        public string GuardianCnicFrontPicturePath { get; set; }
        public byte[] GuardianCnicFrontPictureData { get; set; }
        public string GuardianCnicFrontPictureFileName { get; set; }
        public string GuardianCnicBackPicturePath { get; set; }
        public byte[] GuardianCnicBackPictureData { get; set; }
        public string GuardianCnicBackPictureFileName { get; set; }
        public string GuardianPhone { get; set; }
        public string EmergencyContactNumber { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public decimal MonthlyFee { get; set; }
        public bool IsActive { get; set; } = true;

        public string AdmissionDateDisplay =>
            AdmissionDate.HasValue ? AdmissionDate.Value.ToString("dd MMM yyyy") : string.Empty;
        public ImageSource StudentFormBOrCnicFrontImage => CreateImage(StudentFormBOrCnicFrontPictureData, StudentFormBOrCnicFrontPictureFileName, StudentFormBOrCnicFrontPicturePath);
        public ImageSource StudentFormBOrCnicBackImage => CreateImage(StudentFormBOrCnicBackPictureData, StudentFormBOrCnicBackPictureFileName, StudentFormBOrCnicBackPicturePath);
        public ImageSource GuardianCnicFrontImage => CreateImage(GuardianCnicFrontPictureData, GuardianCnicFrontPictureFileName, GuardianCnicFrontPicturePath);
        public ImageSource GuardianCnicBackImage => CreateImage(GuardianCnicBackPictureData, GuardianCnicBackPictureFileName, GuardianCnicBackPicturePath);
        public string StudentFormBOrCnicFrontStoredLabel => GetStoredLabel(StudentFormBOrCnicFrontPictureData, StudentFormBOrCnicFrontPictureFileName, StudentFormBOrCnicFrontPicturePath);
        public string StudentFormBOrCnicBackStoredLabel => GetStoredLabel(StudentFormBOrCnicBackPictureData, StudentFormBOrCnicBackPictureFileName, StudentFormBOrCnicBackPicturePath);
        public string GuardianCnicFrontStoredLabel => GetStoredLabel(GuardianCnicFrontPictureData, GuardianCnicFrontPictureFileName, GuardianCnicFrontPicturePath);
        public string GuardianCnicBackStoredLabel => GetStoredLabel(GuardianCnicBackPictureData, GuardianCnicBackPictureFileName, GuardianCnicBackPicturePath);

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
                StudentFormBOrCnicFrontPictureData = student.StudentFormBOrCnicFrontPictureData,
                StudentFormBOrCnicFrontPictureFileName = student.StudentFormBOrCnicFrontPictureFileName,
                StudentFormBOrCnicBackPicturePath = student.StudentFormBOrCnicBackPicturePath,
                StudentFormBOrCnicBackPictureData = student.StudentFormBOrCnicBackPictureData,
                StudentFormBOrCnicBackPictureFileName = student.StudentFormBOrCnicBackPictureFileName,
                GuardianCnicNumber = student.GuardianCnicNumber,
                GuardianCnicPicturePath = student.GuardianCnicPicturePath,
                GuardianCnicFrontPicturePath = student.GuardianCnicFrontPicturePath,
                GuardianCnicFrontPictureData = student.GuardianCnicFrontPictureData,
                GuardianCnicFrontPictureFileName = student.GuardianCnicFrontPictureFileName,
                GuardianCnicBackPicturePath = student.GuardianCnicBackPicturePath,
                GuardianCnicBackPictureData = student.GuardianCnicBackPictureData,
                GuardianCnicBackPictureFileName = student.GuardianCnicBackPictureFileName,
                GuardianPhone = student.GuardianPhone,
                EmergencyContactNumber = student.EmergencyContactNumber,
                AdmissionDate = student.AdmissionDate,
                MonthlyFee = student.MonthlyFee,
                IsActive = student.IsActive
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
                StudentFormBOrCnicFrontPictureData = StudentFormBOrCnicFrontPictureData,
                StudentFormBOrCnicFrontPictureFileName = StudentFormBOrCnicFrontPictureFileName,
                StudentFormBOrCnicBackPicturePath = StudentFormBOrCnicBackPicturePath,
                StudentFormBOrCnicBackPictureData = StudentFormBOrCnicBackPictureData,
                StudentFormBOrCnicBackPictureFileName = StudentFormBOrCnicBackPictureFileName,
                GuardianCnicNumber = GuardianCnicNumber,
                GuardianCnicPicturePath = GuardianCnicPicturePath,
                GuardianCnicFrontPicturePath = GuardianCnicFrontPicturePath,
                GuardianCnicFrontPictureData = GuardianCnicFrontPictureData,
                GuardianCnicFrontPictureFileName = GuardianCnicFrontPictureFileName,
                GuardianCnicBackPicturePath = GuardianCnicBackPicturePath,
                GuardianCnicBackPictureData = GuardianCnicBackPictureData,
                GuardianCnicBackPictureFileName = GuardianCnicBackPictureFileName,
                GuardianPhone = GuardianPhone,
                EmergencyContactNumber = EmergencyContactNumber,
                AdmissionDate = AdmissionDate,
                MonthlyFee = MonthlyFee,
                IsActive = IsActive
            };
        }

        private static ImageSource CreateImage(byte[] data, string fileName, string legacyPath)
        {
            try
            {
                if (data != null && data.Length > 0 && IsImageFile(fileName))
                {
                    using (var stream = new MemoryStream(data))
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                        image.Freeze();
                        return image;
                    }
                }

                if (!string.IsNullOrWhiteSpace(legacyPath) && File.Exists(legacyPath))
                {
                    return new BitmapImage(new Uri(legacyPath, UriKind.Absolute));
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static bool IsImageFile(string fileName)
        {
            var extension = Path.GetExtension(fileName ?? string.Empty);
            return string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetStoredLabel(byte[] data, string fileName, string legacyPath)
        {
            if (data != null && data.Length > 0)
            {
                return string.IsNullOrWhiteSpace(fileName)
                    ? "Stored in database"
                    : "Stored in database: " + fileName;
            }

            return string.IsNullOrWhiteSpace(legacyPath)
                ? "-"
                : "Path only: " + legacyPath;
        }
    }
}
