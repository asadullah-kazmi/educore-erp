using System;

namespace SchoolERP.Models
{
    public class FeeCollectionReportRow
    {
        public string StudentName { get; set; }
        public string RegistrationNo { get; set; }
        public string ClassName { get; set; }
        public string Section { get; set; }
        public decimal MonthlyFee { get; set; }
        public decimal AmountPaid { get; set; }
        public string Status { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}
