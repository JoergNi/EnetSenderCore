using Quartz.Logging;
using System;
using System.IO;

namespace EnetSenderCore
{
    public class ConsoleLogProvider : ILogProvider
    {
        private FileInfo _logFile;

        public ConsoleLogProvider()
        {
            _logFile = new FileInfo("log.log");
            Console.WriteLine("Logging to "+_logFile.FullName);
        }

        public Logger GetLogger(string name)
        {
            return (level, func, exception, parameters) =>
            {
                if (level >= LogLevel.Info && func != null)
                {

                    string message = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [" + level + "] " + func() + " " + exception;
                    Console.WriteLine(message);
                    File.AppendAllText(_logFile.FullName, message + "\r\n");
                }
                return true;
            };
        }

        public IDisposable OpenNestedContext(string message)
        {
            throw new NotImplementedException();
        }

        public IDisposable OpenMappedContext(string key, string value)
        {
            throw new NotImplementedException();
        }
    }
}
