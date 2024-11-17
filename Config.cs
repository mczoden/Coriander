using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Coriander;


public class Settings
{
    public bool EnableGpuAccelerate { get; set; } = false;
    public string SnapshotExtension { get; set; } = "jpg";
    public int SnapshotInterval { get; set; } = 30;
    public string SnapshotFolder { get; set; } = @".\snapshots";
    public int TrimOffset { get; set; } = 0;
    public int MarginBefore { get; set; } = 60;
    public int MarginAfter { get; set; } = 30;
    public int ScalarThreshold { get; set; } = 15;
    public int ImageCutMargin { get; set; } = 2;
    public bool KeepSnapshots { get; set; } = true;
    public int ManualTotalLength { get; set; } = 0;

    public override string ToString()
    {
        return $"Enable GPU: {EnableGpuAccelerate}\n"
            + $"Snapshots Extension: {SnapshotExtension}\n"
            + $"Snapshot Interval: {SnapshotInterval}\n"
            + $"Snapshot Folder: {SnapshotFolder}\n"
            + $"Trim Offset: {TrimOffset}\n"
            + $"Margin Before: {MarginBefore}\n"
            + $"Margin After: {MarginAfter}\n"
            + $"Scalar Threshold: {ScalarThreshold}\n"
            + $"Image Cut Margin: {ImageCutMargin}\n"
            + $"Manual Total Length: {ManualTotalLength}";
    }
}

public static class Config
{
    // public static readonly bool EnableGpuAccelerate = false;
    public static readonly Settings Settings = new();
    private static string videoFullPath = "";
    public static string VideoFullPath
    {
        get => videoFullPath;
        set
        {
            if (!File.Exists(value))
            {
                throw new FileNotFoundException(value);
            }
            videoFullPath = Path.GetFullPath(value);
        }
    }

    static Config()
    {
        if (!File.Exists("settings.yaml"))
        {
            Console.WriteLine("settings.yaml not found, use default configuration");
            Console.WriteLine(Settings);
            return;
        }

        string yamlContent = File.ReadAllText("settings.yaml");
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        Settings = deserializer.Deserialize<Settings>(yamlContent);
        Console.WriteLine(Settings);
    }
}
