using System;

namespace DmpScanConsole
{
    class Program
    {
        private const string FilePath = @"C:\Sandbox\process-dumps\my-process-dump-1K.DMP";

        static void Main(string[] args)
        {
            Scanner.Analyze(FilePath);
            Console.ReadLine();
        }
    }
}
