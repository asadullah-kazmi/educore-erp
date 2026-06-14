using System.Windows;

namespace SchoolERP.Controls
{
    public partial class ConfirmationDialog : Window
    {
        public ConfirmationDialog(string message, string title)
        {
            InitializeComponent();
            Title = title;
            MessageTextBlock.Text = message;
        }

        public static bool Show(string message, string title = "Confirm Delete", Window owner = null)
        {
            var dialog = new ConfirmationDialog(message, title);

            if (owner != null)
            {
                dialog.Owner = owner;
            }
            else if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded)
            {
                dialog.Owner = Application.Current.MainWindow;
            }

            return dialog.ShowDialog() == true;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
