using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using Chisato.Shell;

namespace PersonalUniversalSite.Pages
{
    public class AccountCreationModel : PageModel
    {
        public void OnGet(string fail)
        {
            if (fail == "true")
            {
                ViewData["fail"] = "Could Not Create Account: Unknown";
            }
            else if (fail == "exists")
            {
                ViewData["fail"] = "Could Not Create Account: Account with that username already exists";
            }
        }

        public IActionResult OnPost()
        {
            string username = Request.Form["username"];
            string password = Request.Form["password"];
            string email = Request.Form["email"];

            if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(password) || string.IsNullOrEmpty(username) || string.IsNullOrWhiteSpace(username))
            {
                return Redirect("AccountCreation?fail=true");
            }
            else if (DoesExist(username))
            {
                return Redirect("AccountLogin?fail=exists");
            }
            else
            {
                PasswordHandler crypt = new PasswordHandler();
                string salt;
                string hash = crypt.Hash(password, out salt);

                Shell.RunCommandSQLAPI($"INSERT INTO Users VALUES ('{Guid.NewGuid().ToString()}', '{username}', '{email}', '{hash}', '{salt}', '')");
                Response.WriteAsync("<script>window.close();</script>");
            }

            return null;
        }

        public bool DoesExist(string username)
        {
            string response = Shell.RunCommandSQLAPI($"SELECT userID FROM Users WHERE username = {username}");

            if (string.IsNullOrEmpty(response) || string.IsNullOrWhiteSpace(response))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
