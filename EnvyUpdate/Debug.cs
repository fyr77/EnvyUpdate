using System;
using System.Linq;

namespace EnvyUpdate
{
    class Debug
    {
        readonly static string debugFilePath = GlobalVars.exepath + "debug.txt";
        public static int LoadFakeIDs(string idType)
        {
            /* 
             * Usage:
             * Create debug.txt file.
             * Fill in variables: 
             * Line 1: psid
             * Line 2: pfid
             * Line 3: osid
             * Line 4: dtcid
             * Line 5: Local driver version
             * 
             * Supply /debug flag to exe.
             */ 
            string line = null;
            switch (idType)
            {
                case "psid":
                    line = File.ReadLines(debugFilePath).Take(1).First();
                    break;
                case "pfid":
                    line = File.ReadLines(debugFilePath).Skip(1).Take(1).First();
                    break;
                case "osid":
                    line = File.ReadLines(debugFilePath).Skip(2).Take(1).First();
                    break;
                case "dtcid":
                    line = File.ReadLines(debugFilePath).Skip(3).Take(1).First();
                    break;
                default:
                    break;
            }

            return int.Parse(line);
        }
        public static string LocalDriv()
        {
            return File.ReadLines(debugFilePath).Skip(4).Take(1).First();
        }
    }
}
