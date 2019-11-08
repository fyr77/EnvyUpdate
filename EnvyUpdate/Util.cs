using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Net;
using System.IO;
using IWshRuntimeLibrary;

namespace EnvyUpdate
{
    class Util
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
            string shortcutLocation = System.IO.Path.Combine(shortcutPath, shortcutName + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

            shortcut.Description = description;
            shortcut.TargetPath = targetFileLocation;
            shortcut.Save();
        }
    }
}
