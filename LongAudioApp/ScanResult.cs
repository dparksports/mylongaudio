using System.Text.Json.Serialization;

namespace LongAudioApp;

public class ScanReport
{
    [JsonPropertyName("scan_date")]
    public string ScanDate { get; set; } = "";

    [JsonPropertyName("directory")]
    public string Directory { get; set; } = "";

    [JsonPropertyName("total_files")]
    public int TotalFiles { get; set; }

    [JsonPropertyName("files_with_voice")]
    public int FilesWithVoice { get; set; }

    [JsonPropertyName("results")]
    public List<ScanFileResult> Results { get; set; } = new();
}

public class ScanFileResult
{
    [JsonPropertyName("file")]
    public string File { get; set; } = "";

    [JsonPropertyName("duration_sec")]
    public double DurationSec { get; set; }

    [JsonPropertyName("segments_found")]
    public int SegmentsFound { get; set; }

    [JsonPropertyName("speech_duration_sec")]
    public double SpeechDurationSec { get; set; }

    [JsonPropertyName("blocks")]
    public List<VoiceBlock> Blocks { get; set; } = new();

    [JsonPropertyName("transcribe_cmds")]
    public List<string> TranscribeCmds { get; set; } = new();

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    // UI helpers
    [JsonIgnore]
    public string FileName => System.IO.Path.GetFileName(File);

    [JsonIgnore]
    public string FolderName => System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(File) ?? "");

    [JsonIgnore]
    public string Status => Error != null ? "Error" : Blocks.Count > 0 ? "Voice" : "Silent";

    [JsonIgnore]
    public string BlocksSummary => string.Join(", ", Blocks.Select(b => $"{b.Start:F0}s–{b.End:F0}s"));

    [JsonIgnore]
    public string DurationDisplay
    {
        get
        {
            if (DurationSec < 60) return $"{DurationSec:F0}s";
            return $"{DurationSec / 60:F1}m";
        }
    }
}

public class VoiceBlock
{
    [JsonPropertyName("start")]
    public double Start { get; set; }

    [JsonPropertyName("end")]
    public double End { get; set; }
}
