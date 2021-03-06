using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Xabe.FFmpeg;

namespace truyenthanhServerWeb.ServerMp3
{
    public class FFmpegXabe
    {
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        //private bool _bIsConversionRunning = false;
        //internal bool bIsConversionRunning { get => _bIsConversionRunning; set => _bIsConversionRunning = value; }

        private IConversion conversion;

        //private long processID;
        //private double durationPercent = 0;

        public async Task convertMP3(IMediaInfo mediaInfo, int port, uint startPosition_ms)
        {
            //Get latest version of FFmpeg. It's great idea if you don't know if you had installed FFmpeg.
            //await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official)

            //_bIsConversionRunning = true;

            //var mediaInfo = await FFmpeg.GetMediaInfo(pathFile);
            //TimeSpan dur = mediaInfo.Duration;

            //var videoStream = mediaInfo.VideoStreams.First();
            IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault()?
                .SetBitrate(48000)
                .SetChannels(1)
                .SetSampleRate(24000);

            //string outPath = Path.Combine("Song", "out.mp3");
            //string udpParam = "-f mp3 udp://127.0.0.1:" + port.ToString();
            string outPath = "udp://127.0.0.1:" + port.ToString();

            //Create new conversion object
            conversion = FFmpeg.Conversions.New()
                //conversion in realtime
                .AddParameter("-re", ParameterPosition.PreInput)
                //position begin
                .AddParameter($"-ss {TimeSpan.FromSeconds(startPosition_ms/1000)}", ParameterPosition.PreInput)
                //Add audio stream to output file
                .AddStream(audioStream)
                //.SetOutputFormat(Format.mp3)
                //Set output file path
                //.SetOutput(outPath);
            //SetOverwriteOutput to overwrite files. It's useful when we already run application before
            //.SetOverwriteOutput(true)
            //Disable multithreading
            //.UseMultiThread(true)
            //Set conversion preset. You have to chose between file size and quality of video and duration of conversion
            //.SetPreset(ConversionPreset.UltraFast)
            .AddParameter("-f mp3 udp://127.0.0.1:" + port.ToString());
            //.AddParameter(udpParam);

            //Add log to OnProgress
            //conversion.OnProgress += (sender, args) =>
            //{
            //   //Show all output from FFmpeg to console
            //   Console.Out.WriteLineAsync($"[{args.Duration}/{args.TotalLength}][{args.Percent}%]");
            //   //durationPercent = (args.Duration / args.TotalLength);
            //   //processID = args.ProcessId;
            //};

            //conversion.OnProgress += (duration, length) => { currentProgress = duration; }

            //conversion.OnDataReceived += (sender, args) =>
            //{
            //    Console.WriteLine($"{args.Data}{sender.ToString()}");
            //};
            //Start conversion
            await conversion.Start(cancellationTokenSource.Token);

            cancellationTokenSource.Cancel();
            //_bIsConversionRunning = false;

            //await Console.Out.WriteLineAsync($"Finished converion file [{nameFile}]");
        }

        public void StopConversion()
        {
            cancellationTokenSource.Cancel();
            //_bIsConversionRunning = false;
        }

        //public void PauseConversion()
        //{
        //    Thread pauseT = new Thread(() =>
        //    {
        //        Process.Start("kill", "-s SIGSTOP " + processID);
        //    });
        //    pauseT.Priority = ThreadPriority.Highest;
        //    pauseT.Start();
        //    //Process.Start("/bin/bash", "kill -s SIGSTOP " + processID);
        //    //Process.Start("echo", $"{processID}");
        //    //using var process = new Process()
        //    //{
        //    //    StartInfo = new ProcessStartInfo
        //    //    {
        //    //        FileName = "/bin/bash",
        //    //        //Arguments = $"kill -s SIGSTOP {processID}",
        //    //        Arguments = $"echo {processID}",
        //    //        RedirectStandardOutput = false,
        //    //        UseShellExecute = false,
        //    //        CreateNoWindow = true,
        //    //    }
        //    //};

        //    //process.Start();
        //    //string result = process.StandardOutput.ReadToEnd();
        //    //process.WaitForExit();
        //    //try
        //    //{
        //    //    using Process myProcess = Process.Start("echo", $"{processID}");
        //    //    // Free resources associated with process.
        //    //    myProcess.Close();
        //    //}
        //    //catch (Exception e)
        //    //{
        //    //    Console.WriteLine(e.Message);
        //    //}

        //    //try
        //    //{
        //    //    using (Process myProcess = Process.Start("echo" , $"{processID}"))
        //    //    {
        //    //        // Display physical memory usage 5 times at intervals of 2 seconds.
        //    //        //for (int i = 0; i < 5; i++)
        //    //        //{
        //    //        //    if (!myProcess.HasExited)
        //    //        //    {
        //    //        //        // Discard cached information about the process.
        //    //        //        //myProcess.Refresh();
        //    //        //        // Print working set to console.
        //    //        //        Console.WriteLine($"Physical Memory Usage: ");
        //    //        //        // Wait 2 seconds.
        //    //        //        Thread.Sleep(2);
        //    //        //    }
        //    //        //    else
        //    //        //    {
        //    //        //        break;
        //    //        //    }
        //    //        //}

        //    //        myProcess.WaitForExit();
        //    //        // Free resources associated with process.
        //    //        myProcess.Close();
        //    //        Console.WriteLine("Done pause");
        //    //    }
        //    //}
        //    //catch (Exception e)
        //    //{
        //    //    Console.WriteLine("The following exception was raised: ");
        //    //    Console.WriteLine(e.Message);
        //    //}
        //}

        //public void ResumeConversion()
        //{
        //    Thread resumeT = new Thread(() =>
        //    {
        //        Process.Start("kill", "-s SIGCONT " + processID);
        //    });
        //    resumeT.Priority = ThreadPriority.Highest;
        //    resumeT.Start();


        //    //Process.Start("/bin/bash", "kill -s SIGCONT " + processID);

        //    //using var process = new Process()
        //    //{
        //    //    StartInfo = new ProcessStartInfo
        //    //    {
        //    //        FileName = "/bin/bash",
        //    //        Arguments = $"kill -s SIGCONT {processID}",
        //    //        RedirectStandardOutput = false,
        //    //        UseShellExecute = false,
        //    //        CreateNoWindow = true,
        //    //    }
        //    //};

        //    //process.Start();
        //    ////string result = process.StandardOutput.ReadToEnd();
        //    //process.WaitForExit();
        //}

        //public void SetSeek(int _second)
        //{
        //    conversion.SetSeek(TimeSpan.FromSeconds(_second));
        //}

        //private async Task<MediaMetadata> GetVideoThumbnailAsync(IFormFile file, int frameTarget)
        //{
        //    var fileName = file.FileName;
        //    var filePath = Path.Combine(_rootPath, "videos", fileName);
        //    var fileExtension = Path.GetExtension(filePath);

        //    // the xabe wrapper works with only mp4 extension to create thumbnail , if the file is any other format first convert it to
        //    //the mp4 format and then goahead with creating the thumbnail.  
        //    var thumbnailImageName = fileName.Replace(fileExtension, ".jpg");
        //    var thumbnailImagePath = Path.Combine(_rootPath, "thumbnails", thumbnailImageName);

        //    using (Stream fileStream = new FileStream(filePath, FileMode.Create))
        //    {
        //        await file.CopyToAsync(fileStream);
        //    }
        //    Console.WriteLine(Path.Combine(_rootPath, "ffmpeg"));
        //    FFmpeg.SetExecutablesPath(Path.Combine(_rootPath, "ffmpeg"));
        //    IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(filePath);
        //    var videoDuration = mediaInfo.VideoStreams.First().Duration;
        //    IConversion conversion = await FFmpeg.Conversions.FromSnippet.Snapshot(filePath, thumbnailImagePath, TimeSpan.FromSeconds(frameTarget));
        //    IConversionResult result = await conversion.Start();
        //    MediaMetadata media = new MediaMetadata();
        //    media.DurationSeconds = Convert.ToInt32(videoDuration.TotalMilliseconds);
        //    // media.DurationSeconds=10;
        //    media.ThumbnailImagePath = thumbnailImagePath;
        //    return media;

        //}
    }
}
