using System;
using System.IO;
using System.Runtime.InteropServices;

namespace api.stab
{
    public class Log
    {
        static string GetSlash()
        {
            var windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if(windows)
                return "\\";
            
            return "/";
        }

        static string FilePath
        {
            get
            {
                string path = String.Concat(AppDomain.CurrentDomain.BaseDirectory, "Log");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                string fileName = String.Format("{0}.log", DateTime.Now.ToString("yyyyMMdd"));
                path = String.Concat(path, GetSlash(), fileName);

                return path;
            }
        }

        public static void Register(string text)
        {
            if(!Config.EnableLog)   
                return;

            Console.WriteLine(text);
            
            try
            {
                using (var fs = new FileStream(FilePath, FileMode.OpenOrCreate)) { }

                using (var writer = new StreamWriter(FilePath, true))
                {
                    writer.WriteLineAsync(text);
                }
            }
            catch { }
        }
    }
}