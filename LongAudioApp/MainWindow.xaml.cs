using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;

namespace LongAudioApp;

// Simple model for the transcript file list
public class TranscriptFileInfo
{
    public string FullPath { get; set; } = "";
    public string FileName => Path.GetFileName(FullPath);
    public string FolderPath => Path.GetDirectoryName(FullPath) ?? "";
    public long CharCount { get; set; }
    public string SizeLabel => CharCount > 0 ? $"{CharCount:N0} chars" : "empty";

    public void ReadSize()
    {
        try { CharCount = new FileInfo(FullPath).Length; }
        catch { CharCount = 0; }
    }
}

public partial class MainWindow : Window
{
    private PythonRunner _runner;
    private ScanReport? _report;
    private readonly string _scriptDir;
    private readonly string _reportPath;
    private string? _selectedTranscriptPath;

    public MainWindow()
    {
        InitializeComponent();

        _scriptDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\.."));
        if (!File.Exists(Path.Combine(_scriptDir, "fast_engine.py")))
            _scriptDir = AppDomain.CurrentDomain.BaseDirectory;
        if (!File.Exists(Path.Combine(_scriptDir, "fast_engine.py")))
            _scriptDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));

        _reportPath = Path.Combine(_scriptDir, "voice_scan_results.json");
        _runner = new PythonRunner(_scriptDir);

        WireUpRunner();
        DetectGpu();
        TryLoadExistingResults();
    }

    private void WireUpRunner()
    {
        _runner.OutputReceived += line => Dispatcher.BeginInvoke(() =>
        {
            AppendLog(line);

            // When a transcript file is saved, add it to the list immediately
            if (line.Contains("[SAVED]"))
            {
                var path = line.Replace("[SAVED]", "").Trim();
                if (File.Exists(path))
                {
                    var current = TranscriptList.ItemsSource as List<TranscriptFileInfo> ?? new List<TranscriptFileInfo>();
                    if (!current.Any(t => t.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    {
                        var info = new TranscriptFileInfo { FullPath = path };
                        info.ReadSize();
                        current.Add(info);
                        TranscriptList.ItemsSource = current.OrderByDescending(t => t.CharCount).ToList();
                        TranscribeStatusLabel.Text = $"Found {current.Count} transcript files";
                    }
                }
            }
        });

        _runner.ProgressUpdated += (current, total, filename) => Dispatcher.BeginInvoke(() =>
        {
            if (_isScanRunning)
            {
                ScanProgress.Maximum = total;
                ScanProgress.Value = current;
                ScanStatusLabel.Text = $"[{current}/{total}] {Path.GetFileName(filename)}";
                StatusBar.Text = $"Scanning {current}/{total} files...";
            }
            else
            {
                TranscribeProgress.Maximum = total;
                TranscribeProgress.Value = current;
                TranscribeStatusLabel.Text = $"[{current}/{total}] {Path.GetFileName(filename)}";
                StatusBar.Text = $"Transcribing {current}/{total} files...";
            }
        });

        _runner.VoiceDetected += msg => Dispatcher.BeginInvoke(() =>
        {
            AppendLog(msg);
        });

        _runner.ErrorOccurred += err => Dispatcher.BeginInvoke(() =>
        {
            AppendLog($"[ERROR] {err}");
        });

        _runner.RunningChanged += running => Dispatcher.BeginInvoke(() =>
        {
            ScanBtn.IsEnabled = !running;
            BatchTranscribeBtn.IsEnabled = !running;
            TranscribeAllBtn.IsEnabled = !running;
            LoadResultsBtn.IsEnabled = !running;
            BrowseBtn.IsEnabled = !running;
            CancelScanBtn.IsEnabled = running;
            CancelTranscribeBtn.IsEnabled = running;

            if (!running)
            {
                if (_isScanRunning)
                {
                    _isScanRunning = false;
                    ScanStatusLabel.Text = "Scan complete";
                    StatusBar.Text = "Scan complete — loading results...";
                    TryLoadExistingResults();
                }
                else
                {
                    TranscribeStatusLabel.Text = "Transcription complete";
                    StatusBar.Text = "Transcription complete";
                    // Auto-refresh transcript list after transcription
                    RefreshTranscriptList();
                }
            }
        });
    }

    private bool _isScanRunning;

    private async void DetectGpu()
    {
        try
        {
            var psi = new ProcessStartInfo("nvidia-smi", "--query-gpu=name --format=csv,noheader")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            var proc = Process.Start(psi);
            if (proc != null)
            {
                var gpu = await proc.StandardOutput.ReadLineAsync();
                await proc.WaitForExitAsync();
                GpuLabel.Text = $"GPU: {gpu?.Trim() ?? "Unknown"}";
            }
        }
        catch
        {
            GpuLabel.Text = "GPU: Not detected";
        }
    }

    private void TryLoadExistingResults()
    {
        if (!File.Exists(_reportPath)) return;

        try
        {
            var json = File.ReadAllText(_reportPath);
            _report = JsonSerializer.Deserialize<ScanReport>(json);
            if (_report != null)
            {
                ResultsGrid.ItemsSource = _report.Results;
                var voiceCount = _report.Results.Count(r => r.Error == null && r.Blocks.Count > 0);
                var blockCount = _report.Results.Where(r => r.Error == null).Sum(r => r.Blocks.Count);
                ScanStatusLabel.Text = $"Loaded: {_report.TotalFiles} files, {voiceCount} with voice";
                TranscribeCountLabel.Text = $"{voiceCount} files with voice ({blockCount} blocks)";
                StatusBar.Text = $"Loaded scan results from {_report.ScanDate}";
                // Also refresh transcripts
                RefreshTranscriptList();
            }
        }
        catch (Exception ex)
        {
            AppendLog($"[ERROR] Failed to load results: {ex.Message}");
        }
    }

    private void RefreshTranscriptList()
    {
        var transcripts = new List<TranscriptFileInfo>();

        if (_report != null)
        {
            // Look for _transcript.txt files next to each scanned media file
            foreach (var result in _report.Results)
            {
                if (result.Error != null || result.Blocks.Count == 0) continue;

                var basePath = Path.ChangeExtension(result.File, null) + "_transcript.txt";
                if (File.Exists(basePath))
                {
                    var ti = new TranscriptFileInfo { FullPath = basePath };
                    ti.ReadSize();
                    transcripts.Add(ti);
                }
            }
        }

        // Also scan the directory for any _transcript.txt files we might have missed
        var dir = DirectoryBox.Text.Trim();
        if (Directory.Exists(dir))
        {
            try
            {
                var found = Directory.GetFiles(dir, "*_transcript.txt", SearchOption.AllDirectories);
                foreach (var f in found)
                {
                    if (!transcripts.Any(t => t.FullPath.Equals(f, StringComparison.OrdinalIgnoreCase)))
                    {
                        var ti = new TranscriptFileInfo { FullPath = f };
                        ti.ReadSize();
                        transcripts.Add(ti);
                    }
                }
            }
            catch { /* Permission errors on some dirs */ }
        }

        TranscriptList.ItemsSource = transcripts.OrderByDescending(t => t.CharCount).ToList();
        TranscribeStatusLabel.Text = transcripts.Count > 0
            ? $"Found {transcripts.Count} transcript files"
            : "No transcripts yet — run batch transcribe first";
    }

    private void BrowseBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog()
        {
            Title = "Select media directory to scan"
        };
        if (dialog.ShowDialog() == true)
        {
            DirectoryBox.Text = dialog.FolderName;
        }
    }

    private async void ScanBtn_Click(object sender, RoutedEventArgs e)
    {
        var dir = DirectoryBox.Text.Trim();
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        {
            MessageBox.Show("Please select a valid directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _isScanRunning = true;
        ScanProgress.Value = 0;
        ScanStatusLabel.Text = "Starting scan...";
        StatusBar.Text = "Starting voice scan...";
        ClearLog();

        bool useVad = !(NoVadCheck.IsChecked ?? false);
        await _runner.RunBatchScanAsync(dir, useVad);
    }

    private async void BatchTranscribeBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(_reportPath))
        {
            MessageBox.Show("No scan results found. Run a scan first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _isScanRunning = false;
        TranscribeProgress.Value = 0;
        TranscribeStatusLabel.Text = "Starting transcription...";
        StatusBar.Text = "Starting batch transcription with large-v3...";

        await _runner.RunBatchTranscribeAsync(_reportPath);
    }

    private async void TranscribeAllBtn_Click(object sender, RoutedEventArgs e)
    {
        var dir = DirectoryBox.Text.Trim();
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        {
            MessageBox.Show("Please set a valid directory in the Scan tab first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _isScanRunning = false;
        TranscribeProgress.Value = 0;
        TranscribeStatusLabel.Text = "Starting full transcription...";
        StatusBar.Text = "Transcribing all files with large-v3 (no scan)...";

        bool useVad = !(NoVadCheck.IsChecked ?? false);
        await _runner.RunBatchTranscribeDirAsync(dir, useVad);
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        _runner.Cancel();
        StatusBar.Text = "Cancelling...";
    }

    private void LoadResultsBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog()
        {
            Title = "Select scan results JSON",
            Filter = "JSON files (*.json)|*.json",
            InitialDirectory = _scriptDir
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = File.ReadAllText(dialog.FileName);
                _report = JsonSerializer.Deserialize<ScanReport>(json);
                if (_report != null)
                {
                    ResultsGrid.ItemsSource = _report.Results;
                    var voiceCount = _report.Results.Count(r => r.Error == null && r.Blocks.Count > 0);
                    var blockCount = _report.Results.Where(r => r.Error == null).Sum(r => r.Blocks.Count);
                    ScanStatusLabel.Text = $"Loaded: {_report.TotalFiles} files, {voiceCount} with voice";
                    TranscribeCountLabel.Text = $"{voiceCount} files with voice ({blockCount} blocks)";
                    StatusBar.Text = $"Loaded results from {dialog.FileName}";
                    RefreshTranscriptList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void RefreshTranscriptsBtn_Click(object sender, RoutedEventArgs e)
    {
        RefreshTranscriptList();
    }

    private void TranscriptList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TranscriptList.SelectedItem is TranscriptFileInfo info)
        {
            _selectedTranscriptPath = info.FullPath;
            try
            {
                var content = File.ReadAllText(info.FullPath);
                TranscriptContentBox.Text = content;
                TranscriptFileLabel.Text = info.FullPath;
                OpenInExplorerBtn.Visibility = Visibility.Visible;
                StatusBar.Text = $"Viewing: {info.FileName}";
            }
            catch (Exception ex)
            {
                TranscriptContentBox.Text = $"Error reading file: {ex.Message}";
            }
        }
    }

    private void OpenInExplorerBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTranscriptPath != null && File.Exists(_selectedTranscriptPath))
        {
            Process.Start("explorer.exe", $"/select,\"{_selectedTranscriptPath}\"");
        }
    }

    private void AppendLog(string text)
    {
        LogBox.AppendText(text + Environment.NewLine);
        LogBox.ScrollToEnd();
    }

    private void ClearLog()
    {
        LogBox.Clear();
    }
}

