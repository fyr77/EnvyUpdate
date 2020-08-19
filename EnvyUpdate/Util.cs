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
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace EnvyUpdate
{
    internal class Util
    {
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
        public static float GetNewVer()
        {
            // This will fetch the most recent version's tag on GitHub.
            string updPath = "https://api.github.com/repos/fyr77/envyupdate/releases/latest";

            WebClient wc = new WebClient();
            // Use some user agent to not get 403'd by GitHub.
            wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Trident/7.0; rv:11.0) like Gecko");
            string webData = wc.DownloadString(updPath);
            dynamic data = JsonConvert.DeserializeObject(webData);

            // I am not catching a possible parsing exception, because I cannot think of any way it could happen. 
            // If there is no internet connection, it should already throw when using the web client.
            float version = float.Parse(data.tag_name);

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

            MessageBox.Show(Properties.Resources.message_new_version);

            // Replace exe with new one.
            // This starts a seperate cmd process which will wait a bit, then delete EnvyUpdate and rename the previously downloaded EnvyUpdated.exe to EnvyUpdate.exe
            // I know this is a bit dumb, but I honestly couldn't think of a different way to solve this properly, since the Application has to delete itself.
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
        public static void SelfDelete()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\envyupdate\\";

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = appdata,
                FileName = "cmd.exe",
                Arguments = "/C timeout 5 && del EnvyUpdate.exe"
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
            string gpuName = GetGPUName(true);

            switch (IDtype)
            {
                case "psid":
                    id = GetValueFromName(xDoc, gpuName, false);
                    break;
                case "pfid":
                    id = GetValueFromName(xDoc, gpuName, true);
                    break;
                case "osid":
                    id = GetOSID();
                    break;
                case "langid":
                    // Currently unsupported, because Nvidia has a weird way of naming languages in their native OR english version.
                    // https://www.nvidia.com/Download/API/lookupValueSearch.aspx?TypeID=5
                    break;
                default:
                    break;
            }

            return id;
        }
        /// <summary>
        /// Gets Value from Nvidias XML docs by searching for the name. Can be used for OS, Lang and GPU.
        /// This will produce problems when run on Linux. Good thing Linux has nice package managers to take care of driver updating.
        /// </summary>
        /// <param name="xDoc"></param>
        /// <param name="query"></param>
        /// <param name="psid"></param>
        /// <returns></returns>
        private static int GetValueFromName (XDocument xDoc, string query, bool psid)
        {
            int value = 0;
            int i = 0;
            int value1 = 0;
            int value2 = 0;

            var names = xDoc.Descendants("Name");
            foreach (var name in names)
            {
                string sName = name.Value.ToString().ToLower();
                if (sName == query)
                {
                    string cleanResult = null;

                    if (psid)
                    {
                        if (i == 0)
                            value1 = int.Parse(name.Parent.FirstAttribute.Value);
                        else
                            value2 = int.Parse(name.Parent.FirstAttribute.Value);
                    }
                    else
                    {
                        string result = name.Parent.Value.ToLower();
                        int index = result.IndexOf(sName);
                        cleanResult = (index < 0)
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
                    }

                    if (GlobalVars.isMobile && (value2 != 0))
                    {
                        value = value2;
                    }
                    else
                    {
                        value = value1;
                    }

                    if (value2 != 0)
                        break;

                    i++;
                }
            }

            return value;
        }
        /// <summary>
        /// Returns hardcoded values for the supported operating systems.
        /// </summary>
        /// <param name="xDoc"></param>
        /// <returns></returns>
        private static int GetOSID()
        {
            // This is faster than making a whole web request and searching through XML. This application only supports 8 possible IDs, so they are hardcoded.
            int value = 0;
            string OS = Environment.OSVersion.Version.Major.ToString() + "." + Environment.OSVersion.Version.Minor.ToString();

            // Here the 32bit values are used. Later, if the OS is 64bit, we'll add 1, since that is how Nvidia does their IDs.
            switch (OS)
            {
                case "10.0":
                    value = 56;
                    break;
                case "6.1":
                    value = 18;
                    break;
                case "6.2":
                    value = 27;
                    break;
                case "6.3":
                    value = 40;
                    break;
                default:
                    break;
            }

            //Simply increment the ID by 1 if OS is 64bit.
            if (Environment.Is64BitOperatingSystem)
                value++;

            return value;
        }
        /// <summary>
        /// Returns GPU name in lower case.
        /// </summary>
        /// <returns></returns>
        public static string GetGPUName(bool lower)
        {
            string GPUName = null;
            foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT * FROM Win32_VideoController").Get())
            {
                //if (obj["Description"].ToString().ToLower().Contains("radeon"))
                //{
                    // If it's an AMD card, use the "Name" field, because they use chip code numbers in "VideoProcessor", which we do not need.
                    // Todo for 3.0: Find a way to ignore mobile Radeon GPUs in Laptops.
                    // For now: Since we only care about Nvidia GPUs, this is commented out.
                    //GPUName = obj["Name"].ToString().ToLower();
                    //break;
                //}
                if (obj["Description"].ToString().ToLower().Contains("nvidia"))
                {
                    // If it's an Nvidia GPU, use VideoProcessor so we don't have to truncate the resulting string.
                    if (lower)
                    {
                        GPUName = obj["VideoProcessor"].ToString().ToLower();
                        // Remove any 3GB, 6GB or similar from name. We don't need to know the VRAM to get results.
                        GPUName = Regex.Replace(GPUName, "\\d+GB", "");
                    }
                    else
                        GPUName = obj["VideoProcessor"].ToString();

                    break;
                }
            }
            // This should NEVER return null outside of debugging mode, since EnvyUpdate should refuse to start without and Nvidia GPU.
            return GPUName;
        }
        public static bool IsMobile()
        {
            bool result = false;

            foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT * FROM Win32_Battery").Get())
            {
                result = true;
            }
            foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT * FROM Win32_PortableBattery").Get())
            {
                result = true;
            }

            return result;
        }
        public static bool IsDCH()
        {
            RegistryKey nvlddmkm = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\services\nvlddmkm", true);
            return nvlddmkm.GetValueNames().Contains("DCHUVen");
        }
    }
}