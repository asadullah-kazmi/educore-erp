using System;

namespace SchoolERP.Models
{
    public class ExamSlip
    {
        public int ExamSlipID { get; set; }
        public int StudentID { get; set; }
        public string TermName { get; set; }
        public string FeeMonth { get; set; }
        public string ExamNumber { get; set; }
        public DateTime GeneratedOn { get; set; }
        public string StudentName { get; set; }
        public string FatherName { get; set; }
        public string RegistrationNo { get; set; }
        public string ClassName { get; set; }
        public string Section { get; set; }
        public decimal FeeAmount { get; set; }
        public decimal PaidAmount { get; set; }

        public string GeneratedOnDisplay => GeneratedOn.ToString("dd MMM yyyy");
        public string FeeStatusDisplay => PaidAmount.ToString("N0") + " / " + FeeAmount.ToString("N0");
    }
}
