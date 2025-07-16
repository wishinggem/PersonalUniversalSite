using Chisato;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace PersonalUniversalSite.Pages.Tools.Libraries
{
    public class MangaLibraryModel : PageModel
    {
        private readonly string mangaRootPath = Path.Combine("wwwroot", "Libraries", "Manga");
        private readonly string accountToLibraryPath = Path.Combine("wwwroot", "Libraries", "Manga", "accToLib.json");
        private readonly string librariesPath = Path.Combine("wwwroot", "Libraries", "Manga", "Libraries.json");
        private readonly string accountsPath = Path.Combine("wwwroot", "Libraries", "Manga", "Accounts.json");
        private readonly string coverRootPath = Path.Combine("wwwroot", "Libraries", "Manga", "Covers");
        private readonly string cachePath = Path.Combine("wwwroot", "Libraries", "Manga", "Cache.json");

        public string LoggedInUsername { get; private set; }
        private List<MangaEntry> mangaEntries = new();
        private string workingLibraryID = "";
        public bool IsMobile { get; private set; }

        public async Task OnGetAsync()
        {
            var session = HttpContext.Session;
            var userId = session.GetString("loggedInAccountID");

            var userAgent = Request.Headers["User-Agent"].ToString().ToLower();

            // Simple mobile check (you can improve this with a lib or regex)
            IsMobile = userAgent.Contains("mobile") || userAgent.Contains("android") || userAgent.Contains("iphone");

            if (string.IsNullOrEmpty(userId))
            {
                HttpContext.Response.Redirect("/Tools/Libraries/Login");
                return;
            }

            workingLibraryID = GetLibraryIdForUser(userId);
            mangaEntries = GetEntriesForLibrary(workingLibraryID) ?? new List<MangaEntry>();

            int count = 0;

            var tasks = mangaEntries.Select(m =>
            {
                return Task.Run(async () =>
                {
                    var timeout = TimeSpan.FromSeconds(10);

                    try
                    {
                        if (CheckForCache(ExtractMangaIdFromUrl(m.Link)))
                        {
                            CachedManga cachedManga = JsonHandler.DeserializeJsonFile<CachedMangaCollection>(cachePath).cache[ExtractMangaIdFromUrl(m.Link)];

                            string coverFilename = Path.GetFileName(new Uri(cachedManga.coverPhotoPath).LocalPath);
                            string fullCoverPath = Path.Combine(coverRootPath, coverFilename);
                            m.AltTitle = cachedManga.altTitle;
                            m.Genres = cachedManga.genres;
                            m.publicationStatus = cachedManga.publicationStatus;
                            m.pulishedChapterCount = cachedManga.pulishedChapterCount;
                            if (System.IO.File.Exists(fullCoverPath))
                            {
                                m.CoverImageUrl = cachedManga.coverPhotoPath;
                            }
                            else
                            {
                                if (Request.Host.Port != null)
                                {
                                    m.CoverImageUrl = $"https://{Request.Host.Host}:{Request.Host.Port}/MainStaticImages/Simple_Manga.png";
                                }
                                else
                                {
                                    m.CoverImageUrl = $"https://{Request.Host.Host}/MainStaticImages/Simple_Manga.png";
                                }
                                DeleteFromCache(m);
                            }
                        }
                        else
                        {
                            if (count < 40)
                            {
                                var mangaTask = Task.Run(async () =>
                                {
                                    string coverFilename = "";

                                    //add to cache
                                    if (System.IO.File.Exists(cachePath))
                                    {
                                        //work from
                                        bool fail = false;

                                        CachedManga cachedManga = new CachedManga();

                                        coverFilename = await GetMangaCoverUrlAsync(m);

                                        await DownloadFileAsync($"https://uploads.mangadex.org/covers/{ExtractMangaIdFromUrl(m.Link)}/{coverFilename}", coverRootPath);

                                        if (!string.IsNullOrEmpty(coverFilename))
                                        {
                                            if (Request.Host.Port != null)
                                            {
                                                cachedManga.coverPhotoPath = $"https://{Request.Host.Host}:{Request.Host.Port}/Libraries/Manga/Covers/{coverFilename}";
                                            }
                                            else
                                            {
                                                cachedManga.coverPhotoPath = $"https://{Request.Host.Host}/Libraries/Manga/Covers/{coverFilename}";
                                            }
                                        }
                                        else
                                        {
                                            fail = true;
                                        }
                                        var jsonData = await GetMangaJsonDataAsync(m.Link);
                                        if (jsonData == null)
                                        {
                                            fail = true;
                                        }
                                        m.MangaJsonData = jsonData;
                                        cachedManga.altTitle = ExtractAndSetAltTitle(m);
                                        cachedManga.genres = ExtractGenresFromJson(m);
                                        cachedManga.publicationStatus = ExtractStatusFromJson(m);
                                        var publishedCount = await GetAndSetPublishEnglishChapters(m);
                                        if (publishedCount == -1)
                                        {
                                            fail = true;
                                            publishedCount = 0;
                                        }
                                        cachedManga.pulishedChapterCount = publishedCount;
                                        cachedManga.dateAdded = DateTime.Now;
                                        cachedManga.managaId = ExtractMangaIdFromUrl(m.Link);

                                        if (!fail)
                                        {
                                            //add to cache
                                            CachedMangaCollection cachedMangaCollection = JsonHandler.DeserializeJsonFile<CachedMangaCollection>(cachePath);
                                            cachedMangaCollection.cache.Add(ExtractMangaIdFromUrl(m.Link), cachedManga);
                                            JsonHandler.SerializeJsonFile(cachePath, cachedMangaCollection);
                                        }
                                    }
                                    else
                                    {
                                        //create new
                                        bool fail = false;

                                        CachedManga cachedManga = new CachedManga();

                                        coverFilename = await GetMangaCoverUrlAsync(m);

                                        await DownloadFileAsync($"https://uploads.mangadex.org/covers/{ExtractMangaIdFromUrl(m.Link)}/{coverFilename}", coverRootPath);

                                        if (!string.IsNullOrEmpty(coverFilename))
                                        {
                                            if (Request.Host.Port != null)
                                            {
                                                cachedManga.coverPhotoPath = $"https://{Request.Host.Host}:{Request.Host.Port}/Libraries/Manga/Covers/{coverFilename}";
                                            }
                                            else
                                            {
                                                cachedManga.coverPhotoPath = $"https://{Request.Host.Host}/Libraries/Manga/Covers/{coverFilename}";
                                            }
                                        }
                                        else
                                        {
                                            fail = true;
                                        }
                                        var jsonData = await GetMangaJsonDataAsync(m.Link);
                                        if (jsonData == null)
                                        {
                                            fail = true;
                                        }
                                        m.MangaJsonData = jsonData;
                                        cachedManga.altTitle = ExtractAndSetAltTitle(m);
                                        cachedManga.genres = ExtractGenresFromJson(m);
                                        cachedManga.publicationStatus = ExtractStatusFromJson(m);
                                        var publishedCount = await GetAndSetPublishEnglishChapters(m);
                                        if (publishedCount == -1)
                                        {
                                            fail = true;
                                            publishedCount = 0;
                                        }
                                        cachedManga.pulishedChapterCount = publishedCount;
                                        cachedManga.dateAdded = DateTime.Now;
                                        cachedManga.managaId = ExtractMangaIdFromUrl(m.Link);

                                        if (!fail)
                                        {
                                            //add to cache
                                            CachedMangaCollection cachedMangaCollection = new CachedMangaCollection();
                                            cachedMangaCollection.cache.Add(ExtractMangaIdFromUrl(m.Link), cachedManga);
                                            JsonHandler.SerializeJsonFile(cachePath, cachedMangaCollection);
                                        }
                                    }
                                });

                                var completed = await Task.WhenAny(mangaTask, Task.Delay(timeout));

                                if (completed != mangaTask)
                                {
                                    // Timeout fallback
                                    SetMangaDefaults(m);
                                }
                            }
                            else
                            {
                                //exceeded rate limit
                                SetMangaDefaults(m);
                            }

                            count++;
                        }
                    }
                    catch
                    {
                        SetMangaDefaults(m);
                    }
                });
            });

            await Task.WhenAll(tasks);

            ViewData["manga"] = mangaEntries ?? new List<MangaEntry>();

            // Load username
            var accounts = JsonHandler.DeserializeJsonFile<List<Account>>(accountsPath);
            LoggedInUsername = accounts.FirstOrDefault(a => a.ID == userId)?.userName ?? "Unknown";
            ViewData["LoggedInUsername"] = LoggedInUsername;
        }

        private void SetMangaDefaults(MangaEntry m)
        {
            m.CoverImageUrl = "/MainStaticImages/Simple_Manga.png"; // or null
            m.AltTitle = "unknown";
            m.Genres = new List<string>();
            m.publicationStatus = "unknown";
        }

        public IActionResult OnPostAddMangaByUrl(string Link)
        {
            if (string.IsNullOrWhiteSpace(Link))
            {
                ModelState.AddModelError(string.Empty, "URL is required.");
                return Page();
            }

            if (!Uri.TryCreate(Link, UriKind.Absolute, out var uri) || !uri.Host.Contains("mangadex.org"))
            {
                ModelState.AddModelError(string.Empty, "Please enter a valid MangaDex URL.");
                return Page();
            }

            // Example URL: https://mangadex.org/title/a1526a66-5dbf-4fef-9e7b-5f8f48b2ca93/asagiiro-no-saudade
            // We want to get 'asagiiro-no-saudade' from the path segments

            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length < 3 || segments[0] != "title")
            {
                ModelState.AddModelError(string.Empty, "URL format is not recognized.");
                return Page();
            }

            // The title slug is the last segment
            var titleSlug = segments[^1]; // ^1 means last element

            if (string.IsNullOrWhiteSpace(titleSlug))
            {
                ModelState.AddModelError(string.Empty, "Could not extract title from the URL.");
                return Page();
            }

            // Optionally: Replace hyphens with spaces and capitalize words for nicer display
            string title = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(titleSlug.Replace('-', ' '));

            var userId = HttpContext.Session.GetString("loggedInAccountID");
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Tools/Libraries/Login");

            var accToLib = JsonHandler.DeserializeJsonFile<AccountToLibraries>(accountToLibraryPath);
            var libraries = JsonHandler.DeserializeJsonFile<LibraryFromID>(librariesPath);

            if (!accToLib.link.TryGetValue(userId, out var libraryId))
                return RedirectToPage();

            if (!libraries.collection.TryGetValue(libraryId, out var library))
                return RedirectToPage();

            library.entries ??= new List<MangaEntry>();

            // Avoid duplicates by title or link
            if (!library.entries.Any(m => m.Title == title || m.Link == Link))
            {
                library.entries.Add(new MangaEntry
                {
                    Title = title,
                    Link = Link,
                    Status = "PlanToRead",
                    CoverImageUrl = null
                });

                libraries.collection[libraryId] = library;
                JsonHandler.SerializeJsonFile(librariesPath, libraries);
            }

            return RedirectToPage();
        }

        public IActionResult OnPostAddManga(string title, string link)
        {
            string userId = HttpContext.Session.GetString("loggedInAccountID");
            if (string.IsNullOrEmpty(userId)) return RedirectToPage();

            workingLibraryID = GetLibraryIdForUser(userId);
            if (string.IsNullOrEmpty(workingLibraryID)) return RedirectToPage();

            var allLibraries = JsonHandler.DeserializeJsonFile<LibraryFromID>(librariesPath);
            if (!allLibraries.collection.TryGetValue(workingLibraryID, out var library))
                return RedirectToPage();

            library.entries ??= new List<MangaEntry>();

            if (!library.entries.Any(e => e.Title == title))
            {
                library.entries.Add(new MangaEntry
                {
                    Title = title,
                    Link = link,
                    Status = "PlanToRead"
                });

                allLibraries.collection[workingLibraryID] = library;
                JsonHandler.SerializeJsonFile(librariesPath, allLibraries);
            }

            return RedirectToPage();
        }

        public IActionResult OnPostUpdateStatus(string title, string status)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(status))
                return RedirectToPage();

            var userId = HttpContext.Session.GetString("loggedInAccountID");
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Tools/Libraries/Login");

            var accToLib = JsonHandler.DeserializeJsonFile<AccountToLibraries>(accountToLibraryPath);
            if (!accToLib.link.TryGetValue(userId, out workingLibraryID))
                return RedirectToPage();

            var libraries = JsonHandler.DeserializeJsonFile<LibraryFromID>(librariesPath);
            if (!libraries.collection.TryGetValue(workingLibraryID, out var library))
                return RedirectToPage();

            library.entries ??= new List<MangaEntry>();

            var manga = library.entries.FirstOrDefault(m => m.Title == title);
            if (manga != null)
            {
                manga.Status = status;
                libraries.collection[workingLibraryID] = library;

                // Backup before saving
                System.IO.File.Copy(librariesPath, librariesPath + ".bak", overwrite: true);
                JsonHandler.SerializeJsonFile(librariesPath, libraries);
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDeleteManga(string title)
        {
            var userId = HttpContext.Session.GetString("loggedInAccountID");
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Tools/Libraries/Login");

            var accToLib = JsonHandler.DeserializeJsonFile<AccountToLibraries>(accountToLibraryPath);
            var libraries = JsonHandler.DeserializeJsonFile<LibraryFromID>(librariesPath);

            if (!accToLib.link.TryGetValue(userId, out var libraryId))
                return RedirectToPage();

            if (!libraries.collection.TryGetValue(libraryId, out var library))
                return RedirectToPage();

            library.entries ??= new List<MangaEntry>();
            var mangaToRemove = library.entries.FirstOrDefault(m => m.Title == title);
            if (mangaToRemove != null)
            {
                library.entries.Remove(mangaToRemove);
                libraries.collection[libraryId] = library;
                JsonHandler.SerializeJsonFile(librariesPath, libraries);
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDeleteAccount()
        {
            var userId = HttpContext.Session.GetString("loggedInAccountID");
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Tools/Libraries/Login");

            var accounts = JsonHandler.DeserializeJsonFile<List<Account>>(accountsPath);
            var accToLib = JsonHandler.DeserializeJsonFile<AccountToLibraries>(accountToLibraryPath);
            var libraries = JsonHandler.DeserializeJsonFile<LibraryFromID>(librariesPath);

            accounts.RemoveAll(a => a.ID == userId);
            accToLib.link.Remove(userId);
            libraries.collection.Remove(userId);

            JsonHandler.SerializeJsonFile(accountsPath, accounts);
            JsonHandler.SerializeJsonFile(accountToLibraryPath, accToLib);
            JsonHandler.SerializeJsonFile(librariesPath, libraries);

            HttpContext.Session.Clear();
            return RedirectToPage("/Tools/Libraries/Login");
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.SetString("loggedInAccountID", "");
            return RedirectToPage("/Tools/Libraries/Login");
        }

        private string GetLibraryIdForUser(string userId)
        {
            try
            {
                var map = JsonHandler.DeserializeJsonFile<AccountToLibraries>(accountToLibraryPath);
                return map.link.TryGetValue(userId, out var id) ? id : "";
            }
            catch
            {
                HttpContext.Response.Redirect("/Tools/Libraries/Login");
                return null;
            }
        }

        private List<MangaEntry> GetEntriesForLibrary(string libraryID)
        {
            try
            {
                var data = JsonHandler.DeserializeJsonFile<LibraryFromID>(librariesPath);

                if (data.collection.TryGetValue(libraryID, out var lib))
                {
                    lib.entries ??= new List<MangaEntry>();
                    return lib.entries;
                }

                return new List<MangaEntry>();
            }
            catch
            {
                return new List<MangaEntry>(); // ✅ Fallback instead of null
            }
        }

        public async Task<string> GetMangaCoverUrlAsync(MangaEntry manga)
        {
            string defaultImageUrl = "/MainStaticImages/Simple_Manga.png";
            string mangaDexUrl = manga.Link;
            string coverFilename = "";

            try
            {
                var uri = new Uri(mangaDexUrl);
                var segments = uri.Segments;
                if (segments.Length < 3)
                    return defaultImageUrl;

                string mangaId = segments[2].TrimEnd('/');
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                var queryParams = new Dictionary<string, string>
                {
                    ["limit"] = "10",
                    ["manga[]"] = mangaId,
                    ["order[createdAt]"] = "asc"
                };

                var query = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                var response = await client.GetStringAsync($"https://api.mangadex.org/cover?{query}");

                var json = JObject.Parse(response);
                var first = json["data"]?.FirstOrDefault();
                var fileName = first?["attributes"]?["fileName"]?.ToString();

                if (!string.IsNullOrEmpty(fileName))
                {
                    coverFilename = fileName;
                }
                else
                {
                    coverFilename = "404";
                }

                manga.CoverImageUrl = string.IsNullOrEmpty(fileName)
                    ? defaultImageUrl
                    : $"https://uploads.mangadex.org/covers/{mangaId}/{fileName}";
                return coverFilename;
            }
            catch
            {
                return defaultImageUrl;
            }
        }

        public async Task<JObject?> GetMangaJsonDataAsync(string mangaDexUrl)
        {
            try
            {
                var uri = new Uri(mangaDexUrl);
                var segments = uri.AbsolutePath.Trim('/').Split('/');

                if (segments.Length < 2 || segments[0] != "title")
                    return null;

                string mangaId = segments[1];

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                string url = $"https://api.mangadex.org/manga/{mangaId}";
                var response = await client.GetStringAsync(url);

                return JObject.Parse(response);
            }
            catch
            {
                return null;
            }
        }

        public string ExtractAndSetAltTitle(MangaEntry manga)
        {
            if (manga.MangaJsonData == null)
            {
                manga.AltTitle = null;
                return null;
            }

            var attr = manga.MangaJsonData["data"]?["attributes"];
            if (attr == null)
            {
                manga.AltTitle = null;
                return null;
            }

            var altTitles = attr["altTitles"] as JArray;
            if (altTitles == null || altTitles.Count == 0)
            {
                manga.AltTitle = null;
                return null;
            }

            // Try to get English alt title first
            var enAlt = altTitles.FirstOrDefault(t => t["en"] != null);
            if (enAlt != null)
            {
                manga.AltTitle = enAlt["en"]?.ToString();
                return enAlt["en"]?.ToString();
            }

            // Try Japanese romanized alt title next
            var jaRoAlt = altTitles.FirstOrDefault(t => t["ja-ro"] != null);
            if (jaRoAlt != null)
            {
                manga.AltTitle = jaRoAlt["ja-ro"]?.ToString();
                return jaRoAlt["ja-ro"]?.ToString();
            }

            // Fallback to first alt title available
            manga.AltTitle = altTitles[0].First?.ToString();
            return altTitles[0].First?.ToString();
        }

        public List<string> ExtractGenresFromJson(MangaEntry manga)
        {
            if (manga.MangaJsonData == null)
            {
                manga.Genres = new List<string>();
                return null;
            }

            var genres = manga.MangaJsonData["data"]?["attributes"]?["tags"]?
                .Where(t => t["attributes"]?["group"]?.ToString() == "genre")
                .Select(t => t["attributes"]?["name"]?["en"]?.ToString())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            if (genres != null)
                manga.Genres = genres;
            return genres;
        }

        public string ExtractStatusFromJson(MangaEntry manga)
        {
            if (manga.MangaJsonData == null)
            {
                manga.publicationStatus = "unknown";
                return "unknown";
            }

            var status = manga.MangaJsonData["data"]?["attributes"]?["status"]?.ToString();

            if (!string.IsNullOrEmpty(status))
            {
                manga.publicationStatus = status; // or a separate property if you want to keep the original Status for your UI logic
                return status;
            }
            else
            {
                return null;
            }
        }

        public IActionResult OnPostChangePassword(string newPassword)
        {
            var session = HttpContext.Session;
            var userId = session.GetString("loggedInAccountID");

            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Tools/Libraries/Login");

            var accounts = JsonHandler.DeserializeJsonFile<List<Account>>(accountsPath);
            var account = accounts.FirstOrDefault(a => a.ID == userId);

            if (account != null)
            {
                var hasher = new PasswordHandler();
                string hashed = hasher.Hash(newPassword, out string salt);

                account.password = hashed;
                account.salt = salt;

                JsonHandler.SerializeJsonFile(accountsPath, accounts);
            }

            return RedirectToPage(); // reload page
        }

        public string ExtractMangaIdFromUrl(string mangaDexUrl)
        {
            try
            {
                var uri = new Uri(mangaDexUrl);
                var segments = uri.Segments;
                if (segments.Length < 3)
                    return string.Empty;

                return segments[2].TrimEnd('/');
            }
            catch
            {
                return string.Empty;
            }
        }

        public bool CheckForCache(string mangaID)
        {
            CachedMangaCollection cacheFile = JsonHandler.DeserializeJsonFile<CachedMangaCollection>(cachePath);
            if (System.IO.File.Exists(cachePath))
            {
                if (cacheFile.cache.ContainsKey(mangaID))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public async Task<string?> DownloadFileAsync(string url, string destinationDirectory)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(destinationDirectory))
                    return null;

                Uri uri = new Uri(url);
                string fileName = Path.GetFileName(uri.LocalPath);

                // Ensure directory exists
                Directory.CreateDirectory(destinationDirectory);

                string destinationPath = Path.Combine(destinationDirectory, fileName);

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                byte[] fileBytes = await client.GetByteArrayAsync(uri);

                await System.IO.File.WriteAllBytesAsync(destinationPath, fileBytes);

                return destinationPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download file: {ex.Message}");
                return null;
            }
        }

        public void DeleteFromCache(MangaEntry manga)
        {
            if (System.IO.File.Exists(cachePath))
            {
                System.IO.File.Delete(cachePath);
            }

            if (!Directory.Exists(coverRootPath))
                return;

            var cache = JsonHandler.DeserializeJsonFile<CachedMangaCollection>(cachePath);
            if (cache.cache.ContainsKey(ExtractMangaIdFromUrl(manga.Link)))
            {
                cache.cache.Remove(ExtractMangaIdFromUrl(manga.Link));
            }
        }

        public IActionResult OnPostToggleFav(string title)
        {
            if (string.IsNullOrEmpty(title))
                return RedirectToPage();

            var userId = HttpContext.Session.GetString("loggedInAccountID");
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Tools/Libraries/Login");

            var accToLib = JsonHandler.DeserializeJsonFile<AccountToLibraries>(accountToLibraryPath);
            if (!accToLib.link.TryGetValue(userId, out workingLibraryID))
                return RedirectToPage();

            var libraries = JsonHandler.DeserializeJsonFile<LibraryFromID>(librariesPath);
            if (!libraries.collection.TryGetValue(workingLibraryID, out var library))
                return RedirectToPage();

            library.entries ??= new List<MangaEntry>();

            var manga = library.entries.FirstOrDefault(m => m.Title == title);
            if (manga != null)
            {
                if (!manga.favourite)
                {
                    manga.favourite = true;
                }
                else
                {
                    manga.favourite = false;
                }
                libraries.collection[workingLibraryID] = library;

                // Backup before saving
                System.IO.File.Copy(librariesPath, librariesPath + ".bak", overwrite: true);
                JsonHandler.SerializeJsonFile(librariesPath, libraries);
            }

            return RedirectToPage();
        }

        public async Task<int> GetAndSetPublishEnglishChapters(MangaEntry manga)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            // Only need metadata for counting, not full chapter list
            var url = $"https://api.mangadex.org/manga/{ExtractMangaIdFromUrl(manga.Link)}/feed?translatedLanguage[]=en&limit=1";

            try
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("total", out var totalElement))
                {
                    manga.pulishedChapterCount = totalElement.GetInt32();
                    return totalElement.GetInt32(); // number of EN chapters
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch chapter count: {ex.Message}");
            }

            manga.pulishedChapterCount = 0;
            return -1; // fallback on error
        }

        public IActionResult OnPostInramentReadChapters(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return RedirectToPage();
            }

            var userId = HttpContext.Session.GetString("loggedInAccountID");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Tools/Libraries/Login");
            }

            var accToLib = JsonHandler.DeserializeJsonFile<AccountToLibraries>(accountToLibraryPath);
            if (!accToLib.link.TryGetValue(userId, out workingLibraryID))
            {
                return RedirectToPage();
            }

            var libraries = JsonHandler.DeserializeJsonFile<LibraryFromID>(librariesPath);
            if (!libraries.collection.TryGetValue(workingLibraryID, out var library))
            {
                return RedirectToPage();
            }

            library.entries ??= new List<MangaEntry>();

            var manga = library.entries.FirstOrDefault(m => m.Title == title);
            if (manga != null)
            {
                manga.chaptersRead++;

                // Backup before saving
                System.IO.File.Copy(librariesPath, librariesPath + ".bak", overwrite: true);
                JsonHandler.SerializeJsonFile(librariesPath, libraries);
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDecrimentReadChapters(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return RedirectToPage();
            }

            var userId = HttpContext.Session.GetString("loggedInAccountID");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Tools/Libraries/Login");
            }

            var accToLib = JsonHandler.DeserializeJsonFile<AccountToLibraries>(accountToLibraryPath);
            if (!accToLib.link.TryGetValue(userId, out workingLibraryID))
            {
                return RedirectToPage();
            }

            var libraries = JsonHandler.DeserializeJsonFile<LibraryFromID>(librariesPath);
            if (!libraries.collection.TryGetValue(workingLibraryID, out var library))
            {
                return RedirectToPage();
            }

            library.entries ??= new List<MangaEntry>();

            var manga = library.entries.FirstOrDefault(m => m.Title == title);
            if (manga != null)
            {
                manga.chaptersRead--;

                // Backup before saving
                System.IO.File.Copy(librariesPath, librariesPath + ".bak", overwrite: true);
                JsonHandler.SerializeJsonFile(librariesPath, libraries);
            }

            return RedirectToPage();
        }

        public IActionResult OnPostUpdateReadChapters(string title, int readChapters, int maxChapters)
        {
            if (string.IsNullOrEmpty(title))
            {
                return RedirectToPage();
            }

            var userId = HttpContext.Session.GetString("loggedInAccountID");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Tools/Libraries/Login");
            }

            var accToLib = JsonHandler.DeserializeJsonFile<AccountToLibraries>(accountToLibraryPath);
            if (!accToLib.link.TryGetValue(userId, out workingLibraryID))
            {
                return RedirectToPage();
            }

            var libraries = JsonHandler.DeserializeJsonFile<LibraryFromID>(librariesPath);
            if (!libraries.collection.TryGetValue(workingLibraryID, out var library))
            {
                return RedirectToPage();
            }

            library.entries ??= new List<MangaEntry>();

            var manga = library.entries.FirstOrDefault(m => m.Title == title);
            if (manga != null)
            {
                manga.chaptersRead = Math.Clamp(readChapters, 0, maxChapters);

                // Backup before saving
                System.IO.File.Copy(librariesPath, librariesPath + ".bak", overwrite: true);
                JsonHandler.SerializeJsonFile(librariesPath, libraries);
            }

            return RedirectToPage();
        }
    }
}

[Serializable]
public class MangaEntry
{
    public string Title { get; set; }
    public string Link { get; set; }
    public string Status { get; set; }
    public int chaptersRead { get; set; } = 0;
    public bool favourite { get; set; } = false;
    [JsonIgnore]
    public string CoverImageUrl { get; set; }
    [JsonIgnore]
    public JObject? MangaJsonData { get; set; }
    [JsonIgnore]
    public string? AltTitle { get; set; }
    [JsonIgnore]
    public List<string> Genres { get; set; } = new List<string>();
    [JsonIgnore]
    public string? publicationStatus { get; set; }
    [JsonIgnore]
    public int pulishedChapterCount { get; set; }
}

[Serializable]
public class CachedManga
{
    public string managaId { get; set; }
    public string altTitle { get; set; }
    public List<string> genres { get; set; } = new List<string>();
    public string publicationStatus { get; set; }
    public string coverPhotoPath { get; set; }
    public int pulishedChapterCount { get; set; }
    public DateTime dateAdded { get; set; } = DateTime.Now;
}

[Serializable]
public class CachedMangaCollection
{
    public Dictionary<string, CachedManga> cache; //mangaID

    public CachedMangaCollection()
    {
        cache = new Dictionary<string, CachedManga>();
    }
}

[Serializable]
public class AccountToLibraries
{
    public Dictionary<string, string> link;
}

[Serializable]
public class LibraryFromID
{
    public Dictionary<string, Library> collection;
}

[Serializable]
public class Library
{
    public List<MangaEntry> entries;
}

[Serializable]
public class Account
{
    public string ID;
    public string email;
    public string userName;
    public string password;
    public string salt;
}






public static class JsonFileInitializer
{
    public static void EnsureJsonFile<T>(string path, T defaultContent)
    {
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            JsonHandler.SerializeJsonFile(path, defaultContent);
        }
    }

    public static void EnsureAllLibraryFilesExist(string accountsPath, string accToLibPath, string librariesPath)
    {
        EnsureJsonFile(accountsPath, new List<Account>());
        EnsureJsonFile(accToLibPath, new AccountToLibraries { link = new Dictionary<string, string>() });
        EnsureJsonFile(librariesPath, new LibraryFromID { collection = new Dictionary<string, Library>() });
    }
}

public static class JsonHandler
{
    public static void SerializeJsonFile<T>(string filePath, T obj, bool append = false)
    {
        using var writer = new StreamWriter(filePath, append);
        writer.Write(JsonConvert.SerializeObject(obj));
    }

    public static T DeserializeJsonFile<T>(string filePath) where T : new()
    {
        if (!System.IO.File.Exists(filePath))
            return new T();

        using var reader = new StreamReader(filePath);
        return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
    }
}

public class PasswordHandler()
{
    public string Hash(string password, out string salt)
    {
        byte[] saltBytes = new byte[16];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(saltBytes);
        }
        salt = Convert.ToBase64String(saltBytes);

        string saltedPassword = string.Concat(password, saltBytes);
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            string hash = Convert.ToBase64String(hashBytes);
            return hash;
        }
    }

    public string HashWithSalt(string password, string salt)
    {
        string saltedPassword = string.Concat(password, salt);
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            string hash = Convert.ToBase64String(hashBytes);
            return hash;
        }
    }

    public bool CompareHash(string password, string existingHash, string salt)
    {
        string saltedPassword = String.Concat(password, Convert.FromBase64String(salt));

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            string newHash = Convert.ToBase64String(hashBytes);
            // Compare the new hash with the existing hash
            return newHash == existingHash;
        }
    }
}
