using SchoolERP.Models;

namespace SchoolERP.ViewModels
{
    public class FamilyPaymentRowViewModel : ObservableObject
    {
        private decimal allocatedAmount;

        public int StudentID { get; set; }
        public string StudentName { get; set; }
        public string FatherName { get; set; }
        public string RegistrationNo { get; set; }
        public int? ClassID { get; set; }
        public string ClassName { get; set; }
        public string Section { get; set; }
        public decimal MonthlyFee { get; set; }
        public decimal OutstandingBalance { get; set; }

        /// <summary>
        /// The amount auto-allocated to this sibling from the total family payment.
        /// Set by the distribution algorithm (youngest class first).
        /// </summary>
        public decimal AllocatedAmount
        {
            get => allocatedAmount;
            set
            {
                if (SetProperty(ref allocatedAmount, value))
                {
                    OnPropertyChanged(nameof(PendingAmount));
                    OnPropertyChanged(nameof(PaymentStatus));
                }
            }
        }

        /// <summary>
        /// Remaining outstanding balance after this payment is applied.
        /// </summary>
        public decimal PendingAmount => OutstandingBalance - AllocatedAmount;

        /// <summary>
        /// Visual status: Paid (fully covered), Partial, or Unpaid.
        /// </summary>
        public string PaymentStatus
        {
            get
            {
                if (OutstandingBalance <= 0) return "No Dues";
                if (AllocatedAmount >= OutstandingBalance) return "Paid";
                if (AllocatedAmount > 0) return "Partial";
                return "Unpaid";
            }
        }

        public FamilyFeeReceiptItem ToReceiptItem(decimal balanceAfter)
        {
            return new FamilyFeeReceiptItem
            {
                StudentID = StudentID,
                StudentName = StudentName,
                FatherName = FatherName,
                RegistrationNo = RegistrationNo,
                ClassName = ClassName,
                Section = Section,
                AmountPaid = AllocatedAmount,
                BalanceAfter = balanceAfter
            };
        }
    }
}
