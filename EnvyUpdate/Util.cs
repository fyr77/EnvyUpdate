using IWshRuntimeLibrary;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text.RegularExpressions;
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
                Debug.LogToFile("INFO Looking for driver version in ManagementObjects");

                foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT * FROM Win32_VideoController").Get())
                {
                    if (obj["Description"].ToString().ToLower().Contains("nvidia"))
                    {
                        OfflineGPUVersion = obj["DriverVersion"].ToString().Replace(".", string.Empty);
                        OfflineGPUVersion = OfflineGPUVersion.Substring(Math.Max(0, OfflineGPUVersion.Length - 5));
                        OfflineGPUVersion = OfflineGPUVersion.Substring(0, 3) + "." + OfflineGPUVersion.Substring(3); // add dot
                        foundGpu = true;
                        Debug.LogToFile("INFO Found driver in ManagementObjects.");
                        break;
                    }
                }

                if (!foundGpu)
                {
                    Debug.LogToFile("WARN Did NOT find driver in ManagementObjects.");
                    throw new InvalidDataException();
                }

                return OfflineGPUVersion;
            }
            catch (InvalidDataException)
            {
                return null;
            }
        }

        /// <summary>
        /// Check for existence of Nvidia GPU
        /// </summary>
        /// <returns></returns>
        public static bool IsNvidia()
        {
            Debug.LogToFile("INFO Checking for existence of Nvidia GPU.");

            foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT * FROM Win32_VideoController").Get())
            {
                if (obj["Description"].ToString().ToLower().Contains("nvidia"))
                    return true;
            }
            return false;
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
            Debug.LogToFile("INFO Saving shortcut link.");
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
            Application.Current.MainWindow.Activate();
        }

        /// <summary>
        /// Deletes EnvyUpdate.exe by calling cmd
        /// </summary>
        public static void SelfDelete()
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = GlobalVars.legacyAppdata,
                FileName = "cmd.exe",
                Arguments = "/C timeout 5 && del EnvyUpdate.exe"
            };
            process.StartInfo = startInfo;
            process.Start();

            Environment.Exit(2);
        }
        public static int GetIDs(string IDtype, bool retry = true)
        {
            string xmlcontent = null;
            int id = -1;

            Debug.LogToFile("INFO Getting Nvidia GPU list...");
            using (var wc = new WebClient())
            {
                switch (IDtype)
                {
                    case "psid":
                    case "pfid":
                        xmlcontent = wc.DownloadString("https://www.nvidia.com/Download/API/lookupValueSearch.aspx?TypeID=3");
                        break;
                    default:
                        break;
                }
            }

            Debug.LogToFile("INFO Got Nvidia GPU list.");
            if (xmlcontent == null)
            {
                switch (IDtype)
                {
                    case "osid":
                        id = GetOSID();
                        Debug.LogToFile("INFO Got osid: " + id);
                        break;
                    case "psid":
                    case "pfid":
                        Debug.LogToFile("WARN GPU list is NULL! This is a possible error source.");
                        if (retry)
                        {
                            Debug.LogToFile("WARN Trying to get ID again.");
                            id = GetIDs(IDtype, false);
                        }
                        else
                        {
                            Debug.LogToFile("FATAL Could not get GPU list to find IDs.");
                            throw new ArgumentNullException();
                        }
                        break;
                    default:
                        Debug.LogToFile("WARN GetIDs was called, but nothing was specified. THIS SHOULD NOT HAPPEN!");
                        break;
                }
            }
            else
            {
                XDocument xDoc = XDocument.Parse(xmlcontent);
                string gpuName = GetGPUName(true);
                switch (IDtype)
                {
                    case "psid":
                        id = GetValueFromName(xDoc, gpuName, true);
                        Debug.LogToFile("INFO Got psid: " + id);
                        break;
                    case "pfid":
                        id = GetValueFromName(xDoc, gpuName, false);
                        Debug.LogToFile("INFO Got pfid: " + id);
                        break;
                    default:
                        Debug.LogToFile("WARN GetIDs was called, but nothing was specified.");
                        break;
                }
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
            int value2 = 0; //Two values are used to cover the eventuality of there being two Nvidia cards (unlikely) in a mobile device

            var names = xDoc.Descendants("Name");
            foreach (var name in names) // Looping through the XML Doc because the name is not the primary key
            {
                string sName = name.Value.ToString().ToLower();
                if (sName.Contains(query))
                {
                    Debug.LogToFile("INFO Matched GetValueFromName query: " + sName);
                    string cleanResult = null;

                    if (psid)
                    {
                        Debug.LogToFile("INFO Getting psid.");

                        if (i == 0)
                            value1 = int.Parse(name.Parent.FirstAttribute.Value);
                        else
                            value2 = int.Parse(name.Parent.FirstAttribute.Value);
                    }
                    else
                    {
                        Debug.LogToFile("INFO Getting something other than psid.");

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
            int value;

            if (Environment.OSVersion.Version.Build < 22000)
            {
                // This means we are running Windows 10.
                if (Environment.Is64BitOperatingSystem)
                    value = 57;
                else
                    value = 56;
            }
            else
            {
                // This must be Windows 11 (for now, until Windows 12 comes along)
                value = 135; // No need to check for 64bit, Win11 can not be 32bit.
            }

            return value;
        }
        /// <summary>
        /// Returns GPU name in lower case.
        /// </summary>
        /// <returns></returns>
        public static string GetGPUName(bool lower)
        {
            string GPUName = null;

            Debug.LogToFile("INFO Trying to get GPU name from ManagementObjects...");
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
                        GPUName = Regex.Match(GPUName, "(geforce )((.tx )|(mx))?\\w*\\d*( ti)?").Value;
                    }
                    else
                        GPUName = obj["VideoProcessor"].ToString();

                    break;
                }
            }
            Debug.LogToFile("INFO Found GPU name: " + GPUName);
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
                Debug.LogToFile("INFO Found Win32_Battery, assuming mobile device.");
                result = true;
            }
            foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT * FROM Win32_PortableBattery").Get())
            {
                Debug.LogToFile("INFO Found Win32_PortableBattery, assuming mobile device.");
                result = true;
            }

            return result;
        }
        /// <summary>
        /// Checks Windows registry for Nvidia DCH Key. If it is present, returns true.
        /// Can also check file system for existence of DLL if registry access fails
        /// </summary>
        /// <returns></returns>
        public static bool IsDCH()
        {
            try
            {
                Debug.LogToFile("INFO Trying to find DCH key in registry...");
                RegistryKey nvlddmkm = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\services\nvlddmkm", false);
                return nvlddmkm.GetValueNames().Contains("DCHUVen");
            }
            catch (Exception ex)
            {
                if (ex.Message == "Object reference not set to an instance of an object." || ex.InnerException is NullReferenceException)
                {
                    Debug.LogToFile("INFO could not find key. Assuming non-DCH driver.");
                    // Assume no DCH driver is installed if key is not found.
                    return false;
                }
                else
                {
                    try
                    {
                        Debug.LogToFile("WARN Could not read registry, probing file system instead...");
                        //Registry reading error. Check for existance of file nvsvs.dll instead.
                        if (System.IO.File.Exists(Path.Combine(Environment.SystemDirectory, "nvsvs.dll")))
                        {
                            Debug.LogToFile("INFO Found DCH driver file.");
                            return true;
                        }
                        else
                        {
                            Debug.LogToFile("INFO Did not find DCH driver file. Assuming non-DCH driver.");
                            return false;
                        }

                    }
                    catch (Exception)
                    {
                        Debug.LogToFile("FATAL Could not probe file system. Error: " + ex.Message);
                        MessageBox.Show("An error has occured. Please report this on GitHub.\nError:" + ex.Message);
                        Environment.Exit(20);
                        return false;
                    }
                }
            }
        }
        public static int GetDTCID()
        {
            return 1; // Force update to DCH drivers
        }

        public static int GetDTID()
        {
            /*
             * 1 = Game Ready Driver (GRD)
             * 18 = Studio Driver (SD)
             */
            //TODO: find way to differentiate between driver types

            if (System.IO.File.Exists(Path.Combine(GlobalVars.saveDirectory, "sd.envy")))
                return 18;
            else
                return 1;
        }

        public static string GetGpuUrl()
        {
            int psid;
            int pfid;
            int osid;
            int dtcid;
            int dtid;

            if (Debug.isFake)
            {
                Debug.LogToFile("INFO Loading fake IDs.");
                psid = Debug.LoadFakeIDs("psid");
                pfid = Debug.LoadFakeIDs("pfid");
                osid = Debug.LoadFakeIDs("osid");
                dtcid = Debug.LoadFakeIDs("dtcid");
                dtid = Debug.LoadFakeIDs("dtid");
            }
            else
            {
                psid = GetIDs("psid");
                pfid = GetIDs("pfid");
                osid = GetIDs("osid");
                dtcid = GetDTCID();
                dtid = GetDTID();
                Debug.LogToFile("INFO Getting GPU URLs. IDs in order psid, pfid, osid, dtcid, dtid: " + psid + ", " + pfid + ", " + osid + ", " + dtcid + ", " + dtid);
            }
            string gpuUrlBuild = "http://www.nvidia.com/Download/processDriver.aspx?psid=" + psid.ToString() + "&pfid=" + pfid.ToString() + "&osid=" + osid.ToString() + "&dtcid=" + dtcid.ToString() + "&dtid=" + dtid.ToString();

            Debug.LogToFile("INFO Built GPU URL: " + gpuUrlBuild);

            string gpuUrl;

            using (var c = new WebClient())
            {
                gpuUrl = c.DownloadString(gpuUrlBuild);

                Debug.LogToFile("INFO Downloaded driver page URL: " + gpuUrl);

                if (gpuUrl.Contains("https://") || gpuUrl.Contains("http://"))
                {
                    //absolute url
                }
                else if (gpuUrl.Contains("//"))
                {
                    //protocol agnostic url
                    gpuUrl = "https:" + gpuUrl;
                }
                else if (gpuUrl.StartsWith("driverResults.aspx"))
                {
                    //relative url
                    gpuUrl = "https://www.nvidia.com/Download/" + gpuUrl;
                }
                else if (gpuUrl.Contains("No certified downloads were found"))
                {
                    //configuration not supported
                    throw new ArgumentException(gpuUrlBuild);
                }
                else
                {
                    //panic.
                    Debug.LogToFile("FATAL Unexpected web response: " + gpuUrl);
                    MessageBox.Show("ERROR: Invalid API response from Nvidia - unexpected web response. Please file an issue on GitHub.");
                    Environment.Exit(10);
                }
            }

            return gpuUrl;
        }

        public static void UninstallAll()
        {
            if (System.IO.File.Exists(Path.Combine(GlobalVars.startup, "\\EnvyUpdate.lnk")))
            {
                Debug.LogToFile("INFO Deleted startup entry.");
                System.IO.File.Delete(Path.Combine(GlobalVars.startup, "\\EnvyUpdate.lnk"));
            }

            if (System.IO.File.Exists(Path.Combine(GlobalVars.startmenu, "\\EnvyUpdate.lnk")))
            {
                Debug.LogToFile("INFO Deleted start menu entry.");
                System.IO.File.Delete(Path.Combine(GlobalVars.startmenu, "\\EnvyUpdate.lnk"));
            }
            if ((GlobalVars.saveDirectory == GlobalVars.legacyAppdata) && System.IO.File.Exists(Path.Combine(GlobalVars.legacyAppdata, "EnvyUpdate.exe")))
            {
                Debug.LogToFile("INFO Deleting EnvyUpdate appdata and self.");
                MessageBox.Show(Properties.Resources.uninstall_legacy_message);
                Util.SelfDelete();
            }
            else if (Directory.Exists(GlobalVars.legacyAppdata))
            {
                Debug.LogToFile("INFO Deleting EnvyUpdate appdata folder");
                Directory.Delete(GlobalVars.legacyAppdata, true);
            }
        }

        public static bool IsDarkTheme()
        {
            try
            {
                Debug.LogToFile("INFO Trying to get app theme...");
                int res = (int)Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", -1);
                switch (res)
                {
                    case 0:
                        Debug.LogToFile("INFO Using dark theme.");
                        return true;
                    case 1:
                        Debug.LogToFile("INFO Using light theme.");
                        return false;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
            catch (Exception)
            {
                Debug.LogToFile("WARN Could not determine theme. Setting light theme.");
                return false;
            }
        }

        public static string GetDirectDownload(string gpuUrl)
        {
            string webcontent;
            using (var wc = new WebClient())
                webcontent = wc.DownloadString(gpuUrl);

            string directUrl = Regex.Match(webcontent, "\\/Windows\\/\\d+\\.\\d+\\/[\\w\\d\\/\\-\\.]*exe").Value;
            directUrl = "https://us.download.nvidia.com" + directUrl;
            return directUrl;
        }

        public static string GetSevenZip()
        {
            string path;
            if (ExistsOnPath("7zg.exe"))
            {
                path = "7zg.exe";
            }
            else
            {
                path = Path.Combine(GlobalVars.saveDirectory, "7zr.exe");
                using (WebClient client = new WebClient())
                {
                    client.Headers["User-Agent"] = GlobalVars.useragent;
                    client.DownloadFile(new Uri("https://www.7-zip.org/a/7zr.exe"), path);
                }
                Debug.LogToFile("INFO Downloaded 7-zip.");
            }

            return path;
        }

        public static Process ExtractWithSevenZip(string sevenZipPath, string filePath, string destinationDir)
        {
            if (!Directory.Exists(destinationDir))
                Directory.CreateDirectory(destinationDir);

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Minimized,
                WorkingDirectory = destinationDir,
                FileName = sevenZipPath,
                Arguments = "x -aoa -y " + filePath + " Display.Driver Display.Nview Display.Optimus HDAudio MSVCR NVI2 NVPCF PhysX PPC ShieldWirelessController EULA.txt ListDevices.txt setup.cfg setup.exe"
            };
            process.EnableRaisingEvents = true;
            process.StartInfo = startInfo;
            return process;
        }

        public static void CleanInstallConfig(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogToFile("FATAL Driver installer config not found, terminating.");
                MessageBox.Show(Properties.Resources.error_installing);
                Environment.Exit(17);
            }

            Debug.LogToFile("INFO Removing GFE content from installer config.");

            string outfile = filePath + ".out";

            StreamReader sr = new StreamReader(filePath);
            StreamWriter sw = new StreamWriter(outfile);
            string line;

            while ((line = sr.ReadLine()) != null)
            {
                if (new[] { "EulaHtmlFile", "FunctionalConsentFile", "PrivacyPolicyFile" }.Any(c => line.Contains(c)))
                {
                    continue;
                }

                sw.WriteLine(line);
            }

            sw.Close();
            sr.Close();

            System.IO.File.Delete(filePath);
            System.IO.File.Move(outfile, filePath);

            Debug.LogToFile("INFO Finished removing GFE content from installer config.");
        }

        private static bool ExistsOnPath(string fileName)
        {
            return GetFullPath(fileName) != null;
        }

        private static string GetFullPath(string fileName)
        {
            if (System.IO.File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (System.IO.File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }

        public static bool HasWritePermissions()
        {
            try
            {
                System.IO.File.Create(Path.Combine(GlobalVars.saveDirectory, "writeable.envy")).Close();
                System.IO.File.Delete(Path.Combine(GlobalVars.saveDirectory, "writeable.envy"));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void MoveFilesToAppdata()
        {
            string[] envyFiles = Directory.GetFiles(GlobalVars.directoryOfExe, "*.envy");
            foreach (var item in envyFiles)
                System.IO.File.Move(item, Path.Combine(GlobalVars.appdata, Path.GetFileName(item)));
            if (System.IO.File.Exists(Path.Combine(GlobalVars.directoryOfExe, "envyupdate.log")))
                System.IO.File.Move(Path.Combine(GlobalVars.directoryOfExe, "envyupdate.log"), Path.Combine(GlobalVars.appdata, "envyupdate.log"));
        }

        public static void MoveFilesToExe()
        {
            string[] envyFiles = Directory.GetFiles(GlobalVars.appdata, "*.envy");
            foreach (var item in envyFiles)
                System.IO.File.Move(item, Path.Combine(GlobalVars.directoryOfExe, Path.GetFileName(item)));
            if (System.IO.File.Exists(Path.Combine(GlobalVars.appdata, "envyupdate.log")))
                System.IO.File.Move(Path.Combine(GlobalVars.appdata, "envyupdate.log"), Path.Combine(GlobalVars.directoryOfExe, "envyupdate.log"));
        }
    }
}
