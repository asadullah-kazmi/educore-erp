using System;

namespace SchoolERP.Models
{
    public class FeeRecord
    {
        public int FeeID { get; set; }
        public int StudentID { get; set; }
        public string StudentName { get; set; }    // joined from Students
        public string RegistrationNo { get; set; } // joined from Students
        public int? ClassID { get; set; }
        public string ClassName { get; set; }      // joined from Classes
        public string Section { get; set; }
        public string Month { get; set; }
        public string FeeType { get; set; }
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance => Math.Max(Amount - PaidAmount, 0);
        public string Status { get; set; }         // "Due" or "Paid"
        public DateTime? PaymentDate { get; set; }
        public bool HasFeeRecord => FeeID > 0;
    }
}
