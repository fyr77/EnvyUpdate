using System;
using System.IO;

namespace EnvyUpdate
{
    class GlobalVars
    {
        public static bool isMobile = false;
        public static readonly string exeloc = System.Reflection.Assembly.GetEntryAssembly().Location;
        public static readonly string exedirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\";
        public static readonly string startmenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        public static readonly string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\envyupdate\\";
        public static readonly string startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        public static bool monitoringInstall = false;
        public static readonly string useragent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:115.0) Gecko/20100101 Firefox/115.0";
    }
}
