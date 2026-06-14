using System;
using System.Windows.Input;

namespace SchoolERP.ViewModels
{
    public class NavigationViewModel : ObservableObject
    {
        private string currentPage = "Dashboard";

        public NavigationViewModel()
        {
            NavigateCommand = new RelayCommand(
                execute: parameter => CurrentPage = (string)parameter,
                canExecute: parameter => parameter is string);
        }

        public string CurrentPage
        {
            get => currentPage;
            set => SetProperty(ref currentPage, value);
        }

        public ICommand NavigateCommand { get; }
    }
}
