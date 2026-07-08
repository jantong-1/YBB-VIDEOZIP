using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YBBvideozip.Mac.Models;

public enum CodecChoice
{
    H264,
    H265
}

public enum EngineChoice
{
    Cpu,
    Gpu
}

public enum QualityChoice
{
    High,
    Balanced,
    Small
}

public sealed class VideoJob : INotifyPropertyChanged
{
    private string inputPath = "";
    private string outputPath = "";
    private string status = "等待";
    private int progress;
    private double durationSeconds;
    private string errorMessage = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string InputPath
    {
        get => inputPath;
        set => SetField(ref inputPath, value);
    }

    public string OutputPath
    {
        get => outputPath;
        set => SetField(ref outputPath, value);
    }

    public string Status
    {
        get => status;
        set => SetField(ref status, value);
    }

    public int Progress
    {
        get => progress;
        set => SetField(ref progress, value);
    }

    public double DurationSeconds
    {
        get => durationSeconds;
        set => SetField(ref durationSeconds, value);
    }

    public string ErrorMessage
    {
        get => errorMessage;
        set => SetField(ref errorMessage, value);
    }

    public string FileName => Path.GetFileName(InputPath);

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        if (propertyName == nameof(InputPath))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileName)));
        }
    }
}

public sealed record CompressionOptions(
    string InputPath,
    string OutputPath,
    CodecChoice Codec,
    EngineChoice Engine,
    QualityChoice Quality);
