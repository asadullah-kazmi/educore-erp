using System.Windows;
using System.Windows.Controls;

namespace SchoolERP
{
    public class PageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DashboardTemplate { get; set; }

        public DataTemplate StudentsTemplate { get; set; }

        public DataTemplate FeesTemplate { get; set; }

        public DataTemplate FinanceTemplate { get; set; }

        public DataTemplate AttendanceTemplate { get; set; }

        public DataTemplate StaffTemplate { get; set; }

        public DataTemplate ReportsTemplate { get; set; }

        public DataTemplate ExamSlipsTemplate { get; set; }

        public DataTemplate ReceiptsTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var page = item as string;

            if (page == null)
            {
                return base.SelectTemplate(item, container);
            }

            switch (page)
            {
                case "Students":
                    return StudentsTemplate;
                case "Fees":
                    return FeesTemplate;
                case "Finance":
                    return FinanceTemplate;
                case "Attendance":
                    return AttendanceTemplate;
                case "Staff":
                    return StaffTemplate;
                case "Reports":
                    return ReportsTemplate;
                case "ExamSlips":
                    return ExamSlipsTemplate;
                case "Receipts":
                    return ReceiptsTemplate;
                case "Dashboard":
                default:
                    return DashboardTemplate;
            }
        }
    }
}
