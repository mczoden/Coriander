using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Coriander;

record SnapshotIndexPeriodRecord(int StartIndex, int EndIndex);
public record SecondPeriodRecord(int Start, int End)
{
    public override string ToString()
    {
        string start = TimeSpan.FromSeconds(Start).ToString("hh\\:mm\\:ss");
        string end = (End != -1)
            ? TimeSpan.FromSeconds(End).ToString("hh\\:mm\\:ss")
            : "";
        return $"{start} -> {end}";
    }
}

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Write("No Input file");
            return;
        }

        if (Config.Settings.EnableGpuAccelerate)
        {
            Video.EnableGpuAccelerate();
        }

        string path = args[0].Contains('\\')
            ? (Path.GetDirectoryName(args[0]) ?? ".")
            : ".";

        string searchPattern = Path.GetFileName(args[0]);
        foreach (string filename in Directory.GetFiles(path: path, searchPattern: searchPattern))
        {
            // Don't worry, Directory.GetFiles never return folder, only plain files
            if (!HandleOneVideo(filename))
            {
                Console.WriteLine("Skip to next video");
            }
        }
    }

    static bool HandleOneVideo(string videoFullPath)
    {
        Config.VideoFullPath = videoFullPath;
        int duration = Video.GetDuration(Config.VideoFullPath);

        if (duration < 0)
        {
            if (Config.Settings.ManualTotalLength > 0)
            {
                duration = Config.Settings.ManualTotalLength;
                Console.WriteLine($"Invalid length, use {duration} instead");
            }
            else
            {
                Console.WriteLine("Invalid length of video, skip it");
                return false;
            }
        }

        if ((duration < Config.Settings.SnapshotInterval) || duration < 600)
        {
            Console.WriteLine("Original video is too short, do nothing but exit");
            return false;
        }

        if (!Directory.Exists(Config.Settings.SnapshotFolder))
        {
            Directory.CreateDirectory(Config.Settings.SnapshotFolder);
        }
        if (!Video.GetSnapshots(
            video: Config.VideoFullPath,
            interval: Config.Settings.SnapshotInterval,
            folder: Config.Settings.SnapshotFolder,
            extension: Config.Settings.SnapshotExtension))
        {
            Console.WriteLine("Get snapshots failed");
            Directory.Delete(Config.Settings.SnapshotFolder, recursive: true);
            return false;
        }

        List<int> snapshotIndexList = new();
        string pattern = @".*?\w+\\(\d+).\w+";  // xxx\snapshots\072.jpg
        foreach (var snapshot in Directory.GetFiles(@$"{Config.Settings.SnapshotFolder}\"))
        {
            if (Image.IsImageSplittedInThree(snapshot))
            {
                Match match = Regex.Match(snapshot, pattern);
                if (match.Success)
                {
                    int index = int.Parse(match.Groups[1].Value);
                    snapshotIndexList.Add(index);
                }
                else
                {
                    Console.WriteLine($"{snapshot}");
                    Console.WriteLine("Something wrong with get snapshot index, abort!");
                    return false;
                }
            }
        }

        if (snapshotIndexList.Count == 0)
        {
            Console.WriteLine($"No dance cut in this video: {videoFullPath}");
            Directory.Delete(Config.Settings.SnapshotFolder, recursive: true);
            return false;
        }

        int snapshotsNumber = Directory.GetFiles(@$"{Config.Settings.SnapshotFolder}\").Length;
        if (snapshotsNumber > 0)
        {
            return Cut(SnapshotIndexToTime(snapshotIndexList, snapshotsNumber));
        }

        return true;
    }

    static List<SecondPeriodRecord> SnapshotIndexToTime(List<int> snapshotIndexList, int snapshotsNumber)
    {
        List<SnapshotIndexPeriodRecord> snapshotIndexPeriodRecords = new();
        List<SecondPeriodRecord> secondPeriodRecords = new();

        // Merge continuous intervals
        int startIndex = snapshotIndexList.First();
        int lastIndex = startIndex - 1;
        foreach (int index in snapshotIndexList)
        {
            if (index - lastIndex == 1)
            {
                lastIndex = index;
                continue;
            }

            // New section, add previous period
            snapshotIndexPeriodRecords.Add(new(startIndex, lastIndex));
            Console.WriteLine($"snapshot: {startIndex} -> {lastIndex}");
            startIndex = index;
            lastIndex = index;
        }
        // handle with the last period
        snapshotIndexPeriodRecords.Add(new(startIndex, lastIndex));
        Console.WriteLine($"snapshot: {startIndex} -> {lastIndex}");

        // index -> timestamp
        foreach (var record in snapshotIndexPeriodRecords)
        {
            int startSecond = record.StartIndex == 1
                ? 0
                : record.StartIndex * Config.Settings.SnapshotInterval
                    + Config.Settings.TrimOffset - Config.Settings.MarginBefore;
            startSecond = startSecond < 0 ? 0 : startSecond;

            int endSecond = record.EndIndex >= snapshotsNumber
                ? -1
                : record.EndIndex * Config.Settings.SnapshotInterval
                    + Config.Settings.TrimOffset + Config.Settings.MarginAfter;

            secondPeriodRecords.Add(new(startSecond, endSecond));
        }

        // deal with overlap part
        var newRecord = secondPeriodRecords.First();
        List<SecondPeriodRecord> newSecondPeriodRecords = new();
        foreach (var record in secondPeriodRecords.Skip(1))
        {
            if (newRecord.End >= record.Start)  // Overlap here
            {
                Console.WriteLine($"Overlap: ({record}) with ({newRecord})");
                newRecord = new SecondPeriodRecord(newRecord.Start, record.End);
                continue;
            }
            else  // Overlap disappear
            {
                newSecondPeriodRecords.Add(newRecord);
                newRecord = record;
            }
        }
        newSecondPeriodRecords.Add(newRecord);

        return newSecondPeriodRecords;
    }

    static bool Cut(List<SecondPeriodRecord> secondPeriodRecords)
    {
        string outputFolder = $"{Path.GetFileNameWithoutExtension(Config.VideoFullPath)}_cut";
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }
        if (Config.Settings.KeepSnapshots)
        {
            Directory.Move(
                Config.Settings.SnapshotFolder,
                Path.Combine(outputFolder, Config.Settings.SnapshotFolder));
        }
        else
        {
            Directory.Delete(Config.Settings.SnapshotFolder, recursive: true);
        }

        return Video.Cut(secondPeriodRecords, outputFolder);
    }
}