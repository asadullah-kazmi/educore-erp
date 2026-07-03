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

        private static bool CanDownloadImage(string imagePath)
        {
            return !string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath);
        }

        private static void DownloadImage(object parameter)
        {
            var imagePath = parameter as string;

            if (!CanDownloadImage(imagePath))
            {
                MessageBox.Show("The selected image file could not be found.", "Download Image", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Download image",
                FileName = Path.GetFileName(imagePath),
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                File.Copy(imagePath, dialog.FileName, true);
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
    }
}
