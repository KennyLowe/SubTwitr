using System;
using System.IO;
using System.Threading;

namespace SubtitleConverter
{
    public static class Convert
    {
        public static void SubConvert(string args, string outputfolder)
        {
            const int quitDelay = 1500;
            string file;
            if (args.Length == 0)
            {
                Console.Write("Input file: ");
                file = Console.ReadLine()?.Trim();
            }
            else
                file = args;
            if (string.IsNullOrEmpty(file))
            {
                Console.WriteLine("Input file not specified!");
                Thread.Sleep(quitDelay);
                return;
            }
            if (!File.Exists(file))
            {
                Console.WriteLine("File not found!");
                Thread.Sleep(quitDelay);
                return;
            }
            var encoding = new StreamReader(file, true).CurrentEncoding;
            var input = File.ReadAllText(file, encoding);
            var output = SubtitleHelper.ConvertWebvttToSrt(input);
            File.WriteAllText(outputfolder + "\\subtitle.srt", output, encoding);
            Console.WriteLine("Successfully converted!");
        
        }
    }
}