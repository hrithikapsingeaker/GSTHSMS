using GSTSMSHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using static GSTSMSLibrary.AccountManager.AccountManager;

namespace GSTSMSLibrary.AccountManager
{
    public class BALAccountManager

    {

        MSSQL db = new MSSQL();


        #region********************************************************************* notification  ***********************************************************


        /// <summary>
        /// Get list of notifications from DB filtered by StaffCode
        /// </summary>
        public async Task<List<AccountManager>> GetNotificationsPDV(string staffCode)
        {
            var list = new List<AccountManager>();
            try
            {
                if (string.IsNullOrEmpty(staffCode))
                    return list; // Return empty if no staffCode provided

                // Parameters for stored procedure, including StaffCode for filtering
                var param = new Dictionary<string, string>
   {
       { "@flag", "NotificationPDV" },
       { "@StaffCode", staffCode }
   };

                var ds = await new MSSQL().ExecuteStoreProcedureReturnDS("sp_SMS", param);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        try
                        {
                            list.Add(new AccountManager
                            {
                                NotificationId = row["NotificationId"] != DBNull.Value
                                    ? Convert.ToInt32(row["NotificationId"])
                                    : 0,
                                Code = row.Table.Columns.Contains("Code") && row["Code"] != DBNull.Value
                                    ? row["Code"].ToString()
                                    : string.Empty,
                                Name = row.Table.Columns.Contains("Name") && row["Name"] != DBNull.Value
                                    ? row["Name"].ToString()
                                    : string.Empty,
                                Description = row.Table.Columns.Contains("Description") && row["Description"] != DBNull.Value
                                    ? row["Description"].ToString()
                                    : string.Empty,
                                IsSeen = row.Table.Columns.Contains("IsSeen") && row["IsSeen"] != DBNull.Value
                                    && (row["IsSeen"].ToString() == "1" || row["IsSeen"].ToString().ToLower() == "true"),
                                NotificationDate = row.Table.Columns.Contains("CreatedDate") && row["CreatedDate"] != DBNull.Value
                                    ? Convert.ToDateTime(row["CreatedDate"])
                                    : DateTime.MinValue
                            });
                        }
                        catch (Exception rowEx)
                        {
                            Console.WriteLine($"⚠ Error parsing notification row: {rowEx.Message}");
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetNotificationsAsync: {ex.Message}");
            }
            return list;
        }

        /// <summary>
        /// Mark a single notification as seen
        /// </summary>
        public async Task<bool> MarkNotificationAsSeenPDV(int notificationId)
        {
            try
            {
                MSSQL db = new MSSQL();
                var param = new Dictionary<string, string>
   {
       { "@flag", "MarkNotificationReadPDV" },
       { "@NotificationId", notificationId.ToString() }
   };
                int rows = await db.ExecuteStoreProcedureReturnInt("sp_SMS", param);
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in MarkNotificationAsSeen: {ex.Message}");
                return false;
            }
        }



        /// <summary>
        /// Mark all notification as seen
        /// </summary>

        public async Task<bool> MarkAllNotificationsAsSeenPDV(string staffCode)
        {
            try
            {
                if (string.IsNullOrEmpty(staffCode))
                    return false;

                MSSQL db = new MSSQL();
                var param = new Dictionary<string, string>
   {
       { "@flag", "MarkAllReadPDV" },
       { "@StaffCode", staffCode }
   };
                int rows = await db.ExecuteStoreProcedureReturnInt("sp_SMS", param);
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in MarkAllNotificationsAsSeen: {ex.Message}");
                return false;
            }
        }








        public async Task<List<NotificationModel>> GetNotificationsAsync()
        {
            var list = new List<NotificationModel>();

            try
            {
                var param = new Dictionary<string, string> { { "@flag", "NotificationDD" } };
                var ds = await new MSSQL().ExecuteStoreProcedureReturnDS("sp_SMS", param);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        try
                        {
                            list.Add(new NotificationModel
                            {
                                NotificationId = row["NotificationId"] != DBNull.Value ? Convert.ToInt32(row["NotificationId"]) : 0,
                                Code = row["Code"]?.ToString(),  // 🔁 Must be available in SQL
                                Name = row["Name"]?.ToString(),  // ✅ Okay
                                Description = row["Description"]?.ToString(),
                                IsSeen = row["IsSeen"] != DBNull.Value && Convert.ToBoolean(row["IsSeen"]),
                                CreatedDate = row["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(row["CreatedDate"]) : DateTime.MinValue

                            });
                        }
                        catch (Exception rowEx)
                        {
                            // Skip bad row but log error
                            Console.WriteLine("Error parsing notification row: " + rowEx.Message);
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error in GetNotificationsAsync: " + ex.Message);
            }

            return list;
        }


        public async Task<bool> MarkNotificationAsSeen(int notificationId)
        {
            try
            {
                MSSQL db = new MSSQL();
                var param = new Dictionary<string, string>
 {
     { "@flag", "MarkNotificationRead" },   // ✅ fixed
     { "@NotificationId", notificationId.ToString() }
 };

                int rows = await db.ExecuteStoreProcedureReturnInt("sp_SMS", param);
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error in MarkNotificationAsSeen: " + ex.Message);
                return false;
            }
        }


        public async Task<bool> MarkAllNotificationsAsSeen()
        {
            try
            {
                MSSQL db = new MSSQL();
                var param = new Dictionary<string, string>
 {
     { "@flag", "MarkAllRead" }   // ✅ fixed
 };

                int rows = await db.ExecuteStoreProcedureReturnInt("sp_SMS", param);
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error in MarkAllNotificationsAsSeen: " + ex.Message);
                return false;
            }
        }



        #endregion


        #region*********************************************************************  Calendar  ***********************************************************

        /// <summary>
        /// For FullCalendar: Returns basic event info (title, dateGetEventDetailsByDateAsync
        /// </summary>
        public async Task<List<AccountManager>> GetAllEventsAsyncSV()
        {
            var events = new List<AccountManager>();

            Dictionary<string, string> GetData = new Dictionary<string, string>
     {
         { "@flag", "FetchEventsSV" },
         { "@Email", "" },
         { "@Password", "" },
         { "@EventDate", DateTime.Now.ToString("yyyy-MM-dd") } // required even if unused
     };

            using (SqlDataReader dr = await db.ExecuteStoreProcedureReturnDataReader("sp_SMS", GetData))
            {
                while (dr.Read())
                {
                    var evt = new AccountManager
                    {
                        EventId = Convert.ToInt32(dr["EventId"]),
                        EventName = dr["EventName"].ToString(),
                        EventDate = Convert.ToDateTime(dr["EventDate"]),
                        EventCode = dr["EventCode"].ToString() // Ensure this is mapped
                    };
                    events.Add(evt);
                }
            }

            return events;
        }

        /// <summary>
        /// Returns a list of all meetings (for use in calendar or listing views).
        /// </summary>
        public async Task<List<AccountManager>> GetAllMeetingAsyncSV()
        {
            var meetings = new List<AccountManager>();

            Dictionary<string, string> GetData = new Dictionary<string, string>
    {
        { "@flag", "FetchMeetingsSV" }
    };

            using (SqlDataReader dr = await db.ExecuteStoreProcedureReturnDataReader("sp_SMS", GetData))
            {
                while (dr.Read())
                {
                    var meeting = new AccountManager
                    {
                        MeetingCode = dr["MeetingCode"].ToString(),
                        MeetingName = dr["MeetingName"].ToString(),
                        MeetingPlace = dr["MeetingPlace"].ToString(),
                        ScheduledDate = Convert.ToDateTime(dr["ScheduledDate"]),
                        Agenda = dr["Agenda"].ToString()
                    };
                    meetings.Add(meeting);
                }
            }

            return meetings;
        }


        /// <summary>
        /// Called on calendar day click: Returns event details on a specific date.
        /// Includes Location, Status, and Description.
        /// </summary>
        public async Task<List<AccountManager>> GetEventDetailsByDateAsyncSV(DateTime eventDate)
        {
            var details = new List<AccountManager>();

            Dictionary<string, string> GetData = new Dictionary<string, string>
     {
         { "@flag", "EventsByDateSV" },
         { "@Email", "" },
         { "@Password", "" },
         { "@EventDate", eventDate.ToString("yyyy-MM-dd") }
     };

            using (SqlDataReader dr = await db.ExecuteStoreProcedureReturnDataReader("sp_SMS", GetData))
            {
                while (dr.Read())
                {
                    var evt = new AccountManager
                    {
                        EventId = Convert.ToInt32(dr["EventId"]),
                        EventName = dr["EventName"].ToString(),
                        FromDate = Convert.ToDateTime(dr["EventDate"]),
                        StatusName = dr["Status"].ToString(),
                        LocationName = dr["Location"].ToString(),
                        Description = dr["Description"]?.ToString() ?? "-"
                    };
                    details.Add(evt);
                }
            }

            return details;
        }

        /// <summary>
        /// For getting the evnt details on another modal oof calander
        /// </summary>
        public async Task<List<AccountManager>> GetEventDetailsByIdAsyncSV(string eventCode)
        {
            var events = new List<AccountManager>();

            Dictionary<string, string> GetData = new Dictionary<string, string>
    {
        { "@flag", "FetchEventsByIdSV" },
        { "@EventCode", eventCode },
        //{ "@Email", "" },
        //{ "@Password", "" },
        //{ "@EventDate", DateTime.Now.ToString("yyyy-MM-dd") } // required if needed by SP
    };

            using (SqlDataReader dr = await db.ExecuteStoreProcedureReturnDataReader("sp_SMS", GetData))
            {
                while (dr.Read())
                {
                    var evt = new AccountManager
                    {
                        EventCode = dr["EventCode"].ToString(),
                        EventName = dr["EventName"].ToString(),
                        EventDate = Convert.ToDateTime(dr["EventDate"]),
                        EventTime = dr["EventTime"] != DBNull.Value ?
                        TimeSpan.Parse(dr["EventTime"].ToString()) : TimeSpan.Zero,
                        LocationName = dr["Location"].ToString(),
                        StatusName = dr["Status"].ToString(),
                        Description = dr["Description"].ToString()
                    };
                    events.Add(evt);
                }
            }

            return events;
        }



        /// <summary>
        /// Called on calendar day click: Returns meeting details on a specific date.
        /// Includes Location, Status, and Agenda.
        /// </summary>
        public async Task<List<AccountManager>> GetMeetingDetailsByDateAsyncSV(DateTime scheduledDate)
        {
            var details = new List<AccountManager>();

            Dictionary<string, string> GetData = new Dictionary<string, string>
    {
        { "@flag", "MeetingsByDateSV" },
        { "@ScheduledDate", scheduledDate.ToString("yyyy-MM-dd") }
    };

            using (SqlDataReader dr = await db.ExecuteStoreProcedureReturnDataReader("sp_SMS", GetData))
            {
                while (dr.Read())
                {
                    var meeting = new AccountManager
                    {
                        MeetingCode = dr["MeetingCode"]?.ToString() ?? "N/A",
                        MeetingName = dr["MeetingName"]?.ToString() ?? "No Name",
                        LocationName = dr["Location"]?.ToString() ?? "No Location",
                        StatusName = dr["Status"]?.ToString() ?? "No Status",
                        ScheduledDate = dr["MeetingDate"] != DBNull.Value ?
                                     Convert.ToDateTime(dr["MeetingDate"]) : DateTime.MinValue,
                        MeetingTime = dr["MeetingTime"] != DBNull.Value ?
                                    TimeSpan.Parse(dr["MeetingTime"].ToString()) : TimeSpan.Zero,
                        Agenda = dr["Agenda"]?.ToString() ?? "No Agenda Provided",
                        // For compatibility with the partial view
                        EventName = dr["MeetingName"]?.ToString(),
                        Description = dr["Agenda"]?.ToString(),
                        EventDate = dr["MeetingDate"] != DBNull.Value ?
                                  Convert.ToDateTime(dr["MeetingDate"]) : DateTime.MinValue,
                        FromDate = dr["MeetingDate"] != DBNull.Value ?
                                 Convert.ToDateTime(dr["MeetingDate"]) : DateTime.MinValue
                    };
                    details.Add(meeting);
                }
            }

            return details;
        }

        /// <summary>
        /// For getting meeting details by the meetin Id and displaying on another details modal
        /// </summary>

        public async Task<List<AccountManager>> GetMeetingDetailsByIdAsyncSV(string meetingCode)
        {
            var meetings = new List<AccountManager>();

            try
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            { "@flag", "FetchMeetingsByIdSV" },
            { "@MeetingCode", meetingCode }
        };

                using (SqlDataReader dr = await db.ExecuteStoreProcedureReturnDataReader("sp_SMS", parameters))
                {
                    while (dr.Read())
                    {
                        var meeting = new AccountManager
                        {
                            MeetingCode = dr["MeetingCode"]?.ToString() ?? "N/A",
                            MeetingName = dr["MeetingName"]?.ToString() ?? "No Name",
                            LocationName = dr["FacilityName"]?.ToString() ?? dr["MeetingPlace"]?.ToString() ?? "No Location",
                            StatusName = dr["Status"]?.ToString() ?? "No Status",
                            ScheduledDate = dr["MeetingDate"] != DBNull.Value ?
                                         Convert.ToDateTime(dr["MeetingDate"]) : DateTime.MinValue,
                            MeetingTime = dr["MeetingTime"] != DBNull.Value ?
                                        TimeSpan.Parse(dr["MeetingTime"].ToString()) : TimeSpan.Zero,
                            Agenda = dr["Agenda"]?.ToString() ?? "No Agenda Provided",
                            // For compatibility with the partial view
                            EventName = dr["MeetingName"]?.ToString(),
                            Description = dr["Agenda"]?.ToString(),
                            EventDate = dr["MeetingDate"] != DBNull.Value ?
                                      Convert.ToDateTime(dr["MeetingDate"]) : DateTime.MinValue,
                            FromDate = dr["MeetingDate"] != DBNull.Value ?
                                     Convert.ToDateTime(dr["MeetingDate"]) : DateTime.MinValue
                        };
                        meetings.Add(meeting);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error for meeting {meetingCode}: {ex}");
                throw;
            }

            return meetings;
        }



        #endregion


        #region********************************************************************* Profile  ***********************************************************


        /// <summary>
        /// Submits a password change request to the database via stored procedure <c>sp_SMS</c>.
        /// </summary>
        /// <param name="objAcc">
        /// AccountManager object containing details of the request, including 
        /// <see cref="AccountManager.Description"/> and <see cref="AccountManager.StaffCode"/>.
        /// </param>
        public async Task PasswordchangerequestSM(AccountManager objAcc)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "PasswordChangeRequestSM" },
    { "@EntityCode", "CRM001" },
    { "@Description", objAcc.Description },
    { "@StaffCode", objAcc.StaffCode }
};

            await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            // No return value needed
        }


        /// <summary>
        /// Updates the profile photo path for a staff member in the database via stored procedure <c>sp_SMS</c>.
        /// </summary>
        /// <param name="objAcc">
        /// AccountManager object containing the staff <see cref="AccountManager.EntityCode"/> 
        /// and the new <see cref="AccountManager.Attchment"/> path.
        /// </param>

        public async Task ProfilephotoupdateSM(AccountManager objAcc)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "ProfilephotoupdateSM" },

    { "@AttachmentPath", objAcc.Attchment },
      { "@EntityCode", objAcc.EntityCode }

};

            await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            // No return value needed
        }






        /// <summary>
        /// ////////////////////////////////////////////////////////
        /// </summary>
        /// <param name="objAcc"></param>
        /// <returns></returns>

        public async Task Passwordchangerequest(AccountManager objAcc)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "PasswordChangeRequest" },
    { "@EntityCode", "CRM001" },
    { "@Description", objAcc.Description },
    { "@StaffCode", objAcc.StaffCode }
};

            await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            // No return value neededabhi
        }


        #endregion


        #region********************************************************************* Dashboard ***********************************************************






        private readonly MSSQL _db = new MSSQL();





        /// <summary>
        /// this will show month wise transaction history
        /// </summary>
        /// <param name="month"></param>
        /// <param name="bankId"></param>
        /// <returns></returns>

        public async Task<Dictionary<string, decimal>> GetTransactionSummaryByDateYP(
         DateTime fromDate,
         DateTime toDate,
         int bankId)
        {
            var summary = new Dictionary<string, decimal>();

            var param = new Dictionary<string, string>
        {
            { "@Flag", "TotalCreditedAndDebitedYP" },
            { "@FromDate", fromDate.ToString("yyyy-MM-dd") },
            { "@ToDate", toDate.ToString("yyyy-MM-dd") },
            { "@BankId", bankId.ToString() }
        };

            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);

            if (dt.Rows.Count > 0)
            {
                summary["Credited"] = Convert.ToDecimal(dt.Rows[0]["TotalCreditedCash"]);
                summary["Debited"] = Convert.ToDecimal(dt.Rows[0]["TotalDebitedCash"]);
                summary["Balance"] = Convert.ToDecimal(dt.Rows[0]["TotalOpeningBalance"]);
            }
            else
            {
                summary["Credited"] = 0;
                summary["Debited"] = 0;
                summary["Balance"] = 0;
            }

            return summary;
        }

        // ===================== 2. DASHBOARD LIST (DataSet) ===================
        public async Task<DataSet> TransactionDashboardListSM(AccountManager obj)
        {
            var db = new MSSQL();

            // Map TransactionType (26/27) -> "Credit"/"Debit"
            string txName = obj.TransactionType == 26 ? "Credit" : "Debit";

            var parameters = new Dictionary<string, string>
        {
            { "@Flag", "TransactionDashboardListSM" },
            { "@Month", obj.Month.ToString() },
            { "@BankId", obj.BankId.ToString() },
            { "@TransactionTypeName", txName }
        };

            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
        }

        // ===================== 3. BANK TRANSACTION LIST (DataTable) ==========
        // Optional helper – uses SAME flag as above
        public async Task<DataTable> GetBankTransactionList(int bankId, int month, int transactionType)
        {
            string txName = transactionType == 26 ? "Credit" : "Debit";

            var parameters = new Dictionary<string, string>
        {
            { "@Flag", "TransactionDashboardListSM" },
            { "@BankId", bankId.ToString() },
            { "@Month", month.ToString() },
            { "@TransactionTypeName", txName }
        };

            return await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
        }

        // ===================== 4. MONTH WISE SUMMARY =========================
        // We'll convert month -> FromDate/ToDate and reuse the SAME SP flag
        public async Task<Dictionary<string, decimal>> GetMonthlyTransactionSummaryYP(
            int month,
            int bankId,
            int year)
        {
            var summary = new Dictionary<string, decimal>();

            var fromDate = new DateTime(year, month, 1);
            var toDate = fromDate.AddMonths(1).AddDays(-1);

            var param = new Dictionary<string, string>
        {
            { "@Flag", "TotalCreditedAndDebitedYP" },
            { "@FromDate", fromDate.ToString("yyyy-MM-dd") },
            { "@ToDate", toDate.ToString("yyyy-MM-dd") },
            { "@BankId", bankId.ToString() }
        };

            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);

            if (dt.Rows.Count > 0)
            {
                summary["Credited"] = Convert.ToDecimal(dt.Rows[0]["TotalCreditedCash"]);
                summary["Debited"] = Convert.ToDecimal(dt.Rows[0]["TotalDebitedCash"]);
                summary["Balance"] = Convert.ToDecimal(dt.Rows[0]["TotalOpeningBalance"]);
            }
            else
            {
                summary["Credited"] = 0;
                summary["Debited"] = 0;
                summary["Balance"] = 0;
            }

            return summary;
        }














        public async Task<DataSet> EventExpensYPSM(AccountManager objmonth)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
  {
          { "@Flag", "EventExpensYP" },
          { "@Month", objmonth.Month.ToString() },

  };
            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);

        }
        public async Task<DataSet> EventExpensDistributionSM(AccountManager objmonth)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "EventExpenseDistributionSM" },

    { "@EventCode", objmonth.EventCode.ToString() },

};
            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);

        }
        public async Task<DataSet> GetEventExpensesByDate(AccountManager obj)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
    {
        { "@Flag", "EventExpensesByDate" },
        { "@FromDate", obj.FromDate.ToString("yyyy-MM-dd") }, // convert DateTime to string
        { "@ToDate", obj.ToDate.ToString("yyyy-MM-dd") }      // convert DateTime to string
    };
            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
        }







        public async Task<List<AccountManager>> EventBudgetPS(int month, int year)
        {
            MSSQL obj = new MSSQL();
            var result = new List<AccountManager>();

            var parameters = new Dictionary<string, string>
{
    { "@flag", "EventBudgetPS" },
    { "@Month", month.ToString() },
    { "@Year", year.ToString() }
};

            using (SqlDataReader dr = await obj.ExecuteStoreProcedureReturnDataReader("sp_SMS", parameters))
            {
                while (dr.Read())
                {
                    var fromDate = dr["FromDate"] != DBNull.Value ?
                                   Convert.ToDateTime(dr["FromDate"]) : DateTime.MinValue;

                    result.Add(new AccountManager
                    {
                        EventName = dr["EventName"].ToString(),
                        AllocatedBudget = Convert.ToDecimal(dr["AllocatedBudget"]),
                        ActualCost = Convert.ToDecimal(dr["ActualCost"]),
                        FromDate = fromDate,
                        Month = fromDate.Month,
                        Year = fromDate.Year,
                        MonthYear = fromDate.ToString("MMMM yyyy")
                    });
                }
            }

            return result;
        }

        public async Task<List<AccountManager>> EventBudgetDetailsPS()
        {
            MSSQL obj = new MSSQL();
            var result = new List<AccountManager>();

            var parameters = new Dictionary<string, string>
{
    { "@flag", "EventBudgetDetailsPS" }
};

            using (SqlDataReader dr = await obj.ExecuteStoreProcedureReturnDataReader("sp_SMS", parameters))
            {
                while (dr.Read())
                {
                    var fromDate = Convert.ToDateTime(dr["FromDate"]);

                    result.Add(new AccountManager
                    {
                        EventName = dr["EventName"].ToString(),
                        AllocatedBudget = Convert.ToDecimal(dr["AllocatedBudget"]),
                        ActualCost = Convert.ToDecimal(dr["ActualCost"]),
                        FromDate = fromDate,
                        Month = fromDate.Month,
                        Year = fromDate.Year,
                        MonthYear = fromDate.ToString("MMMM yyyy")
                    });
                }
            }

            return result;
        }


        public async Task<DataSet> PaidMaintainceMembersListSM(AccountManager objmonth)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "PaidMaintainceMembersListSM" },
    { "@Month", objmonth.Month.ToString() },
    { "@MaintananceTypeId", objmonth.MaintananceTypeId.ToString() },
    { "@WingId", objmonth.WingId.ToString() },

    // ✅✅✅ FIXED NULL SAFE DATE
    { "@FromDate", objmonth.FromDate == DateTime.MinValue
        ? null
        : objmonth.FromDate.ToString("yyyy-MM-dd")
    },

    { "@ToDate", objmonth.ToDate == DateTime.MinValue
        ? null
        : objmonth.ToDate.ToString("yyyy-MM-dd")
    }
};

            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
        }



        public async Task<DataSet> PendingMaintainceMembersListSM(AccountManager objmonth)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "PendingMaintainceMembersListSM" },
    { "@Month", objmonth.Month.ToString() },
    { "@MaintananceTypeId", objmonth.MaintananceTypeId.ToString() },
    { "@WingId", objmonth.WingId.ToString() },

    // ✅✅✅ FIXED NULL SAFE DATE
    { "@FromDate", objmonth.FromDate == DateTime.MinValue
        ? null
        : objmonth.FromDate.ToString("yyyy-MM-dd")
    },

    { "@ToDate", objmonth.ToDate == DateTime.MinValue
        ? null
        : objmonth.ToDate.ToString("yyyy-MM-dd")
    }
};

            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
        }










        public async Task<DataSet> GetDueMaintenanceAmountSM()
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "MaintainanceDueMemberListYPP" }
};
            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);

        }







        public async Task<DataTable> GetMaintenanceMemberListAsync(string status, int wingId, int month)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
    {
        { "@Flag", "GetMaintenanceMemberList" }, // SP flag
        { "@Status", status },                   // "Paid" / "Pending"
        { "@WingId", wingId.ToString() },
        { "@Month", month.ToString() }
    };

            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SHS", parameters);

            return dt;
        }









       









        public async Task<DataTable> GetBankTransactionListAsync(int bankId, int month, string transactionTypeName)
        {
            if (bankId <= 0)
                throw new ArgumentException("BankId must be greater than zero.", nameof(bankId));

            if (month < 1 || month > 12)
                throw new ArgumentException("Month must be between 1 and 12.", nameof(month));

            if (string.IsNullOrWhiteSpace(transactionTypeName))
                throw new ArgumentException("Transaction type is required (Credit / Debit).", nameof(transactionTypeName));

            // Normalize value: "Credit" or "Debit"
            transactionTypeName = transactionTypeName.Trim();

            var parameters = new Dictionary<string, string>
            {
                { "@Flag", "TransactionDashboardListSM" },
                { "@BankId", bankId.ToString() },
                { "@Month", month.ToString() },
                { "@TransactionTypeName", transactionTypeName } // "Credit" OR "Debit"
            };

            // Call your shared db helper
            var result = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

            // Never return null, always return a DataTable
            return result ?? new DataTable();
        }

        /// <summary>
        /// Convenience method: get only CREDIT transactions for given bank + month.
        /// </summary>
        public Task<DataTable> GetCreditTransactionListAsync(int bankId, int month)
        {
            return GetBankTransactionListAsync(bankId, month, "Credit");
        }

        /// <summary>
        /// Convenience method: get only DEBIT transactions for given bank + month.
        /// </summary>
        public Task<DataTable> GetDebitTransactionListAsync(int bankId, int month)
        {
            return GetBankTransactionListAsync(bankId, month, "Debit");
        }















        public async Task<List<dynamic>> GetWorkerChartListDDAsync(int month, string status)
        {
            var param = new Dictionary<string, string>
    {
        {"@Flag", "workerchartListDD"},
        {"@Month", month.ToString()},
        {"@Status", status }
    };

            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);

            var list = new List<dynamic>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new
                {
                    WorkerCode = row["WorkerCode"].ToString(),
                    WorkerName = row["WorkerName"].ToString(),
                    ContactNo = row["ContactNo"].ToString(),
                    Salary = Convert.ToDecimal(row["Salary"]),
                    WorkerType = row["WorkerType"].ToString(),
                    AttendanceMonth = row["AttendanceMonth"].ToString(),
                    PaidDate = row["PaidDate"] == DBNull.Value ? "-" : row["PaidDate"].ToString(),
                    PaymentStatus = row["PaymentStatus"].ToString(),
                    AmountToPaid = row["AmountToPaid"] == DBNull.Value ? 0 : Convert.ToDecimal(row["AmountToPaid"])
                });
            }

            return list;
        }



        // ============================================================
        // 1️⃣ GET WORKER PAYMENT CHART (COMPLETED vs PENDING)
        // ============================================================
        public object GetWorkerPaymentChart(int month)
        {
            var param = new Dictionary<string, string>
{
    { "@Flag", "WorkerPaymentChart" },
    { "@FromDate", new DateTime(DateTime.Now.Year, month, 1).ToString("yyyy-MM-dd") },
    { "@ToDate", new DateTime(DateTime.Now.Year, month,
        DateTime.DaysInMonth(DateTime.Now.Year, month)).ToString("yyyy-MM-dd") }
};

            DataTable dt = _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param).Result;

            int completed = 0, pending = 0;

            if (dt.Rows.Count > 0)
            {
                completed = Convert.ToInt32(dt.Rows[0]["Completed"]);
                pending = Convert.ToInt32(dt.Rows[0]["Pending"]);
            }

            return new
            {
                Completed = completed,
                Pending = pending
            };
        }



        // ============================================================
        // 2️⃣ GET ALL WORKERS PAYMENT DATA  (VIEW ALL BUTTON)
        // ============================================================
        public async Task<List<Dictionary<string, object>>> GetAllWorkersPaymentData()
        {
            var param = new Dictionary<string, string>
    {
        { "@Flag", "AllWorkersPayment" }
    };

            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);
            return ConvertDataTableToList(dt);
        }



        // ============================================================
        // 3️⃣ GET MONTH-WISE PAYMENT LIST (OPTIONAL FOR CHART CLICK)
        // ============================================================
        public async Task<List<Dictionary<string, object>>> GetWorkersPaymentList(int month)
        {
            var param = new Dictionary<string, string>
{
    { "@Flag", "WorkersPaymentList" },
    { "@Month", month.ToString() }
};

            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);

            return ConvertDataTableToList(dt);
        }




        // ============================================================
        // 4️⃣ COMMON FUNCTION → Convert DataTable → List<Dictionary>
        // ============================================================
        private List<Dictionary<string, object>> ConvertDataTableToList(DataTable dt)
        {
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();

            foreach (DataRow row in dt.Rows)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();

                foreach (DataColumn col in dt.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }

                list.Add(dict);
            }

            return list;
        }




































        public async Task<DataTable> GetMaintenanceMemberListAsync(int wingId, string month)
        {
            Dictionary<string, string> param = new Dictionary<string, string>
    {
        { "@Flag", "MaintenanceMemberList" },
        { "@WingId", wingId.ToString() },
        { "@Month", month }
    };

            // Use DataTable return function
            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);

            return dt ?? new DataTable();
        }



        public async Task<decimal> GetDueMaintenanceAmountYP()
        {
            decimal maintenanceDue = 0;
            var param = new Dictionary<string, string> { { "@Flag", "DueMaintenanceYP" } };
            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);
            if (dt.Rows.Count > 0)
            {
                maintenanceDue = Convert.ToDecimal(dt.Rows[0][0]);
            }
            return maintenanceDue;
        }
        /// <summary>
        /// this will show cash in hand
        /// </summary>
        /// <returns></returns>
        public async Task<decimal> CashinHandYP()
        {
            decimal cashInHand = 0;
            var param = new Dictionary<string, string> { { "@Flag", "CashinHandYP" } };
            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);
            if (dt.Rows.Count > 0)
            {
                cashInHand = Convert.ToDecimal(dt.Rows[0][0]);
            }
            return cashInHand;
        }
        /// <summary>
        /// this will show total bank balance
        /// </summary>
        /// <returns></returns>
        public async Task<decimal> TotalBankBalanceYP()
        {
            decimal totalBankBalance = 0;
            var param = new Dictionary<string, string> { { "@Flag", "TotalBalanceYP" } };
            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);
            if (dt.Rows.Count > 0)
            {
                totalBankBalance = Convert.ToDecimal(dt.Rows[0][0]);
            }
            return totalBankBalance;
        }
        /// <summary>
        /// this will show total complaints
        /// </summary>
        /// <returns></returns>
        public async Task<int> TotalComplaintsYP()
        {
            var param = new Dictionary<string, string> { { "@Flag", "TotalComplaintsYP" } };
            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);
            return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 0;
        }
        /// <summary>
        /// this will show pending complaints
        /// </summary>
        /// <returns></returns>
        public async Task<int> PendingComplaintsYP()
        {
            var param = new Dictionary<string, string> { { "@Flag", "PendingComplaintsYP" } };
            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);
            return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 0;
        }
        /// <summary>
        /// this will show solved complaints
        /// </summary>
        /// <returns></returns>
        public async Task<int> SolvedComplaintsYP()
        {
            var param = new Dictionary<string, string> { { "@Flag", "SolvedComplaintsYP" } };
            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);
            return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 0;
        }
        /// <summary>
        /// this will show bank  balance
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<DataSet> BankBalanceYP()
        {
            try
            {
                var param = new Dictionary<string, string>
        {
            { "@Flag", "BankBalanceYP" }
        };

                // Use MSSQL helper instance to call the stored procedure
                var db = new GSTSMSHelper.MSSQL();
                DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);

                // Convert DataTable to DataSet for compatibility
                DataSet ds = new DataSet();
                ds.Tables.Add(dt.Copy());

                return ds;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to fetch bank balance.", ex);
            }
        }

        /// <summary>
        /// this will show top 5 red list member
        /// </summary>
        /// <returns></returns>
        public async Task<List<Dictionary<string, string>>> GetTop5RedListMembersYP()
        {
            var members = new List<Dictionary<string, string>>();
            var param = new Dictionary<string, string> { { "@Flag", "RedListMemberYP" } };
            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);

            foreach (DataRow row in dt.Rows)
            {
                members.Add(new Dictionary<string, string>
            {

                {"FullName", row["FullName"].ToString()},
                                    {"FlatCode", row["FlatCode"].ToString()},
                                    {"MaintenanceAmount", row["MaintenanceAmount"].ToString()},
                                    {"MonthsPending", row["MonthsPending"].ToString()},
                                    {"PendingAmount", row["PendingAmount"].ToString()},
                                    {"PenaltyPercent", row["PenaltyPercent"].ToString()},
                                    {"GraceDate", row["GraceDate"].ToString()},
                                    {"PenaltyAmount", row["PenaltyAmount"].ToString()},
                                    {"FinalPayable", row["FinalPayable"].ToString()},



            });
            }
            return members;
        }

    

        /// <summary>
        /// this will show worker payment chart
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, int>> GetWorkerPaymentChartAsyncYP(int month)
        {
            var chartData = new Dictionary<string, int>();
            var param = new Dictionary<string, string>
        {
            {"@Flag", "WorkerPaymentYP"},
            {"@Month", month.ToString()}
        };
            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);
            chartData["Completed"] = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["Completed"]) : 0;
            chartData["Pending"] = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["Pending"]) : 0;
            return chartData;
        }

        /// <summary>
        /// this will show bank name with its balace for soceity
        /// </summary>
        /// <returns></returns>
        public async Task<List<Dictionary<string, object>>> GetBankShortCodesAsyncYP()
        {
            var param = new Dictionary<string, string> // <-- FIXED HERE
    {
        { "@Flag", "BankNameYP" }
    };

            var dt = await new MSSQL().ExecuteStoreProcedureReturnDataTable("sp_SMS", param);
            var list = new List<Dictionary<string, object>>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Dictionary<string, object>
        {
            { "BankId", Convert.ToInt32(row["BankId"]) },
            { "BankShortCode", row["BankShortCode"].ToString() }
        });
            }

            return list;
        }





        /// <summary>
        /// this will show worker payment member list
        /// </summary>
        /// <param name="month"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public async Task<List<dynamic>> GetWorkerPaymentDetailsAsyncYP(int month, string status)
        {
            var param = new Dictionary<string, string>
    {
        {"@Flag", "WorkerPaymentListYP"},
        {"@Month", month.ToString()},
        {"@Status", status } // 'Completed' or 'Pending'
    };


            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);
            var list = new List<dynamic>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new
                {
                    WorkerName = row["WorkerName"].ToString(),
                    JoiningDate = row["JoiningDate"].ToString(),
                    PaidDate = row["PaidDate"].ToString(),
                    PaymentStatus = row["PaymentStatus"].ToString()
                });
            }
            return list;
        }
        /// <summary>
        /// this will show bank anmes for total transaction
        /// </summary>
        /// <returns></returns>
        public async Task<List<Dictionary<string, string>>> GetBankBalancesCodeYP()
        {
            var param = new Dictionary<string, string>
    {
        { "@Flag", "BankBalanceYP" }
    };

            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);
            var list = new List<Dictionary<string, string>>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Dictionary<string, string>
        {
            { "OpeningBalance", row["OpeningBalance"].ToString() },
            { "BankShortCode", row["BankShortCode"].ToString() }
        });
            }

            return list;
        }





        /// <summary>
        /// this will show wing list
        /// </summary>
        /// <returns></returns>

        public async Task<Dictionary<int, string>> GetWingListAsyncYP()
        {
            var result = new Dictionary<int, string>();
            var param = new Dictionary<string, string> { { "@Flag", "TotalWingYP" } };
            DataTable dt = await _db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);
            foreach (DataRow row in dt.Rows)
            {
                result[Convert.ToInt32(row["WingId"])] = row["WingName"].ToString();
            }
            return result;
        }

        /// <summary>
        /// this will show maintainance status chart
        /// </summary>
        /// <param name="month"></param>
        /// <param name="wingId"></param>
        /// <returns></returns>

        public async Task<DataSet> GetMaintenanceStatusChartAsyncYP(AccountManager obju1)
        {
            var db = new MSSQL();

            var parameters = new Dictionary<string, string>
{
    { "@Flag", "MaintenanceStatusYP" },
    { "@Month", obju1.Month.ToString() },
    { "@Year", obju1.Year.ToString() }   // ✅ NEW PARAMETER
};

            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
        }
        /// <summary>
        /// this will show worker payment list
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// 


        public async Task<List<Dictionary<string, object>>> GetWorkerPaymentListAsyncYP(int month)
        {
            List<Dictionary<string, object>> workerList = new List<Dictionary<string, object>>();

            try
            {
                var db = new MSSQL(); // ✅ Your MSSQL helper class

                // 👉 Using Dictionary<string, object> as per your preference
                Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            { "@Month", month.ToString() },
            { "@Flag", "WorkerPaymentlistYP" }
        };

                DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

                foreach (DataRow row in dt.Rows)
                {
                    var worker = new Dictionary<string, object>
            {
                { "WorkerId", row["WorkerId"] },
                { "WorkerName", row["WorkerName"] },
                { "WorkerContactNo", row["WorkerContactNo"] },
                { "BaseSalary", row["BaseSalary"] },
                { "JoiningDate", row["JoiningDate"] },
                { "WorkerType", row["WorkerType"] },
                { "PaymentStatus", row["PaymentStatus"] }
            };

                    workerList.Add(worker);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to fetch worker payment list: " + ex.Message);
            }

            return workerList;
        }



        /// <summary>
        /// this will show all compaints list
        /// </summary>
        /// <returns></returns>
        /// 


        public async Task<DataTable> GetAllComplaintsYP()
        {
            Dictionary<string, string> param = new Dictionary<string, string>
    {
        { "@Flag", "AllComplaintsYP" }
    };

            MSSQL db = new MSSQL();
            DataSet ds = await db.ExecuteStoreProcedureReturnDS("sp_SMS", param);

            if (ds != null && ds.Tables.Count > 0)
                return ds.Tables[0];

            return new DataTable();
        }

        #endregion


        #region********************************************************************* Society Ac.Details Bank ***********************************************************


        // <summary>
        /// This method fetches bank account details from the database, adds bank name/branch using IFSC code, and returns the full list.Date 01-07-2025
        ///  </summary>

        public async Task<List<AccountManager>> GetAllBankDetailsSS()
        {
            try
            {
                MSSQL db = new MSSQL();
                var parameters = new Dictionary<string, string>
          {
              { "@Flag", "FetchBankDetailsSS" }
          };

                DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
                List<AccountManager> list = new List<AccountManager>();

                foreach (DataRow row in dt.Rows)
                {
                    string ifsc = row["IFSCCode"].ToString();
                    var bankInfo = await GetBankDetailsFromIfscSS(ifsc);

                    list.Add(new AccountManager
                    {
                        BankCode = row["BankCode"].ToString(),
                        IFSCCode = ifsc,
                        BankId = Convert.ToInt32(row["BankId"]),
                        AccountNo = row["BankAccountNumber"].ToString(),
                        OpeningBalance = row.Field<decimal?>("Balance") ?? 0,
                        AddedDate = row.Field<DateTime?>("Date") ?? DateTime.MinValue,
                        BankName = bankInfo?.Bank ?? "N/A",
                        Branch = bankInfo?.Branch ?? "N/A",
                        BankHolderName = row["BankHolderName"].ToString(),
                        BankBalance = row["BankBalance"].ToString(),
                        AccountType = row["AccountType"].ToString(),
                        IsActive = row["ISActive"] != DBNull.Value ? Convert.ToBoolean(row["IsActive"]) : false

                    });
                }

                return list;
            }
            catch (Exception)
            {

                return new List<AccountManager>();
            }
        }
        /// <summary>
        /// This method updates the active/inactive status of a bank in the database using the flag 'UpdateBankStatusSS'.Date 02-07-2025
        /// </summary>

            
        public async Task<bool> UpdateBankStatusSS(int bankId, bool isActive)
        {
            try
            {
                MSSQL db = new MSSQL();
                var parameters = new Dictionary<string, string>
     {
         { "@Flag", "UpdateBankStatusSS" },
         { "@BankId", bankId.ToString() },
         { "@IsActive", isActive ? "1" : "0" }
     };

                int result = await db.ExecuteStoreProcedureNonQuery("sp_SMS", parameters);
                return result > 0;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Calls the Razorpay IFSC API using the given IFSC code and retrieves bank details like bank name and branch.Date 03-07-2025
        /// </summary>


        private async Task<IfscApiResponse> GetBankDetailsFromIfscSS(string ifsc)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync($"https://ifsc.razorpay.com/{ifsc}");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<IfscApiResponse>(json);
                    }
                }
            }
            catch (Exception)
            {
                // Optionally log the error
            }
            return null;
        }

        /// <summary>
        /// Represents the response from the Razorpay IFSC API containing the bank name and branch name for a given IFSC code.Date 03-07-2025
        /// </summary>

        public class IfscApiResponse
        {
            [JsonProperty("BANK")]
            public string Bank { get; set; }

            [JsonProperty("BRANCH")]
            public string Branch { get; set; }
        }

        /// <summary>
        /// Show transaction list to each bank through the transaction
        /// 03/07/2025
        /// MS
        /// </summary>

        public async Task<List<AccountManager>> GetTransactionStatementByBankMS(string BankCode)
        {
            try
            {
                MSSQL db = new MSSQL();
                var parameters = new Dictionary<string, string>
        {
            { "@Flag", "FetchTransactionStatementMS" },
            { "@BankCode", BankCode }
        };

                DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
                List<AccountManager> list = new List<AccountManager>();

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new AccountManager
                    {
                        TransactionId_ChequeId = row["TransactionId_ChequeId"].ToString(),
                        PaymentPurpose = row["PaymentPurpose"].ToString(), // Existing column
                        ReceiverName = row["ReceiverName"].ToString(),
                        PaidDate = Convert.ToDateTime(row["PaidDate"]),
                        Amount = Convert.ToDecimal(row["Amount"]),
                        Status = row["Status"].ToString()
                    });
                }

                return list;

            }
            catch (Exception)
            {
                // Optionally log the error
                return new List<AccountManager>();
            }
        }

        /// <summary>
        /// this method create to Add Society Bank and save in database  and chechdupicateid and duplicate upi id 
        ///  04/07/2025
        ///  MS
        /// </summary>
        /// <param name="AccountNo"></param>
        /// <returns></returns>
        public async Task<bool> IsDuplicateAccountMS(string accountNo)
        {
            MSSQL db = new MSSQL();

            var checkParams = new Dictionary<string, string>
    {
        { "@flag", "CheckDuplicateAccountNoMS" },
        { "@AccountNo", accountNo }
    };

            DataTable result = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", checkParams);

            return result != null &&
                   result.Rows.Count > 0 &&
                   result.Rows[0]["ExistsFlag"].ToString() == "1";
        }
        public async Task<bool> IsDuplicateUPIMS(string upiId)
        {
            MSSQL db = new MSSQL();

            var checkParams = new Dictionary<string, string>
{
    { "@flag", "CheckDuplicateUPIMS" },
    { "@UPIId", upiId }
};

            DataTable result = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", checkParams);

            return result != null &&
                   result.Rows.Count > 0 &&
                   result.Rows[0]["ExistsFlag"].ToString() == "1";
        }


        public async Task<string> AddBankSocietyMS(AccountManager objbal)
        {
            try
            {
                MSSQL db = new MSSQL();

                // Check duplicate AccountNo
                var checkParams = new Dictionary<string, string>
                {
                    { "@flag", "CheckDuplicateAccountNoMS" },
                    { "@AccountNo", objbal.AccountNo }
                };

                DataTable checkResult = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", checkParams);
                if (checkResult != null && checkResult.Rows.Count > 0 && checkResult.Rows[0]["ExistsFlag"].ToString() == "1")
                {
                    return "duplicate_account";
                }

                // Check duplicate UPI
                if (!string.IsNullOrEmpty(objbal.UPIId))
                {
                    var upiDuplicate = await IsDuplicateUPIMS(objbal.UPIId);
                    if (upiDuplicate)
                        return "duplicate_upi";
                }

                // Insert Bank Details
                var parameters = new Dictionary<string, string>
    {
        { "@flag", "AddBankSocietyMS" },
        { "@AllCode", objbal.AllCode ?? "SC001" },
        { "@AccountTypeId", objbal.AccountTypeId.ToString() },
        { "@OpeningBalance", objbal.OpeningBalance.ToString() },
        { "@IFSCCode", objbal.IFSCCode },
        { "@UPIId", objbal.UPIId ?? string.Empty },
        { "@AccountNo", objbal.AccountNo },
        { "@AddedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
        { "@ISActive", objbal.IsActive.ToString() }
    };

                var resultTable = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

                if (resultTable != null && resultTable.Rows.Count > 0)
                {
                    string statusCode = resultTable.Rows[0]["StatusCode"].ToString();
                    return statusCode == "1" ? "success" : "fail";
                }

                return "fail";
            }
            catch (Exception)
            {
                return "fail";
            }
        }




        /// <summary>
        /// fetch in the dropdown BankType like Saving ,Current 
        ///  04/07/2025
        ///  Ms
        /// </summary>
        /// <returns></returns>
        public async Task<DataSet> FetchAccountTypeMS()
        {
            try
            {
                MSSQL db = new MSSQL();

                var parameters = new Dictionary<string, string>
    {
        { "@flag", "FetchAccountTypeMS" }
    };

                DataSet ds = await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
                return ds;
            }
            catch (Exception)
            {

                return new DataSet();
            }
        }


        #endregion


        #region********************************************************************* Society Ac.Details Cash ***********************************************************
        /// <summary>
        /// Asynchronously fetches a list of cash transaction records from the database using a stored procedure ("sp_SMS").
        /// The stored procedure is executed with the flag "FetchCashTransactionDD".
        /// Each row from the returned DataTable is mapped to an <see cref="AccountManager"/> object with relevant transaction details.
        /// </summary>
        /// <returns>
        /// A task representing the asynchronous operation, with a result of a list of <see cref="AccountManager"/> objects.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when an error occurs while executing the stored procedure or mapping the data.
        /// </exception>

        /// <summary>
        /// Asynchronously fetches a list of cash transaction records from the database using a stored procedure ("sp_SMS").
        /// The stored procedure is executed with the flag "FetchCashTransactionDD".
        /// Each row from the returned DataTable is mapped to an <see cref="AccountManager"/> object with relevant transaction details.
        /// </summary>


        public async Task<List<AccountManager>> CashTransactionDD()
        {
            try
            {
                MSSQL db = new MSSQL();

                var parameters = new Dictionary<string, string>
  {
      { "@Flag", "FetchCashTransactionDD" }
  };

                DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

                var transactions = new List<AccountManager>();

                foreach (DataRow dr in dt.Rows)
                {
                    transactions.Add(new AccountManager
                    {
                        TransactionId = dr.Table.Columns.Contains("TransactionId") ? Convert.ToInt32(dr["TransactionId"]) : 0,
                        TransactionCode = dr.Table.Columns.Contains("TransactionCode") ? dr["TransactionCode"].ToString() : "-",
                        EntityCode = dr.Table.Columns.Contains("EntityCode") ? dr["EntityCode"].ToString() : "-",
                        PaymentByName = dr.Table.Columns.Contains("PaymentByName") ? dr["PaymentByName"].ToString() : "-",
                        PaidToName = dr.Table.Columns.Contains("PaidToName") ? dr["PaidToName"].ToString() : "-",
                        Amount = dr.Table.Columns.Contains("Amount") ? Convert.ToDecimal(dr["Amount"]) : 0,
                        PaymentModeName = dr.Table.Columns.Contains("PaymentModeName") ? dr["PaymentModeName"].ToString() : "-",
                        PaymentPurpose = dr.Table.Columns.Contains("PaymentPurpose") ? dr["PaymentPurpose"].ToString() : "-",
                        TransactionId_ChequeId = dr.Table.Columns.Contains("TransactionId_ChequeId") ? dr["TransactionId_ChequeId"].ToString() : "-",
                        PaidDate = dr.Table.Columns.Contains("PaidDate") ? Convert.ToDateTime(dr["PaidDate"]) : DateTime.MinValue,
                        TransactionNature = dr.Table.Columns.Contains("TransactionNature") ? dr["TransactionNature"].ToString() : "-",
                        Document = dr.Table.Columns.Contains("Documents") ? dr["Documents"].ToString() : "-",
                        CategoryName = dr.Table.Columns.Contains("CategoryName") ? dr["CategoryName"].ToString() : "-"
                    });
                }

                return transactions;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in CashTransactionDD(): " + ex.Message, ex);
            }
        }

        ///////////////////////////////////////////
        ////////////////////////////////////////////////////////////// Shruti Mane ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Fetches the list of available transaction types from the database.
        /// 30/06/2025
        /// </summary>

        public async Task<DataSet> FetchTransactionTypeAsyncSM()
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
   {
    { "@Flag", "TransactionTypeSM" }
    };
            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
        }

        /// <summary>
        /// Fetches the list of available Society Bank from the database.
        /// 15/08/2025
        /// </summary>
        public async Task<DataSet> FetchMaintainanceTypeSM()
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
   {
    { "@Flag", "FetchMaintainanceTypeSM" }
    };
            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
        }


        public async Task<DataSet> FetchBanksForChequeSM()
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
   {
    { "@Flag", "FetchBankofChequeSM" }
    };
            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
        }

        /// <summary>
        /// Fetches the list of available cash Amount From the database.
        /// 14/08/2025
        /// </summary>

        public async Task<DataSet> FetchCashAmountSM()
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
   {
    { "@Flag", "FetchCashAmountSM" }
    };
            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
        }


        /// <summary>
        /// Fetches the list of all workers from the database.
        /// 30/06/2025
        /// </summary>
        public async Task<DataSet> FetchWorkerAsyncSM()
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "FetchWorkerSM" }
};
            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
        }

        /// <summary>
        /// Fetches the list of all registered members from the database.
        /// 01/07/2025
        /// </summary>

        public async Task<DataSet> FetchMemberAsyncSM()
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "FetchmembersSM" }
};
            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
        }


        /// <summary>
        /// Fetches the list of all vendors from the database.
        /// 01/07/2025
        /// </summary>


        public async Task<DataSet> FetchVendorAsyncSM()
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "FetchVendorSM" }
};
            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
        }

        /// <summary>
        /// Fetches the list of event handlers from the database.
        /// 01/07/2025
        /// </summary>

        public async Task<DataSet> FetchEventHandlersAsyncSM()
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "FetchEventHandlersSM" }
};
            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
        }

        /// <summary>
        /// Fetches all maintenance records for a specific month and year.
        /// </summary>
        /// 01/07/2025
        /// <param /name=""/>AccountManager object containing Month and Year</param>



        public async Task<DataSet> FetchMaintenanceAsyncSM(AccountManager obju1)
        {
            var db = new MSSQL();

            var parameters = new Dictionary<string, string>
{
    { "@Flag", "FetchingMaintenanceSM" },
    { "@Month", obju1.Month.ToString() } ,// सुनिश्चित कर की हे string मध्ये convert होतंय
         { "@Year", obju1.Year.ToString() } // सुनिश्चित कर की हे string मध्ये convert होतंय
};

            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
        }


        /// <summary>
        /// Fetches members associated with a specific maintenance code.
        /// </summary>
        /// 02/07/2025
        /// <param /name=""/>AccountManager object containing MaintenanceCode</param>

        public async Task<DataSet> FetchMaintenancebyMaintenanceAsyncSM(AccountManager obju)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "FetchingMembersASperwingSM" },
    { "@MaintenanceCode", obju.MaintenanceCode }
};
            return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
        }

        /// <summary>
        /// Saves a new cash or cheque transaction to the database.
        /// Returns the generated TransactionCode if successful.
        /// </summary>


        public async Task<string> SaveCashTransactionAsyncSM(AccountManager objAcc)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
    {
        { "@Flag", "SavingCashChequeTransactionSM" },  // New flag
        //{ "@BankCodeName", objAcc.BankName  },
        { "@BankCodeName", objAcc.BankCode  },
        { "@EntityCode", objAcc.EntityCode  },
        { "@PaymentBy", objAcc.PaymentBy },
        { "@PaidTo", objAcc.PaidTo  },
        { "@Amount", objAcc.Amount.ToString() },
        { "@PaymentMode", objAcc.PaymentMode.ToString() },
        { "@PaymentPurpose", objAcc.PaymentPurpose },
        { "@ChequeId", objAcc.ChecqueNo  },
        { "@TransactionType", objAcc.TransactionId.ToString() }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

            if (dt.Rows.Count > 0)
                return dt.Rows[0]["TransactionCode"].ToString();

            return null;
        }

        /// <summary>
        /// Saves attachment metadata related to a cash transaction to the database.
        /// </summary>

        public async Task<bool> SaveAttachmentAsyncSM(AccountManager objacc)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
    {
        { "@Flag", "SavingAttachmentSM" }, // New flag
        { "@TransactionCode", objacc.TransactionCode },
        { "@AttachmentPath", objacc.AttachmentPath },
        { "@PaymentMode", objacc.PaymentMode.ToString() }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            return dt.Rows.Count > 0;
        }




        /// <summary>
        /// Retrieves receipt details for a given transaction code.
        /// </summary>
        /// <param /name="transactionCode">/The code of the transaction</param>
        /// 08/07/2025
        /// <returns>DataTable containing receipt information</returns>

        public async Task<DataTable> GetReceiptDataAsyncSM(string transactionCode)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "ReceiptData1SM" },
    { "@TransactionCode", transactionCode }
};

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            return dt;
        }


        /// <summary>
        /// Retrieves detailed maintenance item data for a given maintenance code.
        /// </summary>
        /// <param /name = "maintenanceCode" >/ The code of the maintenance entry</param>
        /// 04/07/2025
        /// <returns>DataTable containing maintenance item details</returns>

        public async Task<DataTable> GetMaintenanceItemsAsyncSM(string maintenanceCode)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "ReceiptData2SM" },
    { "@MaintenanceCode", maintenanceCode }
};

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            return dt;
        }


        /// <summary>
        /// Saves the generated PDF path for a receipt in the database.
        /// </summary>
        /// <param /name="objAcc">/AccountManager object with transaction and PDF path</param>
        /// 07/06/2025
        /// <returns>The result as a string (e.g., success flag or path)</returns>
        public async Task<string> SaveReceiptpdfpathAsyncSM(AccountManager objAcc)
        {
            var db = new MSSQL();

            var parameters = new Dictionary<string, string>
{
    { "@Flag", "InsertReceiptPdfSM" },
    { "@PaymentMode", objAcc.PaymentMode.ToString() },
    { "@TransactionCode", objAcc.TransactionCode },
    { "@AttachmentPath", objAcc.AttachmentPath }
};

            // Call your helper to execute and get scalar result
            object result = await db.ExecuteStoreProcedureReturnObj("sp_SMS", parameters);

            return result?.ToString();
        }


        /// <summary>
        /// Retrieves salary slip details for a worker using their WorkerCode.
        /// </summary>
        /// <param /name="WorkerCode">/The unique code of the worker</param>
        /// 07/07/2025
        /// <returns>DataTable containing worker salary slip details</returns>


        public async Task<DataTable> GenerateSalarySlipSM(string transactionCode)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
    {
        { "@Flag", "FetchingWorkerDetailsSM" },
        { "@TransactionCode", transactionCode }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            return dt;
        }


        /// <summary>
        /// Marks the specified expense as paid in the database by calling the stored procedure
        /// with flag 'UpdateExpensepaid'. This sets StatusId = 10 for the given ExpenseCode.
        /// </summary>
        /// 12/07/2025
        /// <param /name="expenseCode"/>The unique code of the expense to update.</param>


        public async Task MarkExpenseAsPaidAsyncSM(string expenseCode)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "UpdateExpensepaidSM" },
    { "@ExpenseCode", expenseCode }
};

            await db.ExecuteStoreProcedure("sp_SMS", parameters);
        }


        /// <summary>
        /// Marks the specified event budget as paid in the database by calling the stored procedure
        /// with flag 'UpdateEventpaid'. This sets BudgetStatus = 10 for the given EventCode.
        /// </summary>
        /// 12/07/2025
        /// <param /name="eventCode">/The unique code of the event to update.</param>


        public async Task MarkEventBudgetAsPaidAsyncSM(string eventCode)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "UpdateEventpaidSM" },
    { "@EventCode", eventCode }
};

            await db.ExecuteStoreProcedure("sp_SMS", parameters);
        }

        #endregion


        #region********************************************************************* Worker Pay Manage ***********************************************************

        /// <summary>
        /// Asynchronously executes a stored procedure to retrieve worker payment and account details from the database.
        /// Maps the resulting data rows into a list of <see cref="AccountManager"/> objects, handling nulls and conversions safely.
        /// Throws an exception if the database call or data mapping fails.
        /// (Updated: 18-Aug-2025)
        /// </summary>

        public async Task<List<AccountManager>> FetchWorkerInformationADAsync()
        {
            try
            {
                var parameters = new[]
                {
            new SqlParameter("@Flag", "WorkersPaymentList_AD")
        };

                DataTable dt = await MSSQL.ExecuteStoredProcedure("sp_SMS", parameters)
                    .ConfigureAwait(false);

                if (dt == null || dt.Rows.Count == 0)
                    return new List<AccountManager>();

                var workers = new List<AccountManager>();

                foreach (DataRow row in dt.Rows)
                {
                    if (row == null) continue;

                    string paymentStatus = row["PaymentStatus"]?.ToString() ?? string.Empty;

                    workers.Add(new AccountManager
                    {
                        WorkerCode = row["WorkerCode"]?.ToString() ?? string.Empty,
                        RoleName = row["Role"]?.ToString() ?? string.Empty,
                        StaffName = row["WorkerName"]?.ToString() ?? string.Empty,
                        Contact = row["Contact"]?.ToString() ?? string.Empty,
                        DateOfJoining = row["Date"] != DBNull.Value ? Convert.ToDateTime(row["Date"]) : DateTime.MinValue,
                        BaseSalary = row["BaseSalary"] != DBNull.Value ? Convert.ToDecimal(row["BaseSalary"]) : 0m,
                        AccountNo = row["AccountNo"]?.ToString() ?? string.Empty,
                        IFSCCode = row["IFSC Code"]?.ToString() ?? string.Empty,
                        WorkerUPI = row["Worker UPI"]?.ToString() ?? string.Empty,
                        AttendanceMonth = row["AttendanceMonth"]?.ToString() ?? string.Empty,
                        DaysPresent = row["DaysPresent"] != DBNull.Value ? Convert.ToInt32(row["DaysPresent"]) : 0,
                        PerdayPayment = row["PerdayPayment"] != DBNull.Value ? Convert.ToDecimal(row["PerdayPayment"]) : 0m,
                        AmountToBePaid = row["AmountToBePaid"] != DBNull.Value ? Convert.ToDecimal(row["AmountToBePaid"]) : 0m,
                        IsPaid = paymentStatus.Trim().Equals("paid", StringComparison.OrdinalIgnoreCase),
                        IsSelected = false
                    });
                }

                return workers;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to fetch worker data: " + ex.Message, ex);
            }
        }




        public async Task ProcessWorkerPaymentAsyncAD(AccountManager input)
        {
            int paymentModeId = await GetPaymentModeFromRazorpayAsyncAD(input.TransactionId_ChequeId);
            if (paymentModeId == 0 && int.TryParse(input.PaymentMode.ToString(), out int fallback))
                paymentModeId = fallback;

            // Strictly use session value for PaymentBy
            string societyCode = HttpContext.Current?.Session["SocietyCode"]?.ToString();

            if (string.IsNullOrEmpty(societyCode))
                throw new Exception("SocietyCode session value is missing. Cannot proceed with transaction.");

            var transaction = new AccountManager
            {
                BankName_Code = input.BankName_Code,
                TransactionCode = input.TransactionCode,
                EntityCode = input.EntityCode,
                PaymentBy = societyCode,
                PaidTo = input.PaidTo,
                UPIId = input.UPIId,
                Amount = input.Amount,
                PaymentMode = paymentModeId.ToString(),
                PaymentPurpose = input.PaymentPurpose ?? "Monthly Salary",
                TransactionId_ChequeId = input.TransactionId_ChequeId,
                PaidDate = DateTime.Now,
                TransactionType = input.TransactionType != 0 ? input.TransactionType : 27
            };

            await InsertTransactionAsyncAD(transaction);
        }




        public async Task InsertTransactionAsyncAD(AccountManager transaction)
        {
            var parameters = new Dictionary<string, string>
{
    { "@flag", "TransactionPaymentbyAccountant_AD" },
    { "@BankName_Code", transaction.BankName_Code },
    { "@TransactionCode", transaction.TransactionCode ?? "" },
    { "@EntityCode", transaction.EntityCode ?? "" },
    { "@PaymentBy", transaction.PaymentBy },
    { "@PaidTo", transaction.PaidTo },
    { "@Amount", transaction.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture) },
    { "@PaymentMode", transaction.PaymentMode.ToString() },
    { "@PaymentPurpose", transaction.PaymentPurpose },
    { "@TransactionId_ChequeId", transaction.TransactionId_ChequeId },
    //{ "@PaidDate", transaction.PaidDate.ToString("yyyy-MM-dd HH:mm:ss") },
    { "@TransactionType", transaction.TransactionType.ToString() },
    { "@UPIId", transaction.UPIId ?? "" }
};

            await db.ExecuteStoreProcedure("sp_SMS", parameters);
        }



        /// <summary>
        /// Asynchronously saves a cash payment transaction by executing a stored procedure with payment details.
        /// Passes relevant parameters to the database, including cash-specific transaction type.
        /// Returns the generated transaction code if the operation is successful; otherwise, returns null.
        /// (Updated: 18-Aug-2025)
        /// </summary>




      


        public async Task<string> SaveCashTransactionAsyncAD(AccountManager objAcc)
        {
            var db = new MSSQL();

            string staffCode = HttpContext.Current?.Session["StaffCode"]?.ToString() ?? "UnknownStaff";

            var parameters = new Dictionary<string, string>
    {
        { "@Flag", "InsertCashPayment_AD" },
        { "@BankName_Code", objAcc.BankName_Code },
        { "@EntityCode", objAcc.EntityCode },
        { "@PaymentBy", staffCode },
        { "@PaidTo", objAcc.PaidTo },
        { "@Amount", objAcc.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture) },
        { "@PaymentMode", objAcc.PaymentMode.ToString() },
        { "@PaymentPurpose", objAcc.PaymentPurpose },
        { "@TransactionId_ChequeId", objAcc.ChecqueNo },
        { "@TransactionType", "27" }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

            if (dt.Rows.Count > 0)
                return dt.Rows[0]["TransactionCode"].ToString();

            return null;
        }


        /// <summary>
        /// Asynchronously fetches payment data for a single worker and attendance month by executing a stored procedure.
        /// Constructs the required SQL parameters and sends the request to the database.
        /// Returns the resulting data table containing the worker’s payment details.
        /// (Updated: 18-Aug-2025)
        /// </summary>

        public async Task<DataTable> FetchSingleWorkerPaymentDataAD(string workerCode, string attendanceMonth)
        {
            var parameters = new Dictionary<string, string>
{
    { "@flag", "FetchSingleWorkerPaymentData_AD" },
    { "@WorkerCode", workerCode },
    { "@AttendanceMonth", attendanceMonth }
};

            return await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
        }




        /// <summary>
        /// Asynchronously retrieves a worker’s salary transaction information for a given attendance month from the database.
        /// Sets up SQL parameters and executes a stored procedure to fetch relevant data.
        /// Returns a data table containing the worker's payment details.
        /// (Updated: 18-Aug-2025)
        /// </summary>
        public async Task<DataTable> FetchWorkerPaymentDetailsAD(string workerCode, string attendanceMonth)
        {
            var parameters = new Dictionary<string, string>
{
    { "@flag", "FetchWorkerSalaryTxnInfo_AD" },
    { "@WorkerCode", workerCode },
    { "@AttendanceMonth", attendanceMonth }
};

            return await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
        }



        /// <summary>
        /// Fetches payment method from Razorpay using the payment ID,
        /// and maps it to a specific integer code.
        /// </summary>
        /// <param name="paymentId">The Razorpay payment ID.</param>
        /// <returns>
        /// Integer code based on payment method:
        /// 33 = UPI, 35 = NetBanking, 36 = Card, 37 = Wallet, 0 = Unknown/Error.
        /// </returns>

        private async Task<int> GetPaymentModeFromRazorpayAsyncAD(string paymentId)
        {
            try
            {
                var key = "rzp_test_tnu8pNChRc5VBE";
                var secret = "wVSn4S0P2BpbvIiol3zLUzLG";
                var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{key}:{secret}"));

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                    var response = await client.GetAsync($"https://api.razorpay.com/v1/payments/{paymentId}");

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var json = Newtonsoft.Json.Linq.JObject.Parse(jsonString);
                        string method = json["method"]?.ToString();

                        if (method == "upi") return 33;
                        if (method == "netbanking") return 35;
                        if (method == "card") return 36;
                        if (method == "wallet") return 37;
                    }
                }
            }
            catch (Exception)
            {
                // Log error if needed
            }

            return 0;
        }







        /// <summary>
        /// Fetches bank details of all society accountants from the database.
        /// </summary>
        /// <returns>List of accountant bank details.</returns>

        public async Task<List<AccountManager>> FetchingAccountantBankDetailsADD()
        {
            SqlParameter[] parameters = {
        new SqlParameter("@Flag", "FetchSocietyAccountantBankDetails_AD")
    };

            DataTable dt = await MSSQL.ExecuteStoredProcedure("sp_SMS", parameters);
            List<AccountManager> banks = new List<AccountManager>();

            foreach (DataRow row in dt.Rows)
            {
                banks.Add(new AccountManager
                {
                    BankCode = row["BankCode"].ToString(),
                    IFSCCode = row["IFSCCode"].ToString(),
                    OpeningBalance = row["OpeningBalance"] != DBNull.Value ? Convert.ToDecimal(row["OpeningBalance"]) : 0,
                    AccountNo = row["AccountNo"].ToString(),
                    AccountTypeName = row["AccountTypeName"].ToString()
                });
            }

            return banks;
        }
        /// <summary>
        /// Retrieves the current cash-in-hand amount from the database.
        /// </summary>
        /// <returns>A list of <see cref="AccountManager"/> containing cash-in-hand values.</returns>
        /// <exception cref="Exception">Thrown when the operation fails to fetch cash amount.</exception>


        public async Task<List<AccountManager>> GetCashinHandAD()
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@Flag", "CashinHandAB")
                };

                DataTable dt = await MSSQL.ExecuteStoredProcedure("sp_SMS", parameters);
                List<AccountManager> result = new List<AccountManager>();

                foreach (DataRow row in dt.Rows)
                {
                    result.Add(new AccountManager
                    {
                        SocietyCashAmount = row["CashInHand"].ToString()
                    });
                }


                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Fails to fetch bank Cash Amount: " + ex.Message, ex);
            }
        }


        #endregion


        #region********************************************************************* Maintaince Management ***********************************************************

        /// <summary>
        /// This method is use for fetch list
        /// </summary>
        /// <returns> Return member list</returns>
        public async Task<List<AccountManager>> GetMemberMaintenanceListSY()
        {
            List<AccountManager> list = new List<AccountManager>();

            try
            {
                Dictionary<string, string> para = new Dictionary<string, string>
        {
            { "@flag", "FetchMemberListSY" }
        };

                DataSet ds = await db.ExecuteStoreProcedureReturnDS("sp_SMS", para);

                if (ds != null && ds.Tables.Count > 0)
                {
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        AccountManager obj = new AccountManager();

                        //obj.SrNo = Convert.ToInt32(dr["Sr No"]);
                        obj.EntityCode = dr["EntityCode"].ToString();
                        obj.MemberCode = dr["MemberCode"].ToString();

                        // ✅ Correct column names from SQL
                        obj.FullName = dr["FullName"].ToString();
                        obj.Email = dr["Email"].ToString();
                        obj.WingName = dr["WingName"].ToString();
                        obj.FlatCode = dr["FlatCode"].ToString();

                        obj.ScheduleDate = dr["ScheduleDate"] != DBNull.Value
                            ? Convert.ToDateTime(dr["ScheduleDate"])
                            : (DateTime?)null;

                        obj.PaidDate = dr["PaidDate"] != DBNull.Value
                            ? Convert.ToDateTime(dr["PaidDate"])
                            : (DateTime?)null;

                        if (dr["MaintananceTypeId"] != DBNull.Value)
                            obj.MaintananceTypeId = Convert.ToInt32(dr["MaintananceTypeId"]);

                        if (dr["MaintenanceType"] != DBNull.Value)
                            obj.MaintananceType = dr["MaintenanceType"].ToString();

                        obj.TotalAmount = dr["MaintenanceAmount"] != DBNull.Value
                            ? Convert.ToDecimal(dr["MaintenanceAmount"])
                            : 0;

                        obj.PaidAmount = dr["PaidAmount"] != DBNull.Value
                            ? Convert.ToDecimal(dr["PaidAmount"])
                            : 0;

                        obj.PenaltyPercent = dr["PenaltyPercent"] != DBNull.Value
                            ? Convert.ToDecimal(dr["PenaltyPercent"])
                            : 0;

                        obj.PenaltyAmount = dr["PenaltyAmount"] != DBNull.Value
                            ? Convert.ToDecimal(dr["PenaltyAmount"])
                            : 0;

                        obj.FinalPayable = dr["FinalPayable"] != DBNull.Value
                            ? Convert.ToDecimal(dr["FinalPayable"])
                            : 0;

                        obj.Status = dr["Status"].ToString();



                        list.Add(obj);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetMemberMaintenanceListSY: " + ex.Message);
            }

            return list;
        }


        /// <summary>
        /// This methid is use for fetch member details on partial view
        /// </summary>


        public async Task<List<AccountManager>> GetMemberMaintenanceDetailsSY(
     string memberCode,
     string maintainanceTypeId,
     string entityCode,
     DateTime? paidDate)   // ✅ extra param
        {
            List<AccountManager> list = new List<AccountManager>();

            try
            {
                var parameters = new Dictionary<string, string>
        {
            { "@flag", "MemberMaintenanceDetailsSY" },
            { "@MemberCode", memberCode },
            { "@MaintananceTypeId", maintainanceTypeId },
            { "@EntityCode", entityCode }
        };

                // ✅ जर paidDate आला असेल तर parameter add कर
                if (paidDate.HasValue)
                {
                    parameters.Add("@PaidDate", paidDate.Value.ToString("yyyy-MM-dd"));
                }
                else
                {
                    parameters.Add("@PaidDate", null); // pass null
                }

                MSSQL objSql = new MSSQL();
                DataTable dt = await objSql.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new AccountManager
                    {
                        FullName = row["FullName"]?.ToString(),
                        FlatCode = row["FlatCode"]?.ToString(),
                        PaidDate = row["PaidDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["PaidDate"]),
                        ScheduleDate = row["ScheduleDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["ScheduleDate"]),
                        MaintenanceName = row["MaintenanceName"]?.ToString(),
                        ChargeAmount = row["ChargeAmount"] != DBNull.Value ? Convert.ToDecimal(row["ChargeAmount"]) : 0,
                        TotalAmount = row["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(row["TotalAmount"]) : 0,
                        MemberCode = row["MemberCode"]?.ToString(),
                        EntityCode = row["EntityCode"]?.ToString(),
                        MaintananceTypeId = row["MaintananceTypeId"] != DBNull.Value ? Convert.ToInt32(row["MaintananceTypeId"]) : (int?)null,
                        PaidAmount = row["PaidAmount"] != DBNull.Value ? Convert.ToDecimal(row["PaidAmount"]) : 0,
                        PenaltyPercent = row["PenaltyPercent"] != DBNull.Value ? Convert.ToDecimal(row["PenaltyPercent"]) : 0,
                        PenaltyAmount = row["PenaltyAmount"] != DBNull.Value ? Convert.ToDecimal(row["PenaltyAmount"]) : 0
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetMemberMaintenanceDetailsSY: " + ex.Message);
            }

            return list;
        }

        /// <summary>
        /// This methid is use for fetch  yearly member maintenance details on partial view
        /// </summary>
        public async Task<List<AccountManager>> GetYearlyMaintainenaceDetailsSY(string memberCode, string maintainanceTypeId)
        {
            List<AccountManager> list = new List<AccountManager>();

            try
            {
                var parameters = new Dictionary<string, string>
{
    { "@flag", "MemberYearlyMaintainanceDetailsSY" },
    { "@MemberCode", memberCode },
    { "@MaintananceTypeId", maintainanceTypeId }
};


                MSSQL objSql = new MSSQL();
                DataTable dt = await objSql.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new AccountManager
                    {
                        FullName = row["FullName"]?.ToString(),
                        FlatCode = row["FlatCode"]?.ToString(),
                        PaidDate = row["PaidDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["PaidDate"]),
                        ScheduleDate = row["ScheduleDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["ScheduleDate"]),
                        MaintenanceName = row["MaintenanceName"]?.ToString(),
                        ChargeAmount = row["ChargeAmount"] != DBNull.Value ? Convert.ToDecimal(row["ChargeAmount"]) : 0,
                        TotalAmount = row["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(row["TotalAmount"]) : 0,
                        MemberCode = row["MemberCode"]?.ToString(),
                        EntityCode = row["EntityCode"]?.ToString(),
                        MaintananceTypeId = row["MaintananceTypeId"] != DBNull.Value ? Convert.ToInt32(row["MaintananceTypeId"]) : (int?)null,
                        PaidAmount = row["PaidAmount"] != DBNull.Value ? Convert.ToDecimal(row["PaidAmount"]) : 0,
                        PenaltyPercent = row["PenaltyPercent"] != DBNull.Value ? Convert.ToDecimal(row["PenaltyPercent"]) : 0,
                        PenaltyAmount = row["PenaltyAmount"] != DBNull.Value ? Convert.ToDecimal(row["PenaltyAmount"]) : 0
                    });
                }
            }
            catch (Exception ex)
            {
                // Optionally log the error or handle it accordingly
                Console.WriteLine("Error in GetMemberMaintenanceDetailsSY: " + ex.Message);
            }

            return list;
        }
        /// <summary>
        /// This methid is use for fetch  monthly member maintenance details on partial view
        /// </summary>
        public async Task<List<AccountManager>> GetMemberMonthlyMaintenanceDetailsSY(
   string memberCode,
   string maintainanceTypeId,
   string entityCode,
   DateTime? paidDate)   // ✅ extra param
        {
            List<AccountManager> list = new List<AccountManager>();

            try
            {
                var parameters = new Dictionary<string, string>
        {
            { "@flag", "MonthlyMaintenanceDetailsSY" },
            { "@MemberCode", memberCode },
            { "@MaintananceTypeId", maintainanceTypeId },
            { "@EntityCode", entityCode }
        };

                // ✅ जर paidDate आला असेल तर parameter add कर
                if (paidDate.HasValue)
                {
                    parameters.Add("@PaidDate", paidDate.Value.ToString("yyyy-MM-dd"));
                }
                else
                {
                    parameters.Add("@PaidDate", null); // pass null
                }

                MSSQL objSql = new MSSQL();
                DataTable dt = await objSql.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new AccountManager
                    {
                        FullName = row["FullName"]?.ToString(),
                        FlatCode = row["FlatCode"]?.ToString(),
                        PaidDate = row["PaidDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["PaidDate"]),
                        ScheduleDate = row["ScheduleDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["ScheduleDate"]),
                        MaintenanceName = row["MaintenanceName"]?.ToString(),
                        ChargeAmount = row["ChargeAmount"] != DBNull.Value ? Convert.ToDecimal(row["ChargeAmount"]) : 0,
                        TotalAmount = row["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(row["TotalAmount"]) : 0,
                        MemberCode = row["MemberCode"]?.ToString(),
                        EntityCode = row["EntityCode"]?.ToString(),
                        MaintananceTypeId = row["MaintananceTypeId"] != DBNull.Value ? Convert.ToInt32(row["MaintananceTypeId"]) : (int?)null,
                        PaidAmount = row["PaidAmount"] != DBNull.Value ? Convert.ToDecimal(row["PaidAmount"]) : 0,
                        PenaltyPercent = row["PenaltyPercent"] != DBNull.Value ? Convert.ToDecimal(row["PenaltyPercent"]) : 0,
                        PenaltyAmount = row["PenaltyAmount"] != DBNull.Value ? Convert.ToDecimal(row["PenaltyAmount"]) : 0
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetMemberMaintenanceDetailsSY: " + ex.Message);
            }

            return list;
        }
        /// <summary>
        /// This methid is use for fetch member payment details on partial view
        /// </summary>
        public async Task<List<AccountManager>> GetMemberPaymentDetailsSY(
     string memberCode,
     string maintainanceTypeId,
     string entityCode)
        {
            List<AccountManager> list = new List<AccountManager>();

            try
            {
                var parameters = new Dictionary<string, string>
        {
            { "@flag", "MemberPayDetailsSY" },
            { "@MemberCode", memberCode },
            { "@MaintananceTypeId", maintainanceTypeId },
            { "@EntityCode", entityCode }
        };

                MSSQL objSql = new MSSQL();
                DataTable dt = await objSql.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

                // Debugging: log row count
                Console.WriteLine("Rows returned: " + dt.Rows.Count);

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new AccountManager
                    {
                        FullName = row["FullName"]?.ToString(),
                        FlatCode = row["FlatCode"]?.ToString(),
                        ScheduleDate = string.IsNullOrWhiteSpace(row["ScheduleDate"]?.ToString())
                            ? (DateTime?)null : Convert.ToDateTime(row["ScheduleDate"]),
                        MaintenanceName = row["MaintenanceName"]?.ToString(),
                        ChargeAmount = row["ChargeAmount"] != DBNull.Value
                            ? Convert.ToDecimal(row["ChargeAmount"]) : 0,
                        CashInHand = row["CashInHand"] != DBNull.Value
                            ? Convert.ToDecimal(row["CashInHand"]) : 0,

                        // ✅ New columns from SQL
                        MaintenanceAmount = row["MaintenanceAmount"] != DBNull.Value
                            ? Convert.ToDecimal(row["MaintenanceAmount"]) : 0,


                        PenaltyPercent = row["PenaltyPercent"] != DBNull.Value
                            ? Convert.ToDecimal(row["PenaltyPercent"]) : 0,
                        PenaltyAmount = row["PenaltyAmount"] != DBNull.Value
                            ? Convert.ToDecimal(row["PenaltyAmount"]) : 0,
                        FinalPayable = row["FinalPayable"] != DBNull.Value
                            ? Convert.ToDecimal(row["FinalPayable"]) : 0,

                        // ✅ Existing
                        MemberCode = row["MemberCode"]?.ToString(),
                        EntityCode = row["EntityCode"]?.ToString(),
                        MaintananceTypeId = row["MaintananceTypeId"] != DBNull.Value
                            ? Convert.ToInt32(row["MaintananceTypeId"])
                            : (int?)null
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetMemberPaymentDetailsSY: " + ex.Message);
            }

            return list;
        }
        /// <summary>
        /// This methid is use for fetch member payment maintenance details on partial view
        /// </summary>
        public async Task<List<AccountManager>> GetMemberPaymentDetailsSYJ(
    string memberCode,
    string maintainanceTypeId,
    string entityCode)
        {
            List<AccountManager> list = new List<AccountManager>();

            try
            {
                var parameters = new Dictionary<string, string>
   {
       { "@flag", "MemberPayDetailsSYJ" },
       { "@MemberCode", memberCode },
       { "@MaintananceTypeId", maintainanceTypeId },
       { "@EntityCode", entityCode }
   };

                MSSQL objSql = new MSSQL();
                DataTable dt = await objSql.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

                // Debugging: log row count
                Console.WriteLine("Rows returned: " + dt.Rows.Count);

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new AccountManager
                    {
                        FullName = row["FullName"]?.ToString(),
                        FlatCode = row["FlatCode"]?.ToString(),
                        ScheduleDate = string.IsNullOrWhiteSpace(row["ScheduleDate"]?.ToString())
                            ? (DateTime?)null : Convert.ToDateTime(row["ScheduleDate"]),
                        MaintenanceName = row["MaintenanceName"]?.ToString(),
                        ChargeAmount = row["ChargeAmount"] != DBNull.Value
                            ? Convert.ToDecimal(row["ChargeAmount"]) : 0,
                        CashInHand = row["CashInHand"] != DBNull.Value
                            ? Convert.ToDecimal(row["CashInHand"]) : 0,

                        // ✅ New columns from SQL
                        MaintenanceAmount = row["MaintenanceAmount"] != DBNull.Value
                            ? Convert.ToDecimal(row["MaintenanceAmount"]) : 0,


                        PenaltyPercent = row["PenaltyPercent"] != DBNull.Value
                            ? Convert.ToDecimal(row["PenaltyPercent"]) : 0,
                        PenaltyAmount = row["PenaltyAmount"] != DBNull.Value
                            ? Convert.ToDecimal(row["PenaltyAmount"]) : 0,
                        FinalPayable = row["FinalPayable"] != DBNull.Value
                            ? Convert.ToDecimal(row["FinalPayable"]) : 0,

                        // ✅ Existing
                        MemberCode = row["MemberCode"]?.ToString(),
                        EntityCode = row["EntityCode"]?.ToString(),
                        MaintananceTypeId = row["MaintananceTypeId"] != DBNull.Value
                            ? Convert.ToInt32(row["MaintananceTypeId"])
                            : (int?)null
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetMemberPaymentDetailsSY: " + ex.Message);
            }

            return list;
        }



        /// <summary>
        /// This methid is use for save member online maintenance payment
        /// </summary>
        public async Task<bool> SaveMaintainancePayment(
     string memberCode,
     string entityCode,
     decimal amount,
     string transactionId,
     string bankCode,
     string paymentBy,
     int maintananceTypeId,
     string societyCode)   // society code from session
        {
            try
            {
                string paymentPurpose;

                if (maintananceTypeId == 58)
                    paymentPurpose = "Occasional Maintenance";
                else if (maintananceTypeId == 59)
                    paymentPurpose = "Monthly Maintenance";
                else
                    paymentPurpose = "Maintenance Payment";

                var parameters = new Dictionary<string, string>
        {
            { "@flag", "SaveMaintainancePaymentSY" },
            { "@BankCode", bankCode },                // must match SP
            { "@MemberCode", memberCode },
            { "@SocietCode", societyCode },           // dynamic from session
            { "@Amount", amount.ToString() },
            { "@PaymentModeId", "33" },               // numeric or string as SP expects
            { "@PaymentPurpose", paymentPurpose },
            { "@TransactionId_ChequeId", transactionId },
            { "@EntityCode", entityCode }
        };

                MSSQL objSql = new MSSQL();
                int rows = await objSql.ExecuteStoreProcedureNonQuery("sp_SMS", parameters);
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in SaveMaintainancePayment: " + ex.Message);
                return false;
            }
        }


        /// <summary>
        /// This methid is use for save member cash maintenance payment
        /// </summary>
        public async Task<bool> SaveCashMaintainancePayment(
     string memberCode,
     string entityCode,
     decimal amount,
     string paymentBy,      // current user
     int maintananceTypeId,
     string staffCode)      // from session
        {
            try
            {
                string paymentPurpose;

                if (maintananceTypeId == 58)
                    paymentPurpose = "Occasional Maintenance";
                else if (maintananceTypeId == 59)
                    paymentPurpose = "Monthly Maintenance";
                else
                    paymentPurpose = "Maintenance Payment";

                var parameters = new Dictionary<string, string>
        {
            { "@flag", "SaveCashMaintainancePaymentSY" },
            { "@MemberCode", memberCode },
            { "@EntityCode", entityCode },
            { "@Amount", amount.ToString() },
            { "@PaymentBy", paymentBy },        // current user
            { "@StaffCode", staffCode },        // session-based
            { "@PaymentPurpose", paymentPurpose },
            { "@PaymentModeId", "32" }          // fixed for cash
        };

                MSSQL objSql = new MSSQL();
                int rows = await objSql.ExecuteStoreProcedureNonQuery("sp_SMS", parameters);
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in SaveCashMaintainancePayment: " + ex.Message);
                return false;
            }





        }

        /// <summary>
        /// This methid is use for fetch bank in dropdown
        /// </summary>
        public async Task<List<AccountManager>> GetBanksByAllCodeSY(string allCode)
        {
            var list = new List<AccountManager>();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "GetBanksByAllCodeSY" },
    { "@AllCode", allCode }
};

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new AccountManager
                {
                    IFSCCode = row["IFSCCode"].ToString(),
                    BankCode = row["BankCode"].ToString(),
                    AccountNo = row["AccountNo"].ToString(),
                    SubTypeName = row["SubTypeName"].ToString()   // Map SubTypeName
                });
            }

            return list;
        }
        #endregion


        #region********************************************************************* Expence***********************************************************
        /// Summary:
        /// Created by: Priti Shelke
        /// Date: 14 July 2025
        /// Purpose: Fetches a list of all expense records from the database using the stored procedure 'sp_SMS' with the 'FetchExpenseList' flag.

        public async Task<List<AccountManager>> GetAllExpensesPS()
        {
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "FetchExpenseListDD" }
};

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            List<AccountManager> list = new List<AccountManager>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new AccountManager
                {
                    ExpenseId = Convert.ToInt32(row["ExpenseId"]),
                    ExpenseCode = row["ExpenseCode"].ToString(),
                    PaymentTo = row["PaymentTo"].ToString(),
                    VendorName = row["VendorName"].ToString(),
                    ExpenseTypeId = Convert.ToInt32(row["ExpenseTypeId"]),
                    ExpenseType = row["ExpenseTypeName"].ToString(),
                    ExpenseName = row["ExpenseName"].ToString(),
                    WingName = row["WingName"].ToString(),
                    AddedBy = row["AddedBy"].ToString(),
                    Description = row["Description"].ToString(),
                    AddedDate = Convert.ToDateTime(row["AddedDate"]),
                    //Amount = Convert.ToDecimal(row["Amount"]),
                    StatusId = Convert.ToInt32(row["StatusId"]),
                    StatusName = row["StatusName"].ToString(),
                    IFSCCode = row["IFSCCode"].ToString(),
                    // Total Amount (SP मध्ये Amount as TotalAmount)
                    TotalAmount = Convert.ToDecimal(row["TotalAmount"]),

                    // Paid आणि Remaining Amount
                    PaidAmount = Convert.ToDecimal(row["PaidAmount"]),
                    RemainingAmount = Convert.ToDecimal(row["Balance"])
                });
            }

            return list;
        }

        /// Summary:
        /// Created by: Priti Shelke
        ///Date: 04 Aug 2025
        /// Purpose: Retrieves detailed information for a given expense code including main expense details and associated attachments (documents).


        // 1. Expense + Transactions
        public async Task<AccountManager> GetFullExpenseAndTransactionsPS(string expenseCode)
        {
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "GetFullExpenseAndTransactionsPS" },
    { "@ExpenseCode", expenseCode }
};

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            if (dt.Rows.Count == 0) return null;

            var model = new AccountManager
            {
                ExpenseCode = expenseCode,
                ExpenseName = dt.Rows[0]["ExpenseName"].ToString(),
                Description = dt.Rows[0]["Description"].ToString(),
                AddedDate = Convert.ToDateTime(dt.Rows[0]["AddedDate"]),
                VendorName = dt.Rows[0]["VendorName"].ToString(),
                BusinessName = dt.Rows[0]["BusinessName"].ToString(),
                Email = dt.Rows[0]["Email"].ToString(),
                PhoneNumber = dt.Rows[0]["PhoneNumber"].ToString(),
                Address = dt.Rows[0]["Address"].ToString(),
                ExpenseTypeId = Convert.ToInt32(dt.Rows[0]["ExpenseTypeId"]),
                ExpenseType = dt.Rows[0]["ExpenseTypeName"].ToString(),
                Attachments = new List<AccountManager>(), // हे नंतर वेगळे भरणार
                AllTransactions = new List<AccountManager>()
            };

            foreach (DataRow row in dt.Rows)
            {
                // Transactions
                if (row["TransactionCode"] != DBNull.Value)
                {
                    string ifsc = row["IFSCCode"]?.ToString();
                    string bankName = await GetBankNameByIFSCPS(ifsc);

                    model.AllTransactions.Add(new AccountManager
                    {
                        TransactionCode = row["TransactionCode"].ToString(),
                        TransactionAmount = Convert.ToDecimal(row["TransactionAmount"]),
                        PaidDate = row["PaidDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["PaidDate"]),
                        PaymentMode = row["PaymentModeName"].ToString(),
                        TransactionRef = row["TransactionRef"]?.ToString(),
                        IFSCCode = ifsc,
                        BankName = bankName,
                        BankCode = row["BankCode"]?.ToString(),
                        AccountNo = row["AccountNo"]?.ToString()
                    });
                }
            }

            return model;
        }

        public async Task<AccountManager> GetExpenseWithDocumentsPS(string expenseCode)
        {
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "GetExpenseWithDocumentsPS" },
    { "@ExpenseCode", expenseCode }
};

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            var model = new AccountManager
            {
                Attachments = new List<AccountManager>()
            };

            foreach (DataRow row in dt.Rows)
            {
                if (row["Document"] != DBNull.Value)
                {
                    model.Attachments.Add(new AccountManager
                    {
                        Document = row["Document"].ToString()
                    });
                }
            }

            return model;
        }
        /// Summary:
        /// Created by: Priti Shelke
        ///Date: 06 Aug 2025
        /// Purpose: get bank name on expense details list


        public async Task<string> GetBankNameByIFSCPS(string ifsc)
        {
            if (string.IsNullOrWhiteSpace(ifsc))
                return "N/A";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = "https://ifsc.razorpay.com/" + ifsc;
                    Console.WriteLine("Calling API: " + url); // 👈 Debug log

                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("API Response: " + json); // 👈 Debug log

                        var data = JObject.Parse(json);
                        string bankName = data["BANK"]?.ToString();
                        return string.IsNullOrEmpty(bankName) ? "N/A" : bankName;
                    }
                    else
                    {
                        Console.WriteLine("API call failed: " + response.StatusCode); // 👈 Debug log
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("IFSC API Exception: " + ex.Message); // 👈 Debug log
            }

            return "N/A";
        }

        /// Summary:
        /// Created by: Priti Shelke
        /// Date: 14 July 2025
        /// Purpose: Saves payment transaction details (like UTR, bank code, and payment mode) for the given expense code to the database.
        public async Task<bool> SaveTransactionPS(
            string expenseCode,
            string paymentId,
            int paymentModeId,
            string bankCode,
            decimal amount,
            string societyCode,
            string staffCode)
        {
            try
            {
                var parameters = new Dictionary<string, string>
        {
            { "@Flag", "SaveTransactionPS" },
            { "@ExpenseCode", expenseCode },
            { "@PaymentId", paymentId },
            { "@PaymentModeId", paymentModeId.ToString() },
            { "@BankCode", bankCode },
            { "@Amount", amount.ToString(CultureInfo.InvariantCulture) },
            { "@SocietyCode", societyCode },
            { "@StaffCode", staffCode }
        };

                int result = await db.ExecuteStoreProcedureReturnInt("sp_SMS", parameters);
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("BAL Error - SaveTransactionPS: " + ex.Message);
            }
        }


        /// <summary>
        /// Created by: Priti Shelke
        /// Date: 04 Aug 2025
        /// Purpose: Saves cash payment transaction
        /// </summary>
        public async Task<bool> SaveCashTransactionPS(
            string expenseCode,
            decimal amount,
            string societyCode,
            string staffCode)
        {
            try
            {
                var parameters = new Dictionary<string, string>
        {
            { "@Flag", "SavingCashTransactionPS" },
            { "@ExpenseCode", expenseCode },
            { "@Amount", amount.ToString(CultureInfo.InvariantCulture) },
            { "@SocietyCode", societyCode },
            { "@StaffCode", staffCode }
        };

                int result = await db.ExecuteStoreProcedureReturnInt("sp_SMS", parameters);
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("BAL Error - SaveCashTransactionPS: " + ex.Message);
            }
        }


        /// Summary:
        /// Created by: Priti Shelke
        /// Date: 14 July 2025
        /// Purpose: Retrieves full expense details for a specific expense code, primarily to get IFSC and payment-related information for processing.

        public async Task<AccountManager> GetIFSCByCodePS(string expenseCode)
        {
            var parameters = new Dictionary<string, string>
    {
        { "@Flag", "GetExpenseByCodePS" },
        { "@ExpenseCode", expenseCode }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];

            return new AccountManager
            {
                ExpenseId = Convert.ToInt32(row["ExpenseId"]),
                ExpenseCode = row["ExpenseCode"].ToString(),
                PaymentTo = row["PaymentTo"].ToString(),
                ExpenseTypeId = Convert.ToInt32(row["ExpenseTypeId"]),
                ExpenseName = row["ExpenseName"].ToString(),
                WingId = Convert.ToInt32(row["WingId"]),
                AddedBy = row["AddedBy"].ToString(),
                Description = row["Description"].ToString(),
                AddedDate = Convert.ToDateTime(row["AddedDate"]),

                Amount = Convert.ToDecimal(row["Amount"]),
                StatusId = Convert.ToInt32(row["StatusId"]),
                IFSCCode = row["IFSCCode"]?.ToString(),
                UPIId = row["UPIId"]?.ToString(),
                AccountNo = row["AccountNo"] != DBNull.Value ? row["AccountNo"].ToString() : null,
                BusinessName = row["BusinessName"]?.ToString(),
                VendorName = row["VendorName"]?.ToString(),
                PhoneNumber = row["PhoneNumber"]?.ToString(),
                ServiceSubTypeId = Convert.ToInt32(row["ServiceSubTypeId"]),
                SubTypeName = row["ServiceSubTypeName"]?.ToString(),
                PaidAmount = Convert.ToDecimal(row["PaidAmount"]),
                RemainingAmount = Convert.ToDecimal(row["RemainingAmount"]),
                CashInHand = Convert.ToDecimal(row["CashInHand"])
            };
        }


        /// Summary:
        /// Created by: Priti Shelke
        /// Date: 14 July 2025
        /// Purpose: Returns a list of banks based on the provided 'AllCode', retrieving IFSC and BankCode values.

        public async Task<List<AccountManager>> GetBanksByAllCodePS(string allCode)
        {
            var list = new List<AccountManager>();
            var parameters = new Dictionary<string, string>
    {
        { "@Flag", "GetBanksByAllCodePS" },
        { "@AllCode", allCode }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new AccountManager
                {
                    IFSCCode = row["IFSCCode"].ToString(),
                    BankCode = row["BankCode"].ToString(),
                    AccountNo = row["AccountNo"].ToString(),
                    SubTypeName = row["SubTypeName"].ToString()
                });
            }
            return list;
        }


        /// Summary:
        /// Created by: Priti Shelke
        /// Date: 14 July 2025
        /// Purpose: Retrieves the current opening balance for a given IFSC code to validate sufficient funds for processing transactions.

        public async Task<decimal?> GetOpeningBalanceByAccountNoPS(string accountNo)
        {
            var parameters = new Dictionary<string, string>
    {
        { "@Flag", "GetOpeningBalanceByAccountNoPS" },
        { "@AccountNo", accountNo }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            if (dt.Rows.Count > 0)
                return Convert.ToDecimal(dt.Rows[0]["OpeningBalance"]);
            return null;
        }
        /// Summary:
        /// Created by: Priti Shelke
        /// Date: 14 July 2025
        /// Purpose: Returns a list of all available service sub-types, which is typically used for vendor registration and filtering.

        public async Task<List<AccountManager>> GetServiceSubTypesDD()
        {
            var parameters = new Dictionary<string, string>
 {
     { "@Flag", "GetServiceSubTypesDD" }
 };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            List<AccountManager> list = new List<AccountManager>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new AccountManager
                {
                    SubTypeId = Convert.ToInt32(row["SubTypeId"]),
                    SubTypeName = row["SubTypeName"].ToString()
                });
            }

            return list;
        }




        /// Summary:
        /// Created by: Priti Shelke
        /// Date: 14 July 2025
        /// Purpose: Inserts a new vendor's details (including documents, IFSC, UPI, contact, and service type) into the database for expense mapping.

        public async Task<bool> RegisterVendorDD(AccountManager model)
        {
            var parameters = new Dictionary<string, string>
    {
        { "@Flag", "InsertVendorDetailsDD" },
        { "@VendorName", model.VendorName },
        { "@BusinessName", model.BusinessName },
        { "@ServiceSubTypeId", model.ServiceSubTypeId.ToString() },
        { "@Email", model.Email },
        { "@PhoneNumber", model.PhoneNumber },
        { "@AlternatePhoneno", model.AlternatePhoneNumber },
        { "@Address", model.Address },
        { "@SubTypeId1", "46" },
        { "@Document1", model.Document1 },
        { "@SubTypeId2", "44" },
        { "@Document2", model.Document2 },
        { "@Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
        { "@IFSCCode", model.IFSCCode },
        { "@UPIId", model.UPIId },
        { "@AccountNo", model.AccountNo }
    };

            try
            {
                await db.ExecuteStoreProcedureReturnInt("sp_SMS", parameters);
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Inserts a new expense into the system.
        /// Flag: insertExpenseDD
        /// Purpose: Used in Add Expense modal form.
        /// </summary>

        public async Task<string> InsertExpenseAsyncDD(AccountManager obj)
        {
            string documentCsv = (obj.DocumentList != null && obj.DocumentList.Any())
                ? string.Join(",", obj.DocumentList)
                : "";

            var parameters = new Dictionary<string, string>
    {
        { "@Flag", "insertExpenseDD" },
        { "@ExpenseCode", obj.ExpenseCode ?? "" },
        { "@PaymentTo", obj.PaymentTo ?? "" },
        { "@ExpenseTypeId", obj.ExpenseTypeId.ToString() },
        { "@ExpenseName", obj.ExpenseName ?? "" },
        { "@WingId", obj.WingId.ToString() },
        { "@AddedBy", obj.AddedBy ?? "" },
        { "@Description", obj.Description ?? "" },
        { "@AddedDate", obj.Date.ToString("yyyy-MM-dd") },
        { "@Amount", obj.Amount.ToString() },
        { "@CGSTID", obj.CGSTTypeId?.FirstOrDefault().ToString() ?? "0" },
        { "@SGSTID", obj.SGSTTypeId?.FirstOrDefault().ToString() ?? "0" },
        //{ "@IGST", obj.IGSTTypeId?.FirstOrDefault().ToString() ?? "0" },



        { "@Documents", documentCsv }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            return dt.Rows.Count > 0 ? dt.Rows[0]["ExpenseCode"].ToString() : null;
        }


        /// <summary>
        /// Gets vendor dropdown list.
        /// Flag: fetchVendorNameDD
        /// </summary>


        public async Task<List<AccountManager>> GetVendorNamesAsync()
        {
            var parameters = new Dictionary<string, string>
    {
        { "@Flag", "fetchVendorNameDD" }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

            List<AccountManager> list = new List<AccountManager>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new AccountManager
                {
                    VendorCode = row["VendorCode"]?.ToString(),
                    VendorName = row["VendorName"]?.ToString(),
                    BusinessName = row["BusinessName"]?.ToString(),
                    ServiceType = row["ServiceType"]?.ToString()
                });
            }

            return list;
        }






        public async Task<List<AccountManager>> GetVendorTypeAsyncDD()
        {
            var parameters = new Dictionary<string, string> { { "@Flag", "fetchVendorTypeDD" } };
            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

            List<AccountManager> list = new List<AccountManager>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new AccountManager
                {
                    SubTypeId = Convert.ToInt32(row["SubTypeId"]),
                    SubTypeName = row["SubTypeName"].ToString()
                });
            }
            return list;
        }


        /// <summary>
        /// Gets filtered vendor list by type (Regular or Other).
        /// Flags: fetchRegularVendorNameDD / fetchOtherVendorNameDD
        /// </summary>


        public async Task<List<SelectListItem>> GetVendorsByTypeAsyncDD(int vendorType)
        {
            var parameters = new Dictionary<string, string>();

            if (vendorType == 38)
                parameters.Add("@Flag", "fetchRegularVendorNameDD");
            else if (vendorType == 39)
                parameters.Add("@Flag", "fetchOtherVendorNameDD");
            else
                return new List<SelectListItem>();

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

            List<SelectListItem> list = new List<SelectListItem>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new SelectListItem
                {
                    Value = row["VendorCode"].ToString(),
                    Text = row["VendorName"].ToString() + " - " + row["BusinessName"].ToString()
                });
            }

            return list;
        }





        /// <summary>
        /// Gets wing dropdown list.
        /// Flag: fetchWingNameDD
        /// </summary>


        public async Task<List<AccountManager>> GetWingNamesAsyncDD()
        {
            var parameters = new Dictionary<string, string> { { "@Flag", "fetchWingNameDD" } };
            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

            List<AccountManager> list = new List<AccountManager>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new AccountManager
                {
                    WingId = Convert.ToInt32(row["WingId"]),
                    WingName = row["WingName"].ToString()
                });
            }
            return list;
        }

        /// <summary>
        /// Get GstType on GST Dropdown
        /// </summary>
        /// <returns></returns>


        public async Task<List<AccountManager>> GetGSTTypeAsyncDD()
        {
            var parameters = new Dictionary<string, string> { { "@Flag", "fetchGSTTypeDD" } };
            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

            List<AccountManager> list = new List<AccountManager>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new AccountManager
                {
                    GSTTypeId = Convert.ToInt32(row["GSTTypeId"]),
                    GSTTypeName = $"{row["GSTTypeName"]} {row["GSTPercentage"]}"  // 👈 Append percentage
                });
            }
            return list;
        }

        //public async Task<List<AccountManager>> GetVendorTypesAsync()
        //{
        //    var parameters = new Dictionary<string, string> { { "@Flag", "fetchVendorType" } };
        //    DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

        //    List<AccountManager> list = new List<AccountManager>();
        //    foreach (DataRow row in dt.Rows)
        //    {
        //        list.Add(new AccountManager
        //        {
        //            SubTypeId = Convert.ToInt32(row["SubTypeId"]),
        //            SubTypeName = row["SubTypeName"].ToString()
        //        });
        //    }
        //    return list;
        //}


        /// <summary>
        /// Gets GST Type list for dropdown.
        /// Flag: fetchGSTTypeDD
        /// </summary>

        public async Task<bool> InsertGSTTypeDD(AccountManager model)
        {
            var parameters = new Dictionary<string, string>
    {
        { "@Flag", "insertGSTType" },
        { "@GSTTypeName", model.GSTTypeName ?? string.Empty },
        { "@GSTPercentage", model.GSTPercentage.ToString() }
    };

            // DBAccess db = new DBAccess(); // ✅ Use DBAccess, NOT AccountManager
            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
            return true; // ✅ Return true if insert was successful
        }





        #endregion


        #region********************************************************************* Event Management ***********************************************************




        //************************************************************** Savita Salunke ***********************************************************************************************************************************************************



        /// <summary>
        ///  This method fetches Fetches a list of all events from the database,with the flag 'FetchEvents'. Each event includes budgeting and handler information.Date 03-07-2025
        /// </summary>


        public async Task<List<AccountManager>> GetAllEventListSS()
        {
            List<AccountManager> list = new List<AccountManager>();

            try
            {
                MSSQL db = new MSSQL();
                var parameters = new Dictionary<string, string>
{
    { "@Flag", "FetchEventsSS" }
};

                DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

                Console.WriteLine("Fetched rows: " + dt.Rows.Count);

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new AccountManager
                    {
                        EventCode = row["EventCode"].ToString(),
                        EBudgetId = row.Field<int?>("EBudgetId") ?? 0,
                        EBudgetCode = row["EBudgetCode"].ToString(),
                        UPIId = row["UPIId"].ToString(),
                        EventName = row["EventName"].ToString(),
                        WingName = row["WingName"].ToString(),
                        EventHandlerName = row["EventHandlerName"].ToString(),
                        CreatedDate = row.Field<DateTime?>("CreatedDate") ?? DateTime.MinValue,
                        EndDate = row.Field<DateTime?>("EndDate") ?? DateTime.MinValue,

                        AllocatedBudget = row.Field<decimal?>("AllocatedBudget") ?? 0,
                        ActualCost = row.Field<decimal?>("ActualCost") ?? 0,
                        BudgetAddedDate = row.Field<DateTime?>("BudgetAddedDate") ?? DateTime.MinValue,
                        BudgetStatus = Convert.ToInt32(row["BudgetStatus"]),
                        BudgetStatusName = row["BudgetStatusName"].ToString(),
                        IFSCCode = row["IFSCCode"].ToString(),
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetAllEventListSS: " + ex.Message);

            }

            return list;
        }

        /// <summary>
        /// Sends a budget approval request by emailing the admin and then updating the status in the database to "Approved" if the email is successfully sent.Date 05-07-2025
        /// </summary>



        public async Task<bool> SendRequestAsyncSS(int EBudgetId)
        {
            try
            {
                MSSQL db = new MSSQL();

                var parameters = new Dictionary<string, string>
{
    { "@Flag", "UpdateToApprovedSS" },
    { "@EBudgetId", EBudgetId.ToString() }
};

                int rowsAffected = await db.ExecuteStoreProcedureReturn("sp_SMS", parameters);
                return rowsAffected > 0;
            }
            catch
            {


                return false;
            }
        }



        /// <summary>
        /// Gets accountant email/contact info  Date 03/07/2025
        /// </summary>
        public async Task<AccountManager> GetAccountantDetailsSS()
        {
            AccountManager accountant = new AccountManager();

            SqlParameter[] parameters = new SqlParameter[]
            {
               new SqlParameter("@Flag", "FetchAccountantDetailsSS")
            };

            DataTable dt = await GSTSMSHelper.MSSQL.ExecuteStoredProcedure("sp_SMS", parameters);



            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                accountant.StaffId = Convert.ToInt32(row["StaffId"]);
                accountant.StaffCode = row["StaffCode"].ToString();
                accountant.FromEmailAddress = row["Email"].ToString();
                accountant.FlatCode = row["FlatCode"].ToString();
                accountant.ContactNumber = row["ContactNumber"].ToString();
            }

            return accountant;
        }

        /// <summary>
        /// This method fetches a single event's budget details from the database using the flag 'FetchEventByIdSS' and the provided EBudgetId. date 04-07-2025
        /// </summary>

        public async Task<AccountManager> GetEventByIdAsyncSS(int id)
        {
            try
            {
                MSSQL db = new MSSQL();
                var parameters = new Dictionary<string, string>
 {
     { "@Flag", "FetchEventByIdSS" },
     { "@EBudgetId", id.ToString() }
 };

                DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];

                    return new AccountManager
                    {
                        EBudgetId = Convert.ToInt32(row["EBudgetId"]),
                        EventName = row["EventName"]?.ToString(),
                        EventHandlerName = row["EventHandlerName"]?.ToString(),
                        AllocatedBudget = row.Field<decimal?>("AllocatedBudget") ?? 0,
                        CreatedDate = row.Field<DateTime?>("CreatedDate") ?? DateTime.MinValue,

                        WingName = row["WingName"]?.ToString(),
                    };
                }

                return null;
            }

            catch (Exception ex)
            {
                // Temporary: Error file मध्ये exception टाका
                System.IO.File.WriteAllText("C:\\temp\\GetEventById_Error.txt", ex.ToString());
                return null;
            }


        }

        //************************************************************** Pradnya Mane ***********************************************************************************************************************************************************
        /// <summary>
        /// Deletes an event from the database using the specified budget code.
        /// Executes the <c>sp_SMS</c> stored procedure with the flag <c>DeleteEventSS</c>.
        /// </summary>


        public async Task<bool> DeleteEventByBudgetCodeAsyncSS(string eBudgetCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eBudgetCode))
                    return false;

                var mssql = new MSSQL();

                var parameters = new Dictionary<string, string>
      {
          { "@Flag", "DeleteEventSS" },
          { "@EBudgetCode", eBudgetCode.Trim() }
      };

                // ✅ Correct stored procedure name
                var ds = await mssql.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    int rowsAffected = Convert.ToInt32(ds.Tables[0].Rows[0]["RowsAffected"]);
                    return rowsAffected > 0;
                }

                return false;
            }
            catch (Exception ex)
            {
                // Optional: log error
                Console.WriteLine("Delete Error: " + ex.Message);
                return false;
            }
        }





        /// <summary>
        /// Retrieves a list of all approved events from the database
        /// by executing the <c>sp_SMS</c> stored procedure with the flag <c>FetchApprovedEventMD</c>.
        /// </summary>

        public async Task<List<AccountManager>> GetApprovedEventsAsyncMD()
        {
            try
            {
                MSSQL db = new MSSQL();

                var parameters = new Dictionary<string, string>
      {
          { "@Flag", "FetchApprovedEventMD" }
      };

                DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
                List<AccountManager> list = new List<AccountManager>();

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new AccountManager
                    {
                        EventName = row["EventName"].ToString(),
                        CreatedDate = row["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(row["CreatedDate"]) : DateTime.MinValue,
                        EventHandlerName = row["EventHandlerName"].ToString(),
                        PhoneNumber = row["PhoneNumber"].ToString()
                    });
                }

                return list;
            }
            catch (Exception ex)
            {
                // You can log the error here if needed
                Console.WriteLine("Error in GetApprovedEventsAsync: " + ex.Message);
                return new List<AccountManager>();
            }
        }





        /// <summary>
        /// Retrieves the opening balance from the database by executing the
        /// <c>sp_SMS</c> stored procedure with the flag <c>FetchOpeningBalanceMD</c>.
        /// </summary>


        public async Task<decimal> GetOpeningBalanceAsyncMD()
        {
            try
            {
                MSSQL db = new MSSQL();

                var parameters = new Dictionary<string, string>
      {
          { "@Flag", "FetchOpeningBalanceMD" }
      };

                DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
                if (dt.Rows.Count > 0 && dt.Rows[0]["OpeningBalance"] != DBNull.Value)
                {
                    return Convert.ToDecimal(dt.Rows[0]["OpeningBalance"]);
                }

                return 0;
            }
            catch (Exception ex)
            {
                // Optional: log exception
                Console.WriteLine("Error in GetOpeningBalanceAsync: " + ex.Message);
                return 0;
            }
        }



        /// <summary>
        /// Submits the allocated budget for a specified event to the database by executing the
        /// <c>sp_SMS</c> stored procedure with the flag <c>SaveAllocatedBudgetMD</c>.
        /// Sets the actual cost to <c>0</c> by default and the budget status to <c>11</c>.
        /// </summary>



        public async Task<bool> SubmitBudgetAsyncMD(string eventName, decimal allocatedBudget)
        {
            try
            {
                MSSQL db = new MSSQL();

                var parameters = new Dictionary<string, string>
      {
          { "@Flag", "SaveAllocatedBudgetMD" },
          { "@EventName", eventName },
          { "@ActualCost", "0" },
          { "@BudgetAddedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
          { "@AllocatedBudget", allocatedBudget.ToString() },
          { "@BudgetStatus", "11" }
      };

                int result = await db.ExecuteStoreProcedureReturnInt("sp_SMS", parameters);
                return result > 0;
            }
            catch (Exception ex)
            {
                // Optional: log exception
                Console.WriteLine("Error in SubmitBudgetAsync: " + ex.Message);
                return false;
            }
        }


      

        public async Task<bool> SubmitBudgetWithDistributionAsync(string eventName, decimal allocatedBudget, List<(string Category, decimal Budget)> categories)
        {
            try
            {
                // Convert categories to XML
                var xml = new XElement("Categories",
                    categories.Select(c =>
                        new XElement("Cat",
                            new XAttribute("Name", c.Category),
                            new XAttribute("Budget", c.Budget))
                    )
                );

                // Build SqlParameters
                SqlParameter[] parameters =
                {
                  new SqlParameter("@Flag", SqlDbType.NVarChar, 100) { Value = "SaveBudgetWithDistributionMD" },
                  new SqlParameter("@EventName", SqlDbType.NVarChar, 200) { Value = eventName },
                  new SqlParameter("@ActualCost", SqlDbType.Decimal) { Value = 0 },
                  new SqlParameter("@BudgetAddedDate", SqlDbType.DateTime) { Value = DateTime.Now },
                  new SqlParameter("@AllocatedBudget", SqlDbType.Decimal) { Value = allocatedBudget },
                  new SqlParameter("@BudgetStatus", SqlDbType.Int) { Value = 11 },
                  new SqlParameter("@CategoryXML", SqlDbType.Xml) { Value = xml.ToString() }
              };

                // Call your helper
                DataTable dt = await MSSQL.ExecuteStoredProcedure("sp_SMS", parameters);

                // Stored proc returns 1 (success) or -1 (fail)
                if (dt.Rows.Count > 0 && int.TryParse(dt.Rows[0]["Result"].ToString(), out int result))
                {
                    return result > 0;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in SubmitBudgetWithDistributionAsync: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Retrieves detailed information about a specific event from the database
        /// by executing the <c>sp_SMS</c> stored procedure with the flag <c>FetchUpdateEventMD</c>
        /// and the provided event code.
        /// </summary>
        /// <param name="eventCode">The unique code identifying the event.</param>


        public async Task<AccountManager> GetEventDetailsByCodeAsyncMD(string eventCode)
        {
            try
            {
                MSSQL db = new MSSQL();

                var parameters = new Dictionary<string, string>
      {
          { "@Flag", "FetchUpdateEventMD" },
          { "@EventCode", eventCode }
      };

                DataSet ds = await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);

                if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                    return null;

                // --- Event Master ---
                DataRow row = ds.Tables[0].Rows[0];
                var eventDetails = new AccountManager
                {
                    EventCode = row["EventCode"].ToString(),
                    EventName = row["EventName"].ToString(),
                    CreatedDate = row["CreatedDate"] != DBNull.Value
              ? Convert.ToDateTime(row["CreatedDate"])
              : DateTime.MinValue,

                    EventEndDate = row["EventEndDate"] != DBNull.Value
        ? Convert.ToDateTime(row["EventEndDate"])
        : DateTime.MinValue,

                    EventHandlerName = row["EventHandlerName"].ToString(),
                    PhoneNumber = row["PhoneNumber"].ToString(),
                    AllocatedBudget = row["AllocatedBudget"] != DBNull.Value ? Convert.ToDecimal(row["AllocatedBudget"]) : 0,
                    BankAccount = row["BankAccount"].ToString(),
                    IFSCCode = row["IFSCCode"].ToString(),
                    EventDistributions = new List<AccountManager>() // initialize
                };

                // --- Event Distribution ---
                if (ds.Tables.Count > 1)
                {
                    // DEBUG: Check if we have distribution data
                    Console.WriteLine($"Distribution table rows: {ds.Tables[1].Rows.Count}");

                    foreach (DataRow dr in ds.Tables[1].Rows)
                    {
                        // DEBUG: Check each row
                        Console.WriteLine($"Distribution: {dr["CategoryName"]}, {dr["AllocatedBudget"]}");

                        eventDetails.EventDistributions.Add(new AccountManager
                        {
                            EventDCode = dr["EventDCode"].ToString(),
                            EventCode = dr["EventCode"].ToString(),
                            CategoryName = dr["CategoryName"].ToString(),
                            AllocatedBudget = dr["AllocatedBudget"] != DBNull.Value ? Convert.ToDecimal(dr["AllocatedBudget"]) : 0,
                            ActualBudget = dr["ActualBudget"] != DBNull.Value
    ? Convert.ToDecimal(dr["ActualBudget"])
    : 0,
                        });
                    }
                }
                else
                {
                    Console.WriteLine("No distribution table found in dataset");
                }

                return eventDetails;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetEventDetailsByCodeAsyncMD: " + ex.Message);
                return null;
            }
        }




        /// <summary>
        /// Updates the actual cost and related details of a specific event in the database
        /// by executing the <c>sp_SMS</c> stored procedure with the flag <c>UpdateActualCostMD</c>.
        /// </summary>

        public async Task<int> UpdateActualCostAsyncMD(
      string eventCode,
      string eventDCode,
      decimal actualCost,
      string document,
      DateTime addedDate)
        {
            try
            {
                // Add null checks for required parameters
                if (string.IsNullOrEmpty(eventCode) || string.IsNullOrEmpty(eventDCode))
                {
                    return 0; // Indicate failure
                }

                Dictionary<string, string> param = new Dictionary<string, string>
      {
          { "@Flag", "UpdateActualCostMD" },
          { "@EventCode", eventCode },
          { "@EventDCode", eventDCode },
          { "@ActualCost", actualCost.ToString(CultureInfo.InvariantCulture) },
          { "@Document", document ?? string.Empty },
          { "@AddedDate", addedDate.ToString("yyyy-MM-dd HH:mm:ss") }
      };

                return await db.ExecuteStoreProcedureReturn("sp_SMS", param);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateActualCostAsyncMD: {ex.Message}");
                return 0; // Indicate failure
            }
        }

        //    public async Task<int> UpdateActualCostAsyncMD(string eventCode, decimal actualCost, string document, DateTime addedDate)
        //    {
        //        Dictionary<string, string> param = new Dictionary<string, string>
        //{
        //    { "@Flag", "UpdateActualCostMD" },
        //    { "@EventCode", eventCode },
        //    { "@ActualCost", actualCost.ToString() },
        //    { "@Document", document ?? string.Empty },
        //     { "@AddedDate", addedDate.ToString("yyyy-MM-dd HH:mm:ss") }
        //};

        //        return await db.ExecuteStoreProcedureReturn("sp_SMS", param);
        //    }




        /// <summary>
        /// Retrieves comprehensive details of a specific event for viewing purposes
        /// by executing the <c>sp_SMS</c> stored procedure with the flag <c>ViewEventDetailsMD</c>.
        /// The result includes event details, budget information, payment details,
        /// and any associated document attachments.
        /// </summary>


        public async Task<AccountManager> GetEventDetailsForViewAsyncMD(string eventCode)
        {
            try
            {
                MSSQL db = new MSSQL();
                var parameters = new Dictionary<string, string>
        {
            { "@Flag", "ViewEventDetailsMD" },
            { "@EventCode", eventCode }
        };

                using (DataSet ds = await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters))
                {
                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        DataRow row = ds.Tables[0].Rows[0];
                        var result = new AccountManager
                        {
                            EventName = row["EventName"]?.ToString(),
                            EventHandlerName = row["EventHandlerName"]?.ToString(),
                            BankAccount = row["BankAccount"]?.ToString(),
                            IFSCCode = row["IFSCCode"]?.ToString(),
                            PhoneNumber = row["PhoneNumber"]?.ToString(),
                            Email = row["Email"]?.ToString(),
                            AllocatedBudget = row["TotalAllocatedBudget"] != DBNull.Value
    ? Convert.ToDecimal(row["TotalAllocatedBudget"])
    : 0m,

                            BudgetAddedDate = row["BudgetAddedDate"] != DBNull.Value
    ? Convert.ToDateTime(row["BudgetAddedDate"])
    : DateTime.MinValue,

                            ActualCost = row["TotalActualCost"] != DBNull.Value
    ? Convert.ToDecimal(row["TotalActualCost"])
    : 0m,

                            PaymentMode = row["PaymentMode"]?.ToString(),
                            TransactionId_ChequeId = row["TransactionId_ChequeId"] != DBNull.Value ? row["TransactionId_ChequeId"].ToString() : "-",
                            BankName = row["BankName"]?.ToString(),
                            PaidDate = row["PaidDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["PaidDate"]),

                            DebitedAccountNo = row["DebitedAccountNo"]?.ToString(),
                            Attachments = new List<AccountManager>(),
                            BudgetBreakups = new List<AccountManager>()
                        };

                        // Add documents if available
                        if (ds.Tables.Count > 1)
                        {
                            foreach (DataRow docRow in ds.Tables[1].Rows)
                            {
                                result.Attachments.Add(new AccountManager
                                {
                                    DocumentId = docRow["DocumentId"] != DBNull.Value ? Convert.ToInt32(docRow["DocumentId"]) : 0,
                                    DocumentName = docRow["DocumentName"]?.ToString(),
                                    DocumentDate = docRow["DocumentDate"] != DBNull.Value
    ? Convert.ToDateTime(docRow["DocumentDate"])
    : DateTime.MinValue,
                                    DocumentSubTypeId = docRow["DocumentSubTypeId"] != DBNull.Value
    ? Convert.ToInt32(docRow["DocumentSubTypeId"])
    : 0
                                });
                            }
                        }

                        // Add budget breakup data if available
                        if (ds.Tables.Count > 2)
                        {
                            foreach (DataRow budgetRow in ds.Tables[2].Rows)
                            {
                                result.BudgetBreakups.Add(new AccountManager
                                {
                                    EventDCode = budgetRow["EventDCode"]?.ToString(),
                                    DistributionCategory = budgetRow["DistributionCategory"]?.ToString(),
                                    AllocatedBudget = budgetRow["AllocatedBudget"] != DBNull.Value
    ? Convert.ToDecimal(budgetRow["AllocatedBudget"])
    : 0m,
                                    ActualBudget = budgetRow["ActualBudget"] != DBNull.Value
    ? Convert.ToDecimal(budgetRow["ActualBudget"])
    : 0m,
                                    DocumentId = budgetRow["DocumentId"] != DBNull.Value ? Convert.ToInt32(budgetRow["DocumentId"]) : 0,
                                    DocumentName = budgetRow["DocumentName"]?.ToString(),
                                    DocumentDate = budgetRow["DocumentDate"] != DBNull.Value
    ? Convert.ToDateTime(budgetRow["DocumentDate"])
    : DateTime.MinValue,
                                    DocumentSubTypeId = budgetRow["DocumentSubTypeId"] != DBNull.Value
    ? Convert.ToInt32(budgetRow["DocumentSubTypeId"])
    : 0
                                });
                            }
                        }

                        return result;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetEventDetailsForViewAsync: " + ex.Message);
                return null;
            }
        }

        //Shubham******************************************************************************

        /// <summary>
        /// For Saving the transaction of banks, transaction made by upi, netbanking and etc (not cash)
        /// </summary>

        public async Task<bool> SavePaymentSVTransactionSV(string eventCode, string transactionId, int paymentMode, string bankCode, string societyCode,
      string staffCode)

        {
            MSSQL db = new MSSQL();
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "InsertTransactionSV" },
    { "@EventCode", eventCode },
    { "@TransactionId_ChequeId", transactionId },
    { "@PaymentMode", paymentMode.ToString() },
    { "@BankCode", bankCode }, // ✅ New param
          { "@SocietyCode", societyCode },
      { "@StaffCode", staffCode }


};

            int rowsAffected = await db.ExecuteStoreProcedureReturnInt("sp_SMS", parameters);
            return rowsAffected > 0;
        }



        /// <summary>
        /// For getting the banks name in the dropdown
        /// </summary>

        public async Task<List<AccountManager>> GetBanksByAllCodeSV(string allCode)
        {
            var list = new List<AccountManager>();
            var parameters = new Dictionary<string, string>
    {
        { "@Flag", "GetBanksByAllCodeSV" },
        { "@AllCode", allCode }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new AccountManager
                {
                    IFSCCode = row["IFSCCode"].ToString(),
                    BankCode = row["BankCode"].ToString(),
                    AccountNo = row["AccountNo"].ToString(),  // ✅ Added
                    SubTypeName = row["SubTypeName"].ToString()

                });
            }

            return list;
        }


        /// <summary>
        /// For getting the opening balance according to the account number
        /// </summary>
        public async Task<decimal?> GetOpeningBalanceByAccountNoSV(string accountNo)
        {
            var parameters = new Dictionary<string, string>
    {
        { "@Flag", "GetOpeningBalanceByAccountNoSV" },
        { "@AccountNo", accountNo }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

            if (dt.Rows.Count > 0)
            {
                return Convert.ToDecimal(dt.Rows[0]["OpeningBalance"]);
            }

            return null;
        }

        /// <summary>
        /// For getting the opening balance accordinyt to the bank selected
        /// </summary>

        public async Task<decimal?> GetOpeningBalanceByIFSCSV(string ifsc)
        {
            var parameters = new Dictionary<string, string>
{
    { "@Flag", "GetOpeningBalanceByIFSCSV" },
    { "@IFSCCode", ifsc }
};

            // Execute the stored procedure and return a data table
            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

            // If data is found, return the opening balance
            if (dt.Rows.Count > 0)
            {
                return Convert.ToDecimal(dt.Rows[0]["OpeningBalance"]);
            }

            // Return null if no opening balance is found
            return null;
        }

        /// <summary>
        /// For getting theEvents details  on the cash modal 
        /// </summary>

        public async Task<AccountManager> GetEventDetailsCashSV(string eventCode)
        {
            try
            {
                MSSQL db = new MSSQL();

                var parameters = new Dictionary<string, string>
   {
       { "@Flag", "FetchEventDetailsCashSV" },
       { "@EventCode", eventCode }
   };

                DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    return new AccountManager
                    {
                        EventCode = row["EventCode"]?.ToString(),
                        EventName = row["EventName"]?.ToString(),
                        EventHandlerName = row["EventHandlerName"]?.ToString(),
                        MemberCode = row["MemberCode"]?.ToString(),
                        WingName = row["WingName"]?.ToString(),
                        AllocatedBudget = row.Field<decimal?>("AllocatedBudget") ?? 0,
                        ActualCost = row.Field<decimal?>("ActualCost") ?? 0,
                        CashInHand = row.Field<decimal?>("CashInHand") ?? 0
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetEventDetailsCashSV: " + ex.Message);
                return null;
            }
        }




        /// <summary>
        /// For saving the cash transaction in the database 
        /// </summary>
        public async Task<bool> SaveCashTransactionSV(string eventCode, string memberCode, decimal amount, string societyCode,
      string staffCode)

        {
            try
            {
                var parameters = new Dictionary<string, string>
   {
       { "@Flag", "SavingCashTransactionSV" },
       { "@EventCode", eventCode },
       { "@MemberCode", memberCode },
       { "@Amount", amount.ToString(CultureInfo.InvariantCulture) },
       { "@SocietyCode", societyCode },
       { "@StaffCode", staffCode }

   };

                int result = await db.ExecuteStoreProcedureReturnInt("sp_SMS", parameters);
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("BAL Error - SaveCashTransactionSV: " + ex.Message);
            }
        }

        #endregion


        #region********************************************************************* Community Send Email ***********************************************************


        /// <summary>
        /// Sends email with optional attachment and CC using async SMTP. 03/07/2025
        /// </summary>
        /// <summary>
        /// Sends email using EmailHelper and returns success or error message.
        /// </summary>
        public async Task<string> SendEMailVM(AccountManager model, List<HttpPostedFileBase> attachments)
        {
            if (string.IsNullOrWhiteSpace(model.ToEmailAddress))
                return "❌ Missing recipient.";
            if (string.IsNullOrWhiteSpace(model.Subject))
                return "❌ Subject required.";

            var helper = new EmailHelper();
            string body = model.EmailBodyMessage;
            // Fallback default body if not provided
            if (string.IsNullOrWhiteSpace(body))
            {
                body = helper.GetAccountantMessageVM(model.userTypedMessage, model.FromEmailAddress, model.ContactNumber, model.FullName);
            }

            return await helper.SendEmailHelperVM(
                model.ToEmailAddress,
                model.Subject,
                body,
                attachments,
                model.CcEmailAddresses // Pass CC emails
            );
        }

        /// <summary>
        /// Gets accountant email/contact info   03/07/2025
        /// </summary>
        public async Task<AccountManager> GetAccountantDetailsVM()
        {
            AccountManager accountant = new AccountManager();
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@Flag", "FetchAccountantVM")
            };
            DataTable dt = await GSTSMSHelper.MSSQL.ExecuteStoredProcedure("sp_SMS", parameters);
            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                accountant.StaffId = Convert.ToInt32(row["StaffId"]);
                accountant.StaffCode = row["StaffCode"].ToString();
                accountant.FromEmailAddress = row["Email"].ToString();
                accountant.FlatCode = row["FlatCode"].ToString();
                accountant.ContactNumber = row["ContactNumber"].ToString();
            }
            return accountant;
        }

        /// <summary>
        /// Returns member list for a wing or specific IDs .  03/07/2025
        /// </summary>
        public async Task<List<AccountManager>> GetMembersVM(string wing, List<int> specificMemberIds = null)
        {
            List<AccountManager> members = new List<AccountManager>();
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@WingName", wing),
        new SqlParameter("@MemberIds", specificMemberIds != null && specificMemberIds.Count > 0
            ? string.Join(",", specificMemberIds)
            : (object)DBNull.Value),
        new SqlParameter("@Flag", "FetchMemberVM")
            };
            DataTable dt = await GSTSMSHelper.MSSQL.ExecuteStoredProcedure("sp_SMS", parameters);
            foreach (DataRow row in dt.Rows)
            {
                members.Add(new AccountManager
                {
                    MemberId = Convert.ToInt32(row["MemberId"]),
                    FullName = row["FullName"].ToString(),
                    FlatCode = row["FlatCode"].ToString(),
                    ToEmailAddress = row["Email"].ToString(),
                    ContactNumber = row["PhoneNumber"].ToString()
                });
            }
            return members;
        }

        public async Task<List<AccountManager>> GetStaffVM()
        {
            List<AccountManager> staffList = new List<AccountManager>();
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@Flag", "FetchStaffVM")
            };

            DataTable dt = await GSTSMSHelper.MSSQL.ExecuteStoredProcedure("sp_SMS", parameters);

            foreach (DataRow row in dt.Rows)
            {
                staffList.Add(new AccountManager
                {
                    StaffId = row.Table.Columns.Contains("StaffId") && row["StaffId"] != DBNull.Value ? Convert.ToInt32(row["StaffId"]) : 0,
                    StaffCode = row.Table.Columns.Contains("StaffCode") ? row["StaffCode"].ToString() : string.Empty,
                    RoleId = row.Table.Columns.Contains("RoleId") && row["RoleId"] != DBNull.Value ? Convert.ToInt32(row["RoleId"]) : 0,
                    RoleName = row.Table.Columns.Contains("RoleName") ? row["RoleName"].ToString() : string.Empty,
                    Email = row.Table.Columns.Contains("Email") ? row["Email"].ToString() : string.Empty,
                    ContactNumber = row.Table.Columns.Contains("ContactNumber") ? row["ContactNumber"].ToString() : string.Empty,
                    FlatCode = row.Table.Columns.Contains("FlatCode") ? row["FlatCode"].ToString() : string.Empty,
                    IsActive = row.Table.Columns.Contains("IsActive") && row["IsActive"] != DBNull.Value ? Convert.ToBoolean(row["IsActive"]) : false,
                    WingName = row.Table.Columns.Contains("WingName") ? row["WingName"].ToString() : string.Empty,
                    StaffName = row.Table.Columns.Contains("StaffName") ? row["StaffName"].ToString() : string.Empty,
                });
            }
            return staffList;
        }





        #endregion



        #region*********************************************************************  Community Complaints ***********************************************************
        /// <summary>
        /// showing complaint List--------------------------------------------------//
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<DataSet> FetchAllComplaintsAsyncSB()
        {
            try
            {
                var parameters = new Dictionary<string, string>
{
    { "@Flag", "ComplaintFetchSB" }
};

                return await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to fetch complaints: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Return mapped List<AccountManager> of Complaints
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>

        public async Task<List<AccountManager>> GetAllComplaintsAsyncSB()
        {
            List<AccountManager> complaints = new List<AccountManager>();

            try
            {
                DataSet ds = await FetchAllComplaintsAsyncSB();

                if (ds == null || ds.Tables.Count == 0) return complaints;

                DataTable dt = ds.Tables[0];

                // Debug: log column names (View Output / VS Output window)
                var colNames = string.Join(", ", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
                System.Diagnostics.Debug.WriteLine("ComplaintFetchSB Columns: " + colNames);

                foreach (DataRow row in dt.Rows)
                {
                    // Optional debug: log first few rows values for Description and Wing
                    // System.Diagnostics.Debug.WriteLine($"Row Desc='{(dt.Columns.Contains("Description") ? row["Description"]?.ToString() : "<no col>")}', Wing='{(dt.Columns.Contains("Wing") ? row["Wing"]?.ToString() : "<no col>")}'");

                    var am = new AccountManager
                    {
                        ComplaintId = dt.Columns.Contains("ComplaintId") && row["ComplaintId"] != DBNull.Value
                                      ? Convert.ToInt32(row["ComplaintId"]) : 0,

                        ComplaintType = dt.Columns.Contains("ComplaintName") && row["ComplaintName"] != DBNull.Value
                                        ? row["ComplaintName"].ToString() : string.Empty,

                        // try multiple possible column names for description
                        Description = (dt.Columns.Contains("Description") && row["Description"] != DBNull.Value)
                                      ? row["Description"].ToString()
                                      : (dt.Columns.Contains("Complaint Description") && row["Complaint Description"] != DBNull.Value)
                                        ? row["Complaint Description"].ToString()
                                        : string.Empty,

                        ComplaintDate = dt.Columns.Contains("ComplaintDate") && row["ComplaintDate"] != DBNull.Value
                                        ? Convert.ToDateTime(row["ComplaintDate"]) : DateTime.MinValue,

                        Wing = (dt.Columns.Contains("Wing") && row["Wing"] != DBNull.Value)
                               ? row["Wing"].ToString()
                               : (dt.Columns.Contains("WingName") && row["WingName"] != DBNull.Value)
                                 ? row["WingName"].ToString()
                                 : string.Empty,

                        RaisedBy = dt.Columns.Contains("RaisedBy") && row["RaisedBy"] != DBNull.Value
                                   ? row["RaisedBy"].ToString() : string.Empty,

                        AssignBy = dt.Columns.Contains("AssignBy") && row["AssignBy"] != DBNull.Value
                                   ? row["AssignBy"].ToString() : string.Empty,

                        StatusName = dt.Columns.Contains("StatusName") && row["StatusName"] != DBNull.Value
                                     ? row["StatusName"].ToString() : string.Empty
                    };

                    complaints.Add(am);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error mapping complaint data: " + ex.Message, ex);
            }

            return complaints;
        }


        /// <summary>
        /// fetch all specific details staff complaint  and accountantant related details
        /// </summary>
        /// <param name="ComplaintId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<AccountManager> GetResolvedComplaintDetailsByIdAsyncMs(int ComplaintId)
        {
            AccountManager complaint = null;

            try
            {
                MSSQL db = new MSSQL();

                var parameters = new Dictionary<string, string>
{
    { "@flag", "ViewfetchcomplaintMS" },
    { "@complainId", ComplaintId.ToString() }
};

                DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    complaint = new AccountManager
                    {
                        SecretoryName = row["Secretary Name"] != DBNull.Value ? row["Secretary Name"].ToString() : string.Empty,
                        RaisedBy = row["Raised By"] != DBNull.Value ? row["Raised By"].ToString() : string.Empty,
                        ComplaintType = row["Complaint Type"] != DBNull.Value ? row["Complaint Type"].ToString() : string.Empty,
                        Description = row["Complaint Description"] != DBNull.Value ? row["Complaint Description"].ToString() : string.Empty,
                        ComplaintDate = row["ComplaintDate"] != DBNull.Value ? Convert.ToDateTime(row["ComplaintDate"]) : DateTime.MinValue,
                        Status = row["Status"] != DBNull.Value ? row["Status"].ToString() : string.Empty,
                        Reasonsolvecomplaint = row["SolveReason"] != DBNull.Value
                        ? row["SolveReason"].ToString()
                        : string.Empty,
                        DocumentPath = row.Table.Columns.Contains("DocumentPath") && row["DocumentPath"] != DBNull.Value
                            ? row["DocumentPath"].ToString()
                            : null,
                        ResolvedDate = row.Table.Columns.Contains("ResolvedDate") && row["ResolvedDate"] != DBNull.Value
                    ? Convert.ToDateTime(row["ResolvedDate"])
                    : (DateTime?)null
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching resolved complaint details", ex);
            }

            return complaint;
        }

        /// <summary>
        /// complaint solve button the account related complaint
        /// </summary>
        /// <param name="complaintId"></param>
        /// <param name="resolvedBy"></param>
        /// <returns></returns>
        public async Task<bool> ResolveComplaintAsyncSB(int complaintId, string reason)
        {
            try
            {
                var parameters = new Dictionary<string, string>
        {
            { "@flag", "ResolveComplaintSB" },
            { "@complainId", complaintId.ToString() },
            { "@ResolveReason", reason ?? "" }
        };

                DataSet ds = await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);

                if (ds != null && ds.Tables.Count > 0)
                {
                    int affected = Convert.ToInt32(ds.Tables[0].Rows[0]["AffectedRows"]);
                    return affected > 0;
                }

                return false;
            }
            catch
            {
                throw;
            }
        }



        #endregion


        #region********************************************************************* Community Notice  ***********************************************************



        /// <summary>
        ///  This method fetches Fetches a list of all notices from the database,with the flag 'FetchNotice'. Each notice includes title, description, and publish date.Date 04-07-2025
        /// </summary>

        public async Task<List<AccountManager>> GetAllNoticeListSS()
        {
            try
            {
                MSSQL db = new MSSQL();
                var parameters = new Dictionary<string, string>
{
    { "@Flag", "FetchNoticeSS" }
};

                DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);
                List<AccountManager> list = new List<AccountManager>();

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new AccountManager
                    {
                        NoticeAnnouncementId = Convert.ToInt32(row["NoticeAnnouncementId"]),
                        NoticeAnnouncementCode = row["NoticeAnnouncementCode"]?.ToString(),
                        NoticeTitle = row["NoticeTitle"]?.ToString(),
                        Description = row["Description"]?.ToString(),
                        PublishDate = row.Field<DateTime?>("PublishDate") ?? DateTime.MinValue,
                        EndDate = row.Field<DateTime?>("EndDate") ?? DateTime.MinValue,
                        SendBy = row["SendBy"]?.ToString(),
                        SendByRole = row["SendByRole"]?.ToString(),
                        CreatedDate = row.Field<DateTime?>("CreatedDate") ?? DateTime.MinValue,
                        Document = row["Document"]?.ToString()
                    });
                }

                return list;
            }
            catch
            {

                return new List<AccountManager>();
            }
        }

        public async Task<bool> DeleteNoticeSS(string noticeCode)
        {
            try
            {
                MSSQL db = new MSSQL();

                var parameters = new Dictionary<string, string>
 {
     { "@Flag", "DeleteNoticeSS" },
     { "@NoticeAnnouncementCode", noticeCode }
 };

                await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// THIS METHOD IS FOR SAVE NOTICE
        /// </summary>

        public async Task<string> SaveNoticeRM(AccountManager prop)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Flag", "SaveNoticeRM"),
                new SqlParameter("@NoticeTitle", prop.NoticeTitle ?? string.Empty),
                new SqlParameter("@Description", prop.Description ?? string.Empty),
                new SqlParameter("@PublishDate", prop.PublishDate),
                new SqlParameter("@EndDate", prop.EndDate),
                new SqlParameter("@SendBy", prop.SendBy),
                new SqlParameter("@CreatedDate", DateTime.Now),
                //new SqlParameter("@Document", string.IsNullOrEmpty(prop.Document) ? "" : prop.Document),
                new SqlParameter("@WingName", string.IsNullOrEmpty(prop.WingName) ? DBNull.Value : (object)prop.WingName),
                new SqlParameter("@MemberIds", prop.SelectedMemberCodes != null ? string.Join(",", prop.SelectedMemberCodes) : (object)DBNull.Value)
            };

            DataTable dt = await MSSQL.ExecuteStoredProcedure("sp_SMS", parameters);
            return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
        }

        public async Task SaveNoticeDocumentsRM(string noticeCode, List<string> fileNames)
        {
            foreach (var fileName in fileNames)
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@Flag", "SaveDocumentRM"),
            new SqlParameter("@NoticeCode", noticeCode),
            new SqlParameter("@Document", fileName)
                };

                await MSSQL.ExecuteStoredProcedure("sp_SMS", parameters);
            }
        }




        /// <summary>
        /// /this is for insert notice log by wing
        /// </summary>

        public async Task InsertNoticeLogByWingRM(string noticeCode, string wingName)
        {
            SqlParameter[] param = new SqlParameter[]
            {
                new SqlParameter("@Flag", "InsertNoticeLogRM"),
                new SqlParameter("@NoticeCode", noticeCode),
                new SqlParameter("@WingName", wingName)
            };

            await MSSQL.ExecuteStoredProcedure("sp_SMS", param);
        }




        /// <summary>
        /// /this is for insert notice log by Member
        /// </summary>
        public async Task InsertNoticeLogsByMembersRM(string noticeCode, List<string> memberCodes)
        {
            if (string.IsNullOrWhiteSpace(noticeCode) || memberCodes == null) return;

            foreach (var memberCode in memberCodes)
            {
                if (string.IsNullOrWhiteSpace(memberCode)) continue;

                SqlParameter[] param = new SqlParameter[]
                {
                    new SqlParameter("@Flag", "InsertNoticeLogRM"),
                    new SqlParameter("@NoticeCode", noticeCode),
                    new SqlParameter("@MemberCode", memberCode)
                };

                await MSSQL.ExecuteStoredProcedure("sp_SMS", param);
            }
        }



        /// <summary>
        /// THIS METHOD IS FOR THE GETMEMBERS in the dropdown 
        /// </summary>

        public async Task<List<AccountManager>> GetMembersRM(string wing, List<int> memberIds = null)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@WingName", wing),
                new SqlParameter("@MemberIds", DBNull.Value),
                new SqlParameter("@Flag", "FetchMemberRM")
            };

            DataTable dt = await MSSQL.ExecuteStoredProcedure("sp_SMS", parameters);
            List<AccountManager> members = new List<AccountManager>();

            foreach (DataRow row in dt.Rows)
            {
                members.Add(new AccountManager
                {
                    MemberId = Convert.ToInt32(row["MemberId"]),
                    MemberCode = row["MemberCode"].ToString(),
                    FullName = row["FullName"].ToString(),
                    FlatCode = row["FlatCode"].ToString(),
                    ToEmailAddress = row["Email"].ToString()
                });
            }

            return members;
        }




        /// <summary>
        /// THIS METHOD IS FOR GET NOTICE BY ITS CODE
        /// </summary>
        /// <param name="noticeCode"></param>
        /// <returns></returns>
        public async Task<DataTable> GetNoticeByCodeRM(string noticeCode)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Flag", "ShowDataRM"),
                new SqlParameter("@NoticeCode", noticeCode)
            };

            return await MSSQL.ExecuteStoredProcedure("sp_SMS", parameters);
        }




        /// <summary>
        /// This method is for the Fetch the Documentes on notice code 
        /// </summary>

        public async Task<AccountManager> GetNoticeWithDocumentsByCodeRM(string noticeCode)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@Flag", "ShowDataRM"),
        new SqlParameter("@NoticeCode", noticeCode)
            };

            DataTable noticeTable = await MSSQL.ExecuteStoredProcedure("sp_SMS", parameters);

            SqlParameter[] docParams = new SqlParameter[]
            {
        new SqlParameter("@Flag", "GetDocumentsByNoticeRM"),
        new SqlParameter("@NoticeCode", noticeCode)
            };

            DataTable docTable = await MSSQL.ExecuteStoredProcedure("sp_SMS", docParams);

            var model = new AccountManager();
            if (noticeTable.Rows.Count > 0)
            {
                var row = noticeTable.Rows[0];
                model.NoticeCode = row["NoticeAnnouncementCode"].ToString();
                model.NoticeTitle = row["NoticeTitle"].ToString();
                model.Description = row["Description"].ToString();
                model.PublishDate = Convert.ToDateTime(row["PublishDate"]);
                model.EndDate = Convert.ToDateTime(row["EndDate"]);
            }

            // Enhanced deduplication
            model.AttachmentList = docTable.Rows
                .OfType<DataRow>()
                .Select(docRow => docRow["Document"].ToString().Trim())
                .Where(doc => !string.IsNullOrWhiteSpace(doc))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return model;
        }













        /// <summary>
        /// THIS METHOD IS FOR THE SEND EMAIL
        /// </summary>
        /// <returns></returns>

        public async Task<string> SendEmailRM(AccountManager prop)
        {
            try
            {
                if (prop.ToEmailAddresses == null || !prop.ToEmailAddresses.Any())
                    return "❌ Failed: No recipient email provided.";

                string host = ConfigurationManager.AppSettings["EmailHost"];
                int port = Convert.ToInt32(ConfigurationManager.AppSettings["EmailPort"]);
                string fromEmail = ConfigurationManager.AppSettings["EmailFrom"];
                string username = ConfigurationManager.AppSettings["EmailUsername"];
                string password = ConfigurationManager.AppSettings["EmailPassword"];
                string contactNumber = ConfigurationManager.AppSettings["EmailContact"]; // Add this key to Web.config

                Regex emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

                using (SmtpClient smtp = new SmtpClient(host, port))
                {
                    smtp.Credentials = new NetworkCredential(username, password);
                    smtp.EnableSsl = true;

                    foreach (var email in prop.ToEmailAddresses)
                    {
                        if (string.IsNullOrWhiteSpace(email) || !emailRegex.IsMatch(email))
                            continue;

                        MailMessage message = new MailMessage
                        {
                            From = new MailAddress(fromEmail),
                            Subject = prop.Subject,
                            IsBodyHtml = true,
                            Body = $@"
<div style='font-family: Poppins, sans-serif; color: #333;'>
    <p style='font-size: 12px; color: #777;'>Important updates regarding your monthly maintenance</p>

    <p style='font-size: 14px;'><strong>Dear Member,</strong></p>

    <p style='font-size: 14px;'>We hope you are doing well and staying safe.</p>

    <p style='font-size: 14px;'><strong>Notice:</strong> {prop.Description}</p>

    

   <p style='font-size: 14px; margin-top: 25px;'>
    <strong>Warm regards,</strong><br />
    <b>{prop.FullName}</b><br />
    {prop.SendByRole},<br />
    Green Valley Housing Society<br />
    Email: {fromEmail}<br />
    Phone: {prop.ContactNumber}
</p>


</div>"

                        };

                        message.To.Add(email);

                        // Attach any files
                        if (prop.AttachmentList != null)
                        {
                            foreach (var file in prop.AttachmentList)
                            {
                                if (!string.IsNullOrWhiteSpace(file))
                                {
                                    string safeFile = Path.GetFileName(file);
                                    string path = HostingEnvironment.MapPath("~/Uploads/Notices/" + safeFile);
                                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                                    {
                                        message.Attachments.Add(new Attachment(path));
                                    }
                                }
                            }
                        }

                        await smtp.SendMailAsync(message);
                    }
                }

                return "✅ Emails sent successfully.";
            }
            catch (Exception ex)
            {
                return $"❌ Failed: {ex.Message}";
            }
        }












        /// <summary>
        /// THIS METHOD IS FOR UPDATE NOTICE
        /// </summary>

        public async Task UpdateNoticeRM(AccountManager prop)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Flag", "UpdateNoticeRM"),
                new SqlParameter("@NoticeCode", prop.NoticeCode),
                new SqlParameter("@NoticeTitle", prop.NoticeTitle ?? ""),
                new SqlParameter("@Description", prop.Description ?? ""),
                new SqlParameter("@PublishDate", prop.PublishDate),
                new SqlParameter("@EndDate", prop.EndDate),
                new SqlParameter("@Document", string.IsNullOrEmpty(prop.Document) ? DBNull.Value : (object)prop.Document)
            };

            await MSSQL.ExecuteStoredProcedure("sp_SMS", parameters);
        }



        #endregion



        #region********************************************************************* Reports  ***********************************************************


        /// <summary>
        /// //FOR  COUNT ALL MEMEBERS IN  SOCIETY AND SHOW IN CARD/TAB*************************************************************************
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetTotalMemberCountNK()
        {
            MSSQL db = new MSSQL();
            Dictionary<string, string> param = new Dictionary<string, string>
        {
            { "@Flag", "CountMembersNK" }

        };

            object result = await db.ExecuteStoreProcedureReturnObj("sp_SMS", param);
            return result != null ? Convert.ToInt32(result) : 0;
        }

        /// <summary>
        /// //SHOW ALL MEMBER LIST  WHEN CLICK ON CARD/TAB*************************************************************************
        /// </summary>
        /// <returns></returns>

        public async Task<List<AccountManager>> GetAllMemberDetailsNK()
        {
            MSSQL db = new MSSQL();
            Dictionary<string, string> param = new Dictionary<string, string>
    {
        { "@Flag", "FetchAllMemberDetailsNK" }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);
            List<AccountManager> members = new List<AccountManager>();

            foreach (DataRow row in dt.Rows)
            {
                members.Add(new AccountManager
                {
                    FlatCode = row["FlatCode"]?.ToString() ?? "",
                    FullName = row["FullName"]?.ToString() ?? "",
                    Email = row["Email"]?.ToString() ?? "",
                    PhoneNumber = row["PhoneNumber"]?.ToString() ?? "",
                    Gender = row["Gender"]?.ToString() ?? "",
                    FamilyMemberCount = row.Table.Columns.Contains("FamilyMemberCount") && row["FamilyMemberCount"] != DBNull.Value
                                        ? Convert.ToInt32(row["FamilyMemberCount"]) : 0,
                    NoOfVehicle = row.Table.Columns.Contains("NoofVehicle") && row["NoofVehicle"] != DBNull.Value
                                  ? Convert.ToInt32(row["NoofVehicle"]) : 0,
                    RegisterationDate = row.Table.Columns.Contains("RegisterationDate") && row["RegisterationDate"] != DBNull.Value
                                        ? Convert.ToDateTime(row["RegisterationDate"])
                                        : DateTime.MinValue,
                    RegistrationFee = row.Table.Columns.Contains("RegistrationFee") && row["RegistrationFee"] != DBNull.Value
                                      ? Convert.ToDecimal(row["RegistrationFee"])
                                      : 0,
                    MemberCode = row["MemberCode"]?.ToString() ?? ""
                });
            }

            return members;
        }


        /// <summary>
        /// For Show member Document
        /// </summary>
        /// <param name="memberCode"></param>
        /// <returns></returns>

        public async Task<List<AccountManager>> GetMemberDocumentsNK(string memberCode)
        {
            MSSQL db = new MSSQL();
            Dictionary<string, string> param = new Dictionary<string, string>
    {
        { "@Flag", "FetchMemberDocumentsNK" },
        { "@MemberCode", memberCode }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);

            List<AccountManager> docs = new List<AccountManager>();

            foreach (DataRow row in dt.Rows)
            {
                docs.Add(new AccountManager
                {
                    DocumentId = Convert.ToInt32(row["DocumentId"]),
                    SubTypeId = Convert.ToInt32(row["SubTypeId"]),
                    Document = row["Document"].ToString(),
                    DocumentDate = row["Date"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["Date"])
                });
            }

            return docs;
        }



        /// <summary>
        ///  //FOR  COUNT ALL  WORKERS IN  SOCIETY AND SHOW IN CARD/TAB*************************************************************************
        /// </summary>
        /// <returns></returns>

        public async Task<int> GetTotalWorkerCountNK()
        {
            MSSQL db = new MSSQL();
            Dictionary<string, string> param = new Dictionary<string, string>
        {
            { "@Flag", "CountWorkerNK" }

        };

            object result = await db.ExecuteStoreProcedureReturnObj("sp_SMS", param);
            return result != null ? Convert.ToInt32(result) : 0;
        }


        /// <summary>
        /// //SHOW ALL Worker LIST  WHEN CLICK ON CARD/TAB*************************************************************************
        /// </summary>
        /// <returns></returns>
        public async Task<List<AccountManager>> GetAllWorkerDetailsNK()
        {
            MSSQL db = new MSSQL();
            Dictionary<string, string> param = new Dictionary<string, string>
{
    { "@Flag", "FetchAllWorkerDetailsNK" }
};

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);

            List<AccountManager> workers = new List<AccountManager>();

            foreach (DataRow row in dt.Rows)
            {
                workers.Add(new AccountManager
                {
                    WorkerName = row["WorkerName"].ToString(),
                    JobRole = row["JobRole"].ToString(),
                    WingName = row["WingName"].ToString(),
                    Status = row["Status"].ToString(),
                    WorkerContactNo = row["WorkerContactNo"].ToString(),
                    JoiningDate = Convert.ToDateTime(row["JoiningDate"]),
                    RegisterDate = Convert.ToDateTime(row["RegisterDate"]),
                    Address = row["Address"].ToString(),
                    BaseSalary = row["BaseSalary"] != DBNull.Value ? Convert.ToDecimal(row["BaseSalary"]) : 0,

                });
            }

            return workers;
        }


        /// <summary>
        /// //FOR  COUNT ALL  Vendor IN  SOCIETY AND SHOW IN CARD/TAB*************************************************************************
        /// </summary>
        /// <returns></returns>

        public async Task<int> GetTotalVendorCountNK()
        {
            MSSQL db = new MSSQL();
            Dictionary<string, string> param = new Dictionary<string, string>
        {
            { "@Flag", "CountVendorNK" }

        };

            object result = await db.ExecuteStoreProcedureReturnObj("sp_SMS", param);
            return result != null ? Convert.ToInt32(result) : 0;
        }


        /// <summary>
        /// //SHOW ALL Vendor LIST  WHEN CLICK ON CARD/TAB*************************************************************************
        /// </summary>
        /// <returns></returns>
        public async Task<List<AccountManager>> GetAllVendorDetailsNK()
        {
            // 1) Set up DB helper and parameters
            MSSQL db = new MSSQL();
            var param = new Dictionary<string, string>
    {
        { "@Flag", "FetchAllVendorDetailsNK" }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);

            var vendors = new List<AccountManager>();
            foreach (DataRow row in dt.Rows)
            {
                vendors.Add(new AccountManager
                {
                    VendorName = row["VendorName"].ToString(),
                    VendorTypeName = row["VendorTypeName"].ToString(),
                    BusinessName = row["BusinessName"].ToString(),

                    SubTypeName = row["ServiceName"].ToString(),
                    Email = row["Email"].ToString(),
                    PhoneNumber = row["PhoneNumber"].ToString(),
                    Address = row["Address"].ToString(),
                    RegisterDate = Convert.ToDateTime(row["RegistrationDate"]),
                });
            }

            return vendors;
        }




        /// <summary>
        /// //METHOD  FOR PIE CHART OF DIRECT AND INDIRECT EXPENCE**********************************************************  
        /// </summary>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>




        public async Task<List<AccountManager>> GetMonthlyExpensePieDataAsyncNK(int month, int year)
        {
            MSSQL db = new MSSQL();
            Dictionary<string, string> param = new Dictionary<string, string>
    {
        { "@Month", month.ToString() },
        { "@Year", year.ToString() },
        { "@Flag", "ShowpiechartDirectIndirectNK" }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);

            List<AccountManager> list = new List<AccountManager>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new AccountManager
                {
                    ExpenseType = row["ExpenseType"].ToString(),
                    TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                    Percentage = Convert.ToDecimal(row["Percentage"])
                });
            }

            return list;
        }



        /// <summary>
        /// //For LIST OF dIRECT INDIRECT GRAPH WHEN CLICK ON EACH SLICE*************************
        /// </summary>
        /// <param name="expenseTypeId"></param>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>


        public async Task<List<AccountManager>> GetExpenseDetailsByTypeNK(int expenseTypeId, int month, int year)
        {
            Dictionary<string, string> para = new Dictionary<string, string>()
    {
        { "@flag", "ShowListDirectIndirectNK" },
        { "@ExpenseTypeId", expenseTypeId.ToString() },
        { "@Month", month.ToString() },
        { "@Year", year.ToString() }
    };

            MSSQL db = new MSSQL();
            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", para);

            return dt.AsEnumerable().Select(row => new AccountManager
            {
                ExpenseTypeName = row["ExpenseTypeName"]?.ToString(),
                ExpenseName = row["ExpenseName"]?.ToString(),
                WingName = row["WingName"]?.ToString(),
                PaidTo = row["PaidTo"]?.ToString(),


                ExpenseDate = DateTime.TryParse(row["ExpenseDate"]?.ToString(), out DateTime tempDate)
                                ? tempDate
                                : (DateTime?)null,

                // Total Expense
                TotalAmount = dt.Columns.Contains("TotalAmount") && decimal.TryParse(row["TotalAmount"]?.ToString(), out decimal totalAmt) ? totalAmt : 0,

                // Paid Amount
                PaidAmount = dt.Columns.Contains("PaidAmount") && decimal.TryParse(row["PaidAmount"]?.ToString(), out decimal paidAmt) ? paidAmt : 0,

                // Remaining Amount
                RemainingAmount = dt.Columns.Contains("RemainingAmount") && decimal.TryParse(row["RemainingAmount"]?.ToString(), out decimal remainAmt) ? remainAmt : 0,

                // Payment Status (Unpaid / Pending / Paid)
                PaymentStatus = row["PaymentStatus"]?.ToString(),
                PaidDate = DateTime.TryParse(row["PaidDate"]?.ToString(), out DateTime paidDate)
    ? paidDate
    : DateTime.MinValue,

                // GST Info
                CGST = row["CGST"]?.ToString(),
                SGST = row["SGST"]?.ToString(),
                TotalGSTAmount = dt.Columns.Contains("TotalGSTAmount") && decimal.TryParse(row["TotalGSTAmount"]?.ToString(), out decimal gstAmt) ? gstAmt : 0

            }).ToList();
        }



        /// <summary>
        /// Show monthly worker salary  
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        public async Task<List<AccountManager>> GetMonthlyTotalWorkerSalaryAsyncNK(int year)
        {
            MSSQL db = new MSSQL();
            Dictionary<string, string> param = new Dictionary<string, string>
    {
        { "@Flag", "GetWorkerSalaryTotalPerMonthNK" },
        { "@Year", year.ToString() } // ✅ Pass the selected year
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);

            List<AccountManager> list = new List<AccountManager>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new AccountManager
                {
                    MonthName = row["MonthName"].ToString(),
                    TotalAmount = Convert.ToDecimal(row["TotalAmount"])
                });
            }

            return list;
        }


        /// <summary>
        ///  //for  show list of worker payment when click on column********************
        /// </summary>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>

        public async Task<List<AccountManager>> GetWorkerSalaryListByMonthAsyncNK(int month, int year)
        {
            MSSQL db = new MSSQL();
            Dictionary<string, string> param = new Dictionary<string, string>
    {
        { "@Flag", "GetWorkerSalaryListByMonthNK" },
        { "@Month", month.ToString() },
        { "@Year", year.ToString() }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);

            return dt.AsEnumerable().Select(row => new AccountManager
            {
                RoleName = row["Role"].ToString(),
                WorkerName = row["WorkerName"].ToString(),
                WorkerCode = row["WorkerCode"].ToString(),
                AccountNo = row["AccountNo"].ToString(),
                WorkerContactNo = row["Contact"].ToString(),
                JoiningDate = Convert.ToDateTime(row["Date"]),
                BaseSalary = Convert.ToDecimal(row["BaseSalary"]),
                IFSCCode = row["IFSC Code"].ToString(),
                WorkerUPI = row["Worker UPI"].ToString(),

                AttendanceMonth = row["AttendanceMonth"].ToString(),
                DaysPresent = Convert.ToInt32(row["DaysPresent"]),

                PerdayPayment = Convert.ToDecimal(row["PerdayPayment"]),
                AmountToBePaid = Convert.ToDecimal(row["AmountToBePaid"]),

                PaymentStatus = row["PaymentStatus"].ToString(),
                TransactionRef = row["TransactionRef"].ToString(),
                PaidDate = row["PaidDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["PaidDate"])
            }).ToList();
        }

        /// <summary>
        /// //for  show details on list of worker payment when click on  See DETails********************
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        public async Task<List<AccountManager>> ShowWorkerSalaryListALLMONTHNK(int year)
        {
            MSSQL db = new MSSQL();
            Dictionary<string, string> param = new Dictionary<string, string>
    {
        { "@Flag", "GetFullWorkerSalaryDetailsListNK" },

        { "@Year", year.ToString() }
    };

            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", param);

            return dt.AsEnumerable().Select(row => new AccountManager
            {
                WorkerName = row["WorkerName"].ToString(),
                SubTypeName = row["SubTypeName"].ToString(),
                WorkerContactNo = row["WorkerContactNo"].ToString(),
                Address = row["Address"].ToString(),
                Amount = Convert.ToDecimal(row["Amount"]),
                PaidDate = Convert.ToDateTime(row["PaidDate"])
            }).ToList();
        }



        /// <summary>
        /// /Show all expence deatils list 
        /// </summary>
        /// <returns></returns>
        public async Task<List<AccountManager>> ShowALLExpenceDetailsNK()
        {
            Dictionary<string, string> para = new Dictionary<string, string>()
    {
        { "@flag", "ShowALLExpenseListNK" }
    };

            MSSQL db = new MSSQL();
            DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", para);

            return dt.AsEnumerable().Select(row => new AccountManager
            {
                ExpenseTypeName = row["ExpenseTypeName"]?.ToString(),
                ExpenseName = row["ExpenseName"]?.ToString(),
                WingName = row["WingName"]?.ToString(),
                PaidTo = row["PaidTo"]?.ToString(),


                ExpenseDate = DateTime.TryParse(row["ExpenseDate"]?.ToString(), out DateTime tempDate)
                                ? tempDate
                                : (DateTime?)null,

                // Total Expense
                TotalAmount = dt.Columns.Contains("TotalAmount") && decimal.TryParse(row["TotalAmount"]?.ToString(), out decimal totalAmt) ? totalAmt : 0,

                // Paid Amount
                PaidAmount = dt.Columns.Contains("PaidAmount") && decimal.TryParse(row["PaidAmount"]?.ToString(), out decimal paidAmt) ? paidAmt : 0,

                // Remaining Amount
                RemainingAmount = dt.Columns.Contains("RemainingAmount") && decimal.TryParse(row["RemainingAmount"]?.ToString(), out decimal remainAmt) ? remainAmt : 0,

                // Payment Status (Unpaid / Pending / Paid)
                PaymentStatus = row["PaymentStatus"]?.ToString(),
                PaidDate = DateTime.TryParse(row["PaidDate"]?.ToString(), out DateTime temDate)
    ? temDate
    : DateTime.MinValue,


                // GST Info
                CGST = row["CGST"]?.ToString(),
                SGST = row["SGST"]?.ToString(),
                TotalGSTAmount = dt.Columns.Contains("TotalGSTAmount") && decimal.TryParse(row["TotalGSTAmount"]?.ToString(), out decimal gstAmt) ? gstAmt : 0

            }).ToList();
        }






        //VIRAJ   BALLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLL  10-7-2025


        // 🔹 1. For Bar Chart: Event Budget by Month
        //VIRAJ   BALLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLL  10-7-2025


        // 🔹 1. For Bar Chart: Event Budget by Month
        // 🔹 1. For Bar Chart: Event Budget by Month
        //VIRAJ   BALLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLL  10-7-2025


        /// <summary>
        /// // 🔹 1. For Bar Chart: Event Budget by Month
        /// </summary>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public async Task<List<AccountManager>> GetBudgetVsActualDataAsyncVD(int month, int year)
        {
            MSSQL obj = new MSSQL();
            var result = new List<AccountManager>();

            var parameters = new Dictionary<string, string>
{
    { "@flag", "EventBudgetByMonthVD" },
    { "@Month", month.ToString() },
    { "@Year", year.ToString() },
    { "@Email", "" },
    { "@Password", "" }
};

            using (SqlDataReader dr = await obj.ExecuteStoreProcedureReturnDataReader("sp_SMS", parameters))
            {
                while (dr.Read())
                {
                    var fromDate = dr["FromDate"] != DBNull.Value ? Convert.ToDateTime(dr["FromDate"]) : DateTime.MinValue;

                    result.Add(new AccountManager
                    {
                        EventName = dr["EventName"].ToString(),
                        AllocatedBudget = Convert.ToDecimal(dr["AllocatedBudget"]),
                        ActualCost = Convert.ToDecimal(dr["ActualCost"]),
                        FromDate = fromDate,
                        Month = fromDate.Month,
                        Year = fromDate.Year,
                        MonthYear = fromDate.ToString("MMMM yyyy")
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// // 🔹 2. For Modal: All Event Budget Details
        /// </summary>
        /// <returns></returns>
        public async Task<List<AccountManager>> GetAllEventBudgetvdDetailsAsyncVD()
        {
            MSSQL obj = new MSSQL();
            var result = new List<AccountManager>();

            var parameters = new Dictionary<string, string>
{
    { "@flag", "EventBudgetDetailsVD" },
    { "@Email", "" },
    { "@Password", "" }
};

            using (SqlDataReader dr = await obj.ExecuteStoreProcedureReturnDataReader("sp_SMS", parameters))
            {
                while (dr.Read())
                {
                    var fromDate = Convert.ToDateTime(dr["FromDate"]);

                    result.Add(new AccountManager
                    {
                        EventName = dr["EventName"].ToString(),
                        AllocatedBudget = Convert.ToDecimal(dr["AllocatedBudget"]),
                        ActualCost = Convert.ToDecimal(dr["ActualCost"]),
                        FromDate = fromDate,
                        Month = fromDate.Month,
                        Year = fromDate.Year,
                        MonthYear = fromDate.ToString("MMMM yyyy")
                    });
                }
            }

            return result;
        }


        ////////////////////////// new one
        public async Task<List<AccountManager>> GetAllDistributionEventDetailsAsync()
        {
            MSSQL obj = new MSSQL();
            var result = new List<AccountManager>();

            var parameters = new Dictionary<string, string>
    {
        { "@flag", "DistrubutionEvent" }
    };

            using (SqlDataReader dr = await obj.ExecuteStoreProcedureReturnDataReader("sp_SMS", parameters))
            {
                while (await dr.ReadAsync())
                {
                    var fromDate = dr["FromDate"] != DBNull.Value
                                    ? Convert.ToDateTime(dr["FromDate"])
                                    : DateTime.MinValue;

                    result.Add(new AccountManager
                    {
                        EventName = dr["EventName"]?.ToString(),
                        DistributionCategory = dr["DistributionCategory"]?.ToString(),
                        AllocatedBudget = dr["AllocatedBudget"] != DBNull.Value ? Convert.ToDecimal(dr["AllocatedBudget"]) : 0,
                        ActualCost = dr["ActualCost"] != DBNull.Value ? Convert.ToDecimal(dr["ActualCost"]) : 0,
                        Document = dr["Document"]?.ToString(),
                        FromDate = fromDate,
                        Month = fromDate != DateTime.MinValue ? fromDate.Month : 0,
                        Year = fromDate != DateTime.MinValue ? fromDate.Year : 0,
                        MonthYear = fromDate != DateTime.MinValue ? fromDate.ToString("MMMM yyyy") : string.Empty
                    });
                }
            }

            return result;
        }

        ////////////////////////// new one fin











        /// <summary>
        /// // 🔹 3. For Line Chart: Monthly Income vs Expense for a Year
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        public async Task<List<AccountManager>> GetMonthlyIncomeExpenseAsyncVD(int year)
        {
            MSSQL obj = new MSSQL();
            var result = new List<AccountManager>();

            var parameters = new Dictionary<string, string>
{
    { "@flag", "IncomeExpenseChartVD" },
    { "@Year", year.ToString() },
    { "@Email", "" },
    { "@Password", "" }
};

            using (SqlDataReader dr = await obj.ExecuteStoreProcedureReturnDataReader("sp_SMS", parameters))
            {
                while (dr.Read())
                {
                    result.Add(new AccountManager
                    {
                        MonthName = dr["MonthName"].ToString(),
                        MonthNumber = Convert.ToInt32(dr["MonthNumber"]),
                        Year = Convert.ToInt32(dr["Year"]),
                        TotalIncome = Convert.ToDecimal(dr["TotalIncome"]),
                        TotalExpense = Convert.ToDecimal(dr["TotalExpense"]),
                        MonthYear = $"{dr["MonthName"]} {dr["Year"]}"
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// // 🔹 4. For Modal: Income vs Expense Details by Month/Year
        /// </summary>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public async Task<List<AccountManager>> GetIncomeExpenseDetailsAsyncVD(int? month = null, int? year = null)
        {
            MSSQL obj = new MSSQL();
            var result = new List<AccountManager>();

            var parameters = new Dictionary<string, string>
   {
       { "@flag", "IncomeExpenseDetailsVD" },
       { "@Year", year?.ToString() ?? "" },     // Null-coalescing to empty string
       { "@Email", "" },
       { "@Password", "" }
   };

            using (SqlDataReader dr = await obj.ExecuteStoreProcedureReturnDataReader("sp_SMS", parameters))
            {
                while (dr.Read())
                {
                    result.Add(new AccountManager
                    {
                        TypeLabel = dr["TypeLabel"].ToString(),
                        Amount = Convert.ToDecimal(dr["Amount"]),
                        PaymentPurpose = dr["PaymentPurpose"].ToString(),
                        PaymentByName = dr["PaymentByName"].ToString(),
                        PaidToName = dr["PaidToName"].ToString(),
                        PaidDate = dr["PaidDate"] != DBNull.Value ? Convert.ToDateTime(dr["PaidDate"]) : DateTime.MinValue


                    });
                }
            }

            return result;
        }


        #endregion



    }

}





      

