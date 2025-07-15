using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PersonalUniversalSite.Pages.Tools.Libraries
{
    public class LoginModel : PageModel
    {
        private readonly string accountsPath = Path.Combine("wwwroot", "Accounts", "Accounts.json");
        private string accountToLibraryPath = Path.Combine("wwwroot", "Libraries", "Manga", "accToLib.json");
        private string librariesPath = Path.Combine("wwwroot", "Libraries", "Manga", "Libraries.json");

        public void OnGet()
        {

        }

        [BindProperty]
        public string ErrorMessage { get; set; }

        public IActionResult OnPost(string username, string password)
        {
            JsonFileInitializer.EnsureAllLibraryFilesExist(accountsPath, accountToLibraryPath, librariesPath);

            var accounts = JsonHandler.DeserializeJsonFile<List<Account>>(accountsPath);

            var matchingAccount = accounts.FirstOrDefault(acc => acc.userName == username);
            if (matchingAccount == null)
            {
                ErrorMessage = "Invalid username or password.";
                return Page();
            }

            var hasher = new PasswordHandler();
            bool valid = hasher.CompareHash(password, matchingAccount.password, matchingAccount.salt);
            if (!valid)
            {
                ErrorMessage = "Invalid username or password.";
                return Page();
            }

            // ✅ Store user ID in session
            HttpContext.Session.SetString("loggedInAccountID", matchingAccount.ID);

            // Redirect to home/tools/whatever
            return RedirectToPage("/Tools/Libraries/MangaLibrary");
        }
    }
}
