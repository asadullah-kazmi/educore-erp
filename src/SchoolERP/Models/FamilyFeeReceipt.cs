using System;
using System.Collections.Generic;
using System.Linq;

namespace SchoolERP.Models
{
    public class FamilyFeeReceipt
    {
        public int FamilyReceiptID { get; set; }
        public string ReceiptNumber { get; set; }
        public string GuardianCnic { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalBalanceAfter { get; set; }
        public List<FamilyFeeReceiptItem> Items { get; set; } = new List<FamilyFeeReceiptItem>();

        public string AggregatedAdmissionNo => string.Join(" + ", Items.Select(i => i.RegistrationNo));
        public string AggregatedStudentNames => string.Join(" + ", Items.Select(i => i.StudentName));
        public string AggregatedFatherNames => string.Join(" + ", Items.Select(i => i.FatherName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct());
        public string AggregatedClasses => string.Join(" + ", Items.Select(i => i.ClassName));
        public decimal TotalCalculated => TotalPaid + TotalBalanceAfter;
        public string Description => "Combined Family Fee Payment for: " + AggregatedStudentNames;
    }

    public class FamilyFeeReceiptItem
    {
        public int StudentID { get; set; }
        public string StudentName { get; set; }
        public string FatherName { get; set; }
        public string RegistrationNo { get; set; }
        public string ClassName { get; set; }
        public string Section { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceAfter { get; set; }
    }
}
