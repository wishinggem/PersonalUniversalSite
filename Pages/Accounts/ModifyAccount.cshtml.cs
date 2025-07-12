using Chisato.Shell;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonalUniversalSite.Pages.Accounts;

namespace PersonalUniversalSite.Pages.Accounts
{
    public class ModifyAccountModel : PageModel
    {
        public void OnGet(string error)
        {
            if (error == "userExists")
            {
                ViewData["error"] = "Couldnt Change Username: New Username Already Exists";
            }
            else
            {
                ViewData["error"] = "";
            }
        }

        public IActionResult OnPostModify()
        {
            string username = Request.Form["username"];
            string email = Request.Form["email"];
            string info = Request.Form["info"];

            var session = HttpContext.Session;
            string oldUsername = session.GetString("account");
            string userID = Shell.RunCommandSQLAPI($"SELECT userID FROM Users WHERE username = {oldUsername}");
            if (DoesExist(username))
            {
                Shell.RunCommandSQLAPI($"UPDATE Users SET username = '{oldUsername}', email = '{email}', info = '{info}' WHERE userID = '{userID}'");
                Logout();
                SignIn(oldUsername);
                return Redirect("/Accounts/ModifyAccount?error=userExists");
            }
            else
            {
                Shell.RunCommandSQLAPI($"UPDATE Users SET username = '{username}', email = '{email}', info = '{info}' WHERE userID = '{userID}'");
                Logout();
                SignIn(username);
                return Redirect("/Accounts/ModifyAccount");
            }
        }

        private void Logout()
        {
            var session = HttpContext.Session;
            session.SetString("account", "");
            session.SetString("email", "");
            session.SetString("login", "false");
        }

        private void SignIn(string username)
        {
            string email = Shell.RunCommandSQLAPI($"SELECT email FROM Users WHERE username = '{username}'");
            string info = Shell.RunCommandSQLAPI($"SELECT info FROM Users WHERE username = '{username}'");

            var session = HttpContext.Session;
            session.SetString("account", username);
            session.SetString("email", email);
            session.SetString("login", "true");
            session.SetString("info", info);
        }

        private bool DoesExist(string username)
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
