using System;
using System.Collections.Generic;
using System.Globalization;
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

                for (int hookIndex = 0; hookIndex < hooks.Count; hookIndex++)
                {
                    string hookName = hooks[hookIndex];
                    Console.WriteLine("[{0}/{1}] {2}", hookIndex + 1, hooks.Count, hookName);

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

            List<string> hookPaths = Directory
                .EnumerateFiles(HooksZipDirectory, "xwa_hook_*.zip")
                .ToList()
                .Select(t => new
                {
                    IsTop = Path.GetFileName(t).StartsWith("xwa_hook_main", StringComparison.OrdinalIgnoreCase),
                    Path = t
                })
                .OrderByDescending(t => t.IsTop)
                .ThenBy(t => t.Path)
                .Select(t => t.Path)
                .ToList();

            using (var readmeFile = new FileStream(HooksSetupDirectory + "Hooks_Readme.txt", FileMode.Create, FileAccess.Write))
            using (var configFile = new FileStream(HooksSetupDirectory + "Hooks.ini", FileMode.Create, FileAccess.Write))
            {
                readmeFile.WriteText("XWA Hooks Readme\n");
                readmeFile.WriteText("This file contains the readme files for the hooks.\n");
                readmeFile.WriteText("\n");

                for (int hookIndex = 0; hookIndex < hookPaths.Count; hookIndex++)
                {
                    string hookPath = hookPaths[hookIndex];
                    string hookName = Path.GetFileNameWithoutExtension(hookPath);

                    readmeFile.WriteText(string.Format(CultureInfo.InvariantCulture, "[{0}/{1}] {2}\n", hookIndex + 1, hookPaths.Count, hookName));
                }

                readmeFile.WriteText("\n");

                for (int hookIndex = 0; hookIndex < hookPaths.Count; hookIndex++)
                {
                    string hookPath = hookPaths[hookIndex];
                    string hookName = Path.GetFileNameWithoutExtension(hookPath);
                    Console.WriteLine("[{0}/{1}] {2}", hookIndex + 1, hookPaths.Count, hookName);

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
                                //entry.CopyTo(HooksSetupDirectory + GetFormattedFileName(hookName + "_readme.txt"));

                                readmeFile.WriteText(new string('=', 40) + "\n");
                                readmeFile.WriteText(string.Format(CultureInfo.InvariantCulture, "[{0}/{1}] ", hookIndex + 1, hookPaths.Count));

                                using (Stream stream = entry.Open())
                                {
                                    stream.CopyTo(readmeFile);
                                }

                                readmeFile.WriteText("\n");
                            }
                            else if (entry.Name.EndsWith(".cfg"))
                            {
                                //entry.CopyTo(HooksSetupDirectory + GetFormattedFileName(entry.Name));

                                configFile.WriteText("[" + Path.GetFileNameWithoutExtension(entry.Name) + "]\n");

                                using (Stream stream = entry.Open())
                                {
                                    while (true)
                                    {
                                        int b = stream.ReadByte();

                                        if (b == -1)
                                        {
                                            break;
                                        }

                                        if (b != '\r' && b != '\n')
                                        {
                                            configFile.WriteByte((byte)b);
                                            break;
                                        }
                                    }

                                    stream.CopyTo(configFile);
                                }

                                configFile.WriteText("\n");
                            }
                            else if (entry.Name.EndsWith(".dll")
                                || entry.Name.EndsWith(".exe")
                                || entry.Name.EndsWith(".exe.config"))
                            {
                                entry.CopyTo(HooksSetupDirectory + GetFormattedFileName(entry.Name));
                            }
                            else
                            {
                                if (entry.FullName.StartsWith("Examples/", StringComparison.OrdinalIgnoreCase))
                                {
                                    entry.CopyTo(examplesDirectory + GetFormattedFileName(entry.Name));
                                }
                                else
                                {
                                    string path = examplesDirectory + GetFormattedFileName(entry.FullName);
                                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                                    entry.CopyTo(path);
                                }
                            }
                        }

                        if (Directory.EnumerateFiles(examplesDirectory).FirstOrDefault() == null)
                        {
                            Directory.Delete(examplesDirectory);
                        }
                    }
                }
            }

            Console.WriteLine();
        }
    }
}
