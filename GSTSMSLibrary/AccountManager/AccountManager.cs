using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Xml.Linq;

namespace GSTSMSLibrary.AccountManager
{
    public class AccountManager
    {
        #region********************************************************************* Notification  ***********************************************************
        public class NotificationModel
        {

            public int NotificationId { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }  // ✅ Add this line
            public string Description { get; set; }
            public bool IsSeen { get; set; }
            public DateTime NotificationDate { get; set; }
            public string Addedby { get; set; }
            public DateTime CreatedDate { get; internal set; }
        }

        #endregion

        #region********************************************************************* Calendar ***********************************************************

        public string MeetingCode { get; set; }
        public string MeetingName { get; set; }
        public string MeetingPlace { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string Agenda { get; set; }


        public TimeSpan EventTime { get; set; }
        public TimeSpan MeetingTime { get; set; }
        public int NotificationId { get; internal set; }
        public string Code { get; internal set; }
        public string Name { get; internal set; }
        public bool IsSeen { get; internal set; }
        public DateTime NotificationDate { get; internal set; }


        #endregion


        #region********************************************************************* Society Ac.Details Bank ***********************************************************


        public string BankBalance { get; set; }
        public string BankCode { get; set; }
        public string IFSCCode { get; set; }
        public decimal OpeningBalance { get; set; }
        public string AllCode { get; set; }
        public bool IsActive { get; set; } = true;
        public string BankName { get; set; }
        public string Branch { get; set; }
        public string BankHolderName { get; set; }
        public string AccountNo { get; set; }
        public DateTime AddedDate { get; set; }
        public int BankId { get; set; }

        public decimal Amount { get; set; }
        public string PaymentByName { get; set; }
        public string SenderName { get; set; }
        public List<AccountManager> TransactionList { get; set; }
        #endregion

        #region********************************************************************* Society Ac.Details Cash ***********************************************************

        public int TransactionId { get; set; }

        public string PaidToName { get; set; }
        public string PaymentModeName { get; set; }

        public string TransactionNature { get; set; }

        public string CategoryName { get; set; }
        public string AccountType { get; set; }
        public int TransactionTypeId { get; set; }

        public string ChecqueNo { get; set; }


        public string AttachmentPath { get; set; }
        public string Type { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverCode { get; set; }

        public string MaintenanceCode { get; set; }
        public string OtherReceiver { get; set; }

        public string PDFPath { get; set; }
        public string Attchment { get; set; }
        public string SalarySlip { get; set; }

        public List<AccountManager> lstTransactionType { get; set; }


        public class ReceiptViewModel
        {
            public string TransactionCode { get; set; }
            public string PaidDate { get; set; }
            public string PaymentBy { get; set; }
            public string PaidTo { get; set; }
            public decimal Amount { get; set; }
            public string AmountInWords { get; set; }
            public string PaymentPurpose { get; set; }
            public string PaymentMode { get; set; }
            public string FlatCode { get; set; }
            public decimal PenaltyAmount { get; set; }
            public int MonthsPending { get; set; }
            public decimal BaseMaintenanceAmount { get; set; }
            public List<ReceiptItem> Items { get; set; }
        }



        public class ReceiptItem
        {
            public string Description { get; set; }
            public decimal Amount { get; set; }

            public int MonthsPending { get; set; }
            public bool IsPenalty { get; set; }
        }


        #endregion


        #region********************************************************************* Worker Pay.Manage ***********************************************************
        public string Password { get; set; }

        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string StaffName { get; set; }

        public string ProfilePic { get; set; }

        public string BankName_Code { get; set; }

        public string PaymentBy { get; set; }
        public string PaidTo { get; set; }

        public string PaymentPurpose { get; set; }

        public int TransactionType { get; set; }

        public string WorkerCode { get; set; }
        public string AttendanceMonth { get; set; }
        public string Contact { get; set; }
        public DateTime DateOfJoining { get; set; }
        public decimal BaseSalary { get; set; }
        public string AccountNumber { get; set; }

        public string WorkerUPI { get; set; }
        public int DaysPresent { get; set; }
        public decimal PerdayPayment { get; set; }
        public decimal AmountToBePaid { get; set; }

        public string PaymentStatus { get; set; }
        public string TransactionRef { get; set; }
        public DateTime MonthDate { get; set; }

        public bool IsPaid { get; set; }
        public int SerialNumber { get; set; }

        public string AccountTypeName { get; internal set; }
        public string SocietyCashAmount { get; internal set; }

        #endregion


        #region********************************************************************* Maintaince Management ***********************************************************
        public string WingName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string Status { get; set; }
        public DateTime? PaidDate { get; set; }

        public string MaintenanceId { get; set; }
        public decimal ChargeAmount { get; set; }
        public string EntityCode { get; set; }
        public string MemberCode { get; set; }
        public int? MaintananceTypeId { get; set; }
        public DateTime? ScheduleDate { get; set; }
        public string MaintananceType { get; set; }
        public string MaintenanceName { get; set; }
        public string MaintenanceType { get; set; }
        public string MaintananceCode { get; set; }
        public decimal MaintenanceAmount { get; set; }
        public int MonthsPending { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal PenaltyPercent { get; set; }

        public decimal PenaltyAmount { get; set; }
        public decimal FinalPayable { get; set; }



        public List<ChargeItem> ChargeDetails { get; set; } = new List<ChargeItem>();

        public class ChargeItem
        {
            public string MaintenanceName { get; set; }
            public decimal ChargeAmount { get; set; }
            public string MaintenanceCode { get; set; }
        }
        #endregion

        #region********************************************************************* Expence ***********************************************************

        // Transaction
        public decimal RemainingAmount { get; set; }

        public decimal CashInHand { get; set; }

        public List<AccountManager> AllTransactions { get; set; }
        public string TransactionCode { get; set; }
        public string ServiceType { get; set; }

        public decimal TransactionAmount { get; set; }
        public string PaymentMode { get; set; }


        public int DocumentId { get; set; }


        public int ExpenseId { get; set; }
        public string ExpenseCode { get; set; }
        public string PaymentTo { get; set; }
        public int ExpenseTypeId { get; set; }
        //public string ExpenseTypeName { get; set; }
        public string ExpenseName { get; set; }
        public string AddedBy { get; set; }

        public int StatusId { get; set; }
        public string StatusName { get; set; }

        // --- Vendor Info ---
        public string VendorName { get; set; }
        public string PhoneNumber { get; set; }
        public string AlternatePhoneNumber { get; set; }
        public string Address { get; set; }

        // --- Documents ---
        public string Document1 { get; set; } // PAN or any other doc
        public string Document2 { get; set; } // GST or other doc




        public string UPIId { get; set; }
        public int AccountTypeId { get; set; } // fixed




        public int ServiceSubTypeId { get; set; }

        public string SubTypeName { get; set; }




        public string ExpenseType { get; set; }
        public string VendorCode { get; set; }

        //public string GSTType { get; set; }

        public String DocumentBase64String { get; set; }

        //public string ExpenseFor { get; set; }
        //public string VendorType { get; set; }

        public DateTime Date { get; set; }

        //public int AmountToPay { get; set; }

        //public string StaffName { get; set; }

        public int GSTTypeId { get; set; }
        public string GSTTypeName { get; set; }

        public List<AccountManager> Attachments { get; set; }




        public string GSTPercentage { get; set; }

        public List<string> DocumentList { get; set; } // ✅ Correct

        public List<int> CGSTTypeId { get; set; }
        public List<int> SGSTTypeId { get; set; }
        public List<int> IGSTTypeId { get; set; }



        #endregion


        #region********************************************************************* Event Management ***********************************************************


        // Add this class inside your AccountManager class or namespace
        public class BudgetCategory
        {
            public string Name { get; set; }
            public decimal Budget { get; set; }
        }

        public List<AccountManager> EventDistributions { get; set; }


        // ✅ Category list (each category and budget)
        public List<BudgetCategory> Categories { get; set; }
        public List<AccountManager> Documents { get; set; } // ✅ Holds document li
        public string EventDCode { get; set; }


        public decimal ActualBudget { get; set; }


        public decimal Budget { get; set; }
        public DateTime EventEndDate { get; set; }

        public int EBudgetId { get; set; }
        public string EventCode { get; set; }
        public string EventName { get; set; }
        public string EventHandlerName { get; set; }

        public decimal AllocatedBudget { get; set; }
        public decimal ActualCost { get; set; }
        public DateTime BudgetAddedDate { get; set; }
        public int BudgetStatus { get; set; }
        public string BudgetStatusName { get; set; }

        public string BankAccount { get; set; }


        public string TransactionId_ChequeId { get; set; }
        public string PaymentId { get; set; }
        public int PaymentModeId { get; set; }

        public string SelectedBankCode { get; set; }
        public string EBudgetCode { get; set; }



        public string DocumentName { get; set; }
        public DateTime DocumentDate { get; set; }
        public int? DocumentSubTypeId { get; set; }


        public string DebitedAccountNo { get; set; }



        public string DistributionCategory { get; set; }


        public List<AccountManager> BudgetBreakups { get; set; }






        #endregion


        #region********************************************************************* Community Send Email ***********************************************************

       

        public string userTypedMessage { get; set; }
        public string FromEmailAddress { get; set; }

        public string ToEmailAddress { get; set; }

        public List<string> ToEmailAddresses { get; set; }

        public string Subject { get; set; }

        public string EmailBodyMessage { get; set; }
        public List<string> CcEmailAddresses { get; set; }


        public string Attachment { get; set; }

        public string FlatCode { get; set; }
        public bool IsSelected { get; set; }

        public int MemberId { get; set; }
        public int StaffId { get; set; }
        public string StaffCode { get; set; }
        public string ContactNumber { get; set; }
        public List<string> ContactNumbers { get; set; }

        public string WhatsAppMessage { get; set; }

        #endregion


        #region********************************************************************* Community Notice  ***********************************************************

        public string NoticeCode { get; set; }
        public string NoticeTitle { get; set; }
        public string Description { get; set; }
        public DateTime  PublishDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string Document { get; set; }
        public string DocumentPath { get; set; }
        public int SubTypeId { get; set; } = 71;


        public List<string> AttachmentList { get; set; }

        public string SendBy { get; set; }
        public string SendByRole { get; set; }


        public List<string> SelectedMemberCodes { get; set; }

        public int WingId { get; set; }

        public int NoticeAnnouncementId { get; set; }
        public string NoticeAnnouncementCode { get; set; }







        #endregion


        #region********************************************************************* Complaints ***********************************************************



        // ✅ Complaint List Properties
        public DateTime? ResolvedDate { get; set; }
        public string Reasonsolvecomplaint { get; set; }
        public string ComplaintType { get; set; }
        public string Complaint { get; set; }
        public DateTime ComplaintDate { get; set; }
        public string AssignBy { get; set; }



        public string RaisedBy { get; set; }
        public int ComplaintId { get; set; }
        public string SecretoryName { get; set; }

     

        public string Wing { get; set; }

      
        #endregion

        #region********************************************************************* Reports  ***********************************************************


        public DateTime PossessionDate { get; set; }
        public string Gender { get; set; }
        public int FamilyMemberCount { get; set; }
        public int NoOfVehicle { get; set; }
        public DateTime RegisterationDate { get; set; }

        public string WorkerName { get; set; }
        public string WorkerContactNo { get; set; }
        public DateTime? JoiningDate { get; set; }
        public DateTime? RegisterDate { get; set; }


        public string RegistrationDate { get; set; }

        public decimal RegistrationFee { get; set; }

        public decimal Percentage { get; set; }



        public string ExpenseTypeName { get; set; }

        public DateTime? ExpenseDate { get; set; }  // Stored as string because formatted date (dd/MM/yyyy)

        public string MonthName { get; set; }      //COMMONNNNNNNN

        public string CGST { get; set; }
        public string SGST { get; set; }
        public decimal TotalGSTAmount { get; set; }

        public string BusinessName { get; set; }
        public string VendorTypeName { get; set; }
        public string JobRole { get; set; }


        /////////////////////////////////////////////////////////vraj ////////////////////////////////////////////////////////////////
        ///
        // For the grouped bar chart (Event Budget)
        public string MonthYear { get; set; }


        public int Month { get; set; }
        public int Year { get; set; }

        // For event budget detail popup
        public DateTime FromDate { get; set; }
       // public decimal BudgetDifference => (AllocatedBudget ?? 0) - (ActualCost ?? 0);
        public int MonthNumber { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }

        // 🔽 NEW: For clicked month income/expense breakdown
        public string TypeLabel { get; set; }




        // "Income" / "Expense"


        #endregion



        #region********************************************************************* Complaints ***********************************************************
        // ───── Basic Event Info ─────
        public int EventId { get; set; }

        public DateTime EventDate { get; set; } // For calendar view (typically FromDate)


        public DateTime ToDate { get; set; }    // End DateTime
        public string LocationName { get; set; } // From GSTtblFacility


        public string FromTime => FromDate.ToString("hh:mm tt"); // 10:00 AM
        public string ToTime => ToDate.ToString("hh:mm tt");     // 12:30 PM
        public string FromDateLabel => FromDate.ToString("dd-MMM-yyyy"); // 15-Jul-2025
        public string ToDateLabel => ToDate.ToString("dd-MMM-yyyy");

        #endregion


    
    }

}
