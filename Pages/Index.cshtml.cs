using Chisato;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PersonalUniversalSite.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            WWWRootFolderFind.SetPath();

            try
            {
                if (System.IO.File.Exists("wwwroot/sitemap.xml"))
                {
                    if ((DateTime.Now - System.IO.File.GetLastWriteTime("wwwroot/sitemap.xml")).TotalDays > 7)
                    {
                        Console.WriteLine("Sitemap last updated over 7 days ago creating...");
                        new SitemapGenerator("https://chinatsuservices.ddns.net").GenerateSitemap();
                        System.IO.File.SetLastWriteTime("wwwroot/sitemap.xml", DateTime.Now);
                    }
                }
                else
                {
                    Console.WriteLine("Sitemap doesnt exist creating...");
                    new SitemapGenerator("https://chinatsuservices.ddns.net").GenerateSitemap();
                }
            }
            catch (Exception ex) 
            { 
                
            }
        }
    }
}
