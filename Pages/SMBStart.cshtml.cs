using Chisato;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PersonalUniversalSite.Pages
{
    public class SMBStartModel : PageModel
    {
        public void OnGet(string failed)
        {
            if (failed == "failed")
            {
                ViewData["retry"] = "Login Failed";
            }
            else
            {
                ViewData["retry"] = "";
            }
        }

        public IActionResult OnPost()
        {
            //CredHolding.dataSource = Request.Form["dataSource"];
            //CredHolding.username = Request.Form["username"];
            //CredHolding.password = Request.Form["password"];

            var session = HttpContext.Session;
            session.SetString("dataSource", Request.Form["dataSource"]);
            session.SetString("username", Request.Form["username"]);
            session.SetString("password", Request.Form["password"]);

            return Redirect("/SMBConnection");
        }
    }

    public static class CredHolding
    {
        public static string dataSource;
        public static string username;
        public static string password;

        public static void ClearCreds(ISession session)
        {
            dataSource = string.Empty;
            username = string.Empty;
            password = string.Empty;

            session.Remove("dataSource");
            session.Remove("username");
            session.Remove("password");

            SMBHolding.smb = null;
        }
    }
}
