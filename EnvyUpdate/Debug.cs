using System;
using System.IO;
using System.Linq;

namespace EnvyUpdate
{
    class Debug
    {
        public static int LoadFakeIDs(string idType)
        {
            /* 
             * Usage: Supply /debug flag to exe. Imitates a GTX 1080ti on Win10 x64 DCH Game Ready Driver.
             */ 
            switch (idType)
            {
                case "psid":
                    return 101;
                case "pfid":
                    return 845;
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
            return "Nvidia GeForce GTX 1080ti (debug)";
        }
    }
}
