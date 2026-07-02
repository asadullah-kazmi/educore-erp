using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SchoolERP.Models;

namespace SchoolERP.ViewModels
{
    public class StaffViewModel : ViewModelBase
    {
        public int TeacherID { get; set; }
        public string Name { get; set; }
        public int? Age { get; set; }
        public string Experience { get; set; }
        public DateTime? DOB { get; set; }
        public string ContactNumber { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public string Designation { get; set; }
        public decimal Salary { get; set; }
        public string Address { get; set; }
        public string CnicNumber { get; set; }
        public string CnicFrontImagePath { get; set; }
        public byte[] CnicFrontImageData { get; set; }
        public string CnicFrontImageFileName { get; set; }
        public string CnicBackImagePath { get; set; }
        public byte[] CnicBackImageData { get; set; }
        public string CnicBackImageFileName { get; set; }
        public string EducationalDocumentsPath { get; set; }
        public byte[] EducationalDocumentsData { get; set; }
        public string EducationalDocumentsFileName { get; set; }
        public string CertificatesPath { get; set; }
        public byte[] CertificatesData { get; set; }
        public string CertificatesFileName { get; set; }
        public int? FingerprintId { get; set; }

        public string SalaryDisplay => Salary.ToString("N0");
        public string DOBDisplay => DOB?.ToString("dd MMM yyyy") ?? "-";
        public string DateOfJoiningDisplay => DateOfJoining?.ToString("dd MMM yyyy") ?? "-";
        public ImageSource CnicFrontImage => CreateImage(CnicFrontImageData, CnicFrontImageFileName);
        public ImageSource CnicBackImage => CreateImage(CnicBackImageData, CnicBackImageFileName);
        public ImageSource EducationalDocumentImage => CreateImage(EducationalDocumentsData, EducationalDocumentsFileName);
        public ImageSource CertificateImage => CreateImage(CertificatesData, CertificatesFileName);
        public bool HasCnicFrontImage => CnicFrontImage != null;
        public bool HasCnicBackImage => CnicBackImage != null;
        public bool HasEducationalDocumentImage => EducationalDocumentImage != null;
        public bool HasCertificateImage => CertificateImage != null;
        public string CnicFrontStoredLabel => GetStoredLabel(CnicFrontImageData, CnicFrontImageFileName, CnicFrontImagePath);
        public string CnicBackStoredLabel => GetStoredLabel(CnicBackImageData, CnicBackImageFileName, CnicBackImagePath);
        public string EducationalDocumentStoredLabel => GetStoredLabel(EducationalDocumentsData, EducationalDocumentsFileName, EducationalDocumentsPath);
        public string CertificateStoredLabel => GetStoredLabel(CertificatesData, CertificatesFileName, CertificatesPath);

        public static StaffViewModel FromModel(Teacher teacher)
        {
            if (teacher == null)
            {
                return null;
            }

            return new StaffViewModel
            {
                TeacherID = teacher.TeacherID,
                Name = teacher.Name,
                Age = teacher.Age,
                Experience = teacher.Experience,
                DOB = teacher.DOB,
                ContactNumber = teacher.ContactNumber,
                DateOfJoining = teacher.DateOfJoining,
                Designation = teacher.Designation,
                Salary = teacher.Salary,
                Address = teacher.Address,
                CnicNumber = teacher.CnicNumber,
                CnicFrontImagePath = teacher.CnicFrontImagePath,
                CnicFrontImageData = teacher.CnicFrontImageData,
                CnicFrontImageFileName = teacher.CnicFrontImageFileName,
                CnicBackImagePath = teacher.CnicBackImagePath,
                CnicBackImageData = teacher.CnicBackImageData,
                CnicBackImageFileName = teacher.CnicBackImageFileName,
                EducationalDocumentsPath = teacher.EducationalDocumentsPath,
                EducationalDocumentsData = teacher.EducationalDocumentsData,
                EducationalDocumentsFileName = teacher.EducationalDocumentsFileName,
                CertificatesPath = teacher.CertificatesPath,
                CertificatesData = teacher.CertificatesData,
                CertificatesFileName = teacher.CertificatesFileName,
                FingerprintId = teacher.FingerprintID
            };
        }

        public Teacher ToModel()
        {
            return new Teacher
            {
                TeacherID = TeacherID,
                Name = Name,
                Age = Age,
                Experience = Experience,
                DOB = DOB,
                ContactNumber = ContactNumber,
                DateOfJoining = DateOfJoining,
                Designation = Designation,
                Salary = Salary,
                Address = Address,
                CnicNumber = CnicNumber,
                CnicFrontImagePath = CnicFrontImagePath,
                CnicFrontImageData = CnicFrontImageData,
                CnicFrontImageFileName = CnicFrontImageFileName,
                CnicBackImagePath = CnicBackImagePath,
                CnicBackImageData = CnicBackImageData,
                CnicBackImageFileName = CnicBackImageFileName,
                EducationalDocumentsPath = EducationalDocumentsPath,
                EducationalDocumentsData = EducationalDocumentsData,
                EducationalDocumentsFileName = EducationalDocumentsFileName,
                CertificatesPath = CertificatesPath,
                CertificatesData = CertificatesData,
                CertificatesFileName = CertificatesFileName,
                FingerprintID = FingerprintId
            };
        }

        private static ImageSource CreateImage(byte[] data, string fileName)
        {
            if (data == null || data.Length == 0 || !IsImageFile(fileName))
            {
                return null;
            }

            try
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
            catch
            {
                return null;
            }
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
                    : $"Stored in database: {fileName}";
            }

            return string.IsNullOrWhiteSpace(legacyPath)
                ? "-"
                : "Path only: " + legacyPath;
        }
    }
}
