using Chisato;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace PersonalUniversalSite
{
    public class SitemapGenerator
    {
        private readonly HashSet<string> _visited = new();
        private readonly string baseUrl;

        public SitemapGenerator(string IbaseUrl)
        {
            baseUrl = IbaseUrl.TrimEnd('/');
        }

        public void GenerateSitemap(string wwwRootPath = "wwwroot", string pagesDirectory = "Pages")
        {
            string pagesPath = Path.Combine(Directory.GetCurrentDirectory(), pagesDirectory);
            var sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            foreach (var file in Directory.GetFiles(pagesPath, "*.cshtml", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(pagesPath.Length).Replace('\\', '/');

                // Skip partials and special files
                if (Path.GetFileName(file).StartsWith("_"))
                    continue;

                // Remove "/Index.cshtml" or ".cshtml" to get the route
                string urlPath = relativePath
                    .Replace("/Index.cshtml", "/")
                    .Replace(".cshtml", "");

                // Ensure it starts with "/"
                if (!urlPath.StartsWith("/"))
                    urlPath = "/" + urlPath;

                // Clean up double slashes
                urlPath = urlPath.Replace("//", "/");

                string fullUrl = baseUrl + urlPath;

                string year = File.GetLastWriteTime(Path.GetFullPath(file)).Year.ToString();
                string month = File.GetLastWriteTime(Path.GetFullPath(file)).Month.ToString();
                string day = File.GetLastWriteTime(Path.GetFullPath(file)).Day.ToString();

                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{fullUrl}</loc>");
                sb.AppendLine($"    <lastmod>{$"{year}-{month}-{day}"}</lastmod>");
                sb.AppendLine("  </url>");
            }

            sb.AppendLine("</urlset>");

            string sitemapPath = Path.Combine(wwwRootPath, "sitemap.xml");
            string newSitemap = sb.ToString();

            bool shouldWrite = true;

            if (File.Exists(sitemapPath))
            {
                string existingSitemap = File.ReadAllText(sitemapPath);
                if (existingSitemap == newSitemap)
                {
                    shouldWrite = false;
                    Console.WriteLine("Sitemap is up to date. No changes made.");
                }
            }

            if (shouldWrite)
            {
                File.WriteAllText(sitemapPath, newSitemap);
                Console.WriteLine($"Sitemap created/updated at: {sitemapPath}");
            }
        }

    }
}
