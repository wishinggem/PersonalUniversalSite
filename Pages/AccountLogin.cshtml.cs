using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Diagnostics;
using Chisato;
using System.Security.Cryptography;
using System.Text;
using Chisato.Shell;

namespace PersonalUniversalSite.Pages
{
    public class AccountLoginModel : PageModel
    {
        public void OnGet(string fail)
        {
            if (fail == "true")
            {
                ViewData["fail"] = "Could Not Login: null data";
            }
            else if (fail == "unauth")
            {
                ViewData["fail"] = "Could Not Login: Username or Password Incorect";
            }
        }

        public IActionResult OnPost()
        {
            string username = Request.Form["username"];
            string password = Request.Form["password"];

            string hash = Shell.RunCommandSQLAPI($"SELECT password FROM Users WHERE username = '{username}'");
            string salt = Shell.RunCommandSQLAPI($"SELECT salt FROM Users WHERE username = '{username}'");
            string email = Shell.RunCommandSQLAPI($"SELECT email FROM Users WHERE username = '{username}'");
            string info = Shell.RunCommandSQLAPI($"SELECT info FROM Users WHERE username = '{username}'");

            if (string.IsNullOrEmpty(hash) || string.IsNullOrWhiteSpace(hash) || string.IsNullOrEmpty(salt) || string.IsNullOrWhiteSpace(salt))
            {
                return Redirect("AccountLogin?fail=unauth");
            }
            else
            {
                PasswordHandler crypt = new PasswordHandler();
                if (crypt.CompareHash(password, hash, salt))
                {
                    var session = HttpContext.Session;
                    session.SetString("account", username);
                    session.SetString("email", email);
                    session.SetString("login", "true");
                    session.SetString("info", info);
                    session.SetString("auth", password);

                    UserAccount.username = username;
                    UserAccount.email = email;
                    return Redirect("Index");
                }
                else
                {
                    return Redirect("AccountLogin?fail=unauth");
                }
            }
        }
    }

    public static class UserAccount
    {
        public static string username;
        public static string email;
    }
}
