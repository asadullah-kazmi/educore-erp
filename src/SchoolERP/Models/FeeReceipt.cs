using System;

namespace SchoolERP.Models
{
    public class FeeReceipt
    {
        public int ReceiptID { get; set; }
        public string ReceiptNumber { get; set; }
        public int StudentID { get; set; }
        public string StudentName { get; set; }
        public string RegistrationNo { get; set; }
        public string ClassName { get; set; }
        public string Section { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Details { get; set; }
        public DateTime CreatedOn { get; set; }

        public string PaymentDateDisplay => PaymentDate.ToString("dd MMM yyyy");
    }
}
