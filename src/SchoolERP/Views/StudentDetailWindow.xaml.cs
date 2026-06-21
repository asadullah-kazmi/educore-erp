using System.Collections.Generic;
using System.Windows;
using SchoolERP.Models;
using SchoolERP.ViewModels;

namespace SchoolERP.Views
{
    public partial class StudentDetailWindow : Window
    {
        public StudentDetailWindow(StudentViewModel student, List<FeeRecord> fees)
        {
            InitializeComponent();
            DataContext = new StudentDetailViewModel(student, fees);
            Loaded += StudentDetailWindow_Loaded;
        }

        private void StudentDetailWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var workArea = SystemParameters.WorkArea;
            var availableHeight = workArea.Height - 40;
            var availableWidth = workArea.Width - 40;

            if (Height > availableHeight)
            {
                Height = availableHeight;
            }

            if (Width > availableWidth)
            {
                Width = availableWidth;
            }

            MaxHeight = availableHeight;
            MaxWidth = availableWidth;
            Top = workArea.Top + ((workArea.Height - ActualHeight) / 2);
            Left = workArea.Left + ((workArea.Width - ActualWidth) / 2);

            if (Top < workArea.Top + 10)
            {
                Top = workArea.Top + 10;
            }

            if (Left < workArea.Left + 10)
            {
                Left = workArea.Left + 10;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
