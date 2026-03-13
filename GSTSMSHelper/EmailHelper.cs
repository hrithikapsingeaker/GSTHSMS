using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;


namespace GSTSMSHelper
{
    public class EmailHelper
    {
        private readonly string host = ConfigurationManager.AppSettings["EmailHost"];
        private readonly int port = int.Parse(ConfigurationManager.AppSettings["EmailPort"]);
        private readonly string username = ConfigurationManager.AppSettings["EmailUsername"];
        private readonly string password = ConfigurationManager.AppSettings["EmailPassword"];
        private readonly string fromEmail = ConfigurationManager.AppSettings["OfficialEmail"];
      






        public async Task<bool> SendOtpEmail(string toEmail, string otp)
        {
            try
            {
                string subject = "Your OTP - Housing Society Login";
                string body = $@"
                <div style='font-family: Poppins, sans-serif; max-width: 600px; margin: auto; background: #f9f9f9; padding: 30px; border-radius: 10px; box-shadow: 0 0 10px rgba(0,0,0,0.05); color: #333;'>
                    <div style='text-align: center; border-bottom: 1px solid #eee; padding-bottom: 20px; margin-bottom: 30px;'>
                        <h1 style='color: #5c6bc0;'>🏢 Housing Society Portal</h1>
                        <p style='font-size: 14px; color: #777;'>Secure • Reliable • Community Focused</p>
                    </div>

                    <p style='font-size: 16px;'>Dear Resident,</p>
                    <p style='font-size: 15px;'>We've received a request to reset your password for your Housing Society account.</p>

                    <div style='margin: 30px 0; text-align: center;'>
                        <p style='font-size: 16px; margin-bottom: 10px;'>Your One-Time Password (OTP) is:</p>
                        <div style='display: inline-block; font-size: 28px; color: #fff; background: #5c6bc0; padding: 10px 30px; border-radius: 8px; letter-spacing: 6px; font-weight: bold;'>{otp}</div>
                        <p style='margin-top: 10px; font-size: 13px; color: #888;'>This OTP is valid for 3 minutes</p>
                    </div>

                    <p style='font-size: 15px;'>Please enter this OTP on the website to continue resetting your password.</p>

                    <div style='margin-top: 40px; border-top: 1px solid #eee; padding-top: 20px; text-align: center;'>
                        <p style='font-size: 13px; color: #aaa;'>Need help? Email us at <a href='mailto:support@gayasofttech.com' style='color: #5c6bc0;'>support@gayasofttech.com</a></p>
                        <p style='font-size: 13px; color: #aaa;'>© {DateTime.Now.Year} Housing Society System. All rights reserved.</p>
                    </div>
                </div>";

                MailMessage mail = new MailMessage(fromEmail, toEmail, subject, body);
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient(); // pulls SMTP settings from web.config
                await smtp.SendMailAsync(mail);
                return true;
            }
            catch (SmtpException smtpEx)
            {
                throw new Exception("SMTP error: " + smtpEx.Message, smtpEx);
            }
            catch (Exception ex)
            {
                throw new Exception("General error while sending OTP: " + ex.Message, ex);
            }
        }

        public async Task<bool> SendEnquiryConfirmationEmail(string toEmail, string chairmanName)
        {
            try
            {
                string subject = "Thank You for Your Enquiry - Housing Society Management";
                //string videoLink = "https://youtu.be/Fgnl6QbQiQk?si=xf_kl7GsKD94p9Lj";

                string body = $@"
                Hi {chairmanName},<br><br>
                Thank you for your interest in our Housing Society Management Platform!<br>
                We’ve received your enquiry and will reach out shortly.<br><br>
                Meanwhile, here’s a quick demo video: 
                <a href='https://youtu.be/Fgnl6QbQiQk?si=xf_kl7GsKD94p9Lj' target='_blank'>Click here to watch</a><br><br>
                Regards,<br>Housing Society Team";

                MailMessage mail = new MailMessage(fromEmail, toEmail, subject, body);
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient();
                await smtp.SendMailAsync(mail);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while sending enquiry email: " + ex.Message, ex);
            }
        }
        public async Task<bool> SendSubscriptionOtpEmail(string toEmail, string otp)
        {
            try
            {
                string subject = "Your OTP - Housing Society Subscription";
                string body = $@"
                <div style='font-family: Poppins, sans-serif; max-width: 600px; margin: auto; background: #f9f9f9; padding: 30px; border-radius: 10px; box-shadow: 0 0 10px rgba(0,0,0,0.05); color: #333;'>
                <div style='text-align: center; border-bottom: 1px solid #eee; padding-bottom: 20px; margin-bottom: 30px;'>
                    <h1 style='color: #5c6bc0;'>📩 Housing Society Subscription</h1>
                    <p style='font-size: 14px; color: #777;'>Secure your subscription with the OTP below</p>
                </div>

                <p style='font-size: 16px;'>Dear User,</p>
                <p style='font-size: 15px;'>To proceed with your subscription, please verify your email by entering this OTP:</p>

                <div style='margin: 30px 0; text-align: center;'>
                    <div style='display: inline-block; font-size: 28px; color: #fff; background: #5c6bc0; padding: 10px 30px; border-radius: 8px; letter-spacing: 6px; font-weight: bold;'>{otp}</div>
                    <p style='margin-top: 10px; font-size: 13px; color: #888;'>Valid for 3 minutes</p>
                </div>

                <p style='font-size: 15px;'>Thank you for trusting our platform to manage your society better!</p>

                <div style='margin-top: 40px; border-top: 1px solid #eee; padding-top: 20px; text-align: center;'>
                    <p style='font-size: 13px; color: #aaa;'>Need help? Email us at <a href='mailto:support@gayasofttech.com' style='color: #5c6bc0;'>support@gayasofttech.com</a></p>
                    <p style='font-size: 13px; color: #aaa;'>© {DateTime.Now.Year} Housing Society System. All rights reserved.</p>
                </div>
                </div>";

                MailMessage mail = new MailMessage(fromEmail, toEmail, subject, body);
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient(); // Reads settings from Web.config
                await smtp.SendMailAsync(mail);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while sending subscription OTP email: " + ex.Message, ex);
            }
        }

        public async Task<bool> SendSubscriptionReceiptEmail(string toEmail, string planName, decimal amount, string duration, DateTime startDate, DateTime expiryDate, string paymentId)
        {
            try
            {
                string subject = "🧾 Your Housing Society Subscription Receipt";

                decimal total = amount;

                //decimal tax = Math.Round(amount * 0.18m, 2);
                //decimal total = amount + tax;

                string body = $@"
                <div style='font-family: Poppins, sans-serif; max-width: 600px; margin: auto; background: #ffffff; padding: 30px; border-radius: 10px; box-shadow: 0 0 20px rgba(0,0,0,0.08); color: #333;'>
                    <h2 style='text-align: center; color: #5d3fd3;'>Thank you for subscribing!</h2>
                    <p><strong>Order number:</strong> {paymentId}</p>
                    <p><strong>Order date:</strong> {DateTime.Now.ToString("MMM dd, yyyy hh:mm tt")}</p>
    
                    <hr style='margin: 20px 0;' />

                    <table width='100%' cellpadding='10'>
                        <tr>
                            <td><strong>Plan</strong></td>
                            <td align='right'>{planName} ({duration})</td>
                        </tr>
                        <tr>
                            <td><strong>Base Price</strong></td>
                            <td align='right'>₹{amount:F2}</td>
                        </tr>
                       <tr style='font-weight: bold;'>
                        <td>Total</td>
                        <td align='right'>₹{total:F2}</td>
                    </tr>

                    </table>

                    <hr style='margin: 20px 0;' />

                    <p><strong>Start Date:</strong> {startDate:dd MMM yyyy}</p>
                    <p><strong>Expiry Date:</strong> {expiryDate:dd MMM yyyy}</p>

                    <p style='margin-top: 20px;'>This subscription is now active. Thank you for choosing Housing Society System! 🎉</p>

                    <p style='font-size: 12px; color: #888; margin-top: 40px; text-align: center;'>
                        Need help? Contact <a href='mailto:support@gayasofttech.com'>support@gayasofttech.com</a><br />
                        © {DateTime.Now.Year} Housing Society System. All rights reserved.
                    </p>
                </div>";

                MailMessage mail = new MailMessage(fromEmail, toEmail, subject, body);
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient();
                await smtp.SendMailAsync(mail);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while sending subscription receipt email: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Returns the default accountant message template.
        /// </summary>
        public string GetBudgetApprovalMessage(
      string eventName,
      string createdDate,
      string eventHandlerName,
      string wingName,
      string allocatedBudget,
      string fromEmailAddress,
      string contactNumber)
        {
            return $@"
<html>
<body style='font-family: Arial, sans-serif; color: #333;'>
    <h3 Budget Approval Request For Event </h3>

     <p>Dear Chairman,</p>
    <p> I am writing to formally request budget approval for the following event scheduled under the GreenValley Society </p>
    <p><strong>Event Name:</strong> {eventName}</p>
    <p><strong>Created Date:</strong> {createdDate}</p>
    <p><strong>Event Handler:</strong> {eventHandlerName}</p>
    <p><strong>Wing Name:</strong> {wingName}</p>
    <p><strong>Allocated Budget:</strong> ₹{allocatedBudget}</p>
    <br />
    <p> Kindly review the above details and take necessary action to approve the requested budget at your earliest convenience.</p>
    <p>For more information, contact:</p>
    <p><strong>Email:</strong> {fromEmailAddress}</p>
    <p><strong>ContactNumber:</strong> {contactNumber}</p>
    <hr />
    <p Warm regards,<br/>GreenValley Society – Accountant Team</p>
  </body>
  </html>";
        }



        #region********************************************************************* Community Send Email ***********************************************************




        //To send default msg and default footer to selected memeber 

        public string GetAccountantMessageVM(string userTypedMessage, string fromEmail, string contactNo, string fullName)
        {
            string footer = $@"
    <p style='font-size: 14px; margin-top: 25px;'>
        <strong>Warm regards,</strong><br/>
        <b>Vishwaraj Magar</b><br/>
        Accountant, <br/> 🏢 Green Valley Housing Society
        <br/>
        ✉ Email: {fromEmail}<br/>
        📞 Phone: {contactNo}
    </p>";

            if (string.IsNullOrWhiteSpace(userTypedMessage))
            {
                // Default message + footer
                return $@"
<div style='font-family: Poppins, sans-serif; color: #333;'>
    <p style='font-size: 14px;'><strong>Dear {fullName},</strong></p>
    <p style='font-size: 14px;'>We hope you are doing well and staying safe.</p>
    <p style='font-size: 14px;'><strong>Notice:</strong> Your monthly maintenance bill and society updates are now available.<br />
    You may view the details via our website</p>
    <p style='font-size: 14px;'><strong>Payment Reminder:</strong> Please ensure that your dues are cleared before the due date to avoid any late fees.</p>
    {footer}
</div>";
            }
            else
            {
                // Custom user message + footer only
                return $@"
<div style='    font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial,  sans-serif;
; color: #333;'>
    <p style='font-size: 14px;'><strong>Dear {fullName},</strong></p>
    <p style='font-size: 14px;'>{userTypedMessage}</p>
    {footer}
</div>";
            }
        }



        /// <summary>
        /// Mail sending function it checks all esentiallity of SMTP  22/07/25
        /// </summary>


        public async Task<string> SendEmailHelperVM(string toEmail, string subject, string body, List<HttpPostedFileBase> attachments, List<string> ccEmails = null)
        {
            try
            {
                var mail = new MailMessage
                {
                    From = new MailAddress(fromEmail, "GreenValley Society"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                // Add primary recipient
                mail.To.Add(toEmail);

                // Add CC recipients if any
                if (ccEmails != null && ccEmails.Any())
                {
                    foreach (var cc in ccEmails.Distinct())
                    {
                        if (!string.IsNullOrEmpty(cc))
                        {
                            mail.CC.Add(new MailAddress(cc));
                        }
                    }
                }

                // Attach files if any
                if (attachments != null)
                {
                    foreach (var file in attachments)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            mail.Attachments.Add(new Attachment(file.InputStream, Path.GetFileName(file.FileName)));
                        }
                    }
                }

                using (var smtp = new SmtpClient(host, port))
                {
                    smtp.Credentials = new NetworkCredential(username, password);
                    smtp.EnableSsl = true;
                    await smtp.SendMailAsync(mail);
                }

                return $"✅ Sent: {toEmail}";
            }
            catch (Exception ex)
            {
                return $"❌ Failed: {toEmail} → {ex.Message}";
            }
        }

        #endregion



    }
}
