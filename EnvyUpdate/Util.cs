using IWshRuntimeLibrary;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Windows;
using Newtonsoft.Json;
using System.Xml;
using System.Xml.Linq;

namespace EnvyUpdate
{
    internal class Util
    {
        /// <summary>
        /// Parses GPU info from a cookie file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        public static int GetData(string path, string term)
        {
            string found = null;
            string line;
            using (StreamReader file = new StreamReader(path))
            {
                while ((line = file.ReadLine()) != null)
                {
                    if (line.Contains(term))
                    {
                        found = line;
                        break;
                    }
                }
            }
            int lastno = Convert.ToInt32(found.Split().Last());
            return lastno;
        }

        /// <summary>
        /// Gets local driver version.
        /// </summary>
        /// <returns></returns>
        public static string GetLocDriv()
        {
            bool foundGpu = false;
            string OfflineGPUVersion = null;

            // query local driver version
            try
            {
                foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT * FROM Win32_VideoController").Get())
                {
                    if (obj["Description"].ToString().ToLower().Contains("nvidia"))
                    {
                        OfflineGPUVersion = obj["DriverVersion"].ToString().Replace(".", string.Empty).Substring(5);
                        OfflineGPUVersion = OfflineGPUVersion.Substring(0, 3) + "." + OfflineGPUVersion.Substring(3); // add dot
                        foundGpu = true;
                        break;
                    }
                }

                if (!foundGpu)
                    throw new InvalidDataException();

                return OfflineGPUVersion;
            }
            catch (InvalidDataException)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a standard Windows shortcut.
        /// </summary>
        /// <param name="shortcutName"></param>
        /// <param name="shortcutPath"></param>
        /// <param name="targetFileLocation"></param>
        /// <param name="description"></param>
        public static void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation, string description)
        {
            // It seems unnecessarily complex to create a simple shortcut using C#. Oh well.
            string shortcutLocation = Path.Combine(shortcutPath, shortcutName + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

            shortcut.Description = description;
            shortcut.TargetPath = targetFileLocation;
            shortcut.Save();
        }
        /// <summary>
        /// Checks if application is already running.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsInstanceOpen(string name)
        {
            // This basically counts the processes named like the supplied string. If the count is more than 0, it will return true.
            // Let's hope nobody manages to open this application 2,147,483,647 times, because then the int would overflow and crash EnvyUpdate. But I suppose you've got worse problems than that if you've got 2,147,483,647 instances of any process.
            int count = 0;
            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName.Contains(name))
                {
                    count++;
                }
            }

            if (count > 1)
                return true;
            else
                return false;
        }
        /// <summary>
        /// Shows main window and restores WindowState
        /// </summary>
        public static void ShowMain()
        {
            Application.Current.MainWindow.Show();
            Application.Current.MainWindow.WindowState = WindowState.Normal;
        }
        /// <summary>
        /// Checks for newest EnvyUpdate version.
        /// </summary>
        /// <returns></returns>
        public static string GetNewVer()
        {
            // This will fetch the most recent version's tag on GitHub.
            string updPath = "https://api.github.com/repos/fyr77/envyupdate/releases/latest";

            WebClient wc = new WebClient();
            // Use some user agent to not get 403'd by GitHub.
            wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Trident/7.0; rv:11.0) like Gecko");
            string webData = wc.DownloadString(updPath);
            dynamic data = JsonConvert.DeserializeObject(webData);
            string version = data.tag_name;

            return version;
        }

        /// <summary>
        /// Updates the application by downloading the new version from Github and replacing the old file using a seperate CMD instance.
        /// </summary>
        public static void UpdateApp()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\envyupdate\\";

            using (var client = new WebClient())
            {
                client.DownloadFile("https://github.com/fyr77/EnvyUpdate/releases/latest/download/EnvyUpdate.exe", appdata + "EnvyUpdated.exe");
            }

            MessageBox.Show("New version of EnvyUpdate found. Application will restart.\nThis will probably take a few seconds.");

            // Replace exe with new one.
            // This starts a seperate cmd process which will wait a bit, then delete EnvyUpdate and rename the previously downloaded EnvyUpdated.exe to EnvyUpdate.exe
            // I know this is a bit dumb, but I honestly couldn't think of a different way to solve this properly, since the Application would need to delete itself.
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = appdata,
                FileName = "cmd.exe",
                Arguments = "/C timeout 5 && del EnvyUpdate.exe && ren EnvyUpdated.exe EnvyUpdate.exe && EnvyUpdate.exe"
            };
            process.StartInfo = startInfo;
            process.Start();

            Environment.Exit(2);
        }
        public static int GetIDs(string IDtype)
        {
            // TODO: check for 2 occurences of GPU - if yes ask if mobile!!!
            string xmlcontent = null;
            int id = -1;

            using (var wc = new WebClient())
            {
                switch (IDtype)
                {
                    case "psid":
                    case "pfid":
                        xmlcontent = wc.DownloadString("https://www.nvidia.com/Download/API/lookupValueSearch.aspx?TypeID=3");
                        break;
                    case "osid":
                        xmlcontent = wc.DownloadString("https://www.nvidia.com/Download/API/lookupValueSearch.aspx?TypeID=4");
                        break;
                    case "langid":
                        xmlcontent = wc.DownloadString("https://www.nvidia.com/Download/API/lookupValueSearch.aspx?TypeID=5");
                        break;
                    default:
                        break;
                }
            }
            XDocument xDoc = XDocument.Parse(xmlcontent);

            if (IDtype == "pfid")
            {
                string gpuName = GetGPUName();
                id = GetValueFromName(xDoc, gpuName); 
            }

            return id;
        }
        /// <summary>
        /// Gets Value from Nvidias XML docs by searching for the name. Can be used for OS, Lang and GPU.
        /// </summary>
        /// <param name="xDoc"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        private static int GetValueFromName (XDocument xDoc, string query)
        {
            int value = 0;

            var names = xDoc.Descendants("Name");
            foreach (var name in names)
            {
                int i = 0;
                string sName = name.Value.ToString().ToLower();
                if (sName == query)
                {
                    int value1 = 0;
                    int value2 = 0;

                    string result = name.Parent.Value;
                    int index = result.IndexOf(sName);
                    string cleanResult = (index < 0)
                        ? result
                        : result.Remove(index, sName.Length);

                    if (i == 0)
                    {
                        value1 = int.Parse(cleanResult);
                    }
                    else
                    {
                        value2 = int.Parse(cleanResult);
                    }
                    
                    value = value1;

                    if (GlobalVars.mobile)
                    {
                        value = value2;
                    }

                    i++;
                }
            }

            return value;
        }
        /// <summary>
        /// Returns GPU name in lower case.
        /// </summary>
        /// <returns></returns>
        private static string GetGPUName()
        {
            string GPUName = null;
            foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT * FROM Win32_VideoController").Get())
            {
                if (obj["Description"].ToString().ToLower().Contains("radeon"))
                {
                    // If it's an AMD card, use the "Name" field, because they use chip code numbers in "VideoProcessor", which we do not need.
                    // Todo for 3.0: Find a way to ignore mobile Radeon GPUs in Laptops.
                    // For now: Since we only care about Nvidia GPUs, don't break even if an AMD card is found.
                    GPUName = obj["Name"].ToString().ToLower();
                    //break;
                }
                if (obj["Description"].ToString().ToLower().Contains("nvidia"))
                {
                    // If it's an Nvidia GPU, use VideoProcessor so we don't have to truncate the resulting string.
                    GPUName = obj["VideoProcessor"].ToString().ToLower();
                    break;
                }
            }
            // This should NEVER return null outside of debugging mode, since EnvyUpdate should refuse to start without and Nvidia GPU.
            return GPUName;
        }
    }
}