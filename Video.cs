using System.Diagnostics;

namespace Coriander;


public static class Video
{
    static readonly string FfmpegCommonArguments = "-loglevel error -hide_banner";
    static string decodeAccelerator = "";
    static string encodeAccelerator = "";

    public static void EnableAutoHardwareAccelerate()
    {
        decodeAccelerator = "-hwaccel auto";
        encodeAccelerator = "";
    }

    public static bool EnableGpuAccelerate()
    {
        Process? process = Process.Start(new ProcessStartInfo()
            {
                FileName = "ffmpeg.exe",
                Arguments = $"{FfmpegCommonArguments} -codecs",
                RedirectStandardOutput = true,
            });
        if (process is null)
        {
            return false;
        }

        foreach (string line in process.StandardOutput.ReadToEnd().Split("\n"))
        {
            if (line.Contains("DEV.LS h264"))
            {
                if (line.Contains("h264_cuvid"))
                {
                    Console.WriteLine("Update decode accelerator of h264_cuvid");
                    decodeAccelerator = "-c:v h264_cuvid";
                }
                else
                {
                    Console.WriteLine("Update decode accelerator of auto mode");
                    decodeAccelerator = "-hwaccel auto";
                }

                if (line.Contains("h264_nvenc"))
                {
                    Console.WriteLine("Update encode accelerator of h264_nvenc");
                    encodeAccelerator = "-c:v h264_nvenc";
                }
                // DO NOT break here, to avoid stdout buffer full
            }
        }
        process.WaitForExit();
        return process.ExitCode == 0;
    }

    public static int GetDuration(string video)
    {
        double duration = -1;

        Process? process = Process.Start(new ProcessStartInfo()
            {
                FileName = "ffprobe.exe",
                Arguments = $"{FfmpegCommonArguments} -i {video} -show_format -v quiet",
                RedirectStandardOutput = true
            });
        if (process is null)
        {
            return -1;
        }

        foreach (string line in process.StandardOutput.ReadToEnd().Split("\n"))
        {
            if (line.StartsWith("duration="))
            {
                Console.WriteLine($"\n==> {line}");
                if (line.Contains("N/A"))
                {
                    break;
                }
                duration = double.Parse(line.Split("=")[1]);
                duration = Math.Floor(duration);
                // DO NOT break here, to avoid stdout buffer full
            }
        }

        process.WaitForExit();
        return (process.ExitCode == 0) ? (int)duration : -1;
    }

    public static bool GetSnapshots(string video, int interval, string folder, string extension)
    {
        Console.WriteLine("Get snapshots ...");
        Console.Out.FlushAsync();

        Process? process = Process.Start(new ProcessStartInfo()
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-stats {FfmpegCommonArguments} -y" +
                    $" {decodeAccelerator} -i {video} -vf fps=1/{interval}" +
                    $" {folder}\\%04d.{extension}",
                RedirectStandardOutput = true
            });
        if (process is null)
        {
            return false;
        }
        while (!process.HasExited)
        {
            Console.Write(process.StandardOutput.ReadToEnd());
            Thread.Sleep(1000);
        }
        process.WaitForExit();
        return process.ExitCode == 0;
    }

    public static bool Cut(List<SecondPeriodRecord> records, string outputFolder)
    {
        string basename = Path.GetFileNameWithoutExtension(Config.VideoFullPath);
        string extension = Path.GetExtension(Config.VideoFullPath);
        using var file = new StreamWriter(@$"{outputFolder}\a.txt", append: false);
    
        List<string> arguments = new()
        {
            "-stats",
            FfmpegCommonArguments,
            $"-y -i {Config.VideoFullPath}"
        };

        var enumerableList = records.Select((value, index) => (Index: index, Value: value));
        foreach (var (index, record) in enumerableList)
        {
            string start = (record.Start != 0)
                ? $"{record.Start / 3600}:{TimeSpan.FromSeconds(record.Start):mm\\:ss}"
                : "";
            string end = (record.End != -1)
                ? $"{record.End / 3600}:{TimeSpan.FromSeconds(record.End):mm\\:ss}"
                : "";
            arguments.Add(string.IsNullOrEmpty(start) ? "" : $"-ss {start}");
            arguments.Add(string.IsNullOrEmpty(end) ? "" : $"-to {end}");
            arguments.Add(@$"-c copy {outputFolder}\{basename}_cut_{index:D2}{extension}");
            file.WriteLine($"{start},{end}");
        }
        Console.WriteLine(string.Join(" ", arguments));

        var process = Process.Start(new ProcessStartInfo()
            {
                FileName = "ffmpeg",
                Arguments = string.Join(" ", arguments),
            });
        if (process is null)
        {
            Console.WriteLine("Failed to run ffmpeg to cut video");
            return false;
        }

        process.WaitForExit();
        return process.ExitCode == 0;
    }
}
