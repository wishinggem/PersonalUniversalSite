using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Chisato;
using Chisato.File;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using System.Net;
using System.Runtime.InteropServices;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.IO;
using System.Data.Common;

namespace PersonalUniversalSite.Pages
{
    public class SMBConnectionModel : PageModel
    {
        public static List<string> holdingPaths = new List<string>();
        public static string persistantPath = "";
        #region ContentTypes
        public static Dictionary<string, string> contentTypeDict = new Dictionary<string, string> {
            {".aac", "audio/aac"},
    {".abw", "application/x-abiword"},
    {".apng", "image/apng"},
    {".arc", "application/x-freearc"},
    {".avif", "image/avif"},
    {".avi", "video/x-msvideo"},
    {".azw", "application/vnd.amazon.ebook"},
    {".bin", "application/octet-stream"},
    {".bmp", "image/bmp"},
    {".bz", "application/x-bzip"},
    {".bz2", "application/x-bzip2"},
    {".cda", "application/x-cdf"},
    {".csh", "application/x-csh"},
    {".css", "text/css"},
    {".csv", "text/csv"},
    {".doc", "application/msword"},
    {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
    {".eot", "application/vnd.ms-fontobject"},
    {".epub", "application/epub+zip"},
    {".gz", "application/gzip"},
    {".gif", "image/gif"},
    {".htm", "text/html"},
    {".html", "text/html"},
    {".ico", "image/vnd.microsoft.icon"},
    {".ics", "text/calendar"},
    {".jar", "application/java-archive"},
    {".jpeg", "image/jpeg"},
    {".jpg", "image/jpeg"},
    {".js", "text/javascript"},
    {".json", "application/json"},
    {".jsonld", "application/ld+json"},
    {".mid", "audio/midi"},
    {".midi", "audio/x-midi"},
    {".mjs", "text/javascript"},
    {".mp3", "audio/mpeg"},
    {".mp4", "video/mp4"},
    {".mpeg", "video/mpeg"},
    {".mpkg", "application/vnd.apple.installer+xml"},
    {".odp", "application/vnd.oasis.opendocument.presentation"},
    {".ods", "application/vnd.oasis.opendocument.spreadsheet"},
    {".odt", "application/vnd.oasis.opendocument.text"},
    {".oga", "audio/ogg"},
    {".ogv", "video/ogg"},
    {".ogx", "application/ogg"},
    {".opus", "audio/opus"},
    {".otf", "font/otf"},
    {".png", "image/png"},
    {".pdf", "application/pdf"},
    {".php", "application/x-httpd-php"},
    {".ppt", "application/vnd.ms-powerpoint"},
    {".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
    {".rar", "application/vnd.rar"},
    {".rtf", "application/rtf"},
    {".sh", "application/x-sh"},
    {".svg", "image/svg+xml"},
    {".tar", "application/x-tar"},
    {".tif", "image/tiff"},
    {".tiff", "image/tiff"},
    {".ts", "video/mp2t"},
    {".ttf", "font/ttf"},
    {".txt", "text/plain"},
    {".vsd", "application/vnd.visio"},
    {".wav", "audio/wav"},
    {".weba", "audio/webm"},
    {".webm", "video/webm"},
    {".webp", "image/webp"},
    {".woff", "font/woff"},
    {".woff2", "font/woff2"},
    {".xhtml", "application/xhtml+xml"},
    {".xls", "application/vnd.ms-excel"},
    {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
    {".xml", "application/xml"},
    {".xul", "application/vnd.mozilla.xul+xml"},
    {".zip", "application/zip"},
    {".3gp", "video/3gpp"},
    {".3g2", "video/3gpp2"},
    {".7z", "application/x-7z-compressed"}
        }; //extension : MIME type
        #endregion

        public void OnGet(string workingPath)
        {
            if (workingPath == null)
            {
                workingPath = "";
                persistantPath = "";
                holdingPaths.Clear();
            }
            persistantPath = Path.Combine(persistantPath, workingPath);
            holdingPaths.Add(persistantPath);

            SetDisplayPath();

            var session = HttpContext.Session;
            string dataSource = session.GetString("dataSource");
            string username = session.GetString("username");
            string password = session.GetString("password");

            try
            {
                ViewData["Title"] = dataSource;

                if (dataSource == string.Empty || dataSource == "" || dataSource == null || IsUnauthorisedPath(dataSource))
                {
                    ViewData["ViewDirectories"] = new List<DirectoryInfo>();
                    ViewData["ViewFiles"] = new List<FileInfo>();
                    Response.Redirect("/SMBStart?failed=failed");
                }
                else
                {
                    SMBHolding.smb = new SmbContainer(false, dataSource, username, password);
                    SetViewDirsFiles();
                }
            }
            catch
            {
                ViewData["ViewDirectories"] = new List<DirectoryInfo>();
                ViewData["ViewFiles"] = new List<FileInfo>();
                SetDisplayPath();
                Response.Redirect("/SMBStart?failed=failed");
            }
        }

        private void SetDisplayPath()
        {
            if (persistantPath == null || persistantPath == string.Empty || persistantPath == "")
            {
                ViewData["workingPath"] = "(root)";
            }
            else
            {
                ViewData["workingPath"] = "(root)" + "\\" + persistantPath;
            }
        }

        private bool IsUnauthorisedPath(string path)
        {
            List<string> unauthPaths = new List<string> { "A:\\", "B:\\", "C:\\", "D:\\", "E:\\", "F:\\", "G:\\", "H:\\", "I:\\", "J:\\", "K:\\", "L:\\", "M:\\", "N:\\", "O:\\", "P:\\", "Q:\\", "R:\\", "S:\\", "T:\\", "U:\\", "V:\\", "W:\\", "X:\\", "Y:\\", "Z:\\" };
            foreach (string unauthPath in unauthPaths)
            {
                if (path.Contains(unauthPath))
                {
                    return true;
                }
            }
            if (IsUnauthorisedIP(path))
            {
                return true;
            }
            return false;
        }

        private bool IsUnauthorisedIP(string path)
        {
            //check for local ip
            path.Replace("\\", "/");
            var match = Regex.Match(path, @"(10\.(?:[0-9]{1,3}\.){2}[0-9]{1,3})|(172\.(?:1[6-9]|2[0-9]|3[0-1])\.[0-9]{1,3}\.[0-9]{1,3})|(192\.168\.[0-9]{1,3}\.[0-9]{1,3})");
            if (match.Success)
            {
                return true;
            }
            return false;
        }

        private void SetViewDirsFiles()
        {
            List<DirectoryInfo> dirs = new List<DirectoryInfo>();
            List<FileInfo> files = new List<FileInfo>();
            List<FileObject> fileObjects = SMBHolding.smb.GetDirectories(persistantPath);
            foreach (FileObject obj in fileObjects)
            {
                DirectoryInfo dir = obj.directory;
                FileInfo file = obj.file;

                if (dir != null)
                {
                    dirs.Add(dir);
                }
                if (file != null)
                {
                    files.Add(file);
                }
            }
            ViewData["ViewDirectories"] = dirs;
            ViewData["ViewFiles"] = files;
            SetDisplayPath();
        }

        public IActionResult OnPostNavUp()
        {
            if (holdingPaths.Count > 1)
            {
                holdingPaths.Remove(persistantPath);
                persistantPath = holdingPaths[holdingPaths.Count - 1];
                SetDisplayPath();
            }
            SetViewDirsFiles();
            return null;
        }

        public IActionResult OnPostDownloadFile()
        { 
            string fileName = Request.Form["fileName"];
            SetDisplayPath();
            bool isDirectory = false;
            if (fileName[0] == 'f')
            {
                isDirectory = true;
                fileName = fileName.Substring(1);
            }

            var session = HttpContext.Session;
            string dataSource = session.GetString("dataSource");
            string username = session.GetString("username");
            string password = session.GetString("password");

            if (string.IsNullOrEmpty(dataSource) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"SMB connection information is missing. \n Data Source: {dataSource} /// username: {username} /// password: {password}");
            }

            string networkPath = dataSource;
            NetworkCredential networkCredential = new NetworkCredential(username, password);

            string path = "";
            if (!isDirectory)
            {
                path = Path.Combine(networkPath, persistantPath, fileName);
            }

            FileStream fileStream = null;

            using (var network = new NetworkConnection(networkPath, networkCredential))
            {
                network.Connect();
                if (!isDirectory)
                {
                    if (System.IO.File.Exists(path))
                    {
                        fileStream = System.IO.File.OpenRead(path);
                    }
                }
                network.Dispose();
            }

            if (!isDirectory)
            {
                return File(fileStream, "application/octet-stream", fileName); //contentTypeDict["." + extension]
            }
            else
            {
                using (var network = new NetworkConnection(networkPath, networkCredential))
                {
                    network.Connect();

                    string folderPath = Path.Combine(networkPath, persistantPath, fileName);

                    string tempPath = Path.Combine(VisualStudioProvider.TryGetPageDirectoryInfo().FullName, "wwwroot", "DownloadDirs", "data");
                    string zipPath = Path.Combine(VisualStudioProvider.TryGetPageDirectoryInfo().FullName, "wwwroot", "DownloadDirs", "DowanloadData.zip");

                    DirectoryInfo dir = new DirectoryInfo(folderPath);
                    DirectoryInfo tempDir = new DirectoryInfo(tempPath);

                    tempDir.ClearDirectoryContents();
                    if (System.IO.File.Exists(zipPath))
                    {
                        System.IO.File.Delete(zipPath);
                    }

                    dir.DeepCopy(tempPath);


                    ZipFile.CreateFromDirectory(tempPath, zipPath);

                    byte[] fileBytes = System.IO.File.ReadAllBytes(zipPath);

                    network.Dispose();

                    tempDir.ClearDirectoryContents();
                    if (System.IO.File.Exists(zipPath))
                    {
                        System.IO.File.Delete(zipPath);
                    }

                    // Return the zip file as a byte array
                    return File(fileBytes, "application/zip", "fileDownload.zip");
                }
            }
        }
    }
}

public class SmbContainer
{
    private readonly NetworkCredential networkCredential;
    // Path to shared folder:
    public string networkPath;
    public bool local;

    public SmbContainer(bool isLocal, string sharePath, string SMBusername, string SMBpassword, string credDomain = "")
    {
        this.networkPath = sharePath;
        networkCredential = new NetworkCredential(SMBusername, SMBpassword, credDomain);
        local = isLocal;
    }

    public NetworkCredential GetCreds()
    {
        return networkCredential;
    }

    public bool IsValidConnection()
    {
        using (var network = new NetworkConnection($"{networkPath}", networkCredential))
        {
            var result = network.Connect();
            network.Dispose();
            return result == 0;
        }
    }

    public void CreateFile(string targetFile, string content = "", string root = "")
    {
        string rootTemp = root;
        if (root == "")
        {
            rootTemp = networkPath;
        }
        var path = Path.Combine(rootTemp, targetFile);
        if (!local)
        {
            using (var network = new NetworkConnection(networkPath, networkCredential))
            {
                network.Connect();
                if (!File.Exists(path))
                {
                    using (File.Create(path)) { };
                    using (StreamWriter sw = File.CreateText(path))
                    {
                        sw.WriteLine(content);
                    }
                }
                network.Dispose();
            }
        }
        else
        {
            if (!File.Exists(path))
            {
                using (File.Create(path)) { };
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(content);
                }
            }
        }
    }

    public void DeleteFile(string targetFile, string root = "")
    {
        string rootTemp = root;
        if (root == "")
        {
            rootTemp = networkPath;
        }
        var path = Path.Combine(rootTemp, targetFile);
        if (!local)
        {
            using (var network = new NetworkConnection(networkPath, networkCredential))
            {
                network.Connect();
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                network.Dispose();
            }
        }
        else
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    public List<FileInfo> GetFiles(string root = "")
    {
        string rootTemp = root;
        if (root == "")
        {
            rootTemp = networkPath;
        }
        if (!local)
        {
            using (var network = new NetworkConnection(networkPath, networkCredential))
            {
                network.Connect();
                List<FileInfo> files = new List<FileInfo>();
                DirectoryInfo dir = new DirectoryInfo(rootTemp);
                files = dir.GetFiles().ToList();
                network.Dispose();
                return files;
            }
        }
        else
        {
            List<FileInfo> files = new List<FileInfo>();
            DirectoryInfo dir = new DirectoryInfo(rootTemp);
            files = dir.GetFiles().ToList();
            return files;
        }
    }

    public List<FileInfo> OpenDriectory(string target, string root = "")
    {
        string rootTemp = root;
        if (root == "")
        {
            rootTemp = networkPath;
        }
        string path = Path.Combine(rootTemp, target);
        if (!local)
        {
            using (var network = new NetworkConnection(networkPath, networkCredential))
            {
                network.Connect();
                if (File.Exists(path))
                {
                    network.Dispose();
                    return null;
                }
                else if (Directory.Exists(target))
                {
                    var files = new List<FileInfo>();
                    files = GetFiles(path);
                    network.Dispose();
                    return files;
                }
                else
                {
                    network.Dispose();
                    return null;
                }
            }
        }
        else
        {
            if (File.Exists(path))
            {
                return null;
            }
            else if (Directory.Exists(target))
            {
                var files = new List<FileInfo>();
                files = GetFiles(path);
                return files;
            }
            else
            {
                return null;
            }
        }
    }

    public List<FileObject> GetDirectories(string workingPath = "")
    {
        List<FileObject> FileObj = new List<FileObject>();
        string[] dirPaths;
        string[] filePaths;
        if (workingPath == "" || workingPath == null || workingPath == string.Empty)
        {
            workingPath = networkPath;
        }
        else
        {
            workingPath = Path.Combine(networkPath, workingPath);
        }
        if (!local)
        {
            using (var network = new NetworkConnection(networkPath, networkCredential))
            {
                network.Connect();
                dirPaths = Directory.GetDirectories(workingPath);
                filePaths = Directory.GetFiles(workingPath);
                network.Dispose();
            }
        }
        else
        {
            dirPaths = Directory.GetDirectories(workingPath);
            filePaths = Directory.GetFiles(workingPath);
        }
        foreach (string dir in dirPaths)
        {
            FileObj.Add(new FileObject(new DirectoryInfo(dir), null));
        }
        foreach (string file in filePaths)
        {
            FileObj.Add(new FileObject(null, new FileInfo(file)));
        }
        return FileObj;
    }

    public byte[] OpenFile(string target, string root = "")
    {
        string rootTemp = root;
        if (root == "")
        {
            rootTemp = networkPath;
        }
        string path = Path.Combine(rootTemp, target);
        if (!local)
        {
            using (var network = new NetworkConnection(networkPath, networkCredential))
            {
                network.Connect();
                if (File.Exists(path))
                {
                    byte[] file = File.ReadAllBytes(path);
                    network.Dispose();
                    return file;
                }
                else if (Directory.Exists(path))
                {
                    network.Dispose();
                    return null;
                }
                else
                {
                    network.Dispose();
                    return null;
                }
            }
        }
        else
        {
            if (File.Exists(path))
            {
                byte[] file = File.ReadAllBytes(path);
                return file;
            }
            else if (Directory.Exists(path))
            {
                return null;
            }
            else
            {
                return null;
            }
        }
    }

    public string OpenTxtFile(string target, string root = "")
    {
        string rootTemp = root;
        if (root == "")
        {
            rootTemp = networkPath;
        }
        string path = Path.Combine(rootTemp, target);
        if (!local)
        {
            using (var network = new NetworkConnection(networkPath, networkCredential))
            {
                network.Connect();
                if (File.Exists(path))
                {
                    string file = File.ReadAllText(path);
                    network.Dispose();
                    return file;
                }
                else if (Directory.Exists(path))
                {
                    network.Dispose();
                    return null;
                }
                else
                {
                    network.Dispose();
                    return null;
                }
            }
        }
        else
        {
            if (File.Exists(path))
            {
                string file = File.ReadAllText(path);
                return file;
            }
            else if (Directory.Exists(path))
            {
                return null;
            }
            else
            {
                return null;
            }
        }
    }

    public string ConvertByteToString(byte[] stream)
    {
        return stream.ToString();
    }

    public string GetFilePath(string name, string root = "")
    {
        string rootTemp = root;
        if (root == "")
        {
            rootTemp = networkPath;
        }
        var path = Path.Combine(rootTemp);
        string returnPath = null;
        if (!local)
        {
            using (var network = new NetworkConnection(networkPath, networkCredential))
            {
                network.Connect();
                EnumerationOptions opt = new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true,
                };
                string[] files = Directory.GetFiles(path, name, opt);
                if (files.Length > 0)
                {
                    returnPath = files[0];
                }
                network.Dispose();
            }
        }
        else
        {
            EnumerationOptions opt = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
            };
            string[] files = Directory.GetFiles(path, name, opt);
            if (files.Length > 0)
            {
                returnPath = files[0];
            }
        }
        return returnPath;
    }

    public string GetDirectory(string name, string root = "")
    {
        string rootTemp = root;
        if (root == "")
        {
            rootTemp = networkPath;
        }
        var path = Path.Combine(rootTemp);
        string returnPath = null;
        if (!local)
        {
            using (var network = new NetworkConnection(networkPath, networkCredential))
            {
                network.Connect();
                EnumerationOptions opt = new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true,
                };
                string[] dirs = Directory.GetDirectories(path, name, opt);
                if (dirs.Length > 0)
                {
                    returnPath = dirs[0];
                }
                network.Dispose();
            }
        }
        else
        {
            EnumerationOptions opt = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
            };
            string[] dirs = Directory.GetDirectories(path, name, opt);
            if (dirs.Length > 0)
            {
                returnPath = dirs[0];
            }
        }
        return returnPath;
    }

    public bool IsDirectory(string workingPath = "")
    {
        workingPath = Path.Combine(networkPath, workingPath);
        bool isDir = false;
        if (!local)
        {
            using (var network = new NetworkConnection(networkPath, networkCredential))
            {
                network.Connect();
                if (Directory.Exists(workingPath))
                {
                    isDir = true;
                }
                else if (File.Exists(workingPath))
                {
                    isDir = false;
                }
                else
                {
                    throw new Exception("Not Dir or File Potentially Does not exist");
                }
                network.Dispose();
            }
        }
        else
        {
            if (Directory.Exists(workingPath))
            {
                isDir = true;
            }
            else if (File.Exists(workingPath))
            {
                isDir = false;
            }
            else
            {
                throw new Exception("Not Dir or File Potentially Does not exist");
            }
        }
        return isDir;
    }
}

public class FileObject
{
    public DirectoryInfo directory;
    public FileInfo file;

    public FileObject(DirectoryInfo Idirectory, FileInfo Ifile)
    {
        this.directory = Idirectory;
        this.file = Ifile;
    }
}

[StructLayout(LayoutKind.Sequential)]
public class NetResource
{
    public ResourceScope Scope;
    public ResourceType ResourceType;
    public ResourceDisplaytype DisplayType;
    public int Usage;
    public string LocalName;
    public string RemoteName;
    public string Comment;
    public string Provider;
}

public class NetworkConnection : IDisposable
{
    private string _networkName;
    private NetworkCredential _credentials;

    public NetworkConnection(string networkName, NetworkCredential credentials)
    {
        _networkName = networkName;
        _credentials = credentials;
    }

    public int Connect()
    {
        var netResource = new NetResource()
        {
            Scope = ResourceScope.GlobalNetwork,
            ResourceType = ResourceType.Disk,
            DisplayType = ResourceDisplaytype.Share,
            RemoteName = _networkName
        };

        var userName = string.IsNullOrEmpty(_credentials.Domain)
            ? _credentials.UserName
            : string.Format(@"{0}\{1}", _credentials.Domain, _credentials.UserName);

        var result = WNetAddConnection2(
            netResource,
            _credentials.Password,
            userName,
            0);
        return result;
    }

    ~NetworkConnection()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        WNetCancelConnection2(_networkName, 0, true);
    }

    [DllImport("mpr.dll")]
    private static extern int WNetAddConnection2(NetResource netResource,
        string password, string username, int flags);

    [DllImport("mpr.dll")]
    private static extern int WNetCancelConnection2(string name, int flags,
        bool force);
}

public enum ResourceScope : int
{
    Connected = 1,
    GlobalNetwork,
    Remembered,
    Recent,
    Context
};

public enum ResourceType : int
{
    Any = 0,
    Disk = 1,
    Print = 2,
    Reserved = 8,
};

public enum ResourceDisplaytype : int
{
    Generic = 0x0,
    Domain = 0x01,
    Server = 0x02,
    Share = 0x03,
    File = 0x04,
    Group = 0x05,
    Network = 0x06,
    Root = 0x07,
    Shareadmin = 0x08,
    Directory = 0x09,
    Tree = 0x0a,
    Ndscontainer = 0x0b
};