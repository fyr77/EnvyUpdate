using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.Linq;


namespace xmltest
{
    class Program
    {
        static void Main(string[] args)
        {
            string xmlcontent = null;

            using (var wc = new WebClient())
            {
                xmlcontent = wc.DownloadString("https://www.nvidia.com/Download/API/lookupValueSearch.aspx?TypeID=3");
            }
            var xDoc = XDocument.Parse(xmlcontent);

            var names = xDoc.Descendants("Name");
            foreach (var name in names)
            {
                string sname = name.Value.ToString();
                if (sname == "GeForce RTX 2080") 
                {
                    string value = name.Parent.Value;
                    int index = value.IndexOf(sname);
                    string cleanValue = (index < 0)
                        ? value
                        : value.Remove(index, sname.Length);

                    Console.WriteLine(cleanValue);
                }
            }
        }
    }
}