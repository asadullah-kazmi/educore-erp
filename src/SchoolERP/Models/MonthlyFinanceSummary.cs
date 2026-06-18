namespace SchoolERP.Models
{
    public class MonthlyFinanceSummary
    {
        public string Month { get; set; }
        public decimal TotalFeesCollected { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalSalariesPaid { get; set; }
        public decimal NetSurplus => TotalFeesCollected - TotalExpenses - TotalSalariesPaid;
    }
}
