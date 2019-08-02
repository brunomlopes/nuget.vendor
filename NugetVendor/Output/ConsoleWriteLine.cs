using System;
using System.Drawing;
using Console = Colorful.Console;

namespace NugetVendor
{
    class ConsoleWriteLine
    {
        public ConsoleWriteLine(bool useColours)
        {
            if (useColours)
            {
                WriteLineWarning = line => Console.WriteLine(line, Color.Yellow);
                WriteLineError = line => Console.WriteLine(line, Color.Red);
                WriteLineSuccess = line => Console.WriteLine(line, Color.Green);
                WriteLineProgress = line => Console.WriteLine(line, Color.Green);
            }
            else
            {
                WriteLineWarning = WriteLineError = WriteLineSuccess = WriteLineProgress = WriteLine;
            }
        }

        public Action<string> WriteLineWarning { get; set; }
        public Action<string> WriteLineError { get; set; }
        public Action<string> WriteLineSuccess { get; set; }
        public Action<string> WriteLineProgress { get; set; }

        public void WriteLine(string line)
        {
            Console.WriteLine(line);
        }
    }
}