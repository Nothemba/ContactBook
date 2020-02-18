using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using ContactBook.Models;

namespace ContactBook.Controllers
{
    public class UserController : Controller
    {
        //Registration Action 
        [HttpGet]
        public ActionResult Registration()
        {
            return View();
        }

        //Registration POST action 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registration([Bind(Exclude = "IsEmailVerified, ActivationCode")] User user)
        {
            bool Status = false;
            string message = "";
            //Model Validation
            if (ModelState.IsValid)
            {

                #region //Email Exists

                var isExist = IsEmailExist(user.Email);
                if (isExist)
                {
                    ModelState.AddModelError("EmailExist", "Email already exist");
                    return View(user);
                }
                #endregion


                #region Generate Activation Code
                user.ActivationCode = Guid.NewGuid();
                #endregion

                #region Password Hashnig
                user.Password = Crypto.Hash(user.Password);
                user.ConfirmPassword = Crypto.Hash(user.ConfirmPassword);
                #endregion
                user.IsEmailVerified = false;

                #region Save to Database
                using (UserDBEntities dc = new UserDBEntities())
                {
                    dc.Users.Add(user);
                    dc.SaveChanges();

                    //Send Email to User
                    SendVerificationLinkEmail(user.Email, user.ActivationCode.ToString());
                    message = "Registration successfully done. Account activation link " +
                        " has been sent to your Email:" + user.Email;
                    Status = true;
                }
                #endregion
            }
            else
            {
                message = "Invalid Request";
            }

            ViewBag.Message = message;
            ViewBag.Status = Status;
            return View(user);
        }
        
        [NonAction]
        public bool IsEmailExist(string emailID)
        {
            using (UserDBEntities dc = new UserDBEntities())
            {
                var v = dc.Users.Where(a => a.Email == emailID).FirstOrDefault();
                return v != null;
            }
        }
        
        [NonAction]
        public void SendVerificationLinkEmail(string emailID, string activationCode)
    {
        var verifyUrl = "/User/VerifyAccount/" + activationCode;
        var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

        var fromEmail = new MailAddress("ml.modisadife@gmail.com", "Contact Book Account");
        var toEmail = new MailAddress(emailID);
        var fromEmailPassword = "1792fd2749bb31";
        string subject = "Your account is successfully created!";

        string body = "<br/><br/>We are excited to tell you that you may now use your Contact Book. Please click on the below link to verify your email and start saving contacts" +
            " <br/><br/><a href='" + link + "'>" + link + "</a> ";

        var smtp = new SmtpClient
        {
            Host = "smtp.gmail.com",
            Port = 587,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
        };

        using (var message = new MailMessage(fromEmail, toEmail)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        })
            smtp.Send(message);
    }
}
}
           