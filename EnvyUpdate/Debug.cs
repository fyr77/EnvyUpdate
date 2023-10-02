using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;

namespace EnvyUpdate
{
    class Debug
    {
        public static bool isFake = false;
#if DEBUG
        public static bool isVerbose = true;
#else
        public static bool isVerbose = false;
#endif

        public static int LoadFakeIDs(string idType)
        {
            /* 
             * Usage: Supply /debug flag to exe. Imitates a GTX 1080ti on Win10 x64 DCH Game Ready Driver.
             */ 
            switch (idType)
            {
                case "psid":
                    return 127;
                case "pfid":
                    return 999;
                case "osid":
                    return 57;
                case "dtcid":
                    return 1;
                case "dtid":
                    return 1;
                default:
                    return -1;
            }
        }
        public static string LocalDriv()
        {
            return "466.11";
        }

        public static string GPUname()
        {
            return "Nvidia GeForce RTX 4080 (debug)";
        }

        public static void LogToFile(string content)
        {
            if (isVerbose)
            {
                if (GlobalVars.useAppdata)
                    File.AppendAllText(Path.Combine(GlobalVars.appdata, "envyupdate.log"), content + "\n");
                else
                    File.AppendAllText(Path.Combine(GlobalVars.directoryOfExe, "envyupdate.log"), content + "\n");
            }    
        }
    }
}
