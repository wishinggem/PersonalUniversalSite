using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PersonalUniversalSite.Pages.Tools.Libraries
{
    public class RegisterModel : PageModel
    {
        private readonly string accountsPath = Path.Combine("wwwroot", "Accounts", "Accounts.json");
        private readonly string librariesPath = Path.Combine("wwwroot", "Libraries", "Manga", "Libraries.json");
        private readonly string accToLibPath = Path.Combine("wwwroot", "Libraries", "Manga", "accToLib.json");

        [BindProperty]
        public string ErrorMessage { get; set; }

        public void OnGet()
        {

        }

        public IActionResult OnPost(string username, string password)
        {
            JsonFileInitializer.EnsureAllLibraryFilesExist(accountsPath, accToLibPath, librariesPath);

            var accounts = JsonHandler.DeserializeJsonFile<List<Account>>(accountsPath);
            var accToLib = JsonHandler.DeserializeJsonFile<AccountToLibraries>(accToLibPath);
            var libraries = JsonHandler.DeserializeJsonFile<LibraryFromID>(librariesPath);

            // Prevent duplicate usernames
            if (accounts.Any(a => a.userName == username))
            {
                ErrorMessage = "That username is already taken.";
                return Page();
            }

            // Generate new account
            string newID = Guid.NewGuid().ToString();
            var hasher = new PasswordHandler();
            string hashed = hasher.Hash(password, out string salt);

            var newAccount = new Account
            {
                ID = newID,
                userName = username,
                password = hashed,
                salt = salt
            };

            // Add account
            accounts.Add(newAccount);
            JsonHandler.SerializeJsonFile(accountsPath, accounts);

            // Add account → library ID mapping
            accToLib.link[newID] = newID;
            JsonHandler.SerializeJsonFile(accToLibPath, accToLib);

            // Create empty library for the user
            libraries.collection[newID] = new Library { entries = new List<MangaEntry>() };
            JsonHandler.SerializeJsonFile(librariesPath, libraries);

            // Log the user in
            HttpContext.Session.SetString("loggedInAccountID", newID);

            return RedirectToPage("/Tools/Libraries/MangaLibrary");
        }
    }
}