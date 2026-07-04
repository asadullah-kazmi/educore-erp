using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
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
        public string StaffType { get; set; }
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

        public StaffViewModel()
        {
            DownloadDocumentCommand = new RelayCommand(DownloadDocument);
        }

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
        public ICommand DownloadDocumentCommand { get; }

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
                StaffType = teacher.StaffType,
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
                StaffType = StaffType,
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

        private void DownloadDocument(object parameter)
        {
            var documentKey = parameter as string;
            var document = GetDocument(documentKey);

            if (document == null || !document.HasContent)
            {
                MessageBox.Show("No document is available to download.", "Download Document", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Download document",
                FileName = document.FileName,
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                if (document.Data != null && document.Data.Length > 0)
                {
                    File.WriteAllBytes(dialog.FileName, document.Data);
                }
                else
                {
                    File.Copy(document.Path, dialog.FileName, true);
                }

                MessageBox.Show("Document downloaded successfully.", "Download Document", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (IOException ex)
            {
                MessageBox.Show("Unable to download document: " + ex.Message, "Download Document", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("Unable to download document: " + ex.Message, "Download Document", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DocumentDownloadInfo GetDocument(string documentKey)
        {
            switch (documentKey)
            {
                case "CnicFront":
                    return new DocumentDownloadInfo(CnicFrontImageData, CnicFrontImageFileName, CnicFrontImagePath);
                case "CnicBack":
                    return new DocumentDownloadInfo(CnicBackImageData, CnicBackImageFileName, CnicBackImagePath);
                case "EducationalDocuments":
                    return new DocumentDownloadInfo(EducationalDocumentsData, EducationalDocumentsFileName, EducationalDocumentsPath);
                case "Certificates":
                    return new DocumentDownloadInfo(CertificatesData, CertificatesFileName, CertificatesPath);
                default:
                    return null;
            }
        }

        private class DocumentDownloadInfo
        {
            public DocumentDownloadInfo(byte[] data, string fileName, string path)
            {
                Data = data;
                Path = path;
                FileName = GetDownloadFileName(fileName, path);
            }

            public byte[] Data { get; }

            public string Path { get; }

            public string FileName { get; }

            public bool HasContent =>
                (Data != null && Data.Length > 0) ||
                (!string.IsNullOrWhiteSpace(Path) && File.Exists(Path));

            private static string GetDownloadFileName(string fileName, string path)
            {
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    return fileName;
                }

                if (!string.IsNullOrWhiteSpace(path))
                {
                    return System.IO.Path.GetFileName(path);
                }

                return "document";
            }
        }
    }
}
