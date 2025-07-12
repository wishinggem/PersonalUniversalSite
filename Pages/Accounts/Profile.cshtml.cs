using Chisato.Shell;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PersonalUniversalSite.Pages.Accounts
{
    public class ProfileModel : PageModel
    {
        public void OnGet()
        {
            
        }

        public IActionResult OnPostLogout()
        {
            var session = HttpContext.Session;
            session.SetString("account", "");
            session.SetString("email", "");
            session.SetString("login", "false");
            return Redirect("Index");
        }

        public IActionResult OnPostDelete()
        {
            var session = HttpContext.Session;
            string username = session.GetString("account");
            Shell.RunCommandSQLAPI($"DELETE Users WHERE username = '{username}'");
            return OnPostLogout();
        }

        public IActionResult OnPostModify()
        {
            return Redirect("/Accounts/ModifyAccount");
        }
    }
}
