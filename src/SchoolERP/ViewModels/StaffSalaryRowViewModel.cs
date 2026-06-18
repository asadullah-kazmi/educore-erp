using System;

namespace SchoolERP.ViewModels
{
    public class StaffSalaryRowViewModel : ViewModelBase
    {
        private int teacherId;
        private string name;
        private string designation;
        private decimal baseSalary;
        private bool paidThisMonth;
        private DateTime? lastPaymentDate;

        public int TeacherID
        {
            get => teacherId;
            set => SetProperty(ref teacherId, value);
        }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public string Designation
        {
            get => designation;
            set => SetProperty(ref designation, value);
        }

        public decimal BaseSalary
        {
            get => baseSalary;
            set => SetProperty(ref baseSalary, value);
        }

        public bool PaidThisMonth
        {
            get => paidThisMonth;
            set => SetProperty(ref paidThisMonth, value);
        }

        public DateTime? LastPaymentDate
        {
            get => lastPaymentDate;
            set => SetProperty(ref lastPaymentDate, value);
        }

        public string BaseSalaryDisplay => BaseSalary.ToString("N0");

        public string LastPaymentDateDisplay => LastPaymentDate?.ToString("dd MMM yyyy") ?? "—";

        public string PaidStatusLabel => PaidThisMonth ? "Paid" : "Due";

        public string PaidStatusColor => PaidThisMonth ? "#10B981" : "#FF6B35";
    }
}
