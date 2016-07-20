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

namespace TranscribeVideo
{

    class Program
    {
        //Test
        public static string outFolder = @"C:\SubTwitr\Output\";
        static void Main(string[] args)
        {
            _context = new CloudMediaContext(_accountName, _accountKey);
            var inputFile = args[0];
            Guid g = Guid.NewGuid();
            var outputFolder = @"C:\SubTwitr\Output\" + g.ToString();
            System.IO.Directory.CreateDirectory(outputFolder);
            outFolder = outputFolder;
            RunIndexingJob(g, inputFile, outputFolder, _configurationFile);
        }

        private static CloudMediaContext _context = null;
        private const string _mediaProcessorName = "Azure Media Indexer";
        private const string _configurationFile = "default.config";
        private static readonly string _accountName =
            ConfigurationManager.AppSettings["accountName"];
        private static readonly string _accountKey =
            ConfigurationManager.AppSettings["accountKey"];

        static bool RunIndexingJob(Guid g, string inputFilePath, string outputFolder, string configurationFile = "")
        {
            IAsset asset = _context.Assets.Create("Indexer_Asset", AssetCreationOptions.None);
            string parentpath = System.IO.Directory.GetParent(inputFilePath).FullName;

            FileInfo f = new FileInfo(inputFilePath);

            while (IsFileLocked(f) == true) { Console.WriteLine("Waiting..."); }
 

            File.Move(inputFilePath, parentpath + g.ToString() + ".mp4");
            string newPath = parentpath + g.ToString() + ".mp4";
            var assetFile = asset.AssetFiles.Create(Path.GetFileName(newPath));
            assetFile.Upload(newPath);

            IMediaProcessor indexer = GetLatestMediaProcessorByName(_mediaProcessorName);
            IJob job = _context.Jobs.Create("subtwitr Indexing Job");
            string configuration = "";
            if (!String.IsNullOrEmpty(configurationFile))
            {
                configuration = File.ReadAllText(configurationFile);
            }
            ITask task = job.Tasks.AddNew("Indexing task",
                              indexer,
                              configuration,
                              TaskOptions.None);

            // Specify the input asset to be indexed.
            task.InputAssets.Add(asset);

            // Add an output asset to contain the results of the job.
            task.OutputAssets.AddNew("Indexed video", AssetCreationOptions.None);

            job.StateChanged += new EventHandler<JobStateChangedEventArgs>(StateChanged);
            job.Submit();
            // Check job execution and wait for job to finish. 
            Task progressPrintTask = new Task(() =>
            {
                IJob jobQuery = null;
                do
                {
                    var progressContext = new CloudMediaContext(_accountName,
                                    _accountKey);
                    jobQuery = progressContext.Jobs.Where(j => j.Id == job.Id).First();
                    Console.WriteLine(string.Format("{0}\t{1}\t{2}",
                          DateTime.Now,
                          jobQuery.State,
                          jobQuery.Tasks[0].Progress));
                    Thread.Sleep(10000);
                }
                while (jobQuery.State != JobState.Finished &&
                       jobQuery.State != JobState.Error &&
                       jobQuery.State != JobState.Canceled);
            });
            progressPrintTask.Start();

            // Check job execution and wait for job to finish. 
            Task progressJobTask = job.GetExecutionProgressTask(CancellationToken.None);
            progressJobTask.Wait();

            // If job state is Error, the event handling 
            // method for job progress should log errors.  Here you check 
            // for error state and exit if needed.
            if (job.State == JobState.Error)
            {
                Console.WriteLine("Exiting method due to job error.");
                return false;
            }

            // Download the job outputs.
            DownloadAsset(job.OutputMediaAssets.First(), outputFolder);


            File.Copy(newPath, outputFolder + "\\in.mp4");
            File.Copy("C:\\ffmpeg\\bin\\ffmpeg.exe", outputFolder + "\\ffmpeg.exe");
            SubtitleConverter.Convert.SubConvert(outputFolder + "\\" + g.ToString() + ".mp4.vtt", outputFolder);
            ffmpeg("in.mp4", "subtitle.srt", outputFolder);
            //Directory.Delete(parent, true);
            string[] fileEntries = Directory.GetFiles(outputFolder);
            File.Delete(newPath);
            //cleanup(fileEntries);
            

            job.OutputMediaAssets.First().Delete();
            asset.Delete();

            return true;
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

        // helper function: event handler for Job State
        static void StateChanged(object sender, JobStateChangedEventArgs e)
        {
            Console.WriteLine("Job state changed event:");
            Console.WriteLine("  Previous state: " + e.PreviousState);
            Console.WriteLine("  Current state: " + e.CurrentState);
            switch (e.CurrentState)
            {
                case JobState.Finished:
                    Console.WriteLine();
                    Console.WriteLine("Job finished. Please wait for local tasks/downloads");
                    break;
                case JobState.Canceling:
                case JobState.Queued:
                case JobState.Scheduled:
                case JobState.Processing:
                    Console.WriteLine("Please wait...\n");
                    break;
                case JobState.Canceled:
                    Console.WriteLine("Job is canceled.\n");
                    break;
                case JobState.Error:
                    Console.WriteLine("Job failed.\n");
                    break;
                default:
                    break;
            }
        }


        private static void cleanup(string[] fileEntries)
        {
            foreach (string file in fileEntries)
            {
                if (file == "outfile.mp4") { }
                else
                {
                    if (IsFileInUse(file) == false)
                    {
                        File.Delete(file);
                    }
                } 
            }
        }

        public static bool IsFileInUse(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("'path' cannot be null or empty.", "path");

            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) { }
            }
            catch (IOException)
            {
                var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                stream.Close();
                return true;
            }

            return false;
        }
        // helper method to download the output assets
        static void DownloadAsset(IAsset asset, string outputDirectory)
        {
            foreach (IAssetFile file in asset.AssetFiles)
            {
                    string path = Path.Combine(outputDirectory + "\\" + file.Name);
                    file.Download(path);
            }
        }

        private static IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {
            var processor = _context.MediaProcessors
                        .Where(p => p.Name == mediaProcessorName)
                        .ToList()
                        .OrderBy(p => new Version(p.Version))
                        .LastOrDefault();

            if (processor == null)
                throw new ArgumentException(string.Format("Unknown media processor", mediaProcessorName));

            return processor;
        }

        static void GetParent(string path)
        {
            try
            {
                System.IO.DirectoryInfo directoryInfo =
                    System.IO.Directory.GetParent(path);

                System.Console.WriteLine(directoryInfo.FullName);
            }
            catch (ArgumentNullException)
            {
                System.Console.WriteLine("Path is a null reference.");
            }
            catch (ArgumentException)
            {
                System.Console.WriteLine("Path is an empty string, " +
                    "contains only white spaces, or " +
                    "contains invalid characters.");
            }
        }


        static void ffmpeg(string video, string subtitle, string outfolder)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            // Enter in the command line arguments, everything you would enter after the executable name itself
            start.Arguments = "-i in.mp4 -vf scale:700:-1 -b:v 500k -vf subtitles=subtitle.srt -codec:v libx264 outfiletmp.mp4";
            // Enter the executable to run, including the complete path
            start.FileName = outfolder + "\\ffmpeg.exe";
            // Do you want to show a console window?
            start.WindowStyle = ProcessWindowStyle.Normal;
            start.CreateNoWindow = false;
            start.UseShellExecute = true;
            start.WorkingDirectory = outfolder;
            int exitCode;

            try
            {
                using (Process proc = Process.Start(start))
                {
                    proc.WaitForExit();
                    // Retrieve the app's exit code
                    exitCode = proc.ExitCode;
                }
            }
            catch
            {

            }

            ProcessStartInfo start2 = new ProcessStartInfo();
            // Enter in the command line arguments, everything you would enter after the executable name itself
            start2.Arguments = "-i outfiletmp.mp4 -b:v 500k -vf scale=iw/2:-1 outfile.mp4";
            // Enter the executable to run, including the complete path
            start2.FileName = outfolder + "\\ffmpeg.exe";
            // Do you want to show a console window?
            start2.WindowStyle = ProcessWindowStyle.Normal;
            start2.CreateNoWindow = false;
            start2.UseShellExecute = true;
            start2.WorkingDirectory = outfolder;
            int exitCode2;

            try
            {
                using (Process proc2 = Process.Start(start2))
                {
                    proc2.WaitForExit();
                    // Retrieve the app's exit code
                    exitCode2 = proc2.ExitCode;
                }
            }
            catch
            {

            }


            TweetVideo.TweetVideo.SendTweet(outfolder + "\\outfile.mp4", "Test");

            Thread.Sleep(100000);
        }

        static void tweet(string video, string tweet)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.Arguments = video + " " + tweet;
            start.FileName = @"C:\subtwitr\TranscribeVideo\TweetVideo\bin\Debug\TweetVideo.exe";
            start.WindowStyle = ProcessWindowStyle.Normal;
            start.CreateNoWindow = false;
            start.UseShellExecute = true;
            int exitCode;

            try
            {
                using (Process proc = Process.Start(start))
                {
                    proc.WaitForExit();
                    // Retrieve the app's exit code
                    exitCode = proc.ExitCode;
                }
            }
            catch
            {

            }

        }


    }

}
