using System;
using System.IO;

namespace EnvyUpdate
{
    class GlobalVars
    {
        public static bool isMobile = false;
        public static readonly string pathToAppExe = System.Reflection.Assembly.GetEntryAssembly().Location;
        public static readonly string directoryOfExe = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        public static string saveDirectory = directoryOfExe;
        public static readonly string startmenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        public static readonly string legacyAppdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\envyupdate\\";
        public static readonly string appdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EnvyUpdate_Data");
        public static readonly string startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        public static bool monitoringInstall = false;
        public static bool startMinimized = false;
        public static bool isInstalling = false;
        public static readonly string useragent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:115.0) Gecko/20100101 Firefox/115.0";
        public static bool useAppdata = false;
        public static bool hasWrite = true;
        public static bool autoDownload = false;
        public static bool isDownloading = false;
    }
}
