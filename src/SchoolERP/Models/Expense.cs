using System;

namespace SchoolERP.Models
{
    public class Expense
    {
        public int ExpenseID { get; set; }
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Notes { get; set; }
    }
}
