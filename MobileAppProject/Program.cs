using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace MobileAppProject
{
    class Program
    {
        static void Main(string[] args)
        {
            string strInputFile = ConfigurationManager.AppSettings["inputFile"];
            string[] lines = System.IO.File.ReadAllLines(strInputFile);
            string strOutputFile = ConfigurationManager.AppSettings["outputFile"];
            int Index = -1;
            if (File.Exists( strOutputFile))
            {
                string[] linesOutput = System.IO.File.ReadAllLines(strOutputFile);
                Index = GetRowNumber(lines, linesOutput);
            }

            Console.WriteLine("Starting Index:" + (Index + 2));

            string docPath = ConfigurationManager.AppSettings["APKDirectory"];
            lines= lines.Skip(Index+1 ).ToArray();
            foreach (string line in lines)
            {
                string[] columns = line.Split(','); string baseUri = "https://androzoo.uni.lu/api/download?apikey=ccbc169234b20fef760f7717b0a5a0ad0e7dcafc9c4186804e9a6d03433142c8&sha256=" + columns[0];
                string fName = columns[0];
                string pName = columns[1];
                Console.Write(Index + 2 + ": " + pName);
                string strRatings = GetAppDetails(pName);
                Console.Write("-" + strRatings);
                Dfile(baseUri, fName, pName + ".txt");
                FileChecker(fName, docPath, pName, strRatings);
                Index++;

            }
        }
        public static int GetRowNumber(string[] linesInput, string[] linesOutput)
        {
            int rowNumber = 0;
            try
            {
           
                string strOutputLastRow = "";
                string sHApk = "";
                int index = linesOutput.Length - 1;
                while (linesOutput[index].Split(new string[] { "," }, StringSplitOptions.None).Length < 2)
                {
                    index--;
                }
                sHApk = linesOutput[index].Split(new string[] { "," }, StringSplitOptions.None)[2];

                for (int i = 0; i < linesInput.Length; i++)
                {
                    string line = linesInput[i];
                    if (line.Split(new string[] { "," }, StringSplitOptions.None).Length == 2)
                    {
                        line = line.Split(new string[] { "," }, StringSplitOptions.None)[0];
                        if (line == sHApk)
                        {
                            return i;
                        }
                    }


                }
            }
            catch (Exception)
            {

 
            }
            return rowNumber;
        }
        public static string GetAppDetails(string strAPKName)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create("https://play.google.com/store/apps/details?id=" + strAPKName);
                req.Timeout = 60 * 1000;
                req.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; .NET4.0C; .NET4.0E)";
                using (var resp = req.GetResponse())
                {
                    var html = new StreamReader(resp.GetResponseStream()).ReadToEnd();

                    string strAPpName = "";
                    strAPpName = html.Substring(0, html.IndexOf("Apps on Google Play")).Substring(html.Substring(0, html.IndexOf("Apps on Google Play")).LastIndexOf(">"));
                    strAPpName = strAPpName.Substring(strAPpName.IndexOf("content=\"") + 9);
                    strAPpName = strAPpName.Trim().Substring(0, strAPpName.Trim().Length - 1).Trim();

                    string strRating = "";
                    if (html.Contains("class=\"BHMmbe\""))
                    {
                        strRating = html.Substring(html.IndexOf("class=\"BHMmbe\""));
                        strRating = strRating.Substring(0, html.Substring(html.IndexOf("class=\"BHMmbe\"")).IndexOf("<"));
                        strRating = strRating.Substring(strRating.IndexOf(">") + 1);
                    }

                    string strTotalRating = "";
                    if (html.Contains("class=\"EymY4b\""))
                    {
                        strTotalRating = html.Substring(html.IndexOf("class=\"EymY4b\""));
                        strTotalRating = strTotalRating.Substring(0, strTotalRating.IndexOf("ratings\">"));
                        strTotalRating = strTotalRating.Substring(strTotalRating.LastIndexOf("=\"") + 2);
                        strTotalRating = strTotalRating.Replace(",", "");
                    }

                    return strAPpName + "," + strRating + "," + strTotalRating;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The remote server returned an error: (404) Not Found."))
                {
                    Console.Write("- Playstore Not Found");
                }
                else
                {
                    Console.Write("- Playstore Error:" + ex.Message);
                }

            }
            return "";
        }
        static void Dfile(string baseUri, string fName, string packageName)
        {
            using (var wc = new NoKeepAlivesWebClient())
            {
                try
                {
                    string apkDIrectory = ConfigurationManager.AppSettings["APKDirectory"];
                    wc.DownloadFile(baseUri, apkDIrectory + packageName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        static void FileChecker(string fName, string docPath, string pName, string appDetails)
        {
            try
            {
                string filepath = ConfigurationManager.AppSettings["APKDirectory"] + pName + ".txt";
                string appType;
                if (File.ReadAllText(filepath).Contains("libreactnativejni.so"))
                {
                    appType = "React Native";
                    string[] value1 = { "React Native" + "," + pName + "," + fName + "," + appDetails };
                    File.AppendAllLines(ConfigurationManager.AppSettings["outputFile"], value1);
                }
                else if (File.ReadAllText(filepath).Contains("assemblies/Xamarin."))
                {
                    appType = "Xamarin";
                    string[] value2 = { "Xamarin" + "," + pName + "," + fName + "," + appDetails };
                    File.AppendAllLines(ConfigurationManager.AppSettings["outputFile"], value2);
                }
                else
                {
                    appType = "None";
                    string[] value3 = { "None" + "," + pName + "," + fName + "," + appDetails };
                    File.AppendAllLines(ConfigurationManager.AppSettings["outputFile"], value3);
                }
                Console.Write("-" + appType + "\n");
                File.Delete(filepath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
           
        }
        public class NoKeepAlivesWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = base.GetWebRequest(address);
                if (request is HttpWebRequest)
                {
                    ((HttpWebRequest)request).KeepAlive = false;
                }

                return request;
            }
        }
    }

}

