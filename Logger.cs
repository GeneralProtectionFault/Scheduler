using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler
{
    internal static class Logger
    {
        // Log file - in program folder\Log\ - filename is date, etc...
        private static string logDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\Log\";
        private static string logFile = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "_LOG.txt";
        private static string logPath = logDirectory + logFile;




        /// <summary>
        /// Simply logs whatever text is passed into the string
        /// </summary>
        /// <param name="line"></param>
        public static void Log(string line)
        {
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            // Create the log file if it doesn't exist
            if (!File.Exists(logPath))
                File.Create(logPath).Close();

            using (var tw = new StreamWriter(logPath, true))
            {
                tw.WriteLine(Environment.UserName.ToUpper() + "\t" +
                    DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture) + "\t" + line);
            }
        }
    }



}
