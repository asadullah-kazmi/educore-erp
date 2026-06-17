using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        }

        public StudentViewModel Student { get; }

        public ObservableCollection<FeeRecord> FeeHistory { get; }

        public decimal TotalPaid { get; }

        public decimal TotalDue { get; }

        public string SummaryText =>
            $"Total Paid: Rs {TotalPaid:N0}    |    Total Due: Rs {TotalDue:N0}";
    }
}
