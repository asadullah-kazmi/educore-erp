using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using SchoolERP.Models;

namespace SchoolERP.Services
{
    public static class ReportPrintService
    {
        public static void PrintFeeCollectionReport(IList<FeeCollectionReportRow> rows, string month)
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true)
            {
                return;
            }

            var document = BuildDocument(
                "Fee Collection Report — " + month,
                new[] { "Student", "Reg No", "Class", "Section", "Monthly Fee", "Paid", "Status", "Payment Date" },
                rows.Select(r => new[]
                {
                    r.StudentName ?? string.Empty,
                    r.RegistrationNo ?? string.Empty,
                    r.ClassName ?? string.Empty,
                    r.Section ?? string.Empty,
                    r.MonthlyFee.ToString("N0"),
                    r.AmountPaid.ToString("N0"),
                    r.Status ?? string.Empty,
                    r.PaymentDate?.ToString("dd MMM yyyy") ?? "—"
                }));

            printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Fee Collection Report");
        }

        public static void PrintExpenseReport(IList<Expense> expenses, string month)
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true)
            {
                return;
            }

            var document = BuildDocument(
                "Expense Report — " + month,
                new[] { "Category", "Amount", "Date", "Notes" },
                expenses.Select(e => new[]
                {
                    e.Category ?? string.Empty,
                    e.Amount.ToString("N0"),
                    e.Date.ToString("dd MMM yyyy"),
                    e.Notes ?? string.Empty
                }));

            printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Expense Report");
        }

        public static void PrintAttendanceReport(IList<AttendanceSummaryRow> rows, string title)
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true)
            {
                return;
            }

            var document = BuildDocument(
                title,
                new[] { "Name", "Present Days", "Absent Days", "Total Days", "Attendance %" },
                rows.Select(r => new[]
                {
                    r.Name ?? string.Empty,
                    r.PresentDays.ToString(),
                    r.AbsentDays.ToString(),
                    r.TotalDays.ToString(),
                    r.AttendancePercent.ToString("0.0") + "%"
                }));

            printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, title);
        }

        public static void PrintSalaryReport(IList<SalaryPayment> rows, string month)
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true)
            {
                return;
            }

            var document = BuildDocument(
                "Salary Payment Report — " + month,
                new[] { "Teacher", "Designation", "Base Salary", "Amount Paid", "Payment Date", "Notes" },
                rows.Select(r => new[]
                {
                    r.TeacherName ?? string.Empty,
                    r.Designation ?? string.Empty,
                    r.BaseSalary.ToString("N0"),
                    r.Amount.ToString("N0"),
                    r.PaymentDate.ToString("dd MMM yyyy"),
                    r.Notes ?? string.Empty
                }));

            printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Salary Payment Report");
        }

        public static void PrintFinanceSummaryReport(MonthlyFinanceSummary summary)
        {
            if (summary == null)
            {
                return;
            }

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true)
            {
                return;
            }

            var document = BuildDocument(
                "Finance Summary Report - " + summary.Month,
                new[] { "Item", "Amount" },
                new[]
                {
                    new[] { "Fees Collected", summary.TotalFeesCollected.ToString("N0") },
                    new[] { "Total Expenses", summary.TotalExpenses.ToString("N0") },
                    new[] { "Salaries Paid", summary.TotalSalariesPaid.ToString("N0") },
                    new[] { "Net Surplus", summary.NetSurplus.ToString("N0") }
                });

            printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Finance Summary Report");
        }

        private static FlowDocument BuildDocument(string title, string[] headers, IEnumerable<string[]> rows)
        {
            var document = new FlowDocument
            {
                PagePadding = new Thickness(50),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12
            };

            document.Blocks.Add(new Paragraph(new Run(title))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 16)
            });

            var table = new Table { CellSpacing = 0 };
            for (int i = 0; i < headers.Length; i++)
            {
                table.Columns.Add(new TableColumn());
            }

            var headerGroup = new TableRowGroup();
            var headerRow = new TableRow { Background = Brushes.LightGray, FontWeight = FontWeights.Bold };
            foreach (var header in headers)
            {
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run(header))) { Padding = new Thickness(6) });
            }
            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            var bodyGroup = new TableRowGroup();
            foreach (var row in rows)
            {
                var tableRow = new TableRow();
                foreach (var cell in row)
                {
                    tableRow.Cells.Add(new TableCell(new Paragraph(new Run(cell))) { Padding = new Thickness(6) });
                }
                bodyGroup.Rows.Add(tableRow);
            }
            table.RowGroups.Add(bodyGroup);

            document.Blocks.Add(table);
            return document;
        }
    }
}
