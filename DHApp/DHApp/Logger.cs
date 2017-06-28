using System;
using System.IO;

namespace DHApp
{
    public static class Logger
    {
        static Logger() => Log("Log started");

        public static void Log(string message)
        {
            using (var writer = new StreamWriter($@"DHApp-{DateTime.Now.Date.ToString("d-MMM-yy")}.log", true))
            {
                writer.WriteLine("[{0}] {1}", DateTime.Now.ToString("HH:mm:ss"), message);
                writer.Close();
            }
        }
    }
}
