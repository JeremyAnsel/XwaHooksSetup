using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace XwaHooksSetup
{
    class Program
    {
        const string XwaHooksMainReadmeUrl = @"https://raw.githubusercontent.com/JeremyAnsel/xwa_hooks/master/README.md";
        const string XwaHooksZipUrl = @"https://github.com/JeremyAnsel/xwa_hooks/raw/master/{0}/zip/{0}.zip";

        const string HooksZipDirectory = @"Hooks\";
        const string HooksSetupDirectory = @"Setup\";

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("XwaHooksSetup");
                Console.WriteLine();

                if (!Directory.Exists(HooksZipDirectory))
                {
                    DownloadHooks();
                }

                SetupHooks();

                Console.WriteLine("END");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static string GetFormattedFileName(string name)
        {
            string[] parts = name.Split('_');

            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }

            return string.Join("_", parts);
        }

        static void DownloadHooks()
        {
            Console.WriteLine("Download Hooks");
            Directory.CreateDirectory(HooksZipDirectory);

            using (var client = new WebClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                List<string> hooks = GetHooksList(client, XwaHooksMainReadmeUrl);

                foreach (string hookName in hooks)
                {
                    Console.WriteLine(hookName);

                    string zipUrl = string.Format(XwaHooksZipUrl, hookName);
                    string filePath = HooksZipDirectory + hookName + ".zip";
                    client.DownloadFile(zipUrl, filePath);
                    UpdateZipLastWriteTime(filePath);
                }
            }

            Console.WriteLine();
        }

        static List<string> GetHooksList(WebClient client, string url)
        {
            var list = new List<string>();

            string mainReadme = client.DownloadString(url);

            if (string.IsNullOrEmpty(mainReadme))
            {
                return list;
            }

            using (var reader = new StringReader(mainReadme))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.StartsWith("## xwa_hook_"))
                    {
                        continue;
                    }

                    string hookName = line.Substring(3);
                    list.Add(hookName);
                }
            }

            return list;
        }

        static void UpdateZipLastWriteTime(string path)
        {
            DateTimeOffset date;

            using (var archiveFile = File.OpenRead(path))
            using (var archive = new ZipArchive(archiveFile, ZipArchiveMode.Read))
            {
                date = archive.Entries.Max(t => t.LastWriteTime);
            }

            File.SetLastWriteTimeUtc(path, date.UtcDateTime);
        }

        static void SetupHooks()
        {
            Console.WriteLine("Setup Hooks");

            if (Directory.Exists(HooksSetupDirectory))
            {
                Directory.Delete(HooksSetupDirectory, true);
            }

            Directory.CreateDirectory(HooksSetupDirectory);
            Directory.CreateDirectory(HooksSetupDirectory + @"Examples\");

            foreach (string hookPath in Directory.EnumerateFiles(HooksZipDirectory, "xwa_hook_*.zip"))
            {
                string hookName = Path.GetFileNameWithoutExtension(hookPath);
                Console.WriteLine(hookName);

                using (var archiveFile = File.OpenRead(hookPath))
                using (var archive = new ZipArchive(archiveFile, ZipArchiveMode.Read))
                {
                    string examplesDirectory = HooksSetupDirectory + @"Examples\" + GetFormattedFileName(hookName) + @"\";
                    Directory.CreateDirectory(examplesDirectory);

                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            continue;
                        }

                        if (entry.Name == "readme.txt")
                        {
                            entry.CopyTo(HooksSetupDirectory + GetFormattedFileName(hookName + "_readme.txt"));
                        }
                        else if (entry.Name.EndsWith(".dll") || entry.Name.EndsWith(".cfg"))
                        {
                            entry.CopyTo(HooksSetupDirectory + GetFormattedFileName(entry.Name));
                        }
                        else
                        {
                            entry.CopyTo(examplesDirectory + GetFormattedFileName(entry.Name));
                        }
                    }

                    if (Directory.EnumerateFiles(examplesDirectory).FirstOrDefault() == null)
                    {
                        Directory.Delete(examplesDirectory);
                    }
                }
            }

            Console.WriteLine();
        }
    }
}
