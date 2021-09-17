using IWshRuntimeLibrary;
using Microsoft.Win32;
using Onova;
using Onova.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

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
                        OfflineGPUVersion = obj["DriverVersion"].ToString().Replace(".", string.Empty);
                        OfflineGPUVersion = OfflineGPUVersion.Substring(Math.Max(0, OfflineGPUVersion.Length - 5));
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
        /// <param name="arguments"></param>
        public static void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation, string description, string arguments = "")
        {
            // It seems unnecessarily complex to create a simple shortcut using C#. Oh well.
            string shortcutLocation = Path.Combine(shortcutPath, shortcutName + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

            shortcut.Arguments = arguments;
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
        /// Deletes EnvyUpdate.exe by calling cmd
        /// </summary>
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
                    id = GetValueFromName(xDoc, gpuName, true);
                    break;
                case "pfid":
                    id = GetValueFromName(xDoc, gpuName, false);
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
        private static int GetValueFromName(XDocument xDoc, string query, bool psid)
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
                        GPUName = Regex.Replace(GPUName, " \\d+gb", "");
                        GPUName = Regex.Replace(GPUName, "nvidia ", "");
                    }
                    else
                        GPUName = obj["VideoProcessor"].ToString();

                    break;
                }
            }
            // This should NEVER return null outside of debugging mode, since EnvyUpdate should refuse to start without and Nvidia GPU.
            return GPUName;
        }
        /// <summary>
        /// Checks for Battery and assumes a mobile GPU if present.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Checks Windows registry for Nvidia DCH Key. If it is present, returns true.
        /// Can also check file system for existance of DLL if registry access fails
        /// </summary>
        /// <returns></returns>
        public static bool IsDCH()
        {
            try
            {
                RegistryKey nvlddmkm = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\services\nvlddmkm", true);
                return nvlddmkm.GetValueNames().Contains("DCHUVen");
            }
            catch (Exception ex)
            {
                if (ex.Message == "Object reference not set to an instance of an object." || ex.InnerException is NullReferenceException)
                {
                    // Assume no DCH driver is installed if key is not found.
                    return false;
                }
                else
                {
                    try
                    {
                        //Registry reading error. Check for existance of file nvsvs.dll instead.
                        if (System.IO.File.Exists(Path.Combine(Environment.SystemDirectory, "nvsvs.dll")))
                            return true;
                        else
                            return false;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("An error has occured. Please report this on GitHub.\nError:" + ex.Message);
                        Environment.Exit(20);
                        return false;
                    }
                }
            }
        }
        public static int GetDTCID()
        {
            int dtcid = 0;
            if (IsDCH())
            {
                dtcid = 1;
            }
            return dtcid;
        }

        public static int GetDTID()
        {
            /*
             * 1 = Game Ready Driver (GRD)
             * 18 = Studio Driver (SD)
             */
            //TODO: find way to differentiate between driver types

            return 1;
        }

        public static async Task DoUpdateAsync()
        {
            using (var manager = new UpdateManager(new GithubPackageResolver("fyr77", "EnvyUpdate", "EnvyUpdate*.zip"), new ZipPackageExtractor()))
            {
                // Check for new version and, if available, perform full update and restart
                await manager.CheckPerformUpdateAsync();
            }
        }
    }
}