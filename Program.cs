using Chisato;
using PersonalUniversalSite.Pages;
using System.Diagnostics;
using System.Net;

namespace PersonalUniversalSite
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60); // Set session timeout
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true; // Make the session cookie essential
            });

            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Add services to the container.
            builder.Services.AddRazorPages();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions
            {
                ServeUnknownFileTypes = true,
                DefaultContentType = "application/octet-stream",
                OnPrepareResponse = ctx =>
                {
                    if (ctx.File.Name.EndsWith(".glb"))
                    {
                        ctx.Context.Response.ContentType = "model/gltf-binary";
                    }
                }
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseSession();

            app.MapRazorPages();

            app.Run();
        }
    }
}

namespace Chisato
{
    using global::System.Diagnostics;
    using System.IO;

    public static class VisualStudioProvider
    {
        public static DirectoryInfo TryGetPageDirectoryInfo()
        {
            /*var directory = new DirectoryInfo(
                currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }*/

            DirectoryInfo directory = new DirectoryInfo(WWWRootFolderFind.path);
            return directory;
        }
    }

    public static class PathHandler
    {
        public static string HandleInputSpace(string input)
        {
            if (input != null)
            {
                if (input.Contains(" "))
                {
                    return input.Replace(" ", "_");
                }
                else
                {
                    return input;
                }
            }
            else
            {
                return string.Empty;
            }
        }

        public static string HandleOutputSpace(string output)
        {
            if (output != null)
            {
                if (output.Contains("_"))
                {
                    return output.Replace("_", " ");
                }
                else
                {
                    return output;
                }
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public class WWWRootFolderFind
    {
        public static string path;

        public static void SetPath(string currentPath = null)
        {
            var directory = new DirectoryInfo(
                currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetDirectories("wwwroot").Any())
            {
                directory = directory.Parent;
            }
            path = directory.FullName;
        }
    }

    public static class ListExtentions
    {
        public static void Shuffle<T>(this List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(0, list.Count - 1);
                T value = list[k];
                list[k] = list[i];
                list[i] = value;
            }
        }
    }

    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static System.Random Local;

        public static System.Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new System.Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }

    public static class SMBHolding
    {
        public static SmbContainer smb;
    }
}

namespace Chisato.File
{
    using System;
    using System.IO;
    using System.Xml.Linq;
    using Microsoft.AspNetCore.Mvc;

    public static class DirectoryInfoExtensions
    {
        public static void DeepCopy(this DirectoryInfo directory, string destinationDir)
        {
            foreach (string dir in Directory.GetDirectories(directory.FullName, "*", SearchOption.AllDirectories))
            {
                string dirToCreate = dir.Replace(directory.FullName, destinationDir);
                Directory.CreateDirectory(dirToCreate);
            }

            foreach (string newPath in Directory.GetFiles(directory.FullName, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(directory.FullName, destinationDir), true);
            }
        }

        public static void ClearDirectoryContents(this DirectoryInfo directory)
        {
            var files = directory.GetFiles();
            var dirs = directory.GetDirectories();
            foreach (var file in files)
            {
                File.Delete(file.FullName);
            }
            foreach (var dir in dirs)
            {
                dir.ClearDirectoryContents();
                Directory.Delete(dir.FullName);
            }
        }
    }

    public static class FileHandler
    {
        public static void WriteToJsonFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            TextWriter writer = null;
            try
            {
                //var contentsToWriteToFile =
                //Convert.SerializeObject(objectToWrite);
                //writer = new StreamWriter(filePath, append);
                //writer.Write(contentsToWriteToFile);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        /*public static T ReadFromJsonFile<T>(string filePath) where T : new()
        {
            TextReader reader = null;
            try
            {
                reader = new StreamReader(filePath);
                var fileContents = reader.ReadToEnd();
                //return JsonConvert.DeserializeObject<T>(fileContents);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }*/

        public static byte[] ReadFileBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        public static void RenameFile(this FileInfo fileInfo, string newName)
        {
            fileInfo.MoveTo(fileInfo.Directory.FullName + "\\" + newName);
        }
    }
}

namespace Chisato.Shell
{
    public static class Shell
    {
        public static string RunCommand(string cmd)
        {
            using Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    Arguments = "/c " + cmd,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            return output;
        }

        public static string RunCommandSQLAPI(string query)
        {
            string queryFilePath = "C:\\SQLAPI\\Query.json";
            string apiCmd = "C:\\SQLapi_res\\SQLApi.exe";
            Query querySave = new Query();
            querySave.query = query;

            JsonHandler.SerializeJsonFile(queryFilePath, querySave);

            RunCommand(apiCmd);

            Thread.Sleep(2); //make thread wait for a response

            string output = JsonHandler.DeserializeJsonFile<Query>(queryFilePath).query;

            return output;
        }
    }

    [Serializable]
    public class Query
    {
        public string query;
    }
}