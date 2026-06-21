using System;
using System.Linq;
using SchoolERP.Services;

namespace SchoolERP.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private string welcomeMessage;
        private bool canSeeStudents;
        private bool canSeeFees;
        private bool canSeeAttendance;
        private bool canSeeReports;

        public MainViewModel()
        {
            var session = AppSession.Current;

            if (session != null && session.IsAuthenticated)
            {
                var displayName = string.IsNullOrWhiteSpace(session.FullName) ? session.Username : session.FullName;
                var roles = session.Roles == null || session.Roles.Count == 0
                    ? string.Empty
                    : " (" + string.Join(", ", session.Roles) + ")";

                WelcomeMessage = "Welcome, " + displayName + roles;

                // Set role-based visibility
                SetRoleBasedVisibility(session.Roles);
            }
            else
            {
                WelcomeMessage = "Welcome to School ERP";
            }
        }

        public string WelcomeMessage
        {
            get => welcomeMessage;
            set => SetProperty(ref welcomeMessage, value);
        }

        public bool CanSeeStudents
        {
            get => canSeeStudents;
            set => SetProperty(ref canSeeStudents, value);
        }

        public bool CanSeeFees
        {
            get => canSeeFees;
            set => SetProperty(ref canSeeFees, value);
        }

        public bool CanSeeAttendance
        {
            get => canSeeAttendance;
            set => SetProperty(ref canSeeAttendance, value);
        }

        public bool CanSeeReports
        {
            get => canSeeReports;
            set => SetProperty(ref canSeeReports, value);
        }

        public NavigationViewModel Navigation { get; } = new NavigationViewModel();

        private void SetRoleBasedVisibility(System.Collections.Generic.IReadOnlyList<string> roles)
        {
            var isAdmin = roles != null && roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
            var isReceptionist = roles != null && roles.Contains("Receptionist", StringComparer.OrdinalIgnoreCase);
            var isAccountant = roles != null && roles.Contains("Accountant", StringComparer.OrdinalIgnoreCase);
            var isTeacher = roles != null && roles.Contains("Teacher", StringComparer.OrdinalIgnoreCase);

            CanSeeStudents = isAdmin || isReceptionist;
            CanSeeFees = isAdmin || isAccountant;
            CanSeeAttendance = isAdmin || isTeacher;
            CanSeeReports = isAdmin;
        }
    }
}
