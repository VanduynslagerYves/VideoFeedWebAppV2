namespace Tryouts;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class LiveProcessingService : BackgroundService
{
    private readonly ILogger<LiveProcessingService> _logger;
    private Process? _ffmpeg;
    private BinaryWriter? _ffmpegStdIn;

    // Video settings - change as needed
    private const int Width = 1920;
    private const int Height = 1080;
    private const int Fps = 15;

    // Where FFmpeg will write HLS segments
    private const string OutputFolder = "wwwroot/stream";

    // On Windows use dshow audio device name found with: ffmpeg -list_devices true -f dshow -i dummy
    // Example: "Microphone (Realtek(R) Audio)"
    // On Linux change the audio input in StartFfmpegProcess() (pulse/alsa)
    public string AudioDeviceName { get; set; } = "Microfoon (HD Pro Webcam C920)";//"Microfoon (Razer USB Sound Card)";

    public LiveProcessingService(ILogger<LiveProcessingService> logger)
    {
        _logger = logger;

        // Ensure output folder exists
        Directory.CreateDirectory(OutputFolder);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => RunPipeline(stoppingToken), stoppingToken);
    }

    private void RunPipeline(CancellationToken stoppingToken)
    {
        try
        {
            using var capture = new VideoCapture(0);
            capture.Open(0);
            if (!capture.IsOpened())
            {
                _logger.LogError("Failed to open camera");
                return;
            }

            // Force resolution
            capture.FrameWidth = Width;
            capture.FrameHeight = Height;
            capture.Fps = Fps;

            StartFfmpegProcess();

            using var frameMat = new Mat();

            var frameBytes = new byte[Width * Height * 3]; // bgr24

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!capture.Read(frameMat) || frameMat.Empty())
                {
                    Thread.Sleep(5);
                    continue;
                }

                // ---- YOUR PROCESSING / ANALYSIS HERE ----
                // Example: draw timestamp and a rectangle
                Cv2.PutText(frameMat, DateTime.UtcNow.ToString("HH:mm:ss.fff UTC"), new Point(10, 30),
                    HersheyFonts.HersheySimplex, 0.7, Scalar.Yellow, 2);

                Cv2.Rectangle(frameMat, new Rect(10, 50, 200, 120), Scalar.Red, 2);
                // -----------------------------------------

                // Convert Mat to BGR byte array (bgr24)
                if (frameMat.Width != Width || frameMat.Height != Height)
                {
                    using var resized = new Mat();
                    Cv2.Resize(frameMat, resized, new OpenCvSharp.Size(Width, Height));
                    var ptr = resized.Data;
                    Marshal.Copy(ptr, frameBytes, 0, frameBytes.Length);
                }
                else
                {
                    var ptr = frameMat.Data;
                    Marshal.Copy(ptr, frameBytes, 0, frameBytes.Length);
                }

                // Write raw frame to ffmpeg stdin
                try
                {
                    _ffmpegStdIn?.Write(frameBytes);
                    _ffmpegStdIn?.Flush();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error writing frame to ffmpeg stdin");
                    break;
                }

                // Sleep to match target framerate if needed
                Thread.Sleep(1000 / Fps);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline error");
        }
        finally
        {
            StopFfmpegProcess();
        }
    }

    private void StartFfmpegProcess()
    {
        StopFfmpegProcess();

        // Build ffmpeg arguments.
        // Windows example: capture audio via dshow and video via pipe:0 (rawvideo bgr24)
        // Note: replace AudioDeviceName with your actual device name (check with ffmpeg -list_devices true -f dshow -i dummy)

        var args = new StringBuilder();

        args.Append($"-f rawvideo -pix_fmt bgr24 -s {Width}x{Height} -r {Fps} -i pipe:0 ");

        // Windows: capture audio from dshow device. If you prefer FFmpeg to not capture audio, remove the following and add anullsrc.
        args.Append($"-f dshow -rtbufsize 512M -i audio=\"{AudioDeviceName}\" ");

        // Encoding options: low-latency x264 + aac. Output HLS files into OutputFolder
        //args.Append("-c:v libx264 -preset ultrafast -tune zerolatency -pix_fmt yuv420p -g 50 -keyint_min 25 ");
        args.Append("-c:v libx264 -preset ultrafast -tune zerolatency -b:v 4M -pix_fmt yuv420p -g 50 -r 15 ");
        //args.Append("-c:v h264_nvenc -preset p1 -rc:v cbr -b:v 4M -pix_fmt yuv420p -g 50 -r 15 -c:a aac -ar 44100 -ac 2 ");
        args.Append("-c:a aac -ar 16000 -ac 2 "); // Change to -c:a libmp3lame for mp3 audio //44100
        args.Append($"-f hls -hls_time 2 -hls_list_size 5 -hls_flags delete_segments+append_list ");
        args.Append($"{OutputFolder}/stream.m3u8");

        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = args.ToString(),
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            RedirectStandardOutput = false,
            CreateNoWindow = true
        };

        _logger.LogInformation("Starting ffmpeg: ffmpeg {args}", args.ToString());

        _ffmpeg = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _ffmpeg.ErrorDataReceived += (s, e) => {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogInformation("ffmpeg: {line}", e.Data);
            }
        };

        _ffmpeg.Start();
        _ffmpeg.BeginErrorReadLine();
        _ffmpegStdIn = new BinaryWriter(_ffmpeg.StandardInput.BaseStream);
    }

    private void StopFfmpegProcess()
    {
        try
        {
            if (_ffmpegStdIn != null)
            {
                try { _ffmpegStdIn.Close(); } catch { }
                _ffmpegStdIn = null;
            }

            if (_ffmpeg != null && !_ffmpeg.HasExited)
            {
                try
                {
                    _ffmpeg.Kill(true);
                }
                catch { }
            }
            _ffmpeg = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping ffmpeg");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        StopFfmpegProcess();
        return base.StopAsync(cancellationToken);
    }
}