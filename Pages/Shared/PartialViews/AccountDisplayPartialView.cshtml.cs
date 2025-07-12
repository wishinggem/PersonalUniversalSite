using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PersonalUniversalSite.Pages.Shared.PartialViews
{
    public class AccountDisplayPartialViewModel : PageModel
    {
        public void OnGet()
        {
            var session = HttpContext.Session;
            string username = session.GetString("account");
            ViewData["username"] = username;
            ViewData["email"] = session.GetString("email");
            if (!(string.IsNullOrEmpty(username) || string.IsNullOrWhiteSpace(username)))
            {
                ViewData["login"] = "true";
            }
            else
            {
                ViewData["login"] = "false";
            }
        }
    }
}
