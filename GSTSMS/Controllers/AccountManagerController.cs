using GSTSMSHelper;
using GSTSMSLibrary.Account;
using GSTSMSLibrary.AccountManager;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static GSTSMSLibrary.AccountManager.AccountManager;


namespace GSTSMS.Controllers
{
    public class AccountManagerController : Controller
    {
        // GET: AccountManager
        public ActionResult Index()
        {
            return View();

        }
     

        BALAccountManager objDashbaord = new BALAccountManager();

        #region************************************************************************************Notification***********************************************************************

        /// <summary>
        /// Retrieves unread notifications for the logged-in user.
        /// </summary>
        /// <returns>JSON list of notifications filtered by user's StaffCode.</returns>
        [HttpGet]
        public async Task<JsonResult> GetNotificationsPDV()
        {
            try
            {
                var staffCode = Session["StaffCode"]?.ToString();
                if (string.IsNullOrEmpty(staffCode))
                {
                    // Return empty list if user session is missing
                    return Json(new List<object>(), JsonRequestBehavior.AllowGet);
                }
                var bal = new BALAccountManager();
                var data = await bal.GetNotificationsPDV(staffCode);
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetNotifications: {ex.Message}");
                return Json(new { success = false, message = "Failed to load notifications." }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Marks a specific notification as read in the database.
        /// </summary>
        /// <param name="id">ID of the notification to mark as read.</param>
        /// <returns>JSON indicating success or failure.</returns>
        [HttpPost]
        public async Task<JsonResult> MarkNotificationReadPDV(int id)
        {
            try
            {
                var bal = new BALAccountManager();
                bool success = await bal.MarkNotificationAsSeenPDV(id);
                return Json(new { success });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in MarkNotificationRead: {ex.Message}");
                return Json(new { success = false, message = "Failed to mark notification as read." });
            }
        }

        /// <summary>
        /// Marks all unread notifications for the current user as read.
        /// </summary>
        /// <returns>JSON indicating success or failure.</returns>
        [HttpPost]
        public async Task<JsonResult> MarkNotificationsPDV()
        {
            try
            {
                var staffCode = Session["StaffCode"]?.ToString();
                var bal = new BALAccountManager();
                bool success = await bal.MarkAllNotificationsAsSeenPDV(staffCode);
                return Json(new { success });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in MarkNotifications: {ex.Message}");
                return Json(new { success = false, message = "Failed to mark notifications as read." });
            }
        }






        #endregion


        #region********************************************************************* calender  ***********************************************************
        /// <summary>
        /// For opening the calander
        /// </summary>
        public PartialViewResult CalendarPartial()
        {
            return PartialView("_CalendarPartial");
        }

        /// <summary>
        ///  For FullCalendar: Returns basic event info (title, dateGetEventDetailsByDateAsync
        /// </summary>

        [HttpGet]
        public async Task<JsonResult> GetCalendarEvents()
        {
            try
            {
                var events = await bal.GetAllEventsAsyncSV();
                var formatted = events.Select(e => new {
                    title = e.EventName,
                    start = e.EventDate.ToString("yyyy-MM-dd"),
                    EventCode = e.EventCode, // <-- Ensure this is not null
                    allDay = true
                });
                return Json(formatted, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }



        /// <summary>
        /// Returns a list of all meetings (for use in calendar or listing views).
        /// </summary>


        [HttpGet]
        public async Task<JsonResult> GetCalendarMeetings()
        {
            try
            {
                var meetings = await bal.GetAllMeetingAsyncSV();
                var formatted = meetings.Select(m => new {
                    title = m.MeetingName,
                    start = m.ScheduledDate.ToString("yyyy-MM-dd"),
                    MeetingCode = m.MeetingCode, // <-- Ensure this is not null
                    allDay = true
                });
                return Json(formatted, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }



        /// <summary>
        /// Called on calendar day click: Returns event details on a specific date.
        /// Includes Location, Status, and Description.
        /// Called via AJAX when a date is clicked
        /// </summary>
        /// 
        [HttpGet]
        public async Task<PartialViewResult> GetEventDetailsByDate(string date)
        {
            if (!DateTime.TryParse(date, out DateTime selectedDate))
            {
                ViewBag.ErrorMessage = "Invalid date format.";
                return PartialView("_EventDetails", new List<AccountManager>());
            }

            List<AccountManager> details = await bal.GetEventDetailsByDateAsyncSV(selectedDate);
            return PartialView("_EventDetails", details);
        }


        /// <summary>
        /// For getting the evnt details on another modal oof calander
        /// </summary>

        [HttpGet]
        public async Task<PartialViewResult> GetEventDetailsByCode(string eventCode)
        {
            if (string.IsNullOrWhiteSpace(eventCode))
            {
                ViewBag.ErrorMessage = "Event code is required.";
                return PartialView("_EventDetails", new List<AccountManager>());
            }

            List<AccountManager> details = await bal.GetEventDetailsByIdAsyncSV(eventCode);
            return PartialView("_EventDetails", details);
        }



        /// <summary>
        /// Called on calendar day click: Returns meeting details on a specific date.
        /// Includes Location, Status, and Agenda.
        /// Called via AJAX when a date is clicked
        /// </summary>

        [HttpGet]
        public async Task<PartialViewResult> GetMeetingDetailsByDate(string date)
        {
            if (!DateTime.TryParse(date, out DateTime selectedDate))
            {
                ViewBag.ErrorMessage = "Invalid date format.";
                return PartialView("_MeetingDetails", new List<AccountManager>());
            }

            // Get meeting details based on the date
            List<AccountManager> details = await bal.GetMeetingDetailsByDateAsyncSV(selectedDate);
            return PartialView("_MeetingDetails", details);
        }


        /// <summary>
        /// For getting meeting details by the meetin Id and displaying on another details modal
        /// </summary>

        [HttpGet]
        public async Task<PartialViewResult> GetMeetingDetailsByCode(string meetingCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(meetingCode))
                {
                    ViewBag.ErrorMessage = "Meeting code is required.";
                    return PartialView("_EventDetails", new List<AccountManager>());
                }

                var details = await bal.GetMeetingDetailsByIdAsyncSV(meetingCode);
                return PartialView("_EventDetails", details);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading meeting details: {ex.Message}";
                return PartialView("_EventDetails", new List<AccountManager>());
            }
        }




        #endregion


        #region****************************************************************** Profile ***********************************************************************


        /// <summary>
        /// Logs the current user out by redirecting them to the Account controller's Logout action.
        /// </summary>

        public ActionResult Logout()
        {
            return RedirectToAction("Logout", "Account");
        }



        /// <summary>
        /// Loads the Change Password view with staff details from the current session.
        /// </summary>
        /// <returns>The ChangePassword view with a pre-filled AccountManager model.</returns>

        [HttpGet]
        public ActionResult ChangePassword()
        {
            var model = new AccountManager
            {
                StaffName = Session["StaffName"]?.ToString(),
                Email = Session["Email"]?.ToString(),
                PhoneNumber = Session["ContactNumber"]?.ToString(),
                StaffCode = Session["StaffCode"]?.ToString()
            };

            return View(model);
        }


        // <summary>
        /// Handles submission of the Change Password request.
        /// Populates staff details from session, builds a description, 
        /// and sends the request via BALAccountManager.
        /// </summary>
        /// <param name="model">AccountManager model containing password change request details.</param>


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(AccountManager model)
        {
            if (ModelState.IsValid)
            {
                // Fill in session-based details if needed
                model.StaffCode = Session["StaffCode"]?.ToString();
                model.StaffName = Session["StaffName"]?.ToString();
                model.Email = Session["Email"]?.ToString();

                // Set dynamic description
                model.Description = $"Password Change Request initiated by {model.StaffName} (Staff Code: {model.StaffCode}) on {DateTime.Now:dd-MMM-yyyy hh:mm tt}. The user has requested to update their login password associated with email {model.Email}.";

                await new BALAccountManager().PasswordchangerequestSM(model);

                TempData["SuccessMessage"] = "Password change request submitted successfully.";
                return RedirectToAction("Dashboard");
            }

            return View(model);
        }


        /// <summary>
        /// Updates the user's profile photo either by uploading a new file 
        /// or by selecting an avatar. Saves the path in the database and updates session.
        /// </summary>
        /// <param name="photoFile">Uploaded profile photo (if any).</param>
        /// <param name="avatarPath">Avatar image path (if selected).</param>
        /// <returns>Redirects to Dashboard after updating profile photo.</returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateProfilePhoto(HttpPostedFileBase photoFile, string avatarPath)
        {
            string relativePath = null;

            if (photoFile != null && photoFile.ContentLength > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                string folderPath = Server.MapPath("~/Content/img/ProfileImagesDOC/");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string filePath = Path.Combine(folderPath, fileName);
                photoFile.SaveAs(filePath);

                relativePath = "/Content/img/ProfileImagesDOC/" + fileName;
            }
            else if (!string.IsNullOrEmpty(avatarPath))
            {
                relativePath = avatarPath;
            }
            else
            {
                TempData["ErrorMessage"] = "Please select a photo or avatar to upload.";
                return RedirectToAction("Dashboard");
            }

            // === DB Update ===
            var model = new AccountManager
            {
                EntityCode = Session["StaffCode"]?.ToString(),
                Attchment = relativePath
            };

            await new BALAccountManager().ProfilephotoupdateSM(model);

            // === Session update ===
            Session["ProfilePhoto"] = relativePath;

            TempData["SuccessMessage"] = "Profile photo updated successfully.";

            // Redirect directly to dashboard
            return RedirectToAction("Dashboard", "AccountManager");
        }


        #endregion




        #region********************************************************************* Dashboard ***********************************************************





        [HttpGet]
        public async Task<JsonResult> GetEventExpensesByDate(string fromDate, string toDate)
        {
            var db = new MSSQL();
            var parameters = new Dictionary<string, string>
 {
     { "@Flag", "EventExpensesByDate" },
     { "@FromDate", fromDate },
     { "@ToDate", toDate }
 };

            DataSet dsEvents = await db.ExecuteStoreProcedureReturnDS("sp_SMS", parameters);

            var eventExpenses = new List<object>();

            if (dsEvents != null && dsEvents.Tables.Count > 0)
            {
                eventExpenses = dsEvents.Tables[0]
                    .AsEnumerable()
                    .GroupBy(r => new
                    {
                        EventCode = r.Field<string>("EventCode"),
                        EventName = r.Field<string>("EventName")
                    })
                    .Select(g => new
                    {
                        EventCode = g.Key.EventCode,
                        EventName = g.Key.EventName,
                        Allocated = g.Sum(x => x.Field<decimal?>("DistributionAllocatedBudget") ?? 0),
                        Actual = g.Sum(x => x.Field<decimal?>("DistributionActualBudget") ?? 0)
                    })
                    .ToList<object>();
            }

            return Json(eventExpenses, JsonRequestBehavior.AllowGet);
        }






        public async Task<ActionResult> GetMaintenanceMemberList(string status, int wingId, int month)
        {
            try
            {
                BALAccountManager bal = new BALAccountManager();
                DataTable dt = await bal.GetMaintenanceMemberListAsync(status, wingId, month);

                if (dt != null && dt.Rows.Count > 0)
                {
                    var members = dt.AsEnumerable().Select(row => new
                    {
                        MemberName = row["MemberName"].ToString(),
                        FlatNo = row["FlatNo"].ToString(),
                        Amount = Convert.ToDecimal(row["Amount"]),
                        PaymentStatus = row["PaymentStatus"].ToString()
                    }).ToList();

                    return Json(new { status = "success", data = members }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { status = "empty", data = new List<object>() }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<PartialViewResult> GetEventBudgetDetailsPS(int? month, int? year)
        {
            BALAccountManager _bal = new BALAccountManager();
            try
            {
                List<AccountManager> model;

                if (month.HasValue && year.HasValue)
                {
                    model = await _bal.EventBudgetPS(month.Value, year.Value);
                    ViewBag.MonthLabel = new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy");
                    ViewBag.ShowDatePicker = false;
                }
                else
                {
                    model = await _bal.EventBudgetDetailsPS();
                    ViewBag.MonthLabel = "All Months";
                    ViewBag.ShowDatePicker = true;
                }

                return PartialView("_EventBudgetDetailsPS", model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading budget details: " + ex.Message;
                ViewBag.ShowDatePicker = true;
                return PartialView("_EventBudgetDetailsPS", new List<AccountManager>());
            }
        }






        //      public async Task<ActionResult> TransactionListPartial(int transactionType, int month, string bankId)
        //      {
        //          try
        //          {
        //              Console.WriteLine($"Transaction Type: {transactionType}, Month: {month}, BankId: {bankId}");

        //              var objmonth = new AccountManager
        //              {
        //                  Month = month,
        //                  BankCode = "BANC0" + bankId,
        //                  TransactionType = transactionType
        //              };

        //              BALAccountManager accountManager = new BALAccountManager();

        //              DataSet dataSet = await accountManager.TransactionDashboardListSM(objmonth); // Corrected here

        //              if (dataSet == null)
        //              {
        //                  Console.WriteLine("Dataset is null");
        //                  return PartialView("_TransactionListPartialYP", new System.Data.DataTable());
        //              }

        //              Console.WriteLine("Tables count: " + dataSet.Tables.Count);
        //              for (int i = 0; i < dataSet.Tables.Count; i++)
        //              {
        //                  Console.WriteLine($"Table {i} rows: {dataSet.Tables[i].Rows.Count}");
        //              }

        //              // Access the correct table
        //              DataTable table = dataSet.Tables.Count > 0 ? dataSet.Tables[0] : new System.Data.DataTable(); // Corrected here

        //              return PartialView("_TransactionListPartialYP", table);
        //          }
        //          catch (Exception ex)
        //          {
        //              Console.WriteLine("Exception: " + ex.Message);
        //              return PartialView("_TransactionListPartialYP", new System.Data.DataTable());
        //          }
        //      }











        //      [HttpGet]
        //      public async Task<JsonResult> GetBankTransactionSummary(DateTime? fromDate, DateTime? toDate, int? bankId)
        //      {
        //          var result = new Dictionary<string, object>();

        //          if (!fromDate.HasValue || !toDate.HasValue || !bankId.HasValue)
        //          {
        //              result["status"] = "error";
        //              result["message"] = "Missing parameters.";
        //              return Json(result, JsonRequestBehavior.AllowGet);
        //          }

        //          try
        //          {
        //              var bal = new BALAccountManager();
        //              var data = await bal.GetTransactionSummaryByDateYP(fromDate.Value, toDate.Value, bankId.Value);

        //              result["status"] = "success";
        //              result["data"] = data;
        //          }
        //          catch (Exception ex)
        //          {
        //              result["status"] = "error";
        //              result["message"] = ex.Message;
        //          }

        //          return Json(result, JsonRequestBehavior.AllowGet);
        //      }

        //      [HttpGet]
        //      public async Task<ActionResult> LoadTransactionPartial(int bankId, string month, string type)
        //      {
        //          try
        //          {
        //              var dt = await objDashbaord.GetBankTransactionList(bankId, month, type);

        //              var list = dt.AsEnumerable().Select(row => new Dictionary<string, object>
        //{
        //    { "EntityName", row["EntityName"].ToString() },
        //    { "PaymentBy", row["PaymentBy"].ToString() },
        //    { "PaidTo", row["PaidTo"].ToString() },
        //    { "Amount", row["Amount"] },
        //    { "PaymentMode", row["PaymentMode"].ToString() },
        //    { "PaymentPurpose", row["PaymentPurpose"].ToString() },
        //    { "PaidDate", Convert.ToDateTime(row["PaidDate"]).ToString("dd-MM-yyyy") },
        //    { "TransactionType", row["TransactionType"].ToString() }
        //}).ToList();

        //              return PartialView("_TransactionListPartialYP", list);
        //          }
        //          catch (Exception ex)
        //          {
        //              return Content($"<div class='text-center text-danger'>Error loading transactions: {ex.Message}</div>");
        //          }
        //      }







        public async Task<ActionResult> TransactionListPartial(int transactionType, int month, int bankId)
        {
            try
            {
                Console.WriteLine($"Transaction Type: {transactionType}, Month: {month}, BankId: {bankId}");

                var objmonth = new AccountManager
                {
                    Month = month,
                    BankId = bankId,
                    TransactionType = transactionType
                };

                BALAccountManager accountManager = new BALAccountManager();

                DataSet dataSet = await accountManager.TransactionDashboardListSM(objmonth);

                if (dataSet == null)
                {
                    Console.WriteLine("Dataset is null");
                    return PartialView("_TransactionListPartialYP", new DataTable());
                }

                Console.WriteLine("Tables count: " + dataSet.Tables.Count);
                for (int i = 0; i < dataSet.Tables.Count; i++)
                {
                    Console.WriteLine($"Table {i} rows: {dataSet.Tables[i].Rows.Count}");
                }

                DataTable table = dataSet.Tables.Count > 0 ? dataSet.Tables[0] : new DataTable();

                return PartialView("_TransactionListPartialYP", table);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return PartialView("_TransactionListPartialYP", new DataTable());
            }
        }

        // ===================== 2. API: SUMMARY BY DATE ============================
        [HttpGet]
        public async Task<JsonResult> GetBankTransactionSummary(DateTime? fromDate, DateTime? toDate, int? bankId)
        {
            var result = new Dictionary<string, object>();

            if (!fromDate.HasValue || !toDate.HasValue || !bankId.HasValue)
            {
                result["status"] = "error";
                result["message"] = "Missing parameters.";
                return Json(result, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var bal = new BALAccountManager();
                var data = await bal.GetTransactionSummaryByDateYP(fromDate.Value, toDate.Value, bankId.Value);

                result["status"] = "success";
                result["data"] = data;
            }
            catch (Exception ex)
            {
                result["status"] = "error";
                result["message"] = ex.Message;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // ===================== 3. OPTIONAL: OLD LoadTransactionPartial ============
        // If somewhere in UI you're calling /AccountManager/LoadTransactionPartial,
        // we will route it to SAME logic so both APIs behave identical.
        [HttpGet]
        public async Task<ActionResult> LoadTransactionPartial(int bankId, int month, int transactionType)
        {
            try
            {
                var dt = await bal.GetBankTransactionList(bankId, month, transactionType);
                return PartialView("_TransactionListPartialYP", dt);
            }
            catch (Exception ex)
            {
                return Content($"<div class='text-center text-danger'>Error loading transactions: {ex.Message}</div>");
            }
        }



        public async Task<JsonResult> GetMaintenanceMemberList(int wingId, string month)
        {
            var dt = await objDashbaord.GetMaintenanceMemberListAsync(wingId, month);

            var memberList = (from DataRow row in dt.Rows
                              select new
                              {
                                  MemberName = row["MemberName"].ToString(),
                                  FlatNo = row["FlatNo"].ToString(),
                                  Amount = row["Amount"] != DBNull.Value ? Convert.ToDecimal(row["Amount"]) : 0
                              }).ToList();

            return Json(new { data = memberList }, JsonRequestBehavior.AllowGet);
        }



        /// <summary>
        /// this calls event expense
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>

        [HttpGet]
        public async Task<JsonResult> GetEventExpenses(string month)
        {
            try
            {
                var parameters = new Dictionary<string, string>
            {
                { "@Flag", "EventExpensYP" },
                { "@Month", month }
            };

                MSSQL db = new MSSQL();
                DataTable dt = await db.ExecuteStoreProcedureReturnDataTable("sp_SMS", parameters);

                var labels = new List<string>();
                var allocated = new List<decimal>();
                var actual = new List<decimal>();

                foreach (DataRow row in dt.Rows)
                {
                    labels.Add(row["EventName"].ToString());
                    allocated.Add(Convert.ToDecimal(row["TotalAllocatedBudget"]));
                    actual.Add(Convert.ToDecimal(row["TotalActualCost"]));
                }

                return Json(new
                {
                    labels = labels,
                    allocated = allocated,
                    actual = actual
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = true,
                    message = "An error occurred while loading chart data.",
                    details = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// this will call getwing names method
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<JsonResult> GetWingNames()
        {
            var wingsDict = await new BALAccountManager().GetWingListAsyncYP();

            // Convert Dictionary<int, string> to List of anonymous objects for JS
            var wingList = wingsDict.Select(kvp => new { WingId = kvp.Key, WingName = kvp.Value }).ToList();

            return Json(wingList, JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// this will return top 5 red list member
        /// </summary>
        /// <returns></returns>




        [HttpGet]
        public async Task<JsonResult> GetTop5RedListMembers()
        {
            var result = new Dictionary<string, object>();
            try
            {
                var bal = new BALAccountManager();
                var data = await bal.GetTop5RedListMembersYP();

                result["status"] = "success";
                result["data"] = data;
            }
            catch (Exception ex)
            {
                result["status"] = "error";
                result["message"] = "Failed to fetch red list members.";
                result["details"] = ex.Message;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }



        public async Task<JsonResult> GetBankTransactionSummary(int? month, int? bankId, int? year)
        {
            var result = new Dictionary<string, object>();

            if (!month.HasValue || !bankId.HasValue)
            {
                result["status"] = "error";
                result["message"] = "Missing parameters.";
                return Json(result, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var bal = new BALAccountManager();
                var data = await bal.GetMonthlyTransactionSummaryYP(month.Value, bankId.Value,year.Value);

                result["status"] = "success";
                result["data"] = data;
            }
            catch (Exception ex)
            {
                result["status"] = "error";
                result["message"] = "Error fetching transaction summary.";
                result["details"] = ex.Message;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// this will show worker payment chart
        /// 
        /// 
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>

        //[HttpGet]
        //public async Task<JsonResult> GetWorkerPaymentChart(int month)
        //{
        //    try
        //    {
        //        var bal = new BALAccountManager();
        //        var chartData = await bal.GetWorkerPaymentChartAsyncYP(month); // returns Dictionary<string, int> with keys "Completed" and "Pending"
        //        return Json(chartData, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception)
        //    {
        //        return Json(new { Completed = 0, Pending = 0 }, JsonRequestBehavior.AllowGet);
        //    }
        //}

        ///// <summary>
        ///// this willl shows worker payment details
        ///// </summary>
        ///// <param name="month"></param>
        ///// <param name="status"></param>
        ///// <returns></returns>
        //[HttpGet]
        //public async Task<JsonResult> GetWorkerPaymentDetails(int month, string status)
        //{
        //    try
        //    {
        //        var bal = new BALAccountManager();
        //        var result = await bal.GetWorkerPaymentDetailsAsyncYP(month, status); // returns list
        //        return Json(new { status = "success", data = result }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
        //    }
        //}
        /// <summary>
        /// this will show worker payment chart
        /// 
        /// 
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>

        [HttpGet]
        public async Task<JsonResult> GetWorkerPaymentChart(int month)
        {
            try
            {
                var bal = new BALAccountManager();
                var chartData = await bal.GetWorkerPaymentChartAsyncYP(month); // returns Dictionary<string, int> with keys "Completed" and "Pending"
                return Json(chartData, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { Completed = 0, Pending = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// this willl shows worker payment details
        /// </summary>
        /// <param name="month"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<JsonResult> GetWorkerPaymentDetails(int month, string status)
        {
            try
            {
                var bal = new BALAccountManager();
                var result = await bal.GetWorkerPaymentDetailsAsyncYP(month, status); // returns list
                return Json(new { status = "success", data = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }








        //[HttpGet]
        //public async Task<ActionResult> WorkerPaymentListPartialDD(int month, string status)
        //{
        //    var bal = new BALAccountManager();
        //    try
        //    {
        //        var data = await bal.GetWorkerChartListDDAsync(month, status);
        //        return PartialView("_WorkerPaymentDD", data); // 👈 Passing to partial
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
        //    }
        //}
        private readonly BALAccountManager _bal = new BALAccountManager();

        //public JsonResult GetWorkerPaymentChart(int month)
        //{
        //    var result = _bal.GetWorkerPaymentChart(month);
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}
        public async Task<ActionResult> WorkerPaymentListPartial_All()
        {
            var data = await _bal.GetAllWorkersPaymentData();
            return PartialView("_WorkerPaymentListPartial", data);
        }

        public async Task<ActionResult> WorkerPaymentListPartial_Month(int month)
        {
            var data = await _bal.GetWorkersPaymentList(month);
            return PartialView("_WorkerPaymentListPartial", data);
        }

        [HttpGet]
        public async Task<ActionResult> WorkerPaymentListPartialDD(int month, string status, string search = "")
        {
            var bal = new BALAccountManager();

            try
            {
                var data = await bal.GetWorkerChartListDDAsync(month, status);

                // Client-side search करें या server-side
                if (!string.IsNullOrEmpty(search))
                {
                    data = data.Where(w =>
                        w.WorkerName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        w.WorkerCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        w.ContactNo.Contains(search)
                    ).ToList();
                }

                return PartialView("_WorkerPaymentDD", data);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        /// <summary>
        /// this will shows bank details
        /// </summary>
        /// <returns></returns>
        /// <summary>
        /// this will shows bank details
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<JsonResult> GetBankBalanceList()
        {
            try
            {
                var bal = new BALAccountManager();
                var data = await bal.GetBankBalancesCodeYP();

                return Json(new { status = "success", data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        /// <summary>
        /// this will shows bank names
        /// </summary>
        /// <returns></returns>


        [HttpGet]
        public async Task<JsonResult> GetBankCodes()
        {
            try
            {
                var bal = new BALAccountManager();
                var bankList = await bal.GetBankShortCodesAsyncYP();

                return Json(new
                {
                    status = "success",
                    data = bankList
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = "error",
                    message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }




        /// <summary>
        /// this will shows maitenance status chart
        /// </summary>
        /// <param name="month"></param>
        /// <param name="wing"></param>
        /// <returns></returns>

        [HttpGet]
        public async Task<ActionResult> MaintenanceStatusChart(int month, int year)
        {
            var obj = new AccountManager
            {
                Month = month,
                Year = year
            };

            var bal = new BALAccountManager();
            var ds = await bal.GetMaintenanceStatusChartAsyncYP(obj);

            var data = ds.Tables[0].AsEnumerable().Select(r => new
            {
                WingId = Convert.ToInt32(r["WingId"]),
                WingName = r["WingName"].ToString(),
                TotalMembers = Convert.ToInt32(r["TotalMembers"]),

                TotalMonthlyMaintenanceAmount = Convert.ToDecimal(r["TotalMonthlyMaintenanceAmount"]),
                MonthlyMembersPending = Convert.ToInt32(r["MonthlyMembersPending"]),
                MonthlyPaidAmount = Convert.ToDecimal(r["MonthlyPaidAmount"]),
                MonthlyPendingAmount = Convert.ToDecimal(r["MonthlyPendingAmount"]),

                TotalMonthlyPenaltyAmount = r.Table.Columns.Contains("TotalMonthlyPenaltyAmount")
                    ? Convert.ToDecimal(r["TotalMonthlyPenaltyAmount"])
                    : 0,

                TotalOccasionalMaintenanceAmount = Convert.ToDecimal(r["TotalOccasionalMaintenanceAmount"]),
                OccasionalMembersPending = Convert.ToInt32(r["OccasionalMembersPending"]),
                OccasionalPaidAmount = Convert.ToDecimal(r["OccasionalPaidAmount"]),
                OccasionalPendingAmount = Convert.ToDecimal(r["OccasionalPendingAmount"])
            }).ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }




        /// <summary>
        /// this will show worker payment list
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>



        [HttpGet]
        public async Task<ActionResult> WorkerPaymentListPartial(int month)
        {
            var bal = new BALAccountManager();
            try
            {
                var data = await bal.GetWorkerPaymentListAsyncYP(month);
                return PartialView("_WorkerPaymentYP", data); // 👈 Passing to partial
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }




        public async Task<ActionResult> ComplaintsPartial()
        {
            DataTable complaintTable = await objDashbaord.GetAllComplaintsYP();

            var complaintList = new List<ExpandoObject>();

            if (complaintTable != null && complaintTable.Rows.Count > 0)
            {
                foreach (DataRow row in complaintTable.Rows)
                {
                    dynamic complaint = new ExpandoObject();

                    complaint.ComplaintId = row["ComplaintId"]?.ToString();
                    complaint.MemberName = row["MemberName"]?.ToString();
                    complaint.Complaint = row["Complaint"]?.ToString();
                    complaint.ComplaintDate = row["ComplaintDate"]?.ToString();
                    complaint.ComplaintType = row["ComplaintType"]?.ToString();
                    complaint.ComplaintStatus = row["ComplaintStatus"]?.ToString();

                    complaintList.Add(complaint);
                }
            }

            // ✅ Pass model to partial view (no ViewBag required)
            return PartialView("_ComplaintsPartialYP", complaintList);
        }





        /// <summary>
        /// this will return dashbaord view
        /// </summary>
        /// <returns></returns>
        [HttpGet]

        public async Task<ActionResult> MaintenanceDueListYP()
        {
            var obj = new BALAccountManager();
            var ds = await obj.GetDueMaintenanceAmountSM();
            var dt = ds != null && ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
            return PartialView("_MaintenanceDueListYP", dt);
        }


        [HttpGet]
        public async Task<ActionResult> GetMaintenanceMemberListYP(
   string status,
   int wingId,
   int month,
   int maintenanceTypeId,
   string fromDate,
   string toDate)
        {
            var objMonth = new AccountManager
            {
                Year = DateTime.Now.Year,
                Month = month,
                WingId = wingId,
                MaintananceTypeId = maintenanceTypeId,

                // ✅✅ NON-NULLABLE SAFE MAPPING
                FromDate = string.IsNullOrEmpty(fromDate)
                    ? DateTime.MinValue
                    : Convert.ToDateTime(fromDate),

                ToDate = string.IsNullOrEmpty(toDate)
                    ? DateTime.MinValue
                    : Convert.ToDateTime(toDate)
            };

            DataSet ds = new DataSet();

            if (status == "paid")
                ds = await new BALAccountManager().PaidMaintainceMembersListSM(objMonth);
            else if (status == "pending")
                ds = await new BALAccountManager().PendingMaintainceMembersListSM(objMonth);

            if (ds == null || ds.Tables.Count == 0)
                return PartialView("_MaintainceMembersListSM", new DataTable());

            return PartialView("_MaintainceMembersListSM", ds.Tables[0]);
        }







        //[HttpGet]
        //public async Task<ActionResult> EventDetails(string eventCode)
        //{


        //    // month + event filter पास करून SP call
        //    DataSet ds = await objDashbaord.EventExpensDistributionSM(new AccountManager
        //    {

        //        EventCode = eventCode // EventCode वापरा
        //    });

        //    DataTable dt = null;
        //    if (ds != null && ds.Tables.Count > 0)
        //        dt = ds.Tables[0];

        //    return PartialView("_EventDistributionViewYP", dt);
        //}
        [HttpGet]
        public async Task<ActionResult> EventDetails(string eventCode)
        {
            DataSet ds = await objDashbaord.EventExpensDistributionSM(new AccountManager
            {
                EventCode = eventCode
            });

            DataTable dt = null;
            if (ds != null && ds.Tables.Count > 0)
                dt = ds.Tables[0];

            return PartialView("_EventDistributionViewYP", dt);
        }







        [HttpGet]
        public async Task<JsonResult> GetEventExpensesData(int month)
        {
            DataSet dsEvents = await objDashbaord.EventExpensYPSM(new AccountManager { Month = month });

            var eventExpenses = new List<object>();
            if (dsEvents != null && dsEvents.Tables.Count > 0)
            {
                eventExpenses = dsEvents.Tables[0]
                    .AsEnumerable()
                    .GroupBy(r => new
                    {
                        EventCode = r.Field<string>("EventCode"),
                        EventName = r.Field<string>("EventName")
                    })
                    .Select(g => new
                    {
                        EventCode = g.Key.EventCode,
                        EventName = g.Key.EventName,
                        Allocated = g.Sum(x => x.Field<decimal>("DistributionAllocatedBudget")),
                        Actual = g.Sum(x => x.Field<decimal>("DistributionActualBudget"))
                    })
                    .ToList<object>();
            }

            return Json(eventExpenses, JsonRequestBehavior.AllowGet);
        }






        public async Task<ActionResult> Dashboard()
        {
            // ===== Default Date Range: Current Month =====
            DateTime today = DateTime.Today;
            DateTime startOfMonth = new DateTime(today.Year, today.Month, 1);
            DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            string startDate = startOfMonth.ToString("yyyy-MM-dd");
            string endDate = endOfMonth.ToString("yyyy-MM-dd");

            ViewBag.StartOfMonth = startDate;
            ViewBag.EndOfMonth = endDate;

            // ===== Maintenance Due =====
            DataSet dsd = await objDashbaord.GetDueMaintenanceAmountSM();
            decimal totalDue = 0;
            if (dsd != null && dsd.Tables.Count > 0 && dsd.Tables[0].Rows.Count > 0)
            {
                totalDue = dsd.Tables[0].AsEnumerable().Sum(r => r.Field<decimal>("PendingAmount"));
            }
            ViewBag.MaintenanceDue = totalDue;

            // ===== Cash In Hand =====
            decimal cashInHand = await objDashbaord.CashinHandYP();
            ViewBag.CashInHand = cashInHand;

            // ===== Complaints =====
            ViewBag.TotalComplaints = await objDashbaord.TotalComplaintsYP();
            ViewBag.PendingComplaints = await objDashbaord.PendingComplaintsYP();
            ViewBag.SolvedComplaints = await objDashbaord.SolvedComplaintsYP();

            // ===== Total Bank Balance =====
            ViewBag.TotalBankBalance = await objDashbaord.TotalBankBalanceYP();

            // ===== Event Expenses (Default Current Month) =====
            DataSet dsEvents = await objDashbaord.GetEventExpensesByDate(new AccountManager
            {
                FromDate = startOfMonth,  // DateTime
                ToDate = endOfMonth       // DateTime
            });


            var eventExpenses = new List<object>();
            if (dsEvents != null && dsEvents.Tables.Count > 0)
            {
                eventExpenses = dsEvents.Tables[0]
                    .AsEnumerable()
                    .GroupBy(r => new
                    {
                        EventCode = r.Field<string>("EventCode"),
                        EventName = r.Field<string>("EventName")
                    })
                    .Select(g => new
                    {
                        EventCode = g.Key.EventCode,
                        EventName = g.Key.EventName,
                        Allocated = g.Sum(x => x.Field<decimal?>("DistributionAllocatedBudget") ?? 0),
                        Actual = g.Sum(x => x.Field<decimal?>("DistributionActualBudget") ?? 0)
                    })
                    .ToList<object>();
            }
            ViewBag.EventExpenses = Newtonsoft.Json.JsonConvert.SerializeObject(eventExpenses);

            // ===== Bank-wise Balance (KKBK, HDFC) =====
            DataSet dsBank = await objDashbaord.BankBalanceYP();
            if (dsBank != null && dsBank.Tables.Count > 0 && dsBank.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in dsBank.Tables[0].Rows)
                {
                    string bankName = row["BankShortCode"].ToString();
                    decimal amount = Convert.ToDecimal(row["OpeningBalance"]);

                    if (bankName == "KKBK") ViewBag.KKBK = amount;
                    else if (bankName == "HDFC") ViewBag.HDFC = amount;
                }
            }

            return View();
        }




        #endregion

        #region********************************************************************* Society Ac.Details Bank *************************************************

        /// <summary>
        /// To fetch a list of all bank details from the database and pass it to the view for rendering.Date 05-07-2025
        /// </summary>

        public async Task<ActionResult> BankDetailsListSS()
        {
            BALAccountManager bal = new BALAccountManager();
            List<AccountManager> list = await bal.GetAllBankDetailsSS();
            return View(list);  // Strongly typed model passed
        }

        /// <summary>
        /// Toggles the active status of a bank entry based on the provided BankId.Date 03-07-2025
        /// </summary>


        [HttpPost]
        public async Task<JsonResult> ToggleBankStatus(int bankId, bool isActive)
        {
            try
            {
                BALAccountManager bal = new BALAccountManager();
                bool success = await bal.UpdateBankStatusSS(bankId, isActive);
                return Json(new { success });
            }
            catch
            {

                return Json(new { success = false, message = "An error occurred while updating the bank status." });
            }
        }


        /// <summary>
        /// Create view View BankDetails of Transaction
        /// </summary>
        /// <param name="bankCode"></param>
        /// <returns></returns>
        public async Task<ActionResult> ViewBankDetails(string bankCode)
        {
            BALAccountManager bal = new BALAccountManager();
            var allBanks = await bal.GetAllBankDetailsSS();
            var selectedBank = allBanks.FirstOrDefault(b => b.BankCode == bankCode);

            if (selectedBank == null)
                return HttpNotFound("Bank not found");

            selectedBank.TransactionList = await bal.GetTransactionStatementByBankMS(bankCode);

            return View(selectedBank);
        }






        [HttpPost]
        public JsonResult VerifyPin(string pin)
        {
            // Session मधला login password
            string savedPassword = Session["Password"]?.ToString();

            if (savedPassword == null)
                return Json(new { isValid = false });

            // Compare
            bool isMatch = (savedPassword == pin);

            return Json(new { isValid = isMatch });
        }









        /// <summary>
        /// get the Add Bank Society form generate view
        ///  04/07/2025
        ///  MS
        /// </summary>
        /// <returns></returns>
        // GET: Load form partial
        // GET

        [HttpGet]
        public async Task<PartialViewResult> GetAddBankSocietyMS()
        {
            var model = new AccountManager();
            ViewBag.AccountTypeList = await GetAccountTypesAsync();
            return PartialView("_GetAddBankSocietyMS", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GetAddBankSocietyMS(AccountManager model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.AccountTypeList = await GetAccountTypesAsync();
                    return PartialView("_GetAddBankSocietyMS", model);
                }

                var bal = new BALAccountManager();
                string result = await bal.AddBankSocietyMS(model);

                switch (result)
                {
                    case "success": return Json("success", JsonRequestBehavior.AllowGet);
                    case "duplicate_account": return Json("duplicate_account", JsonRequestBehavior.AllowGet);
                    case "duplicate_upi": return Json("duplicate_upi", JsonRequestBehavior.AllowGet);
                    default: return Json("fail", JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Content("SERVER ERROR: " + ex.ToString());
            }
        }

        [HttpPost]
        public async Task<JsonResult> CheckDuplicateAccount(string accountNo)
        {
            var bal = new BALAccountManager();
            bool exists = await bal.IsDuplicateAccountMS(accountNo);
            return Json(exists);
        }

        [HttpPost]
        public async Task<JsonResult> CheckDuplicateUPI(string upiId)
        {
            var bal = new BALAccountManager();
            bool exists = await bal.IsDuplicateUPIMS(upiId); // create same as IsDuplicateAccountMS
            return Json(exists);
        }







        /// <summary>
        /// use to fetch the Account type like Saving,Credit,Bussines 
        /// MS
        /// 08/07/2025
        /// </summary>
        /// <returns></returns>
        public async Task<List<SelectListItem>> GetAccountTypesAsync()
        {
            BALAccountManager bal = new BALAccountManager();
            DataSet ds = await bal.FetchAccountTypeMS();
            List<SelectListItem> list = new List<SelectListItem>();

            if (ds != null && ds.Tables.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    list.Add(new SelectListItem
                    {
                        Value = row["SubTypeId"].ToString(),
                        Text = row["SubTypeName"].ToString()
                    });
                }
            }

            return list;
        }

        /// <summary>
        /// Fetches all bank details and passes the list to the BankList view.
        /// </summary>

        /// <returns>BankList view with a list of AccountManager objects containing bank information.</returns>

        public async Task<ActionResult> BankList()
        {
            try
            {
                BALAccountManager bal = new BALAccountManager();
                List<AccountManager> list = await bal.GetAllBankDetailsSS();
                return View(list);
            }
            catch
            {

                TempData["Error"] = "An error occurred while fetching the bank list.";
                return RedirectToAction("Error", "Home");
            }
        }
        #endregion


        #region********************************************************************* Society Ac.Details Cash ***********************************************************

        /// <summary>
        /// Controller responsible for displaying and managing cash transaction lists and details,
        /// with in-memory caching for performance optimization.
        /// </summary>

        private static List<AccountManager> _transactionCache;



        /// <summary>
        /// Retrieves a list of cash transactions from cache or database and filters them by type.
        /// </summary>
        /// The transaction type to filter by. Default is "All".

        public async Task<ActionResult> CashTansactionListDD(string type = "All")
        {
            // 1. Cache management - only reload if cache is empty or expired
            if (_transactionCache == null)
            {
                BALAccountManager bal = new BALAccountManager();
                _transactionCache = await bal.CashTransactionDD();
            }

            // 2. Early return for common case (All)
            if (string.IsNullOrEmpty(type) || type.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (Request.IsAjaxRequest())
                {
                    BALAccountManager bal = new BALAccountManager();
                    _transactionCache = await bal.CashTransactionDD();
                    return PartialView("_CashTransactionListAll", _transactionCache);
                }
                return View(_transactionCache);
            }

            // 3. Efficient filtering for other cases
            var filtered = _transactionCache
                .Where(t => t.TransactionNature.Equals(type, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (Request.IsAjaxRequest())
            {
                return PartialView("_CashTransactionTable", filtered);
            }

            return View(filtered);
        }






        /// <summary>
        /// Retrieves details for a single transaction from the in-memory cache.
        /// </summary>

        public ActionResult GetTransactionDetailsDD(int id)
        {
            var transaction = _transactionCache?.FirstOrDefault(t => t.TransactionId == id);
            if (transaction == null)
                return HttpNotFound();

            return PartialView("_ViewTransactionPartialDD", transaction);
        }

        //////////////////////////// Shruti Mane////////////////////////////////////////////////////


        BALAccountManager objACC = new BALAccountManager();



        [HttpGet]
        public ActionResult CashTransaction()
        {
            return View();
        }

        // <summary>
        /// Returns the partial view for the Cash Transaction form.
        /// Initializes the receiver dropdown and transaction type list.
        /// 30/06/2025
        /// </summary>
        public async Task<PartialViewResult> CashTransactionPage()
        {
            try
            {
                await PopulateTransactionList();

                ViewBag.ReceiverList = new List<SelectListItem>();
                return PartialView("_CashTransactionPage", new AccountManager());
            }
            catch (Exception ex)
            {
                // Optional: log the exception
                System.Diagnostics.Debug.WriteLine("Error in CashTransactionPage: " + ex.Message);

                // Return empty model and handle gracefully
                ViewBag.ReceiverList = new List<SelectListItem>();
                return PartialView("_CashTransactionPage", new AccountManager());
            }
        }


        /// <summary>
        /// Loads the partial view for adding a debited cash transaction.
        /// </summary>
        /// <remarks>
        /// - Populates the Transaction dropdown using data from the database.
        /// - Initializes an empty Receiver list (to be populated dynamically via JS).
        /// - Pre-selects the "Debit" transaction type by setting TransactionId = 27.
        /// </remarks>
        public async Task<PartialViewResult> AddDebitedCashTransaction()
        {
            try
            {
                await PopulateTransactionList();

                ViewBag.ReceiverList = new List<SelectListItem>();

                var model = new AccountManager
                {
                    TransactionId = 27
                };

                return PartialView("_AddDebitedCashTransaction", model);
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine("Error in AddDebitedCashTransaction: " + ex.Message);
                return PartialView("_AddDebitedCashTransaction", new AccountManager());
            }
        }





        /// <summary>
        /// Handles file upload, sets PaymentBy/PaidTo/EntityCode, saves transaction data,
        /// and updates expense or event payment status if applicable.
        /// 05/07/2025
        /// </summary>
        /// 

        [HttpPost]

        public async Task<ActionResult> SaveCashTransactionSM(AccountManager objprop)
        {
            try
            {
                List<string> attachmentPaths = new List<string>();
                string folderPath = Server.MapPath("~/Content/Documents/TransactionReceipts/");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);


                for (int i = 0; i < Request.Files.Count; i++)
                {
                    HttpPostedFileBase file = Request.Files[i];

                    if (file != null && file.ContentLength > 0)
                    {
                        if (file.ContentLength > 5 * 1024 * 1024)
                        {
                            return Json(new
                            {
                                success = false,
                                message = $"File '{file.FileName}' exceeds the 5MB size limit."
                            });
                        }

                        string fileName = Path.GetFileName(file.FileName);
                        string savedPath = Path.Combine(folderPath, fileName);


                        file.SaveAs(savedPath);

                        string virtualPath = $"/Content/Documents/TransactionReceipts/{fileName}";
                        attachmentPaths.Add(virtualPath);
                    }
                }

                if (objprop.TransactionId == 26)
                {
                    objprop.PaymentBy = objprop.MemberCode;
                    objprop.EntityCode = objprop.MaintenanceCode;
                    objprop.PaidTo = Session["StaffCode"].ToString();
                }
                else if (objprop.TransactionId == 27)
                {
                    objprop.PaymentBy = Session["StaffCode"].ToString();
                    objprop.PaidTo = (objprop.Type == "Other") ? objprop.OtherReceiver : objprop.ReceiverCode;

                    switch (objprop.Type)
                    {
                        case "Vendor":
                            objprop.EntityCode = objprop.ExpenseCode;
                            break;
                        case "EventHandler":
                            objprop.EntityCode = objprop.EventCode;
                            break;
                        case "Worker":
                            objprop.EntityCode = null;
                            break;
                    }
                }


                if (attachmentPaths.Any())
                    objprop.AttachmentPath = attachmentPaths.First();

                string transactionCode = await objACC.SaveCashTransactionAsyncSM(objprop);

                foreach (string path in attachmentPaths)
                {
                    var doc = new AccountManager
                    {
                        TransactionCode = transactionCode,
                        AttachmentPath = path,
                        PaymentMode = "75"
                    };

                    await objACC.SaveAttachmentAsyncSM(doc);
                }

                if (objprop.TransactionId == 27)
                {
                    if (objprop.Type == "Vendor" && !string.IsNullOrEmpty(objprop.ExpenseCode))
                    {
                        await objACC.MarkExpenseAsPaidAsyncSM(objprop.ExpenseCode);
                    }
                    else if (objprop.Type == "EventHandler" && !string.IsNullOrEmpty(objprop.EventCode))
                    {
                        await objACC.MarkEventBudgetAsPaidAsyncSM(objprop.EventCode);
                    }
                }

                return Json(new
                {
                    success = true,
                    transactionCode = transactionCode,
                    transactionId = objprop.TransactionId,
                    receiverType = objprop.Type,
                    receiverCode = objprop.ReceiverCode
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error while saving: " + ex.Message
                });
            }
        }


        /// <summary>
        /// Converts a numeric amount to a string in currency format with "only".
        /// </summary>
        /// <param /name="amount"/>Amount in decimal</param>
        /// 10/07/2025
        /// <returns>Formatted string</returns>

        private string ConvertAmountToWords(decimal amount)
        {
            try
            {
                return NumberToWordsConverter.ConvertAmountToWords(amount);
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine("Error in ConvertAmountToWords: " + ex.Message);

                return string.Empty;
            }
        }



        /// <summary>
        /// Loads receipt details and returns the Receipt PDF view for preview.
        /// </summary>
        /// 10/07/2025
        /// <param /name="transactionCode"/>Transaction code to lookup</param>

        public async Task<ActionResult> ReceiptPdf(string transactionCode)
        {
            try
            {
                if (string.IsNullOrEmpty(transactionCode))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Transaction code is required.");


                var dtTransaction = await objACC.GetReceiptDataAsyncSM(transactionCode);
                if (dtTransaction == null || dtTransaction.Rows.Count == 0)
                    return HttpNotFound("Transaction not found.");

                var row = dtTransaction.Rows[0];

                string maintenanceCode = row["EntityCode"]?.ToString();
                List<ReceiptItem> items = new List<ReceiptItem>();

                if (!string.IsNullOrEmpty(maintenanceCode))
                {
                    var dtItems = await objACC.GetMaintenanceItemsAsyncSM(maintenanceCode);
                    if (dtItems != null && dtItems.Rows.Count > 0)
                    {
                        foreach (DataRow itemRow in dtItems.Rows)
                        {
                            items.Add(new ReceiptItem
                            {
                                Description = itemRow["MaintenanceId"].ToString(),
                                Amount = Convert.ToDecimal(itemRow["Amount"]),
                                IsPenalty = false
                            });
                        }
                    }
                }

                if (!items.Any())
                {
                    items.Add(new ReceiptItem
                    {
                        Description = row["PaymentPurpose"].ToString(),
                        Amount = Convert.ToDecimal(row["Amount"]),
                        IsPenalty = false
                    });
                }

                var model = new ReceiptViewModel
                {
                    TransactionCode = row["TransactionCode"].ToString(),
                    PaidDate = Convert.ToDateTime(row["PaidDate"]).ToString("dd MMMM yyyy"),
                    PaymentBy = row["FullName"].ToString(),
                    PaidTo = "Housing Society",
                    Amount = Convert.ToDecimal(row["Amount"]),
                    AmountInWords = ConvertAmountToWords(Convert.ToDecimal(row["Amount"])),
                    PaymentPurpose = row["PaymentPurpose"].ToString(),
                    PaymentMode = Convert.ToInt32(row["PaymentMode"]) == 32 ? "Cash" : "Cheque",
                    Items = items
                };

                return View("ReceiptPdf", model);
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine("Error in ReceiptPdf: " + ex.Message);

                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "An error occurred while generating the receipt.");
            }
        }


        /// <summary>
        /// Generates and downloads a Receipt PDF for the given transaction code.
        /// Also saves the file to server's TransactionReceipts folder.
        /// </summary>
        /// 10/07/2025
        /// <param/* name="transactionCode"*/>Transaction code</param>

        public async Task<ActionResult> ReceiptPdfDownload(string transactionCode)
        {
            try
            {
                if (string.IsNullOrEmpty(transactionCode))
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Transaction code is required.");


                var dtTransaction = await objACC.GetReceiptDataAsyncSM(transactionCode);
                if (dtTransaction == null || dtTransaction.Rows.Count == 0)
                    return HttpNotFound("Transaction not found.");

                var row = dtTransaction.Rows[0];

                string maintenanceCode = row["EntityCode"]?.ToString();
                List<ReceiptItem> items = new List<ReceiptItem>();

                if (!string.IsNullOrEmpty(maintenanceCode))
                {
                    var dtItems = await objACC.GetMaintenanceItemsAsyncSM(maintenanceCode);
                    if (dtItems != null && dtItems.Rows.Count > 0)
                    {
                        foreach (DataRow itemRow in dtItems.Rows)
                        {
                            items.Add(new ReceiptItem
                            {
                                Description = itemRow["MaintenanceId"].ToString(),
                                Amount = Convert.ToDecimal(itemRow["Amount"]),
                                IsPenalty = false
                            });
                        }
                    }
                }

                if (!items.Any())
                {
                    items.Add(new ReceiptItem
                    {
                        Description = row["PaymentPurpose"].ToString(),
                        Amount = Convert.ToDecimal(row["Amount"]),
                        IsPenalty = false
                    });
                }

                var model = new ReceiptViewModel
                {
                    TransactionCode = row["TransactionCode"].ToString(),
                    PaidDate = Convert.ToDateTime(row["PaidDate"]).ToString("dd MMMM yyyy"),
                    PaymentBy = row["FullName"].ToString(),
                    PaidTo = "Housing Society",
                    Amount = Convert.ToDecimal(row["Amount"]),
                    AmountInWords = ConvertAmountToWords(Convert.ToDecimal(row["Amount"])),
                    PaymentPurpose = row["PaymentPurpose"].ToString(),
                    PaymentMode = Convert.ToInt32(row["PaymentMode"]) == 32 ? "Cash" : "Cheque",
                    Items = items
                };

                var pdf = new Rotativa.ViewAsPdf("ReceiptPdf", model)
                {
                    PageSize = Rotativa.Options.Size.A4,
                    PageMargins = new Rotativa.Options.Margins(5, 5, 5, 5),
                    CustomSwitches = "--print-media-type --disable-smart-shrinking --zoom 0.9 --no-pdf-compression",
                    MinimumFontSize = 10
                };

                byte[] pdfBytes = pdf.BuildFile(ControllerContext);

                string folderPath = Server.MapPath("~/Content/Documents/TransactionReceipts/");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = $"Receipt_{model.TransactionCode}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string fullFilePath = Path.Combine(folderPath, fileName);
                System.IO.File.WriteAllBytes(fullFilePath, pdfBytes);

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error generating PDF: " + ex.Message);

                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "An error occurred while generating the PDF receipt.");
            }

        }



        /// <summary>
        /// Generates a receipt PDF, saves it to disk, updates the DB with the file path,
        /// and returns the result as JSON.
        /// </summary>
        /// 10/07/2025
        /// <param /name="transactionCode"/>Transaction code to generate PDF for</param>


        [HttpPost]
        public async Task<JsonResult> GenerateReceiptPdf(
      string transactionCode,
      int monthsPending = 0,
      decimal penaltyAmount = 0,
      decimal finalAmount = 0)

        {
            try
            {
                if (string.IsNullOrEmpty(transactionCode))
                    return Json(new { success = false, message = "Transaction code is required." });

                // 1. Fetch transaction data
                var dtTransaction = await objACC.GetReceiptDataAsyncSM(transactionCode);
                if (dtTransaction == null || dtTransaction.Rows.Count == 0)
                    return Json(new { success = false, message = "Transaction not found." });

                var row = dtTransaction.Rows[0];

                // 2. Load maintenance items if any
                string maintenanceCode = row["EntityCode"]?.ToString();
                List<ReceiptItem> items = new List<ReceiptItem>();

                if (!string.IsNullOrEmpty(maintenanceCode))
                {
                    var dtItems = await objACC.GetMaintenanceItemsAsyncSM(maintenanceCode);
                    if (dtItems != null && dtItems.Rows.Count > 0)
                    {
                        foreach (DataRow itemRow in dtItems.Rows)
                        {
                            items.Add(new ReceiptItem
                            {
                                Description = itemRow["MaintenanceId"].ToString(),
                                Amount = Convert.ToDecimal(itemRow["Amount"]),
                                IsPenalty = false
                            });
                        }
                    }
                }

                if (!items.Any())
                {
                    items.Add(new ReceiptItem
                    {
                        Description = row["PaymentPurpose"].ToString(),
                        Amount = Convert.ToDecimal(row["Amount"]),
                        IsPenalty = false
                    });
                }

                var paymentModeValue = Convert.ToInt32(row["PaymentMode"]);
                string paymentMode = paymentModeValue == 32 ? "Cash" :
                                     paymentModeValue == 34 ? "Cheque" : null;

                var model = new ReceiptViewModel
                {
                    TransactionCode = row["TransactionCode"].ToString(),
                    FlatCode = row["FlatCode"].ToString(),
                    PaidDate = Convert.ToDateTime(row["PaidDate"]).ToString("dd MMMM yyyy hh:mm tt"),
                    PaymentBy = row["FullName"].ToString(),
                    PaidTo = "Housing Society",

                    // instead of only DB amount, use final calculation
                    Amount = finalAmount > 0 ? finalAmount : Convert.ToDecimal(row["Amount"]),
                    AmountInWords = ConvertAmountToWords(Convert.ToDecimal(row["Amount"])),

                    PaymentPurpose = row["PaymentPurpose"].ToString(),
                    PaymentMode = paymentMode,
                    Items = items,

                    // extra fields
                    MonthsPending = monthsPending,
                    PenaltyAmount = penaltyAmount
                };


                var pdf = new Rotativa.ViewAsPdf("ReceiptPdf", model)
                {
                    PageSize = Rotativa.Options.Size.A4,
                    PageMargins = new Rotativa.Options.Margins(5, 5, 5, 5),
                    CustomSwitches = "--print-media-type --disable-smart-shrinking --zoom 0.9 --no-pdf-compression",
                    MinimumFontSize = 10
                };

                byte[] pdfBytes = pdf.BuildFile(ControllerContext);

                string folderPath = Server.MapPath("~/Content/Documents/TransactionReceipts/");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = $"Receipt_{model.TransactionCode}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string relativePath = "~/Content/Documents/TransactionReceipts/" + fileName;
                string fullPath = Path.Combine(folderPath, fileName);
                System.IO.File.WriteAllBytes(fullPath, pdfBytes);

                // 3. Save PDF path to the database
                var acc = new AccountManager
                {
                    PaymentMode = "76",
                    TransactionCode = model.TransactionCode,
                    AttachmentPath = relativePath
                };

                string saveResult = await objACC.SaveReceiptpdfpathAsyncSM(acc);

                return Json(new
                {
                    success = true,
                    message = "PDF generated, saved, and path stored successfully.",
                    filePath = Url.Content(relativePath),
                    saveResult = saveResult
                });
            }
            catch (Exception ex)
            {

                Console.WriteLine("Error generating receipt PDF: " + ex.Message);

                return Json(new
                {
                    success = false,
                    message = "An error occurred while generating the receipt PDF.",
                    error = ex.Message
                });
            }
        }






        /// <summary>
        /// Populates dropdowns for transaction types, maintenance list, and members list based on current month/year.
        /// </summary>
        /// <param name="selectedTransactionId">Optional selected transaction ID for preselection.</param>

        private async Task PopulateTransactionList(int? selectedTransactionId = null)
        {
            try
            {
                // ✅ Fetch transaction types
                var dsTrans = await objACC.FetchTransactionTypeAsyncSM();
                ViewBag.TransactionList = new SelectList(dsTrans.Tables[0].AsEnumerable()
                    .Select(row => new SelectListItem
                    {
                        Value = row["SubTypeId"].ToString(),
                        Text = row["SubTypeName"].ToString()
                    }), "Value", "Text", selectedTransactionId);

                // ✅ Fetch maintenance types
                var dsMaintType = await objACC.FetchMaintainanceTypeSM();
                ViewBag.MaintenanceType = new SelectList(dsMaintType.Tables[0].AsEnumerable()
                    .Select(row => new SelectListItem
                    {
                        Value = row["SubTypeId"].ToString(),
                        Text = row["SubTypeName"].ToString()
                    }), "Value", "Text", selectedTransactionId);

                // ✅ Prepare request with current month/year
                var obju = new AccountManager
                {
                    Month = DateTime.Now.Month,
                    Year = DateTime.Now.Year
                };

                // ✅ Fetch maintenance records
                var ds1 = await objACC.FetchMaintenanceAsyncSM(obju);

                var maintenanceList = ds1.Tables[0].AsEnumerable()
                    .Select(row => new
                    {
                        Value = row["MaintananceCode"].ToString(),
                        Text = row["MaintenanceDisplayName"].ToString(),
                        PaymentPurpose = row["PaymentPurpose"].ToString()
                    }).ToList();

                ViewBag.MaintenanceList = maintenanceList;



                // ✅ If maintenance exists, preselect first and fetch its members
                if (maintenanceList.Any())
                {
                    obju.MaintenanceCode = maintenanceList[0].Value;

                    var ds2 = await objACC.FetchMaintenancebyMaintenanceAsyncSM(obju);
                    var memberList = ds2.Tables[0].AsEnumerable().Select(row => new SelectListItem
                    {
                        Value = row["MemberCode"].ToString(),
                        Text = row["FullName"].ToString()
                    }).ToList();

                    ViewBag.ReceiverList = new SelectList(memberList, "Value", "Text");
                }
                else
                {
                    ViewBag.ReceiverList = new List<SelectListItem>();
                }

                // ✅ Fetch cash summary
                var cashDs = await objACC.FetchCashAmountSM();
                if (cashDs != null && cashDs.Tables.Count > 0 && cashDs.Tables[0].Rows.Count > 0)
                {
                    var row = cashDs.Tables[0].Rows[0];
                    ViewBag.NetCashBalance = Convert.ToDecimal(row["CashInHand"]);
                }
                else
                {
                    ViewBag.TotalCredit = 0;
                    ViewBag.TotalDebit = 0;
                    ViewBag.NetCashBalance = 0;
                }


                // ✅ Fetch Bank List for Cheque
                var bankDs = await objACC.FetchBanksForChequeSM();
                var bankList = new List<SelectListItem>();

                if (bankDs != null && bankDs.Tables.Count > 0 && bankDs.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in bankDs.Tables[0].Rows)
                    {
                        var ifscCode = row["IFSCCode"]?.ToString();
                        var accountNo = row["AccountNo"]?.ToString();

                        var last4Digits = !string.IsNullOrEmpty(accountNo) && accountNo.Length >= 4
                            ? accountNo.Substring(accountNo.Length - 4)
                            : accountNo;

                        decimal openingBalance = 0;
                        if (row.Table.Columns.Contains("OpeningBalance") && row["OpeningBalance"] != DBNull.Value)
                        {
                            openingBalance = Convert.ToDecimal(row["OpeningBalance"]);
                        }

                        // ✅ SAFE IFSC API Call
                        var bankNameFromApi = !string.IsNullOrEmpty(ifscCode)
                            ? await GetBankNameFromIfsc(ifscCode)
                            : "";

                        string displayText = !string.IsNullOrEmpty(bankNameFromApi)
                            ? $"{bankNameFromApi} - {last4Digits}"   // ✅ NO AMOUNT VISIBLE IN DROPDOWN
                            : $"Unknown Bank - {last4Digits}";

                        bankList.Add(new SelectListItem
                        {
                            Value = row["BankCode"]?.ToString(),
                            Text = displayText
                        });
                    }
                }

                // ✅ Pass Bank Dropdown List (ALWAYS SAFE)
                ViewBag.BankList = bankList;

                // ✅ SAFE Opening Balance Dictionary (VERY IMPORTANT)
                if (bankDs != null && bankDs.Tables.Count > 0 && bankDs.Tables[0].Rows.Count > 0)
                {
                    ViewBag.BankBalances = bankDs.Tables[0].AsEnumerable()
                        .ToDictionary(
                            r => r["BankCode"]?.ToString(),
                            r => r["OpeningBalance"] != DBNull.Value ? Convert.ToDecimal(r["OpeningBalance"]) : 0
                        );
                }
                else
                {
                    // ✅ NEVER allow null — prevents JS crash
                    ViewBag.BankBalances = new Dictionary<string, decimal>();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in PopulateTransactionList: " + ex.Message);

                ViewBag.TransactionList = new List<SelectListItem>();
                ViewBag.MaintenanceList = new List<SelectListItem>();
                ViewBag.ReceiverList = new List<SelectListItem>();
                ViewBag.BankList = new List<SelectListItem>();

                // Set defaults for cash summary if error
                ViewBag.TotalCredit = 0;
                ViewBag.TotalDebit = 0;
                ViewBag.NetCashBalance = 0;
            }
        }





        /// <summary>
        /// Returns a list of receivers (members, workers, vendors, event handlers) 
        /// based on the given type.
        /// </summary>
        /// 03/07/2025
        /// <param /name="type"/>Receiver type (e.g., Member, Worker)</param>


        [HttpGet]
        public async Task<JsonResult> GetReceiversByType(string type)
        {
            try
            {
                DataSet ds = null;

                switch (type)
                {
                    case "Worker":
                        ds = await objACC.FetchWorkerAsyncSM();
                        break;
                    case "Vendor":
                        ds = await objACC.FetchVendorAsyncSM();
                        break;
                    case "EventHandler":
                        ds = await objACC.FetchEventHandlersAsyncSM();
                        break;
                }

                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    return Json(new { empty = true }, JsonRequestBehavior.AllowGet);
                }

                var receivers = ds.Tables[0].AsEnumerable().Select(row => new
                {
                    Value = row["ReceiverCode"].ToString(),
                    Text = row["ReceiverName"].ToString(),
                    EntityCode = row.Table.Columns.Contains("ExpenseCode") ? row["ExpenseCode"].ToString() :
                  row.Table.Columns.Contains("EventCode") ? row["EventCode"].ToString() : null,
                    Amount = row.Table.Columns.Contains("TotalAmount") ? row["TotalAmount"].ToString() : null,
                    PaymentPurpose = row.Table.Columns.Contains("PaymentPurpose") ? row["PaymentPurpose"].ToString() : null // <--- ADD केले
                }).ToList();


                return Json(receivers, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Fetches members associated with the selected maintenance code and also 
        /// retrieves total amount from the second table (if available).
        /// </summary>
        /// 05/07/2025
        /// <param /name="maintenanceCode"/>Selected maintenance code</param>


        [HttpGet]
        public async Task<JsonResult> GetMembersByMaintenance(string maintenanceCode)
        {
            try
            {
                var obju = new AccountManager { MaintenanceCode = maintenanceCode };
                var ds = await objACC.FetchMaintenancebyMaintenanceAsyncSM(obju);

                decimal totalAmount = 0;
                var memberList = new List<object>();

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    memberList = ds.Tables[0].AsEnumerable().Select(row =>
                    {
                        decimal pending = row["PendingAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["PendingAmount"]);
                        decimal amount = row["Amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Amount"]);
                        decimal penalty = row["PenaltyAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["PenaltyAmount"]);
                        //int months = row["MonthsPending"] == DBNull.Value ? 0 : Convert.ToInt32(row["MonthsPending"]);
                        int months = row.Table.Columns.Contains("MonthsPending") && row["MonthsPending"] != DBNull.Value
    ? Convert.ToInt32(row["MonthsPending"])
    : 0;


                        // total amount increment


                        return new
                        {
                            Value = row["MemberCode"].ToString(),
                            Text = row["FullName"].ToString(),
                            PendingAmount = pending,
                            PenaltyAmount = penalty,
                            MonthsPending = months,
                            Amount = amount
                        };
                    }).ToList<object>();
                }

                return Json(new { members = memberList, totalAmount }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Optional: Log the error here
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }







        /// <summary>
        /// Returns a list of maintenance entries for a given month and year.
        /// </summary>
        /// <param /name="month"/>Month value</param>
        /// 06/07/2025
        /// <param name="year">Year value</param>



        [HttpGet]
        public async Task<JsonResult> GetMaintenanceListByMonth(int month, int year)
        {
            try
            {
                // ✅ Pass both Month & Year to your business object
                var obj = new AccountManager
                {
                    Month = month,
                    Year = year   // ✅ Make sure AccountManager has Year property
                };

                var ds = await objACC.FetchMaintenanceAsyncSM(obj);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var list = ds.Tables[0].AsEnumerable().Select(row => new
                    {
                        Value = row["MaintananceCode"].ToString(),
                        Text = row["MaintenanceDisplayName"].ToString(),
                        PaymentPurpose = row["PaymentPurpose"].ToString()
                    }).ToList();

                    return Json(list, JsonRequestBehavior.AllowGet);
                }

                return Json(new List<SelectListItem>(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }




        /// <summary>
        /// Generates a salary slip PDF for a given worker.
        /// Fetches worker salary details and saves PDF to server.
        /// </summary>
        /// 13/07/2025
        /// <param /name="workerCode"/>Worker code to generate slip for</param>


        [HttpPost]
        public async Task<ActionResult> GenerateSalarySlip(string transactionCode)
        {
            try
            {
                DataTable dt = await objACC.GenerateSalarySlipSM(transactionCode); // This now expects a transactionCode

                if (dt == null || dt.Rows.Count == 0)
                {
                    return Json(new { success = false, message = "No salary data found for this transaction." });
                }

                DataRow row = dt.Rows[0];

                var salarySlipData = new Dictionary<string, string>
     {
         { "WorkerName", row["ReceiverName"]?.ToString() ?? "N/A" },
         { "Contact", row["Contact"]?.ToString() ?? "N/A" },
         { "Role", row["Role"]?.ToString() ?? "N/A" },
         { "Date", row["Date"]?.ToString() ?? "N/A" },
         { "AttendanceMonth", row["AttendanceMonth"]?.ToString() ?? "N/A" },
         { "DaysPresent", row["DaysPresent"]?.ToString() ?? "0" },
         { "BaseSalary", row["BaseSalary"]?.ToString() ?? "0" },
         { "TotalAmount", row["TotalAmount"]?.ToString() ?? "0" },
         { "PaymentModeName", row["PaymentMode"]?.ToString() ?? "N/A" },
         { "TransactionRef", row["TransactionRef"]?.ToString() ?? "-" }
     };

                string fileName = $"SalarySlip_{row["ReceiverCode"]}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string folderPath = Server.MapPath("~/Content/Documents/SalarySlips/");
                string fullPath = Path.Combine(folderPath, fileName);

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var pdfResult = new Rotativa.ViewAsPdf("SalarySlipView", salarySlipData)
                {
                    PageSize = Rotativa.Options.Size.A4,
                    PageOrientation = Rotativa.Options.Orientation.Portrait,
                    PageMargins = new Rotativa.Options.Margins(0, 0, 0, 0),
                    CustomSwitches = "--print-media-type --disable-smart-shrinking"
                };

                byte[] pdfBytes = pdfResult.BuildFile(ControllerContext);
                System.IO.File.WriteAllBytes(fullPath, pdfBytes);

                return Json(new
                {
                    success = true,
                    message = "Salary slip generated successfully.",
                    filePath = Url.Content("~/Content/Documents/SalarySlips/" + fileName)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }




        /// <summary>
        /// Utility class to convert numeric currency amounts into words (Indian format).
        /// </summary>
        /// <remarks>
        /// This class supports conversion of decimal amounts into their word equivalents,
        /// including Rupees and Paise, formatted according to the Indian numbering system
        /// (Crore, Lakh, Thousand, etc.).

        public static class NumberToWordsConverter
        {
            private static readonly string[] Units = {
    "", "One", "Two", "Three", "Four", "Five", "Six",
    "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve",
    "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen",
    "Eighteen", "Nineteen"
};

            private static readonly string[] Tens = {
    "", "", "Twenty", "Thirty", "Forty", "Fifty",
    "Sixty", "Seventy", "Eighty", "Ninety"
};

            public static string ConvertAmountToWords(decimal amount)
            {
                try
                {
                    if (amount == 0)
                        return "Zero Rupees Only";

                    long rupees = (long)Math.Floor(amount);
                    int paise = (int)((amount - rupees) * 100);

                    string rupeeWords = ConvertNumberToWords(rupees) + " Rupees";
                    string paiseWords = paise > 0 ? " and " + ConvertNumberToWords(paise) + " Paise" : "";

                    return rupeeWords + paiseWords + " Only";
                }
                catch (Exception ex)
                {
                    // Optionally log the exception or rethrow
                    return "Error converting amount to words: " + ex.Message;
                }
            }

            private static string ConvertNumberToWords(long number)
            {
                try
                {
                    if (number == 0)
                        return "Zero";

                    if (number < 0)
                        return "Minus " + ConvertNumberToWords(Math.Abs(number));

                    string words = "";

                    if ((number / 10000000) > 0)
                    {
                        words += ConvertNumberToWords(number / 10000000) + " Crore ";
                        number %= 10000000;
                    }

                    if ((number / 100000) > 0)
                    {
                        words += ConvertNumberToWords(number / 100000) + " Lakh ";
                        number %= 100000;
                    }

                    if ((number / 1000) > 0)
                    {
                        words += ConvertNumberToWords(number / 1000) + " Thousand ";
                        number %= 1000;
                    }

                    if ((number / 100) > 0)
                    {
                        words += ConvertNumberToWords(number / 100) + " Hundred ";
                        number %= 100;
                    }

                    if (number > 0)
                    {
                        if (words != "")
                            words += "and ";

                        if (number < 20)
                            words += Units[number];
                        else
                        {
                            words += Tens[number / 10];
                            if ((number % 10) > 0)
                                words += "-" + Units[number % 10];
                        }
                    }

                    return words.Trim();
                }
                catch (Exception ex)
                {
                    return "Error converting number to words: " + ex.Message;
                }
            }
        }



        private async Task<string> GetBankNameFromIfsc(string ifscCode)
        {
            if (string.IsNullOrWhiteSpace(ifscCode))
                return null;

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync("https://ifsc.razorpay.com/" + ifscCode);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                    return data?.BANK;
                }
            }
            return null;
        }

        #endregion


        #region********************************************************************* Worker Pay Manage ***********************************************************

        /// <summary>
        /// Asynchronously retrieves a list of worker account information from the business layer.
        /// Passes the worker list to the view for display; returns an empty list if an error occurs.
        /// Sets an error message in TempData if data loading fails.
        /// (Updated: 18-Aug-2025)
        /// </summary>

        public async Task<ActionResult> FetchingWorkersData()
        {
            try
            {
                BALAccountManager bal = new BALAccountManager();

                // Call the BAL method which returns List<AccountManager>
                List<AccountManager> workers = await bal.FetchWorkerInformationADAsync();

                return View(workers); // Pass the list to view
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Failed to load worker data: " + ex.Message;
                return View(new List<AccountManager>()); // Return empty list on error
            }
        }



        /// <summary>
        /// Processes a worker’s payment using UPI and records the transaction asynchronously via the business layer.
        /// Returns a JSON response indicating success or failure, with corresponding messages.
        /// Facilitates secure processing and recording of worker payments using UPI details.
        /// (Updated: 18-Aug-2025)
        /// </summary>


        [HttpPost]
        public async Task<ActionResult> ProcessPaymentUsingUPIAD(AccountManager model)
        {
            try
            {
                await new BALAccountManager().ProcessWorkerPaymentAsyncAD(model);

                return Json(new
                {
                    success = true,
                    message = " Payment successful! Worker payment processed & recorded."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"❌ Payment failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// Processes a worker's cash payment and saves the transaction asynchronously.
        /// Calls the business logic layer to record the payment details securely.
        /// Returns a JSON result indicating success or failure of the payment process.
        /// (Updated: 18-Aug-2025)
        /// </summary>


        public async Task<ActionResult> ProcessCashPaymentAD(AccountManager model)
        {
            try
            {
                await new BALAccountManager().SaveCashTransactionAsyncAD(model);

                return Json(new
                {
                    success = true,
                    message = "💸 Payment successful! Worker payment processed & recorded."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"❌ Payment failed: {ex.Message}" });
            }
        }




        /// <summary>
        /// Retrieves payment details for a worker based on worker code and attendance month asynchronously.
        /// Fetches data from the business layer and returns a partial view with the worker's details if found.
        /// Returns an empty content result if no payment details are available.
        /// (Updated: 18-Aug-2025)
        /// </summary>



        [HttpGet]
        public async Task<ActionResult> GetWorkerPaymentDetailsAD(string workerCode, string attendanceMonth)
        {
            BALAccountManager bal = new BALAccountManager();
            var dt = await bal.FetchWorkerPaymentDetailsAD(workerCode, attendanceMonth);

            if (dt != null && dt.Rows.Count > 0)
            {
                return PartialView("_ViewWorkerDetailsAD", dt.Rows[0]);
            }
            else
            {
                return Content("");
            }
        }


        /// <summary>
        /// Asynchronously fetches a single worker's payment data for a specified attendance month.
        /// Returns a partial view with the worker's payment details if found.
        /// Returns a message indicating no data if payment details are unavailable.
        /// (Updated: 18-Aug-2025)
        /// </summary>

        [HttpGet]
        public async Task<ActionResult> FetchSingleWorkerPaymentDataAD(string workerCode, string attendanceMonth)
        {
            BALAccountManager bal = new BALAccountManager();
            var dt = await bal.FetchSingleWorkerPaymentDataAD(workerCode, attendanceMonth);

            if (dt != null && dt.Rows.Count > 0)
            {
                var dr = dt.Rows[0]; // ✅ Extract single row here
                return PartialView("_WorkerPaymentFormAD", dr); // pass only single DataRow
            }
            else
            {
                return Content("😶 No payment data found for this worker.");
            }


        }

        /// <summary>
        /// Asynchronously retrieves a single worker's payment data for cash payment based on worker code and attendance month.
        /// Returns a partial view to display the worker’s cash payment details if available.
        /// Returns a message if no payment data is found for the specified worker.
        /// (Updated: 18-Aug-2025)
        /// </summary>


        [HttpGet]
        public async Task<ActionResult> FetchSingleWorkerForCashAD(string workerCode, string attendanceMonth)
        {
            BALAccountManager bal = new BALAccountManager();
            var dt = await bal.FetchSingleWorkerPaymentDataAD(workerCode, attendanceMonth);

            if (dt != null && dt.Rows.Count > 0)
            {
                var dr = dt.Rows[0]; // ✅ Extract single row here
                return PartialView("_WorkerPaymentByCash", dr); // pass only single DataRow
            }
            else
            {
                return Content("😶 No payment data found for this worker.");
            }


        }


        /// <summary>
        /// Asynchronously fetches the list of accountant bank details from the business logic layer.
        /// Returns the list as a JSON result for consumption by the client.
        /// Allows JSON responses via HTTP GET for ease of integration.
        /// (Updated: 18-Aug-2025)
        /// </summary>


        [HttpGet]
        public async Task<JsonResult> GetAccountantBankListAD()
        {
            BALAccountManager bal = new BALAccountManager();
            var list = await bal.FetchingAccountantBankDetailsADD();
            return Json(list, JsonRequestBehavior.AllowGet);
        }





        /// <summary>
        /// Asynchronously retrieves the current cash in hand amount from the business layer.
        /// Returns the cash amount as a JSON object, defaulting to "0.00" if no data is found.
        /// Allows JSON response via HTTP GET request for client consumption.
        /// (Updated: 18-Aug-2025)
        /// </summary>


        [HttpGet]
        public async Task<JsonResult> GetAmountCashinHandAD()
        {
            BALAccountManager bal = new BALAccountManager();
            var list = await bal.GetCashinHandAD();
            var cash = list.FirstOrDefault()?.SocietyCashAmount ?? "0.00";
            return Json(new { cash = cash }, JsonRequestBehavior.AllowGet);
        }



        /// <summary>
        /// Asynchronously fetches the opening balance for the specified bank code from the business layer.
        /// Returns the bank’s opening balance as plain text if found; otherwise, returns "0".
        /// Enables retrieval of bank balance details via HTTP GET for client use.
        /// (Updated: 18-Aug-2025)
        /// </summary
        [HttpGet]
        public async Task<ActionResult> GetBankBalance(string bankCode)
        {
            BALAccountManager bal = new BALAccountManager();
            var banks = await bal.FetchingAccountantBankDetailsADD();

            var selected = banks.FirstOrDefault(b => b.BankCode == bankCode);
            if (selected != null)
                return Content(selected.OpeningBalance.ToString());

            return Content("0");
        }

        #endregion


        #region********************************************************************* Maintaince Management ***********************************************************

        BALAccountManager bal = new BALAccountManager();
        /// <summary>
        /// This method is use for fetch list
        /// </summary>
        /// <returns> Return member list</returns>
        public async Task<ActionResult> MaintenanceManagementSY()
        {
            var data = await bal.GetMemberMaintenanceListSY();
            return View(data);
        }



        [HttpPost]
        public async Task<ActionResult> MemberMaintenanceDetailsSY(
     string memberCode,
     string maintainanceTypeId,
     string entityCode,
     string paidDate)   // ✅ new param from JS
        {
            DateTime? paidDateValue = null;

            if (!string.IsNullOrEmpty(paidDate))
            {
                if (DateTime.TryParse(paidDate, out DateTime parsedDate))
                {
                    paidDateValue = parsedDate;
                }
            }

            var list = await bal.GetMemberMaintenanceDetailsSY(
                memberCode,
                maintainanceTypeId,
                entityCode,
                paidDateValue
            );

            return PartialView("_MemberMaintenanceDetailsSY", list);
        }


        [HttpPost]
        public async Task<ActionResult> MemberPaymentDetailsSY(string memberCode, string maintainanceTypeId, string EntityCode)
        {
            var list = await bal.GetMemberPaymentDetailsSY(memberCode, maintainanceTypeId, EntityCode);
            return PartialView("MemberPaymentDetailsSY", list);
        }

        [HttpPost]
        public async Task<ActionResult> MemberCashPaymentDetailsSY(string memberCode, string maintainanceTypeId, string EntityCode)
        {
            var list = await bal.GetMemberPaymentDetailsSYJ(memberCode, maintainanceTypeId, EntityCode);
            return PartialView("_MemberCashPaymentDetailsSY", list);
        }
        [HttpPost]
        public async Task<ActionResult> YearlyMaintainanceDetailsSY(string memberCode, string maintainanceTypeId)
        {
            var list = await bal.GetYearlyMaintainenaceDetailsSY(memberCode, maintainanceTypeId);
            return PartialView("_MemberMaintenanceDetailsSY", list);
        }
        [HttpPost]
        public async Task<ActionResult> MemberMonthlyMaintenanceDetailsSY(
     string memberCode,
     string maintainanceTypeId,
     string entityCode,
     string paidDate)   // ✅ new param from JS
        {
            DateTime? paidDateValue = null;

            if (!string.IsNullOrEmpty(paidDate))
            {
                if (DateTime.TryParse(paidDate, out DateTime parsedDate))
                {
                    paidDateValue = parsedDate;
                }
            }

            var list = await bal.GetMemberMonthlyMaintenanceDetailsSY(
                memberCode,
                maintainanceTypeId,
                entityCode,
                paidDateValue
            );



            return PartialView("_MemberMaintenanceDetailsSY", list);




        }
        [HttpPost]
        public async Task<JsonResult> ConfirmPayment(string MemberCode, string EntityCode, decimal Amount,
                                  string TransactionId, string BankCode, int maintananceTypeId)
        {
            var bal = new BALAccountManager();
            string currentUser = User.Identity?.Name ?? "Admin";

            // ✅ Read society code from session
            string societyCode = (Session["SocietyCode"] ?? "SC001").ToString();

            bool result = await bal.SaveMaintainancePayment(MemberCode, EntityCode, Amount, TransactionId, BankCode, currentUser, maintananceTypeId, societyCode);

            if (result)
                return Json(new { success = true });
            else
                return Json(new { success = false, message = "Failed to save payment." });
        }


        [HttpPost]
        public async Task<JsonResult> SaveCashPayment(string MemberCode, string EntityCode, decimal Amount, int maintananceTypeId)
        {
            try
            {
                var bal = new BALAccountManager();
                string currentUser = User.Identity?.Name ?? "Admin";

                // ✅ Get StaffCode from session
                string staffCode = (Session["StaffCode"] ?? "STF001").ToString();

                bool result = await bal.SaveCashMaintainancePayment(MemberCode, EntityCode, Amount, currentUser, maintananceTypeId, staffCode);

                if (result)
                    return Json(new { success = true });
                else
                    return Json(new { success = false, message = "Failed to save cash payment." });
            }
            catch (Exception ex)
            {
                // Log the exception
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<JsonResult> GetBanksByAllCodeSY(string allCode)
        {
            BALAccountManager bal = new BALAccountManager();
            var result = await bal.GetBanksByAllCodeSY(allCode);

            return Json(result.Select(b => new
            {
                IFSC = b.IFSCCode,
                BankCode = b.BankCode,
                AccountNo = b.AccountNo,
                SubTypeName = b.SubTypeName  // Return SubTypeName
            }), JsonRequestBehavior.AllowGet);
        }
        #endregion



        #region********************************************************************* Expence***********************************************************
        /// <summary>
        /// Fetches all expenses and displays them on the Index view.
        /// Purpose: Lists all expenses with basic details for the user.
        /// Date: 03-July-2025
        /// </summary>
        public async Task<ActionResult> ExpenseList()
        {
            BALAccountManager bal = new BALAccountManager();
            List<AccountManager> expenses = await bal.GetAllExpensesPS();
            return View(expenses);
        }



        /// <summary>
        /// Saves payment details such as Payment ID and detected Razorpay method (UPI, Netbanking, etc.).
        /// Purpose: Invoked via AJAX after successful Razorpay payment to persist data.
        /// Date: 03-July-2025
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> SavePaymentPS(
     string expenseCode,
     string paymentId,
     string selectedBankCode,
     int paymentModeId = 0,
     decimal amount = 0)
        {
            try
            {
                int finalPaymentMode = GetPaymentModeFromRazorpayPS(paymentId);
                if (finalPaymentMode == 0)
                    finalPaymentMode = paymentModeId;

                // ✅ Get society/staff from Session
                string societyCode = Session["SocietyCode"]?.ToString();
                string staffCode = Session["StaffCode"]?.ToString();

                if (string.IsNullOrEmpty(societyCode) || string.IsNullOrEmpty(staffCode))
                {
                    return Json(new { success = false, error = "Session expired. Please log in again." });
                }

                BALAccountManager bal = new BALAccountManager();
                bool result = await bal.SaveTransactionPS(
                    expenseCode,
                    paymentId,
                    finalPaymentMode,
                    selectedBankCode,
                    amount,
                    societyCode,
                    staffCode
                );

                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }


        [HttpPost]
        public async Task<JsonResult> SaveCashTransactionPS(FormCollection form)
        {
            try
            {
                string expenseCode = form["expenseCode"];
                decimal amount = Convert.ToDecimal(form["amount"], CultureInfo.InvariantCulture);

                // ✅ Read from Session
                string societyCode = Session["SocietyCode"]?.ToString();
                string staffCode = Session["StaffCode"]?.ToString();

                if (string.IsNullOrEmpty(societyCode) || string.IsNullOrEmpty(staffCode))
                {
                    return Json(new { success = false, error = "Session expired. Please log in again." });
                }

                BALAccountManager bal = new BALAccountManager();
                bool success = await bal.SaveCashTransactionPS(expenseCode, amount, societyCode, staffCode);

                return Json(new { success });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }




        /// <summary>
        /// Contacts Razorpay API to determine the payment method (UPI, NetBanking, etc.) for a given Payment ID.
        /// Purpose: Used internally to determine the mode of payment made through Razorpay.
        /// Date: 03-July-2025
        /// </summary>
        private int GetPaymentModeFromRazorpayPS(string paymentId)
        {
            try
            {
                var key = "rzp_test_tnu8pNChRc5VBE";
                var secret = "wVSn4S0P2BpbvIiol3zLUzLG";

                var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{key}:{secret}"));
                var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create($"https://api.razorpay.com/v1/payments/{paymentId}");
                request.Headers["Authorization"] = "Basic " + credentials;
                request.Method = "GET";

                using (var response = (System.Net.HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new System.IO.StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    var json = Newtonsoft.Json.Linq.JObject.Parse(result);
                    string method = json["method"]?.ToString();

                    if (method == "upi") return 33;
                    if (method == "netbanking") return 35;
                }
            }
            catch (Exception)
            {
                // Log error here
            }

            return 0; // unknown or failed to fetch
        }



        /// <summary>
        /// Retrieves full details for a specific expense and returns a partial view (modal) to display them.
        /// Purpose: Opens detailed expense info in a modal for user review.
        /// Date: 03-July-2025
        /// </summary>
        /// 

        [HttpGet]
        public async Task<PartialViewResult> GetExpenseDetailsPS(string expenseCode)
        {
            var model = await new BALAccountManager().GetFullExpenseAndTransactionsPS(expenseCode);

            // Attachments वेगळ्या query ने fetch कर
            var docs = await new BALAccountManager().GetExpenseWithDocumentsPS(expenseCode);
            model.Attachments = docs.Attachments;

            return PartialView("_ExpenseDetailsModal", model);
        }


        /// <summary>
        /// Retrieves IFSC-related information (vendor, IFSC code, etc.) for a given expense.
        /// Purpose: Opens a modal to view or use bank details related to the vendor.
        /// Date: 03-July-2025
        /// </summary>
        /// 

        [HttpGet]
        public async Task<PartialViewResult> GetIFSCDetailsPS(string expenseCode)
        {
            BALAccountManager bal = new BALAccountManager();
            AccountManager data = await bal.GetIFSCByCodePS(expenseCode); // fetches IFSC
            return PartialView("_IFSCModal", data); // loads modal
        }

        /// <summary>
        /// Purpose: Get Socity banks and branch name using IFSC API
        /// Date: 14-July-2025
        /// </summary>
        /// 
        [HttpGet]
        public async Task<JsonResult> GetBanksByAllCodePS(string allCode)
        {
            BALAccountManager bal = new BALAccountManager();
            var result = await bal.GetBanksByAllCodePS(allCode);
            return Json(result.Select(b => new
            {
                IFSC = b.IFSCCode,
                BankCode = b.BankCode,
                AccountNo = b.AccountNo,
                SubTypeName = b.SubTypeName
            }), JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// Purpose: Get Opening Balance according to Bank name
        /// Date: 14-July-2025
        /// </summary>
        /// 
        [HttpGet]
        public async Task<JsonResult> GetOpeningBalancePS(string accountNo)
        {
            BALAccountManager bal = new BALAccountManager();
            var balance = await bal.GetOpeningBalanceByAccountNoPS(accountNo);
            if (balance.HasValue)
                return Json(new { success = true, balance = balance.Value }, JsonRequestBehavior.AllowGet);
            else
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Purpose: open cash transaction modal , to do the cash payment
        /// Date: 15-July-2025
        /// </summary>
        [HttpGet]
        public async Task<PartialViewResult> _PayByCashExpensePS(string expenseCode)
        {
            var bal = new BALAccountManager();
            var model = await bal.GetIFSCByCodePS(expenseCode);
            if (model == null)
                return PartialView("_ErrorPartial");

            return PartialView("_PayByCashExpensePS", model);
        }

        /// <summary>
        /// Displays the Register Vendor form as a Bootstrap modal.
        /// Purpose: Used to open the vendor registration form and populate Service Type dropdown.
        /// Date: 10-July-2025
        /// </summary>

        [HttpGet]
        public async Task<ActionResult> RegisterVendor()
        {
            var bal = new BALAccountManager();
            ViewBag.ServiceSubTypes = await bal.GetServiceSubTypesDD();
            return PartialView("_RegisterVendor", new AccountManager());
        }




        /// <summary>
        /// Submits the Register Vendor form data to insert vendor into the database.
        /// Purpose: Saves vendor details along with uploaded documents.
        /// Date: 10-July-2025
        /// </summary>



        [HttpPost]

        public async Task<ActionResult> RegisterVendor(AccountManager model)
        {
            try
            {
                var doc1 = Request.Files["Document1File"];
                var doc2 = Request.Files["Document2File"];

                if (doc1 != null && doc1.ContentLength > 0)
                {
                    var name1 = Path.GetFileName(doc1.FileName);
                    var path1 = Path.Combine(Server.MapPath("~/Content/Documents/"), name1);
                    doc1.SaveAs(path1);
                    model.Document1 = "~/Content/Documents/" + name1;
                }
                if (doc2 != null && doc2.ContentLength > 0)
                {
                    var name2 = Path.GetFileName(doc2.FileName);
                    var path2 = Path.Combine(Server.MapPath("~/Content/Documents/"), name2);
                    doc2.SaveAs(path2);
                    model.Document2 = "~/Content/Documents/" + name2;
                }

                bool result = await new BALAccountManager().RegisterVendorDD(model);
                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }



        /// <summary>
        /// Loads the Add Expense form with all dropdowns populated.
        /// Purpose: Opens the Add Expense modal view with required data.
        /// Date: 03-July-2025
        /// </summary>



        [HttpGet]
        public async Task<PartialViewResult> AddExpensePage()
        {
            ViewBag.ExpenseTypeList = GetExpenseTypeList();
            ViewBag.VendorTypeList = await bal.GetVendorTypeAsyncDD();
            ViewBag.VendorList = await GetVendorListAsync();
            ViewBag.WingList = await GetWingListAsync();
            ViewBag.VendorType = await GetVendorTypeListAsync();
            // ViewBag.GSTList = await GetGSTTypeListAsync();
            ViewBag.GSTList = await GetGSTTypeListAsync();


            return PartialView("_AddExpensePage", new AccountManager());
        }



        [HttpGet]
        public ActionResult OpenGSTModal()
        {
            //  var model = new GSTModel(); // Empty model
            return PartialView("_AddGSTPartial", new AccountManager());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddGSTType(AccountManager model)
        {
            if (ModelState.IsValid)
            {
                BALAccountManager bal = new BALAccountManager();
                bool isSaved = await bal.InsertGSTTypeDD(model);

                if (isSaved)
                {
                    return Json(new { success = true });
                }
            }

            // If failed or model invalid, return modal again with errors
            return PartialView("_AddGSTPartial", model);
        }


        /// <summary>
        /// Saves new expense entry from form submission.
        /// Purpose: Inserts expense data including uploaded file and returns to list page.
        /// Date: 03-July-2025
        /// </summary>



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateExpense(AccountManager model, List<HttpPostedFileBase> PostedFile, string[] SelectedGSTTypeIds)
        {
            model.AddedBy = "STF002";
            model.Date = model.Date < new DateTime(1753, 1, 1) ? DateTime.Now.Date : model.Date;

            List<int> gstIds = SelectedGSTTypeIds?.Select(int.Parse).ToList() ?? new List<int>();

            // Set CGST, SGST, IGST separately from selected list
            model.CGSTTypeId = gstIds.Count > 0 ? new List<int> { gstIds[0] } : new List<int>();
            model.SGSTTypeId = gstIds.Count > 1 ? new List<int> { gstIds[1] } : new List<int>();
            model.IGSTTypeId = gstIds.Count > 2 ? new List<int> { gstIds[2] } : new List<int>();



            List<string> documentPaths = new List<string>();
            if (PostedFile != null && PostedFile.Count > 0)
            {
                string folderPath = Server.MapPath("~/Content/Documents/");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                foreach (var file in PostedFile)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        string fileName = Path.GetFileName(file.FileName);
                        string filePath = Path.Combine(folderPath, fileName);
                        file.SaveAs(filePath);
                        documentPaths.Add("/Content/Documents/" + fileName);
                    }
                }
            }

            model.DocumentList = documentPaths;

            if (ModelState.IsValid)
            {
                string expenseCode = await bal.InsertExpenseAsyncDD(model);

                if (!string.IsNullOrEmpty(expenseCode))
                {
                    if (Request.IsAjaxRequest())
                        return Json(new { success = true, message = "Expense inserted", code = expenseCode });

                    TempData["Success"] = "Expense inserted: " + expenseCode;
                    return RedirectToAction("ExpenseList");
                }

                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Failed to insert expense" });

                TempData["Error"] = "Failed to insert expense.";
            }

            // Re-bind dropdowns
            ViewBag.ExpenseTypeList = GetExpenseTypeList();
            ViewBag.VendorType = await GetVendorTypeListAsync();
            ViewBag.VendorList = await GetVendorListAsync();
            ViewBag.WingList = await GetWingListAsync();
            ViewBag.GSTList = await GetGSTTypeListAsync();

            if (Request.IsAjaxRequest())
                return Json(new { success = false, message = "Model is invalid", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            return View("_AddExpensePage", model);
        }











        public List<SelectListItem> GetExpenseTypeList()
        {
            return new List<SelectListItem>
{
 new SelectListItem { Text = "Direct", Value = "36" },
 new SelectListItem { Text = "Indirect", Value = "37" }
};
        }

        public async Task<JsonResult> GetVendorListAsync()
        {
            var vendorList = await bal.GetVendorNamesAsync();

            var list = vendorList.Select(v => new
            {
                Value = v.VendorCode,
                Text = $"{v.VendorName} - {v.BusinessName} - {v.ServiceType}"
            }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }





        [HttpPost]

        public async Task<JsonResult> GetVendorsByType(int vendorType)
        {
            var vendorList = await bal.GetVendorsByTypeAsyncDD(vendorType);
            return Json(vendorList);
        }




        public async Task<List<SelectListItem>> GetVendorTypeListAsync()
        {
            var vendorTypes = await bal.GetVendorTypeAsyncDD();
            return vendorTypes.Select(g => new SelectListItem
            {
                Value = g.SubTypeId.ToString(),
                Text = g.SubTypeName
            }).ToList();
        }




        /// <summary>
        /// Purpose: Get Wing name 
        /// Date: 14-July-2025
        /// </summary>
        /// 
        public async Task<List<SelectListItem>> GetWingListAsync()
        {
            var wingList = await bal.GetWingNamesAsyncDD();
            return wingList.Select(w => new SelectListItem { Value = w.WingId.ToString(), Text = w.WingName }).ToList();
        }
        [HttpGet]
        public async Task<List<SelectListItem>> GetGSTTypeListAsync()
        {
            var gstList = await bal.GetGSTTypeAsyncDD();
            return gstList.Select(g => new SelectListItem
            {
                Value = g.GSTTypeId.ToString(),
                Text = g.GSTTypeName
            }).ToList();
        }



        [HttpGet]
        public async Task<ActionResult> GetExpenseListPartial()
        {
            BALAccountManager bal = new BALAccountManager();
            var expenseList = await bal.GetAllExpensesPS();
            return PartialView("_AllExpenseListPartial", expenseList);
        }







        #endregion



        #region********************************************************************* Event Management ***********************************************************

        //*Savita Salunke  ***********************************************************************************************************************************************************

        /// <summary>
        /// Retrieves all event-related data including budget info and passes it to the EventList view.Date 04-07-2025
        /// </summary>


        public async Task<ActionResult> EventList()
        {
            try
            {
                BALAccountManager bal = new BALAccountManager();
                var eventList = await bal.GetAllEventListSS();

                if (eventList == null || !eventList.Any())
                {
                    Console.WriteLine("⚠ No events found in EventList()");
                }
                else
                {
                    Console.WriteLine("✅ Found " + eventList.Count + " events.");
                }

                return View(eventList);
            }
            catch (Exception ex)
            {

                Console.WriteLine("❌ Error in EventList(): " + ex.Message);
                TempData["Error"] = "An error occurred while loading the events.";
                return RedirectToAction("Error", "Home");
            }
        }

        //public async Task<JsonResult> GetEventListAjax()
        //{
        //    try
        //    {
        //        BALAccountManager bal = new BALAccountManager();
        //        var eventList = await bal.GetAllEventListSS();

        //        return Json(new { data = eventList }, JsonRequestBehavior.AllowGet); // ✅ DataTables expects this format
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { data = new List<object>(), error = ex.Message }, JsonRequestBehavior.AllowGet);
        //    }
        //}


        /// <summary>
        /// Retrieves event details by event ID and returns the ApproveBudget view for budget approval.Date 06-07-2025
        /// </summary>


        [HttpGet]
        public async Task<ActionResult> ApproveBudget(int id)
        {
            try
            {
                var bal = new BALAccountManager();
                var eventData = await bal.GetEventByIdAsyncSS(id);

                if (eventData == null)
                {
                    return HttpNotFound();
                }

                return View(eventData);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error: " + ex.Message;
                return View();
            }
        }

        /// <summary>
        /// Sends a budget approval email for the specified event and updates the request status if the email is sent successfully.Date 05-07-2025
        /// </summary>


        [HttpPost]
        public async Task<ActionResult> SendRequestAjax(int id)
        {
            try
            {
                var bal = new BALAccountManager();
                var helper = new EmailHelper();

                // 1. Event Data fetch
                var eventData = await bal.GetEventByIdAsyncSS(id);
                if (eventData == null)
                    return Json(new { success = false, message = "Event not found" });

                // 2. Accountant Details
                var accountant = await bal.GetAccountantDetailsSS();

                // 3. Prepare Email
                string subject = $"Budget Approval Request For Event – {eventData.EventName}";
                string body = helper.GetBudgetApprovalMessage(
                    eventName: eventData.EventName ?? "N/A",

                    createdDate: eventData.CreatedDate.ToString("dd-MM-yyyy"),

                    eventHandlerName: eventData.EventHandlerName ?? "N/A",
                    wingName: eventData.WingName ?? "N/A",
                    allocatedBudget: eventData.AllocatedBudget.ToString(),
                    fromEmailAddress: accountant.FromEmailAddress,
                    contactNumber: accountant.ContactNumber
                );

                // 4. Send Email
                string resultMessage = await helper.SendEmailHelperVM(
                    toEmail: "yash05958@gmail.com", // ✅ Admin email
                    subject: subject,
                    body: body,
                    attachments: null
                );

                // 5. Update status only if email was successful
                if (resultMessage.StartsWith("✅"))
                {
                    bool statusUpdated = await bal.SendRequestAsyncSS(id);
                    if (statusUpdated)
                        return Json(new { success = true, message = "Email Send Successfully" });
                    else
                        return Json(new { success = false, message = "❌ Email sent, but status not updated." });
                }
                else
                {
                    return Json(new { success = false, message = resultMessage });
                }
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("TRACE: " + ex.StackTrace);
                return Json(new { success = false, message = "❌ ERROR: " + ex.Message });
            }

        }










       





        //*********************************************************** Pradnya Mane  **************************************************************************************************
        /// <summary>
        /// Deletes an event budget from the system based on the provided budget code.
        /// </summary>


        [HttpPost]
        public async Task<JsonResult> DeleteBudget(string eBudgetCode)
        {
            if (string.IsNullOrWhiteSpace(eBudgetCode))
                return Json(new { success = false, message = "Invalid budget code." });

            try
            {
                // Call the BAL method that triggers the stored procedure
                var isDeleted = await objDashbaord.DeleteEventByBudgetCodeAsyncSS(eBudgetCode);

                if (isDeleted)
                    return Json(new { success = true });
                else
                    return Json(new { success = false, message = "Cannot delete. Either already requested or not found." });
            }
            catch (Exception ex)
            {
                // You can log the error here
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        /// <summary>
        /// Loads the Create Budget form with a list of approved events and the current opening balance.
        /// Retrieves data asynchronously and returns a partial view for rendering.
        /// </summary>


        [HttpGet]
        public async Task<ActionResult> CreateBudgetMD()
        {
            try
            {
                BALAccountManager obj = new BALAccountManager();
                var approvedEvents = await obj.GetApprovedEventsAsyncMD();

                // Fetch opening balance
                decimal openingBalance = await obj.GetOpeningBalanceAsyncMD();

                ViewBag.OpeningBalance = openingBalance.ToString("N2"); // formatted with comma separator

                return PartialView("_CreateBudget", approvedEvents);
            }
            catch (Exception ex)
            {
                // Optional: log exception
                Console.WriteLine("Error in CreateBudget: " + ex.Message);
                return new HttpStatusCodeResult(500, "An error occurred while loading the budget form.");
            }
        }



        /// <summary>
        /// Submits the allocated budget for a specific event via the business logic layer.
        /// Saves the budget allocation, retrieves the updated list of approved events,
        /// and returns the result as JSON.
        /// Intended to be called via AJAX POST.
        /// </summary>



        //[HttpPost]
        //public async Task<JsonResult> SubmitBudget(string EventName, decimal AllocatedBudget, string CategoryJSON)
        //{
        //    try
        //    {
        //        var categories = new List<(string, decimal)>();
        //        if (!string.IsNullOrEmpty(CategoryJSON))
        //        {
        //            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
        //            var list = serializer.Deserialize<List<AccountManager>>(CategoryJSON);

        //            categories = list.Select(c => (c.Name, c.Budget)).ToList();
        //        }

        //        BALAccountManager bal = new BALAccountManager();
        //        bool isSaved = await bal.SubmitBudgetWithDistributionAsync(EventName, AllocatedBudget, categories);

        //        var approvedEvents = await bal.GetApprovedEventsAsyncMD();
        //        var latestEvent = approvedEvents.FirstOrDefault(e => e.EventName == EventName);

        //        return Json(new
        //        {
        //            success = isSaved,
        //            events = approvedEvents,
        //            latestEvent,
        //            message = isSaved ? "Budget & Categories saved successfully!" : "Failed to save budget."
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Error: " + ex.Message });
        //    }
        //}


        [HttpPost]
        public async Task<JsonResult> SubmitBudget(string EventName, decimal? AllocatedBudget, string CategoryJSON)
        {
            try
            {
                if (!AllocatedBudget.HasValue)
                {
                    return Json(new { success = false });
                }
                var categories = new List<(string, decimal)>();
                if (!string.IsNullOrEmpty(CategoryJSON))
                {
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    var list = serializer.Deserialize<List<BudgetCategory>>(CategoryJSON);
                    categories = list.Select(c => (c.Name, c.Budget)).ToList();
                }
                BALAccountManager bal = new BALAccountManager();
                bool isSaved = await bal.SubmitBudgetWithDistributionAsync(EventName, AllocatedBudget.Value, categories);

                return Json(new
                {
                    success = isSaved
                });
            }
            catch (Exception )
            {
                return Json(new { success = false });
            }
        }



        // Add this class to your AccountManagerController.cs file

        /// <summary>
        /// Retrieves details for a specific event by its name from the list of approved events.
        /// Intended to be called via AJAX POST.
        /// </summary>



        [HttpPost]
        public async Task<JsonResult> GetEventDetails(string eventName)
        {
            try
            {
                BALAccountManager obj = new BALAccountManager();
                var approvedEvents = await obj.GetApprovedEventsAsyncMD();
                var selectedEvent = approvedEvents.FirstOrDefault(e => e.EventName == eventName);

                if (selectedEvent != null)
                {
                    return Json(new
                    {
                        selectedEvent.EventHandlerName,
                        CreatedDateString = selectedEvent.CreatedDate != DateTime.MinValue
    ? selectedEvent.CreatedDate.ToString("yyyy-MM-dd")
    : "",

                    selectedEvent.PhoneNumber
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(null, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Optional: log the exception
                Console.WriteLine("Error in GetEventDetails: " + ex.Message);
                return Json(null, JsonRequestBehavior.AllowGet);
            }
        }



        /// <summary>
        /// Retrieves the details of a specific event by its unique code
        /// and returns a partial view for updating that event.
        /// </summary>


        public async Task<ActionResult> UpdateDetailsMD(string eventCode)
        {
            try
            {
                if (string.IsNullOrEmpty(eventCode))
                {
                    return new HttpStatusCodeResult(400, "Event code is required");
                }
                BALAccountManager bal = new BALAccountManager();
                var eventDetails = await bal.GetEventDetailsByCodeAsyncMD(eventCode);
                // Ensure non-null distribution list to prevent errors in the view
                if (eventDetails == null)
                {
                    eventDetails = new AccountManager
                    {
                        EventCode = eventCode,
                        EventDistributions = new List<AccountManager>()
                    };
                }
                else if (eventDetails.EventDistributions == null)
                {
                    eventDetails.EventDistributions = new List<AccountManager>();
                }
                return PartialView("_UpdateDetails", eventDetails);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in UpdateDetails: " + ex.Message);
                return new HttpStatusCodeResult(500, "An error occurred while loading update details.");
            }
        }

        // POST: Handle updates including multiple file uploads per distribution
        [HttpPost]
        public async Task<JsonResult> UpdateActualCost(
            string eventCode,
            List<string> eventDCodes,
            List<decimal> actualCosts)
        {
            try
            {
                if (string.IsNullOrEmpty(eventCode) || eventDCodes == null || actualCosts == null)
                {
                    return Json(new { success = false, message = "Invalid input parameters." });
                }

                BALAccountManager bal = new BALAccountManager();
                DateTime now = DateTime.Now;
                string folderPath = Server.MapPath("~/Content/Documents/Bill/");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Process each event distribution
                for (int i = 0; i < eventDCodes.Count; i++)
                {
                    string dCode = eventDCodes[i];
                    decimal cost = actualCosts[i];
                    List<string> documentPaths = new List<string>();
                    var files = Request.Files;

                    for (int j = 0; j < files.Count; j++)
                    {
                        string fileKey = files.AllKeys[j];
                        if (fileKey == $"postedFiles[{i}]")
                        {
                            HttpPostedFileBase file = files[j];
                            if (file != null && file.ContentLength > 0)
                            {
                                string uniqueFileName = file.FileName; // e.g. "All Expenses (1).pdf"
                                string fullPath = Path.Combine(folderPath, uniqueFileName);
                                file.SaveAs(fullPath);

                                // Save full relative path including file name
                                string documentPath = $"~/Content/Documents/Bill/{uniqueFileName}";

                                documentPaths.Add(documentPath);
                            }
                        }
                    }

                    foreach (var documentPath in documentPaths)
                    {
                        await bal.UpdateActualCostAsyncMD(eventCode, dCode, cost, documentPath, now);
                    }

                    if (documentPaths.Count == 0)
                    {
                        await bal.UpdateActualCostAsyncMD(eventCode, dCode, cost, null, now);
                    }
                }


                return Json(new { success = true, message = "Actual cost and documents updated successfully." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateActualCost: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }





        /// <summary>
        /// Retrieves the budget details of a specific event by its unique event code
        /// and returns them in a partial view for display.
        /// </summary>

        [HttpGet]
        public async Task<ActionResult> ViewBudget(string eventCode)
        {
            try
            {
                if (string.IsNullOrEmpty(eventCode))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Event code is required");
                }

                BALAccountManager bal = new BALAccountManager();
                var eventDetails = await bal.GetEventDetailsForViewAsyncMD(eventCode);

                if (eventDetails == null)
                {
                    return HttpNotFound("Event not found");
                }

                return PartialView("_ViewBudget", eventDetails);
            }
            catch (Exception ex)
            {
                // Optional: log exception here to avoid warning CS0168
                System.Diagnostics.Debug.WriteLine("Error in ViewBudget: " + ex.Message);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "An error occurred while fetching budget details.");
            }
        }






        // ************************************************************Shubham Vaidya****************************************************************************


        // Shubham Vaidya

        /// <summary>
        /// Doing the payment and saving it in transssaction table
        /// </summary>


        [HttpPost]
        public async Task<JsonResult> SavePaymentSV(AccountManager model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.EventCode) || string.IsNullOrEmpty(model.TransactionId_ChequeId))
                    return Json(new { success = false, message = "Missing required fields." });

                int finalPaymentMode = GetPaymentModeFromRazorpaySV(model.PaymentId);
                if (finalPaymentMode == 0)
                    finalPaymentMode = model.PaymentModeId;

                // ✅ Get society/staff from Session
                string societyCode = Session["SocietyCode"]?.ToString();
                string staffCode = Session["StaffCode"]?.ToString();

                if (string.IsNullOrEmpty(societyCode) || string.IsNullOrEmpty(staffCode))
                {
                    return Json(new { success = false, error = "Session expired. Please log in again." });
                }



                BALAccountManager bal = new BALAccountManager();

                // Pass BankCode to save
                bool isSaved = await bal.SavePaymentSVTransactionSV(
                    model.EventCode,
                    model.TransactionId_ChequeId,
                    finalPaymentMode,
                    model.SelectedBankCode,
                     societyCode,
                    staffCode
                // ✅ Pass this
                );

                return Json(new { success = isSaved, message = isSaved ? "Saved" : "SP returned false" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Exception: " + ex.Message });
            }
        }



        /// <summary>
        /// Forsaving the payment mode wether it is upi or net banking
        /// </summary>

        private int GetPaymentModeFromRazorpaySV(string paymentId)
        {
            try
            {
                var key = "rzp_test_tnu8pNChRc5VBE";
                var secret = "wVSn4S0P2BpbvIiol3zLUzLG";

                var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{key}:{secret}"));
                var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create($"https://api.razorpay.com/v1/payments/{paymentId}");
                request.Headers["Authorization"] = "Basic " + credentials;
                request.Method = "GET";

                using (var response = (System.Net.HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new System.IO.StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    var json = Newtonsoft.Json.Linq.JObject.Parse(result);
                    string method = json["method"]?.ToString();

                    if (method == "upi") return 33;
                    if (method == "netbanking") return 35;
                }
            }
            catch (Exception)
            {
                // Log error here
            }

            return 0; // unknown or failed to fetch
        }



        /// <summary>
        /// Opening razorpay page for payments
        /// </summary>

        public async Task<ActionResult> LoadPaymentPartialSV(string eventCode)
        {
            if (string.IsNullOrEmpty(eventCode))
                return new HttpStatusCodeResult(400, "Missing event code");

            BALAccountManager bal = new BALAccountManager();
            var events = await bal.GetAllEventListSS();

            var selectedEvent = events.FirstOrDefault(e => e.EventCode == eventCode);

            if (selectedEvent == null)
                return HttpNotFound("Event not found");

            return PartialView("_PaymentPartial", selectedEvent);
        }



        /// <summary>
        /// Controller method to get the banks names in the dropdown
        /// </summary>

        public async Task<JsonResult> GetBanksByAllCodeSV(string allCode)
        {
            try
            {
                BALAccountManager bal = new BALAccountManager();
                var result = await bal.GetBanksByAllCodeSV(allCode);

                return Json(new
                {
                    success = true,
                    data = result.Select(b => new
                    {
                        IFSCCode = b.IFSCCode,
                        BankCode = b.BankCode,
                        AccountNo = b.AccountNo,
                        SubTypeName = b.SubTypeName


                        // No need to return OpeningBalance here anymore
                    })
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Controller method to fetch Opening Balance by IFSCCode
        /// </summary>
        /// <summary>
        /// Controller method to fetch Opening Balance by AccountNo
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetOpeningBalanceSV(string accountNo) // Changed parameter name
        {
            try
            {
                BALAccountManager bal = new BALAccountManager();
                var balance = await bal.GetOpeningBalanceByAccountNoSV(accountNo); // Changed method call

                if (balance.HasValue)
                {
                    return Json(new { success = true, balance = balance.Value }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "No balance found." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        /// <summary>
        /// For loading the cash partial modal
        /// </summary>

        [HttpGet]
        public async Task<PartialViewResult> LoadCashEventPartialSV(string eventCode)
        {
            var bal = new BALAccountManager();
            var model = await bal.GetEventDetailsCashSV(eventCode);
            if (model == null)
                return PartialView("_ErrorPartial");

            return PartialView("_CashEventPartial", model);
        }


        /// <summary>
        /// For saving the cash transactions in database
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> SaveCashTransactionSV(FormCollection form)
        {
            try
            {
                string eventCode = form["eventCode"];
                string memberCode = form["memberCode"];
                decimal amount = Convert.ToDecimal(form["amount"], System.Globalization.CultureInfo.InvariantCulture);

                // ✅ Read from Session (added like your friend's code)
                string societyCode = Session["SocietyCode"]?.ToString();
                string staffCode = Session["StaffCode"]?.ToString();

                if (string.IsNullOrEmpty(societyCode) || string.IsNullOrEmpty(staffCode))
                {
                    return Json(new { success = false, error = "Session expired. Please log in again." });
                }

                var bal = new BALAccountManager();
                bool success = await bal.SaveCashTransactionSV(eventCode, memberCode, amount, societyCode, staffCode);

                return Json(new { success = success });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }


        #endregion



        #region********************************************************************* Community Send Email ***********************************************************

        /// <summary>
        /// Loads the Send Email view with accountant's default email, contact, message,
        /// and fetches members plus active staff for CC dropdown. 07/08/25
        /// </summary>
        public async Task<ActionResult> SendEMailVSM(string wing = "All")
        {
            var bal = new BALAccountManager();
            var accountant = await bal.GetAccountantDetailsVM();
            var helper = new EmailHelper();

            string defaultBody = helper.GetAccountantMessageVM(accountant.userTypedMessage, accountant.FromEmailAddress, accountant.ContactNumber, "member");

            var model = new AccountManager
            {
                FromEmailAddress = accountant.FromEmailAddress,
                ContactNumber = accountant.ContactNumber,
                EmailBodyMessage = defaultBody
            };

            ViewBag.SelectedWing = wing;
            ViewBag.MemberList = await bal.GetMembersVM(wing);
            ViewBag.StaffList = await bal.GetStaffVM();  
            ViewBag.Message = TempData["Message"];
            ViewBag.EmailResult = TempData["EmailResult"];
            return View(model);
        }
















        /// <summary>
        /// Handles the submission of the email form. Sends emails to selected or manually entered recipients.
        /// Accepts multiple attachments and sends messages individually to each recipient with CC support. 07/08/25
        /// </summary>
        [HttpPost]
        [ValidateInput(false)]
        public async Task<ActionResult> SendEMailVSM(AccountManager model, string ManualEmail, string[] CcEmailAddresses)
        {
            var bal = new BALAccountManager();
            var results = new List<string>();
            var recipients = new List<string>();

            if (!string.IsNullOrWhiteSpace(ManualEmail))
                recipients.AddRange(ManualEmail.Split(',').Select(e => e.Trim()).Where(e => !string.IsNullOrWhiteSpace(e)));
            if (model.ToEmailAddresses != null)
                recipients.AddRange(model.ToEmailAddresses);

            var attachments = new List<HttpPostedFileBase>();
            for (int i = 0; i < Request.Files.Count; i++)
            {
                var file = Request.Files[i];
                if (file != null && file.ContentLength > 0)
                    attachments.Add(file);
            }

            var allMembers = await bal.GetMembersVM("All");
            var helper = new EmailHelper();

            // Track sent member info for CC summary
            var sentMembersInfo = new List<string>();

            // Send emails to members individually, NO CC
            foreach (var email in recipients.Distinct())
            {
                var member = allMembers.FirstOrDefault(m => m.ToEmailAddress?.Trim().ToLower() == email.Trim().ToLower());
                string memberName = "Member";
                if (member != null && !string.IsNullOrEmpty(member.FullName))
                {
                    var prefix = member.Gender == "Female" ? "Ms." : "Mr.";
                    memberName = $"{prefix} {member.FullName}";
                    sentMembersInfo.Add($"{memberName} ({member.ToEmailAddress})");
                }
                else
                {
                    sentMembersInfo.Add(email);
                }

                string personalizedBody = !string.IsNullOrWhiteSpace(model.EmailBodyMessage)
                    ? model.EmailBodyMessage.Replace("member", memberName)
                    : helper.GetAccountantMessageVM(model.userTypedMessage, model.FromEmailAddress, model.ContactNumber, memberName);

                var emailModel = new AccountManager
                {
                    ToEmailAddress = email,
                    FromEmailAddress = model.FromEmailAddress,
                    Subject = model.Subject,
                    EmailBodyMessage = personalizedBody,
                    ContactNumber = model.ContactNumber,
                    CcEmailAddresses = null // no CC here
                };

                var result = await bal.SendEMailVM(emailModel, attachments);
                results.Add(result);
            }

            // Compose and send one summary email to all CC recipients
            if (CcEmailAddresses != null && CcEmailAddresses.Length > 0)
            {
                string ccEmailBody = $"Dear receiptant,<br/><br/>Accountant has  sent emails to the following members:<br/><ul>";
                foreach (var info in sentMembersInfo)
                    ccEmailBody += $"<li>{info}</li>";
                ccEmailBody += "</ul><br/>Message Content:<br/>";
                ccEmailBody += !string.IsNullOrWhiteSpace(model.EmailBodyMessage)
                    ? model.EmailBodyMessage
                    : helper.GetAccountantMessageVM(model.userTypedMessage, model.FromEmailAddress, model.ContactNumber, "members");

                var ccEmailModel = new AccountManager
                {
                    ToEmailAddress = string.Join(",", CcEmailAddresses.Distinct()),
                    FromEmailAddress = model.FromEmailAddress,
                    Subject = model.Subject + " - Summary",
                    EmailBodyMessage = ccEmailBody,
                    ContactNumber = model.ContactNumber,
                    CcEmailAddresses = null
                };

                var ccResult = await bal.SendEMailVM(ccEmailModel, attachments);
                results.Add(ccResult);
            }

            TempData["EmailResult"] = string.Join("<br/>", results);

            // Reload view data and prepare model for redirected view
            string selectedWing = Request["wing"] ?? "All";
            ViewBag.SelectedWing = selectedWing;
            var memberList = await bal.GetMembersVM(selectedWing);
            foreach (var member in memberList)
                member.IsSelected = model.ToEmailAddresses != null && model.ToEmailAddresses.Contains(member.ToEmailAddress);
            ViewBag.MemberList = memberList;
            ViewBag.StaffList = await bal.GetStaffVM();

            TempData["Message"] = " Email sent successfully! ";
            return RedirectToAction("SendEMailVSM");
        }






        /// <summary>
        /// Helper method to fetch members based on wing or specific member IDs; used in other modules.
        /// </summary>
        public async Task<List<AccountManager>> GetMembersVSM(string wing, List<int> memberIds = null)
        {
            var bal = new BALAccountManager();
            return await bal.GetMembersVM(wing, memberIds);
        }




        public async Task<List<AccountManager>> GetStaffVSM()
        {
            var bal = new BALAccountManager();
            return await bal.GetStaffVM();
        }


        #endregion


        #region********************************************************************* Community Notice  ***********************************************************


        /// <summary>
        /// This action method fetches and categorizes all notices for the Notice List view.Date 05-07-2025
        /// </summary>

        /// <summary>
        /// This action method fetches and categorizes all notices for the Notice List view.Date 05-07-2025
        /// </summary>

        public async Task<ActionResult> NoticeList()
        {
            try
            {
                BALAccountManager bal = new BALAccountManager();
                var allNotices = await bal.GetAllNoticeListSS();

                ViewBag.AllNotices = allNotices;

                // ✅ Safe & correct filter for Accountant notices
                ViewBag.AccountantNotices = allNotices
                    .Where(x => !string.IsNullOrEmpty(x.SendByRole) &&
                                x.SendByRole.Trim()
                                    .Equals("Accountant", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                ViewBag.ExpiredNotices = allNotices
                    .Where(x => x.EndDate < DateTime.Now.Date)
                    .ToList();

                return View();
            }
            catch
            {
                TempData["Error"] = "An error occurred while loading notices.";
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        public async Task<JsonResult> DeleteNotice(string id)
        {
            try
            {
                BALAccountManager bal = new BALAccountManager();

                bool result = await bal.DeleteNoticeSS(id);  // id = NoticeAnnouncementCode

                if (result)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Delete failed in database." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }




        /// <summary>
        /// This  method is for the add notice .
        /// </summary>
        /// <summary>
        /// this method is for the Add Notice
        /// </summary>

        public async Task<ActionResult> AddNotice()
        {
            var model = new AccountManager
            {
                SendBy = "STF002",
                FromEmailAddress = System.Configuration.ConfigurationManager.AppSettings["EmailFrom"],
                Subject = "New Notice from Housing Society",
                EmailBodyMessage = "Dear Member,<br/><br/>Please find the latest notice.<br/><br/>Thanks,<br/>Green Valley Society"
            };

            ViewBag.SelectedWing = "All";
            ViewBag.MemberList = await GetMembersVSM("All");
            return PartialView("_AddNotice", model);
        }


        [HttpPost]
        [ValidateInput(false)]
        public async Task<ActionResult> AddNotice(AccountManager model)
        {
            try
            {
                model.AttachmentList = new List<string>();
                model.SelectedMemberCodes = new List<string>();

                // 🔁 Save files to disk
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    HttpPostedFileBase file = Request.Files[i];
                    if (file != null && file.ContentLength > 0)
                    {
                        string fileName = Path.GetFileName(file.FileName);
                        string folderPath = Server.MapPath("~/Uploads/Notices");
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);

                        string fullPath = Path.Combine(folderPath, fileName);
                        file.SaveAs(fullPath);

                        model.AttachmentList.Add(fileName);
                    }
                }

                // 🔁 Collect emails and member codes
                model.ToEmailAddresses = Request.Form.GetValues("ToEmailAddresses")?.ToList() ?? new List<string>();

                if (model.ToEmailAddresses.Any())
                {
                    var allMembers = await GetMembersVSM("All");
                    foreach (var email in model.ToEmailAddresses)
                    {
                        var member = allMembers.FirstOrDefault(m => m.ToEmailAddress == email);
                        if (member != null)
                            model.SelectedMemberCodes.Add(member.MemberCode);
                    }
                }

                model.Subject = "Notice: " + model.NoticeTitle;
                model.SendBy = "STF002";
                model.CreatedDate = DateTime.Now;

                var bal = new BALAccountManager();

                // ✅ Save notice and get notice code
                string noticeCode = await bal.SaveNoticeRM(model);

                // ✅ Save all documents linked to notice
                if (model.AttachmentList.Any())
                {
                    await bal.SaveNoticeDocumentsRM(noticeCode, model.AttachmentList);
                }

                // ✅ Send email with attachments
                model.NoticeCode = noticeCode;
                string mailStatus = await bal.SendEmailRM(model);

                // Return JSON success response
                return Json(new { success = true, message = "Notice saved successfully." });

            }
            catch (Exception ex)
            {
                // Return JSON error response
                return Json(new { success = false, message = ex.Message });
            }
        }




        /// <summary>
        /// This action method is for the edit notice .
        /// </summary>
        public async Task<ActionResult> EditNotice(string noticeCode)
        {
            var bal = new BALAccountManager();
            var model = await bal.GetNoticeWithDocumentsByCodeRM(noticeCode);

            return PartialView("_EditNoticeModal", model);
        }



        [HttpPost]
        [ValidateInput(false)]
        public async Task<ActionResult> UpdateNotice(AccountManager model, IEnumerable<HttpPostedFileBase> PostedFiles, string[] KeepFiles)
        {
            try
            {
                var bal = new BALAccountManager();
                var keepFilesList = (KeepFiles ?? Array.Empty<string>())
      .Where(x => !string.IsNullOrWhiteSpace(x))
      .Distinct()
      .ToList();

                model.AttachmentList = new List<string>(keepFilesList);


                // 1. Get existing documents from DB
                var existingNotice = await bal.GetNoticeWithDocumentsByCodeRM(model.NoticeCode);
                var existingDocs = existingNotice.AttachmentList ?? new List<string>();

                // 2. Filter KeepFiles (files to keep)


                // 3. Find files to delete (not in KeepFiles[])
                var toDelete = existingDocs
                    .Where(doc => !keepFilesList.Contains(doc))
                    .ToList();

                // 4. Delete removed files from DB and disk
                foreach (var doc in toDelete)
                {
                    // Delete from DB
                    SqlParameter[] deleteParams = new SqlParameter[]
                    {
new SqlParameter("@Flag", "DeleteDocumentByNameRM"),
new SqlParameter("@NoticeCode", model.NoticeCode),
new SqlParameter("@Document", doc)
                    };

                    await MSSQL.ExecuteStoredProcedure("sp_SMS", deleteParams);

                    // Delete from disk with retry logic
                    string filePath = Server.MapPath("~/Uploads/Notices/" + doc);
                    if (System.IO.File.Exists(filePath))
                    {
                        int attempts = 0;
                        bool deleted = false;
                        while (!deleted && attempts < 3)
                        {
                            try
                            {
                                System.IO.File.Delete(filePath);
                                deleted = true;
                            }
                            catch (IOException)
                            {
                                attempts++;
                                System.Threading.Thread.Sleep(300);
                            }
                        }
                    }
                }

                // 5. Save newly uploaded files
                if (PostedFiles != null)
                {
                    foreach (var file in PostedFiles)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            // Validate file type
                            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                            var fileExtension = Path.GetExtension(file.FileName).ToLower();
                            if (!allowedExtensions.Contains(fileExtension)) continue;

                            // Validate file size (max 5MB)
                            if (file.ContentLength > 5 * 1024 * 1024) continue;

                            // Unique filename with GUID
                            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            string folderPath = Server.MapPath("~/Uploads/Notices");
                            if (!Directory.Exists(folderPath))
                            {
                                Directory.CreateDirectory(folderPath);
                            }

                            string fullPath = Path.Combine(folderPath, uniqueFileName);

                            // Save file using stream (safely release resources)
                            using (var stream = new FileStream(fullPath, FileMode.Create))
                            {
                                file.InputStream.CopyTo(stream);
                            }

                            model.AttachmentList.Add(uniqueFileName);
                        }
                    }
                }

                // 6. Update main notice info
                await bal.UpdateNoticeRM(model);

                // 7. Save new documents in DB
                if (model.AttachmentList.Any())
                {
                    await bal.SaveNoticeDocumentsRM(model.NoticeCode, model.AttachmentList);
                }

                TempData["Message"] = "Notice updated successfully.";
                return RedirectToAction("NoticeList");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Error updating notice: " + ex.Message;
                return RedirectToAction("NoticeList");
            }
        }




        /// <summary>
        /// This Method is For the ViewNotice
        /// </summary>

        public async Task<ActionResult> ViewNotice(string noticeCode)
        {
            try
            {
                var bal = new BALAccountManager();
                var model = await bal.GetNoticeWithDocumentsByCodeRM(noticeCode);
                return PartialView("_ViewNotice", model);
            }
            catch (Exception ex)
            {
                // Log error
                return PartialView("_Error", "Failed to load notice details: " + ex.Message);
            }
        }


        #endregion

        #region********************************************************************* Complaints ***********************************************************



        /// <summary>
        /// showing Complaint List via BAL 
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> ComplaintList()
        {
            try
            {
                BALAccountManager bal = new BALAccountManager();

                // Fetch complaints including Wing column
                var complaints = await bal.GetAllComplaintsAsyncSB();

                if (complaints == null)
                    complaints = new List<AccountManager>();

                return View("ComplaintList", complaints);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading complaints: " + ex.Message;

                // Return empty list to avoid null reference exceptions in View
                return View("ComplaintList", new List<AccountManager>());
            }
        }

        /// <summary>
        /// View Complaints Accountant related  details ---------------------------//
        /// </summary>
        /// <param name="complaintId"></param>
        /// <returns></returns>
        public async Task<ActionResult> ViewResolvedComplaintDetailsMS(int complaintId)
        {
            if (complaintId <= 0)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            BALAccountManager obj = new BALAccountManager();
            var data = await obj.GetResolvedComplaintDetailsByIdAsyncMs(complaintId);

            if (data == null)
                return HttpNotFound();

            return PartialView("_ResolvedComplaintDetails", data);
        }
        /// <summary>
        /// view solve button to click button status change and status is solve
        /// </summary>
        /// <param name="complaintId"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<JsonResult> ResolveComplaintSB(int complaintId, string reason)
        {
            try
            {
                BALAccountManager bal = new BALAccountManager();
                bool ok = await bal.ResolveComplaintAsyncSB(complaintId, reason);

                if (ok)
                    return Json(new { success = true, message = "Complaint marked as solved." });

                return Json(new { success = false, message = "No record updated." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }
        #endregion


        #region********************************************************************* Reports  ***********************************************************



        /// <summary>
        /// //FOR SHOW TOTAL MEMEBERS COUNT****************************************************************************** 
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> AuditReport()
        {

            BALAccountManager bal = new BALAccountManager();
            int totalMembers = await bal.GetTotalMemberCountNK();
            int totalWorkers = await bal.GetTotalWorkerCountNK();
            int totalVendor = await bal.GetTotalVendorCountNK();
            ViewBag.TotalMembers = totalMembers;
            ViewBag.TotalWorkers = totalWorkers;
            ViewBag.TotalVendor = totalVendor;
            return View();
        }

        /// <summary>
        ///  //FOR SHOW MEMBER LIST WHEN CLICK ON CARD/TAB*************************************************************** 
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> MemberListNK()
        {
            try
            {
                BALAccountManager bal = new BALAccountManager();
                var members = await bal.GetAllMemberDetailsNK();
                return PartialView("_MemberListNK", members);
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        public async Task<ActionResult> MemberDocumentsNK(string memberCode)
        {
            try
            {
                BALAccountManager bal = new BALAccountManager();
                var docs = await bal.GetMemberDocumentsNK(memberCode);
                return PartialView("_MemberDocumentsNK", docs);
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }

        /// <summary>
        /// //FOR SHOW WORKERLIST LIST WHEN CLICK ON CARD/TAB*************************************************************** 
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> WorkerListNK()
        {
            BALAccountManager bal = new BALAccountManager();
            var worker = await bal.GetAllWorkerDetailsNK();
            return PartialView("_WorkerListNK", worker);
        }

        /// <summary>
        ///  //FOR SHOW VENDOR LIST WHEN CLICK ON CARD/TAB*************************************************************** 
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> VendorListNK()
        {
            BALAccountManager bal = new BALAccountManager();
            var Vendor = await bal.GetAllVendorDetailsNK();
            return PartialView("_VendorListNK", Vendor);
        }
        /// <summary>
        /// //FOR SHOW PIE CHART FOR DIRECT AND INDIRECT EXPENCE*************************************************************** 
        /// </summary>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>

        [HttpPost]
        public async Task<JsonResult> ShowPieChartForExpenceNK(int month, int year)
        {
            var data = await new BALAccountManager().GetMonthlyExpensePieDataAsyncNK(month, year);
            var chartData = data.Select(x => new
            {
                name = x.ExpenseType,
                y = x.Percentage
            }).ToList();

            return Json(chartData, JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        ///  //FOR SHOW LIST WHEN CLICK ON  PIE CHART EACH SLICE*************************************************************** 
        /// </summary>
        /// <param name="expenseTypeName"></param>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> GetExpenseDetailsByTypeNK(string expenseTypeName, int month, int year)
        {
            int expenseTypeId = (expenseTypeName == "Direct Expense") ? 36 : 37;

            BALAccountManager bal = new BALAccountManager();
            var list = await bal.GetExpenseDetailsByTypeNK(expenseTypeId, month, year);

            ViewBag.ExpenseType = expenseTypeName; // This is missing!

            return PartialView("_ExpenceDetailsListPartialNK", list);
        }

        /// <summary>
        /// //FOR SHOW COLUMN CHART FOR WORKER SALARY*************************************************************** 
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        public async Task<JsonResult> GetWorkerSalaryTotalPerMonthNK(int year)
        {
            BALAccountManager bal = new BALAccountManager();
            var data = await bal.GetMonthlyTotalWorkerSalaryAsyncNK(year);  // ✅ pass year here

            var result = data.Select(x => new
            {
                Month = x.MonthName,
                Amount = x.TotalAmount
            }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }




        /// <summary>
        /// //for  show list of worker payment when click on column********************
        /// </summary>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>

        [HttpPost]
        public async Task<ActionResult> GetWorkerSalaryListByMonthNK(int month, int year)
        {
            BALAccountManager bal = new BALAccountManager();
            var data = await bal.GetWorkerSalaryListByMonthAsyncNK(month, year);
            return PartialView("_WorkerSalaryListPartialNK", data);
        }

        /// <summary>
        /// //for  show all details list of worker payment when click on see details ******************** 
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>


        [HttpGet]
        public async Task<ActionResult> ShowWorkerSalaryListALLNK(int year)
        {
            BALAccountManager bal = new BALAccountManager();
            var data = await bal.ShowWorkerSalaryListALLMONTHNK(year);
            return PartialView("_ShowWorkerSalaryListAllNK", data);
        }



        /// <summary>
        ///  //SHow all EXpence when click on SEE DETAILS
        /// </summary>
        /// 
        /// <returns></returns>

        public async Task<ActionResult> AllExpenseListNK()
        {
            BALAccountManager bal = new BALAccountManager();
            var data = await bal.ShowALLExpenceDetailsNK();// BAL method call
            return PartialView("_ShowALLExpenceLISTNK", data);  // Send to partial view
        }





        //Viraj  BALLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLL 10-7-2025



        //Viraj  BALLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLL 10-7-2025



        /// <summary>
        /// // 🔹 Chart 1: Budget vs Actual
        /// </summary>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<JsonResult> GetBudgetVsActualChartDataVD(int? month, int? year)
        {
            BALAccountManager _bal = new BALAccountManager();
            try
            {
                int selectedMonth = month ?? DateTime.Now.Month;
                int selectedYear = year ?? DateTime.Now.Year;

                var events = await _bal.GetBudgetVsActualDataAsyncVD(selectedMonth, selectedYear);

                return Json(new
                {
                    Month = selectedMonth,
                    Year = selectedYear,
                    MonthLabel = new DateTime(selectedYear, selectedMonth, 1).ToString("MMMM yyyy"),
                    Events = events
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        /// <summary>
        /// show Event allocate and actual budget details 
        /// </summary>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<PartialViewResult> EventBudgetDetailsVD(int? month, int? year)
        {
            BALAccountManager _bal = new BALAccountManager();
            try
            {
                List<AccountManager> model;

                if (month.HasValue && year.HasValue)
                {
                    model = await _bal.GetBudgetVsActualDataAsyncVD(month.Value, year.Value);
                    ViewBag.MonthLabel = new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy");
                    // Hide date picker for chart bar clicks (specific month/year provided)
                    ViewBag.ShowDatePicker = false;
                }
                else
                {
                    model = await _bal.GetAllEventBudgetvdDetailsAsyncVD();
                    ViewBag.MonthLabel = "All Months";
                    // Show date picker for "See Details" button (no specific month/year)
                    ViewBag.ShowDatePicker = true;
                }

                return PartialView("_EventBudgetDetailsPartial", model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading budget details: " + ex.Message;
                ViewBag.ShowDatePicker = true; // Default to showing date picker on error
                return PartialView("_EventBudgetDetailsPartial", new List<AccountManager>());
            }
        }
        ///////////////////////////////////////////////new one
        [HttpGet]
        public async Task<PartialViewResult> DistributionEventDetails()
        {
            BALAccountManager _bal = new BALAccountManager();
            try
            {
                // Call the BAL method we just created
                List<AccountManager> model = await _bal.GetAllDistributionEventDetailsAsync();

                ViewBag.Title = "Distribution Event Details";

                return PartialView("_DistributionEventDetailsPartial", model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading distribution event details: " + ex.Message;
                return PartialView("_DistributionEventDetailsPartial", new List<AccountManager>());
            }
        }

        /// new one fin
        /// <summary>
        /// // 🔹 Chart 2: Income vs Expense
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<JsonResult> GetMonthlyIncomeExpenseChartDataVD(int? year)
        {
            BALAccountManager _bal = new BALAccountManager();
            try
            {
                int selectedYear = year ?? DateTime.Now.Year;
                var data = await _bal.GetMonthlyIncomeExpenseAsyncVD(selectedYear);
                return Json(new { year = selectedYear, data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }





        /// <summary>
        ///  
        /// </summary>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <param name="allData"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<PartialViewResult> IncomeExpenseDetailsVD(int? month = null, int? year = null, bool allData = false)
        {
            BALAccountManager _bal = new BALAccountManager();
            try
            {
                var finalMonth = allData ? null : month;
                var finalYear = allData ? null : year;

                var model = await _bal.GetIncomeExpenseDetailsAsyncVD(finalMonth, finalYear);

                ViewBag.MonthLabel = allData
                    ? "All Income/Expense Data"
                    : (month.HasValue && year.HasValue
                        ? new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy")
                        : "Filtered Income/Expense");

                return PartialView("_IncomeExpenseList", model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading data: {ex.Message}";
                return PartialView("_IncomeExpenseList", new List<AccountManager>());
            }
        }




        #endregion





    }
}