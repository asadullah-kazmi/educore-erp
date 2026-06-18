using System;
using SchoolERP.Models;

namespace SchoolERP.ViewModels
{
    public class ExpenseViewModel
    {
        public int ExpenseID { get; set; }
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Notes { get; set; }

        public string AmountDisplay => Amount.ToString("N0");

        public string DateDisplay => Date.ToString("dd MMM yyyy");

        public static ExpenseViewModel FromModel(Expense e)
        {
            if (e == null)
            {
                return null;
            }

            return new ExpenseViewModel
            {
                ExpenseID = e.ExpenseID,
                Category = e.Category,
                Amount = e.Amount,
                Date = e.Date,
                Notes = e.Notes
            };
        }

        public Expense ToModel()
        {
            return new Expense
            {
                ExpenseID = ExpenseID,
                Category = Category,
                Amount = Amount,
                Date = Date,
                Notes = Notes
            };
        }
    }
}
