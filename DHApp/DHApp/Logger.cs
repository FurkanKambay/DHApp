using System;
using System.IO;

namespace DHApp
{
    public static class Logger
    {
        static Logger()
        {
            Log("Log started");
        }

        public static void Log(string text) =>
            File.AppendAllText(
                "DHApp-" + DateTime.Now.Date.ToString("d-MMM-yy") + ".log",
                DateTime.Now.ToString("HH:mm:ss") + " - " + text + Environment.NewLine);
    }
}
