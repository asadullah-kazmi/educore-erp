using System;

namespace SchoolERP.Models
{
    public class SalaryPayment
    {
        public int SalaryPaymentID { get; set; }
        public int TeacherID { get; set; }
        public string TeacherName { get; set; }
        public string Designation { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Notes { get; set; }
    }
}
