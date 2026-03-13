


//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using System.Web.Mvc;
//using GSTSMSHelper;
//using GSTSMSLibrary.Account;

//namespace GSTSMS.Controllers
//{
//    public class AccountController : Controller
//    {
//        BALAccount obj = new BALAccount();

//        [HttpGet]
//        public ActionResult LoginRK()
//        {
//            return View();
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<ActionResult> LoginRK(Account model)
//        {
//            if (!ModelState.IsValid)
//                return View(model);

//            SqlDataReader dr = await obj.Login(model);

//            if (dr.Read())
//            {
//                Session["Email"] = dr["Email"].ToString();
//                Session["Password"] = dr["Password"].ToString();
//                Session["RoleId"] = Convert.ToInt32(dr["RoleId"]);
//                Session["RoleName"] = dr["RoleName"].ToString();
//                //Session["ProfilePic"] = dr["ProfilePic"] != DBNull.Value
//                //    ? dr["ProfilePic"].ToString()
//                //    : "~/Images/default-profile.png";

//                string roleId = dr["RoleId"].ToString();

//                if (roleId == "2")
//                {
//                    return RedirectToAction("Dashboard", "AccountManager");
//                }
//                else
//                {
//                    ViewBag.Message = "Unauthorized role.";
//                    return View(model);
//                }
//            }

//            ViewBag.Message = "Invalid email or password.";
//            return View(model);
//        }

//         //---------------- LANDING PAGE ---------------- //
//        [HttpGet]
//        public ActionResult LandingPage()
//        {
//            return View();
//        }





//    }

//}



using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using GSTSMSHelper;
using GSTSMSLibrary.Account;

namespace GSTSMS.Controllers
{
    public class AccountController : Controller
    {
        BALAccount obj = new BALAccount();

        [HttpGet]
        public ActionResult LoginRK()
        {
            if (Session["Email"] != null)
            {
                return RedirectToAction("Dashboard", "AccountManager");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> LoginRK(Account model)
        {
            if (!ModelState.IsValid)
                return View(model);

            SqlDataReader dr = await obj.Login(model);

            if (dr.Read())
            {
                Session["Email"] = dr["Email"].ToString();
                Session["Password"] = dr["Password"].ToString();
                Session["RoleId"] = Convert.ToInt32(dr["RoleId"]);
                Session["RoleName"] = dr["RoleName"].ToString();
                Session["StaffCode"] = dr["StaffCode"].ToString();
                Session["SocietyCode"] = dr["SocietyCode"].ToString();
                Session["StaffName"] = dr["StaffName"]?.ToString() ?? "User"; // ✅ Staff full name
                Session["ContactNumber"] = dr["ContactNumber"].ToString();
                if (dr["ProfilePhoto"] != DBNull.Value && !string.IsNullOrEmpty(dr["ProfilePhoto"].ToString()))
                    Session["ProfilePhoto"] = dr["ProfilePhoto"].ToString();
                else
                    Session["ProfilePhoto"] = "~/Content/img/avatar/default.png";

                //Session["ProfilePic"] = dr["ProfilePic"] != DBNull.Value
                //    ? dr["ProfilePic"].ToString()
                //    : "~/Images/default-profile.png";

                string roleId = dr["RoleId"].ToString();

                if (roleId == "2")
                {
                    return RedirectToAction("Dashboard", "AccountManager");
                }
                else
                {
                    ViewBag.Message = "Unauthorized role.";
                    return View(model);
                }
            }

            ViewBag.Message = "Invalid email or password.";
            return View(model);
        }


        //---------------- LANDING PAGE ---------------- //
        [HttpGet]
        public ActionResult LandingPage()
        {
            return View();
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

            string currentAction = filterContext.ActionDescriptor.ActionName;
            if (Session["Email"] == null && currentAction != "LoginRK" && currentAction != "RequestOtpRK")
            {
                filterContext.Result = new RedirectResult(Url.Action("LoginRK", "Account"));
            }

            base.OnActionExecuting(filterContext);
        }



        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            Session.RemoveAll();


            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

            // Expire auth cookie
            if (Request.Cookies[".ASPXAUTH"] != null)
            {
                var cookie = new HttpCookie(".ASPXAUTH");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }

            return RedirectToAction("LoginRK", "Account");
        }


    }

}






