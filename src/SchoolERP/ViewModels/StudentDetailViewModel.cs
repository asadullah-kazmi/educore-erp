using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SchoolERP.Models;

namespace SchoolERP.ViewModels
{
    public class StudentDetailViewModel : ObservableObject
    {
        public StudentDetailViewModel(StudentViewModel student, List<FeeRecord> fees)
        {
            Student = student;
            FeeHistory = new ObservableCollection<FeeRecord>(fees ?? new List<FeeRecord>());
            TotalPaid = FeeHistory.Where(f => f.Status == "Paid").Sum(f => f.Amount);
            TotalDue = FeeHistory.Where(f => f.Status == "Due").Sum(f => f.Amount);
            DownloadImageCommand = new RelayCommand(DownloadImage);
        }

        public StudentViewModel Student { get; }

        public ObservableCollection<FeeRecord> FeeHistory { get; }

        public decimal TotalPaid { get; }

        public decimal TotalDue { get; }

        public ICommand DownloadImageCommand { get; }

        public string SummaryText =>
            $"Total Paid: Rs {TotalPaid:N0}    |    Total Due: Rs {TotalDue:N0}";

        private void DownloadImage(object parameter)
        {
            var image = GetImageDownloadInfo(parameter as string);

            if (image == null || !image.HasContent)
            {
                MessageBox.Show("The selected image file could not be found.", "Download Image", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Download image",
                FileName = image.FileName,
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                if (image.Data != null && image.Data.Length > 0)
                {
                    File.WriteAllBytes(dialog.FileName, image.Data);
                }
                else
                {
                    File.Copy(image.Path, dialog.FileName, true);
                }

                MessageBox.Show("Image downloaded successfully.", "Download Image", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (IOException ex)
            {
                MessageBox.Show("Unable to download image: " + ex.Message, "Download Image", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("Unable to download image: " + ex.Message, "Download Image", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ImageDownloadInfo GetImageDownloadInfo(string imageKey)
        {
            switch (imageKey)
            {
                case "StudentFront":
                    return new ImageDownloadInfo(Student.StudentFormBOrCnicFrontPictureData, Student.StudentFormBOrCnicFrontPictureFileName, Student.StudentFormBOrCnicFrontPicturePath);
                case "StudentBack":
                    return new ImageDownloadInfo(Student.StudentFormBOrCnicBackPictureData, Student.StudentFormBOrCnicBackPictureFileName, Student.StudentFormBOrCnicBackPicturePath);
                case "GuardianFront":
                    return new ImageDownloadInfo(Student.GuardianCnicFrontPictureData, Student.GuardianCnicFrontPictureFileName, Student.GuardianCnicFrontPicturePath);
                case "GuardianBack":
                    return new ImageDownloadInfo(Student.GuardianCnicBackPictureData, Student.GuardianCnicBackPictureFileName, Student.GuardianCnicBackPicturePath);
                default:
                    return null;
            }
        }

        private class ImageDownloadInfo
        {
            public ImageDownloadInfo(byte[] data, string fileName, string path)
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

                return "image";
            }
        }
    }
}
