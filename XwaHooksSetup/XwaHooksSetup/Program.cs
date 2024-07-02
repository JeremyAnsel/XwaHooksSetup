using JeremyAnsel.Xwa.HooksConfig;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        const string XwaHooksSetupUrl = @"https://github.com/JeremyAnsel/XwaHooksSetup/raw/master/XwaHooksSetup/zip/XwaHooksSetup.zip";

        const string XwaHooksMainReadmeUrl = @"https://raw.githubusercontent.com/JeremyAnsel/xwa_hooks/master/README.md";
        const string XwaHooksZipUrl = @"https://github.com/JeremyAnsel/xwa_hooks/raw/master/{0}/zip/{0}.zip";

        const string HooksZipDirectory = @"Hooks\";
        const string HooksWipZipDirectory = @"Hooks_WIP\";
        const string HooksSetupDirectory = @"Setup\";

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("XwaHooksSetup");
                Console.WriteLine();

                SetWorkingDirectory();
                SelfUpdate();
                DeleteHooksDirectories();
                DownloadHooks();
                DownloadHooksWip();
                SetupHooks();
                UpdateHooks();

                Console.WriteLine("END");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static void SetWorkingDirectory()
        {
            using var process = Process.GetCurrentProcess();
            using var module = process.MainModule;
            string path = module.FileName;
            string directory = Path.GetDirectoryName(path);
            Directory.SetCurrentDirectory(directory);
        }

        static void SelfUpdate()
        {
            if (Path.GetFileName(Directory.GetCurrentDirectory()).IndexOf("_wip_", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return;
            }

            if (File.Exists("XwaHooksSetup.bak"))
            {
                File.Delete("XwaHooksSetup.bak");
                return;
            }

            Console.WriteLine("Self Update");
            Console.WriteLine();

            File.Delete("XwaHooksSetup.zip");

            using (var client = new WebClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                client.DownloadFile(XwaHooksSetupUrl, "XwaHooksSetup.zip");
                UpdateZipLastWriteTime("XwaHooksSetup.zip");
            }

            File.Delete("XwaHooksSetup.bak");
            File.Move("XwaHooksSetup.exe", "XwaHooksSetup.bak");

            foreach (string file in Directory.EnumerateFiles("."))
            {
                string extension = Path.GetExtension(file);

                if (string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(extension, ".bak", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                File.Delete(file);
            }

            ZipFile.ExtractToDirectory("XwaHooksSetup.zip", ".");

            using (var process = Process.GetCurrentProcess())
            {
                Process.Start(process.MainModule.FileName, process.StartInfo.Arguments);
            }

            Environment.Exit(0);
        }

        static void DeleteHooksDirectories()
        {
            if (Directory.Exists(HooksZipDirectory))
            {
                Directory.Delete(HooksZipDirectory, true);
            }

            if (Directory.Exists(HooksWipZipDirectory))
            {
                Directory.Delete(HooksWipZipDirectory, true);
            }

            if (Directory.Exists(HooksSetupDirectory))
            {
                Directory.Delete(HooksSetupDirectory, true);
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

        static void DownloadHooksWip()
        {
            Console.WriteLine("Download Hooks WIP");
            Directory.CreateDirectory(HooksWipZipDirectory);

            using (var client = new WebClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                List<string> hooks = GetHooksWipList(client, XwaHooksMainReadmeUrl);

                for (int hookIndex = 0; hookIndex < hooks.Count; hookIndex++)
                {
                    string hookName = hooks[hookIndex];
                    Console.WriteLine("[{0}/{1}] WIP {2}", hookIndex + 1, hooks.Count, hookName);

                    string zipUrl = string.Format(XwaHooksZipUrl, hookName);
                    string filePath = HooksWipZipDirectory + hookName + ".zip";
                    client.DownloadFile(zipUrl, filePath);
                    UpdateZipLastWriteTime(filePath);
                }
            }

            Console.WriteLine();
        }

        static List<string> GetHooksWipList(WebClient client, string url)
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
                    if (!line.StartsWith("## WIP xwa_hook_"))
                    {
                        continue;
                    }

                    string hookName = line.Substring(7);
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
            Directory.CreateDirectory(HooksSetupDirectory);
            //Directory.CreateDirectory(HooksSetupDirectory + @"Examples\");

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

            using (var setupFile = new FileStream(HooksSetupDirectory + "Hooks_Setup.txt", FileMode.Create, FileAccess.Write))
            using (var readmeFile = new FileStream(HooksSetupDirectory + "Hooks_Readme.txt", FileMode.Create, FileAccess.Write))
            using (var configFile = new FileStream(HooksSetupDirectory + "Hooks.ini", FileMode.Create, FileAccess.Write))
            {
                setupFile.WriteText("XWA Hooks Setup\n");
                setupFile.WriteText("This file contains the setup sections of the readme files for the hooks.\n");
                setupFile.WriteText("\n");

                readmeFile.WriteText("XWA Hooks Readme\n");
                readmeFile.WriteText("This file contains the readme files for the hooks.\n");
                readmeFile.WriteText("\n");

                for (int hookIndex = 0; hookIndex < hookPaths.Count; hookIndex++)
                {
                    string hookPath = hookPaths[hookIndex];
                    string hookName = Path.GetFileNameWithoutExtension(hookPath);

                    setupFile.WriteText(string.Format(CultureInfo.InvariantCulture, "[{0}/{1}] {2}\n", hookIndex + 1, hookPaths.Count, hookName));
                    readmeFile.WriteText(string.Format(CultureInfo.InvariantCulture, "[{0}/{1}] {2}\n", hookIndex + 1, hookPaths.Count, hookName));
                }

                setupFile.WriteText("\n");
                readmeFile.WriteText("\n");

                for (int hookIndex = 0; hookIndex < hookPaths.Count; hookIndex++)
                {
                    string hookPath = hookPaths[hookIndex];
                    string hookName = Path.GetFileNameWithoutExtension(hookPath);
                    Console.WriteLine("[{0}/{1}] {2}", hookIndex + 1, hookPaths.Count, hookName);

                    using (var archiveFile = File.OpenRead(hookPath))
                    using (var archive = new ZipArchive(archiveFile, ZipArchiveMode.Read))
                    {
                        //string examplesDirectory = HooksSetupDirectory + @"Examples\" + GetFormattedFileName(hookName) + @"\";
                        //Directory.CreateDirectory(examplesDirectory);

                        foreach (var entry in archive.Entries)
                        {
                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                continue;
                            }

                            if (entry.Name == "readme.txt")
                            {
                                //entry.CopyTo(HooksSetupDirectory + GetFormattedFileName(hookName + "_readme.txt"));

                                Dictionary<string, string> sections;

                                using (Stream stream = entry.Open())
                                {
                                    //stream.CopyTo(readmeFile);
                                    sections = stream.ReadDictionary();
                                }

                                setupFile.WriteText(new string('=', 40) + "\n");
                                setupFile.WriteText(string.Format(CultureInfo.InvariantCulture, "[{0}/{1}] ", hookIndex + 1, hookPaths.Count));

                                readmeFile.WriteText(new string('=', 40) + "\n");
                                readmeFile.WriteText(string.Format(CultureInfo.InvariantCulture, "[{0}/{1}] ", hookIndex + 1, hookPaths.Count));

                                foreach (KeyValuePair<string, string> section in sections)
                                {
                                    if (string.IsNullOrEmpty(section.Key))
                                    {
                                        setupFile.WriteText(section.Value);
                                        readmeFile.WriteText(section.Value);
                                    }
                                    else if (section.Key.Equals("*** Setup ***", StringComparison.OrdinalIgnoreCase))
                                    {
                                        setupFile.WriteText(section.Key + "\n");
                                        setupFile.WriteText(section.Value);

                                        readmeFile.WriteText(section.Key + "\n");
                                        readmeFile.WriteText(section.Value);
                                    }
                                    else if (section.Key.Equals("*** Usage ***", StringComparison.OrdinalIgnoreCase))
                                    {
                                        readmeFile.WriteText(section.Key + "\n");
                                        readmeFile.WriteText(section.Value);
                                    }
                                    else
                                    {
                                        setupFile.WriteText(section.Key + "\n");
                                        setupFile.WriteText(section.Value);
                                    }
                                }

                                setupFile.WriteText("\n");
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
                                //if (entry.FullName.StartsWith("Examples/", StringComparison.OrdinalIgnoreCase))
                                //{
                                //    entry.CopyTo(examplesDirectory + GetFormattedFileName(entry.Name));
                                //}
                                //else
                                //{
                                //    string path = examplesDirectory + GetFormattedFileName(entry.FullName);
                                //    Directory.CreateDirectory(Path.GetDirectoryName(path));
                                //    entry.CopyTo(path);
                                //}
                            }
                        }

                        //if (Directory.EnumerateFiles(examplesDirectory, "*", SearchOption.AllDirectories).FirstOrDefault() == null)
                        //{
                        //    Directory.Delete(examplesDirectory);
                        //}
                    }
                }
            }

            Console.WriteLine();
        }

        static void UpdateHooks()
        {
            if (!File.Exists(@"..\XWingAlliance.exe") || !File.Exists(@"..\Hooks.ini"))
            {
                return;
            }

            Console.WriteLine("Update Hooks.ini");

            var currentHooksIni = new XwaIniFile(@"..\Hooks.ini");
            currentHooksIni.ParseIni();
            currentHooksIni.ParseSettings();

            var newHooksIni = new XwaIniFile(@"Setup\Hooks.ini");
            newHooksIni.ParseIni();

            foreach (var currentSection in currentHooksIni.Sections)
            {
                string sectionKey = currentSection.Key;
                XwaIniSection section = currentSection.Value;

                Console.WriteLine($"[{sectionKey}]");

                newHooksIni.CreateSectionIfNotExists(sectionKey);
                XwaIniSection newSection = newHooksIni.Sections[sectionKey];

                if (string.Equals(newSection.GetKeyValueInLines("OverrideSettings"), "1", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var keys = section.GetSettingKeys();

                foreach (string key in keys)
                {
                    string value = section.GetKeyValueInSettings(key);

                    newSection.SetKeyValueInLines(key, value);
                }
            }

            newHooksIni.Save();
            Console.WriteLine();

            Console.WriteLine("Update Hooks");
            File.Copy(@"Setup\Hooks.ini", @"..\Hooks.ini", true);

            foreach (string file in Directory.EnumerateFiles("Setup", "*.*"))
            {
                string extension = Path.GetExtension(file).ToLowerInvariant();

                string[] keepExtensions = new string[]
                {
                    ".ini",
                    ".dll",
                    ".exe",
                    ".config"
                };

                bool copyFile = false;

                foreach (string keepExtension in keepExtensions)
                {
                    if (string.Equals(extension, keepExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        copyFile = true;
                        break;
                    }
                }

                if (!copyFile)
                {
                    continue;
                }

                string fileName = Path.GetFileName(file);
                string setupFile = Path.Combine("Setup", fileName);
                string xwaFile = Path.Combine("..", fileName);

                Console.WriteLine(fileName);
                File.Copy(setupFile, xwaFile, true);
            }

            Console.WriteLine();
        }
    }
}
