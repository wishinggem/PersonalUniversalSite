using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace PersonalUniversalSite.Pages.Tools.Libraries
{
    public class RegisterModel : PageModel
    {
        private readonly string accountsPath = Path.Combine("wwwroot", "Libraries", "Manga", "Accounts.json");
        private readonly string librariesPath = Path.Combine("wwwroot", "Libraries", "Manga", "Libraries.json");
        private readonly string accToLibPath = Path.Combine("wwwroot", "Libraries", "Manga", "accToLib.json");

        [BindProperty]
        public string ErrorMessage { get; set; }

        public void OnGet()
        {

        }

        public IActionResult OnPost(string username, string password, string email)
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

            if (accounts.Any(a => a.email == email))
            {
                ErrorMessage = "That email is already in use.";
                return Page();
            }

            if (!CheckValidEmailFormat(email))
            {
                ErrorMessage = "That email is in an incorrect format.";
                return Page();
            }

            // Generate new account
            string newID = Guid.NewGuid().ToString();
            var hasher = new PasswordHandler();
            string hashed = hasher.Hash(password, out string salt);

            var newAccount = new Account
            {
                ID = newID,
                email = email,
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

        private bool CheckValidEmailFormat(string email)
        {
            Regex emailRegex = new Regex(@"(?:[a-z0-9!#$%&'*+\/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+\/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9]))\.){3}(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9])|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])");
            if (emailRegex.IsMatch(email))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}