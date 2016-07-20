using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.MediaServices.Client;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace WatchFolder
{
    class Program
    {

        public static string outFolder = @"C:\SubTwitr\Output\";

        static void Main(string[] args)
        {
            watchFolder();
        }

        public static void watchFolder()
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = "S:\\";

            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Press \'q\' to quit.");
            while (Console.Read() != 'q') ;

        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType.ToString() == "Created")
            {
                bool isDirectory = Path.GetExtension(e.FullPath) == string.Empty;
                if ((isDirectory == true) && (e.FullPath.Length == 39))
                {
                    Transcribe(e.FullPath);
                }
                else
                {
                    FileInfo f = new FileInfo(e.FullPath);
                    while (IsFileLocked(f) == true) { Console.WriteLine("Waiting..."); }
                    File.Delete(e.FullPath);
                }
            }
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            //Just popping in for potential future usage.
        }

        static void Transcribe (string inputFile)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            // Enter in the command line arguments, everything you would enter after the executable name itself
            start.Arguments = inputFile;
            // Enter the executable to run, including the complete path
            start.FileName = "TranscribeVideo.exe";
            // Do you want to show a console window?
            start.WindowStyle = ProcessWindowStyle.Normal;
            start.CreateNoWindow = false;
            start.UseShellExecute = true;
            start.WorkingDirectory = @"C:\subtwitr\TranscribeVideo\TranscribeVideo\bin\Debug";
            //int exitCode;

            try
            {
                Process proc = Process.Start(start);
            }
            catch
            {

            }

        }

        static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }


    }
}
