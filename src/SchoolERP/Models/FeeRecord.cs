using System;

namespace SchoolERP.Models
{
    public class FeeRecord
    {
        public int FeeID { get; set; }
        public int StudentID { get; set; }
        public string StudentName { get; set; }    // joined from Students
        public string RegistrationNo { get; set; } // joined from Students
        public string ClassName { get; set; }      // joined from Classes
        public string Month { get; set; }
        public string FeeType { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }         // "Due" or "Paid"
        public DateTime? PaymentDate { get; set; }
    }
}
