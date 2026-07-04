using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Windows.Forms;
using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using static AudioConverter.AudioHelper;
using System.Linq;

namespace AudioConverter
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource _cts = new();
        private Channel<AudioConversionJob>? _jobChannel;
        private readonly ConcurrentDictionary<string, AudioConversionJob> _runningJobs = new();
        private readonly ConcurrentDictionary<int, double> _fileProgress = new();
        private readonly ConcurrentDictionary<int, double> _progressWeights = new();
        private readonly object _progressLock = new();
        private double _totalProgressWeight;
        private double _finishedProgressWeight;
        private readonly GitHubUpdateService _updateService = new();
        private UpdateCheckResult? _updateResult;
        private static readonly int[] DefaultSampleRates = { 8000, 16000, 22050, 32000, 44100, 48000, 96000, 192000 };
        private static readonly string[] SupportedBitDepths = { "16", "24" };
        private bool _isFirstLaunch;
        private string _lastSupportedBitDepth = "16";
        private bool _suppressSettingsSave;
        private long _totalBeforeSize = 0;
        private long _totalAfterSize = 0;
        private long _processedInputBytes = 0;
        private int _completedCount = 0;
        private int _failedCount = 0;
        private int _totalFilesProcessing = 0;
        private DateTime _startTime;
        private bool _isProcessing = false;
        private readonly object _lockObj = new();
        private System.Windows.Forms.Timer? _uiUpdateTimer;
        private System.Windows.Forms.Timer? _elapsedTimeTimer;
        private Stopwatch _sw = new Stopwatch();

        public Form1()
        {
            try
            {
                Logger.LogInfo("Form1 constructor - Starting");

                Logger.LogInfo("Calling InitializeComponent");
                InitializeComponent();

                Logger.LogInfo("Initializing FFmpeg");
                InitializeFFmpeg();

                Logger.LogInfo("Initializing UI");
                InitializeUI();

                // Set the form title explicitly to override ClickOnce version display
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                Text = $"AudioConverter v{version}";
                Logger.LogInfo($"Form title set to: {Text}");

                Logger.LogInfo("Form1 constructor - Completed successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError("Exception in Form1 constructor", ex);
                MessageBox.Show($"Failed to initialize form: {ex.Message}\n\nLog: {Logger.GetLogPath()}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void InitializeFFmpeg()
        {
            try
            {
                Logger.LogInfo("Form1.InitializeFFmpeg - Calling FFmpegConverter.Initialize");
                FFmpegConverter.Initialize();
                Logger.LogInfo("Form1.InitializeFFmpeg - Success");
            }
            catch (Exception ex)
            {
                Logger.LogError("Form1.InitializeFFmpeg - Failed", ex);
                MessageBox.Show($"Failed to initialize FFmpeg: {ex.Message}\n\nLog: {Logger.GetLogPath()}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void InitializeUI()
        {
            try
            {
                _suppressSettingsSave = true;
                Logger.LogInfo("Initializing UI controls");
                AddRelativePathColumn();
                AddStatusColumns();
                InitializeUpdateButton();

                // Initialize format combo box
                comboBoxFormat.Items.Clear();
                comboBoxFormat.Items.AddRange(OutputPresetCatalog.GetSupportedPresets().Select(p => (object)p.Id).ToArray());
                SelectFormatOrFirst("mp3");
                comboBoxFormat.SelectedIndexChanged += (_, _) =>
                {
                    UpdateSampleRateOptions();
                    UpdateBitDepthVisibility();
                    SaveSettingsIfReady();
                };
                Logger.LogDebug("Format combo box initialized");

                // Initialize sample rate combo box
                comboBoxSampleRate.Items.Clear();
                comboBoxSampleRate.Items.AddRange(new object[] { "8000", "16000", "22050", "44100", "48000", "96000", "192000" });
                comboBoxSampleRate.SelectedItem = "44100";
                Logger.LogDebug("Sample rate combo box initialized");

                comboBoxSampleRate.SelectedIndexChanged += (_, _) => SaveSettingsIfReady();

                checkBoxUseSourceSampleRate.CheckedChanged += (_, _) =>
                {
                    UpdateSampleRateMode();
                    SaveSettingsIfReady();
                };

                // Initialize bit depth combo box
                comboBoxBitDepth.Items.Clear();
                comboBoxBitDepth.Items.AddRange(SupportedBitDepths);
                comboBoxBitDepth.SelectedItem = "16";
                comboBoxBitDepth.SelectedIndexChanged += (_, _) => SaveSettingsIfReady();
                Logger.LogDebug("Bit depth combo box initialized");

                comboBoxChannelMode.SelectedIndexChanged += (_, _) => SaveSettingsIfReady();
                numericUpDownBitrate.ValueChanged += (_, _) => SaveSettingsIfReady();
                numericUpDownThreads.ValueChanged += (_, _) => SaveSettingsIfReady();
                checkBoxOutputFolder.CheckedChanged += (_, _) => SaveSettingsIfReady();
                textBoxOutput.TextChanged += (_, _) => SaveSettingsIfReady();

                // Load saved settings
                Logger.LogInfo("Loading saved settings");
                _isFirstLaunch = !AppSettings.HasSavedSettings;
                LoadSettings();



                UpdateBitDepthVisibility();
                UpdateSampleRateOptions();
                UpdateSampleRateMode();

                Logger.LogInfo("UI initialization completed");
            }
            catch (Exception ex)
            {
                Logger.LogError("InitializeUI failed", ex);
                MessageBox.Show($"Failed to initialize UI: {ex.Message}\n\nLog: {Logger.GetLogPath()}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _suppressSettingsSave = false;
            }
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            bool wasSuppressed = _suppressSettingsSave;
            _suppressSettingsSave = true;
            var settings = AppSettings.Load();
                textBoxOutput.Text = settings.OutputPath;
            numericUpDownBitrate.Value = AudioHelper.ClampNumeric(settings.Bitrate, (int)numericUpDownBitrate.Minimum, (int)numericUpDownBitrate.Maximum);
            numericUpDownThreads.Value = AudioHelper.ClampNumeric(settings.Threads, (int)numericUpDownThreads.Minimum, (int)numericUpDownThreads.Maximum);
            SelectFormatOrFirst(settings.Format);
            comboBoxSampleRate.SelectedItem = comboBoxSampleRate.Items.Contains(settings.SampleRate) ? settings.SampleRate : "44100";
            _lastSupportedBitDepth = SupportedBitDepths.Contains(settings.BitDepth) ? settings.BitDepth : "16";
            comboBoxBitDepth.SelectedItem = _lastSupportedBitDepth;
            SelectChannelMode(settings.ChannelMode);
            checkBoxUseSourceSampleRate.Checked = settings.UseSourceSampleRate;
            checkBoxOutputFolder.Checked = settings.OutputFolderStructure;
            UpdateSampleRateOptions();
            UpdateBitDepthVisibility();
            UpdateSampleRateMode();
            _suppressSettingsSave = wasSuppressed;
        }

        private void SaveSettings()
        {
            var settings = new AppSettings
            {
                OutputPath = textBoxOutput.Text,
                Bitrate = (int)numericUpDownBitrate.Value,
                Threads = (int)numericUpDownThreads.Value,
                Format = comboBoxFormat.SelectedItem?.ToString() ?? "mp3",
                SampleRate = comboBoxSampleRate.SelectedItem?.ToString() ?? "44100",
                BitDepth = comboBoxBitDepth.SelectedItem?.ToString() ?? "16",
                ChannelMode = GetSelectedChannelMode().ToString(),
                UseSourceSampleRate = checkBoxUseSourceSampleRate.Checked,
                OutputFolderStructure = checkBoxOutputFolder.Checked
            };
            settings.Save();
        }

        private void SaveSettingsIfReady()
        {
            if (_suppressSettingsSave)
                return;
            SaveSettings();
        }

        private void InitializeUpdateButton()
        {
            update.Enabled = false;
            update.Text = "Update";
            update.Click += update_Click;
            Shown += async (_, _) => await CheckForUpdatesAsync();
        }

        private async System.Threading.Tasks.Task CheckForUpdatesAsync()
        {
            try
            {
                update.Enabled = false;
                update.Text = "...";
                var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);
                _updateResult = await _updateService.CheckForUpdateAsync(currentVersion, CancellationToken.None).ConfigureAwait(true);
                update.Text = "Update";
                update.Enabled = _updateResult.CanDownload;
                Logger.LogInfo($"Update check: {_updateResult.Message}");
            }
            catch (Exception ex)
            {
                update.Text = "Update";
                update.Enabled = false;
                Logger.LogError("Update check failed", ex);
            }
        }

        private async void update_Click(object? sender, EventArgs e)
        {
            if (_updateResult?.CanDownload != true)
                return;

            if (_isProcessing)
            {
                MessageBox.Show("Finish or cancel conversions before updating.", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                update.Enabled = false;
                update.Text = "...";
                string updatedExe = await _updateService.DownloadAndVerifyAsync(_updateResult, CancellationToken.None).ConfigureAwait(true);
                string currentExe = Environment.ProcessPath ?? Application.ExecutablePath;
                var start = new ProcessStartInfo
                {
                    FileName = updatedExe,
                    UseShellExecute = false
                };
                start.ArgumentList.Add("--apply-update");
                start.ArgumentList.Add(currentExe);
                Process.Start(start);
                Application.Exit();
            }
            catch (Exception ex)
            {
                update.Text = "Update";
                update.Enabled = true;
                MessageBox.Show($"Update failed: {ex.Message}", "Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelectFormatOrFirst(string preferred)
        {
            if (comboBoxFormat.Items.Contains(preferred))
            {
                comboBoxFormat.SelectedItem = preferred;
                return;
            }

            if (comboBoxFormat.Items.Contains("mp3"))
            {
                comboBoxFormat.SelectedItem = "mp3";
                return;
            }

            if (comboBoxFormat.Items.Count > 0)
                comboBoxFormat.SelectedIndex = 0;
        }

        private AudioChannelMode GetSelectedChannelMode()
        {
            return comboBoxChannelMode.SelectedItem?.ToString() switch
            {
                "Mono" => AudioChannelMode.Mono,
                "Stereo" => AudioChannelMode.Stereo,
                _ => AudioChannelMode.Preserve
            };
        }

        private void SelectChannelMode(string value)
        {
            comboBoxChannelMode.SelectedItem = value switch
            {
                nameof(AudioChannelMode.Mono) => "Mono",
                nameof(AudioChannelMode.Stereo) => "Stereo",
                _ => "Original"
            };
        }

        private void SetCellValue(DataGridViewRow row, string columnName, object value)
        {
            var column = dataGridView1.Columns[columnName];
            if (column != null)
                row.Cells[column.Index].Value = value;
        }

        private void dataGridView1_DragDrop(object? sender, DragEventArgs e)
        {
            _ = HandleDragDropAsync(e);
        }

        private async System.Threading.Tasks.Task HandleDragDropAsync(DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
                return;

            var import = await System.Threading.Tasks.Task.Run(() =>
            {
                var validFiles = new List<(string fullPath, string relativePath)>();
                var skippedFiles = new List<(string fullPath, string reason)>();

                void ProbeAndAdd(string file, string relativePath)
                {
                    var probe = FileProbeService.Probe(file);
                    if (probe.Accepted)
                    {
                        validFiles.Add((file, relativePath));
                    }
                    else
                    {
                        skippedFiles.Add((file, probe.ErrorMessage ?? "Unsupported file."));
                    }
                }

                foreach (var item in files)
                {
                    if (Directory.Exists(item))
                    {
                        string topFolderName = Path.GetFileName(item.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                        var filesInDir = Directory.EnumerateFiles(item, "*.*", SearchOption.AllDirectories);

                        foreach (var file in filesInDir)
                        {
                            if (!IsAudioFile(file))
                                continue;

                            string relativePath = Path.Combine(topFolderName, Path.GetRelativePath(item, file));
                            relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
                            ProbeAndAdd(file, relativePath);
                        }
                    }
                    else if (File.Exists(item))
                    {
                        if (!IsAudioFile(item))
                        {
                            skippedFiles.Add((item, "Unsupported extension."));
                            continue;
                        }

                        ProbeAndAdd(item, Path.GetFileName(item));
                    }
                }

                return (validFiles, skippedFiles);
            });

            dataGridView1.SuspendLayout();
            foreach (var (fullPath, relativePath) in import.validFiles)
            {
                var row = new DataGridViewRow();
                row.CreateCells(dataGridView1);
                row.Cells[0].Value = fullPath;

                var relCol = dataGridView1.Columns["RelativePath"];
                if (relCol != null)
                {
                    row.Cells[relCol.Index].Value = relativePath;
                }
                SetCellValue(row, "Status", "Probed");
                SetCellValue(row, "Progress", "0%");

                dataGridView1.Rows.Add(row);
            }
            dataGridView1.ResumeLayout();

            if (import.skippedFiles.Count > 0)
            {
                if (!tabControl.TabPages.Contains(tabPageFailedFiles))
                    tabControl.TabPages.Add(tabPageFailedFiles);

                foreach (var (fullPath, reason) in import.skippedFiles)
                    dataGridViewFailed.Rows.Add(fullPath, reason);
            }

            dataGridView1_Changed();
        }

        private void dataGridView1_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void AddRelativePathColumn()
        {
            if (!dataGridView1.Columns.Contains("RelativePath"))
            {
                var relCol = new DataGridViewTextBoxColumn
                {
                    Name = "RelativePath",
                    HeaderText = "RelativePath",
                    Visible = false
                };
                dataGridView1.Columns.Add(relCol);
            }
        }

        private void AddStatusColumns()
        {
            if (!dataGridView1.Columns.Contains("Status"))
            {
                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Status",
                    HeaderText = "Status",
                    Width = 90,
                    ReadOnly = true
                });
            }

            if (!dataGridView1.Columns.Contains("Progress"))
            {
                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Progress",
                    HeaderText = "Progress",
                    Width = 70,
                    ReadOnly = true
                });
            }
        }

        private void buttonAbout_Click(object? sender, EventArgs e) => ShowAboutDialog();

        private void UpdateFileCount()
        {
            numFiles.Text = dataGridView1.Rows.Count.ToString();
        }

        private void dataGridView1_Changed()
        {
            // Compute file sizes off the UI thread to avoid blocking the UI for large lists.
            var paths = new List<string>();
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                var val = dataGridView1.Rows[i].Cells[0].Value;
                var s = val?.ToString();
                if (!string.IsNullOrEmpty(s))
                    paths.Add(s);
            }

            long beforeSize = Task.Run(() =>
            {
                long sum = 0;
                foreach (var p in paths)
                {
                    try { sum += new FileInfo(p).Length; } catch { }
                }
                return sum;
            }).GetAwaiter().GetResult();

            Invoke(() =>
            {
                numFiles.Text = dataGridView1.Rows.Count.ToString();
                beforeSizeLabel.Text = FormatBytes(beforeSize);
            });
        }

        private void dataGridView1_MouseLeave(object? sender, EventArgs e)
        {
            dataGridView1_Changed();
        }

        private void buttonOpen_Click(object? sender, EventArgs e)
        {
            var outputPath = textBoxOutput.Text;
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                MessageBox.Show("Output folder is not set.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                if (!Directory.Exists(outputPath))
                {
                    MessageBox.Show("Output folder does not exist.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = outputPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open output folder: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBoxOutput_DoubleClick(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBoxOutput.Text = dialog.SelectedPath;
                SaveSettings();
            }
        }

        private async void buttonConvert_Click(object? sender, EventArgs e)
        {
            if (_isProcessing) return;
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("No files to convert.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _isProcessing = true;
            _cts = new CancellationTokenSource();
            _startTime = DateTime.Now;
            _totalBeforeSize = 0;
            _totalAfterSize = 0;
            _processedInputBytes = 0;
            _completedCount = 0;
            _failedCount = 0;
            _sw.Reset();
            _sw.Start();

            buttonConvert.Enabled = false;
            cancelButton.Enabled = true;
            clearListButton.Enabled = false;

            SaveSettings();

            try
            {
                await ThreadHandler2Async();
            }
            finally
            {
                _isProcessing = false;
                buttonConvert.Enabled = true;
                cancelButton.Enabled = false;
                clearListButton.Enabled = true;
                buttonConvert.Text = "Convert";
                _uiUpdateTimer?.Stop();
                _elapsedTimeTimer?.Stop();
            }
        }

        private async System.Threading.Tasks.Task ThreadHandler2Async()
        {
            try
            {
                // Snapshot UI state to avoid cross-thread access during processing
                var rows = dataGridView1.Rows.Cast<DataGridViewRow>()
                    .Where(r => r.Cells.Count > 0 && r.Cells[0].Value != null)
                    .Select(r => (inPath: r.Cells[0].Value?.ToString() ?? string.Empty, relativePath: r.Cells["RelativePath"]?.Value?.ToString() ?? string.Empty, rowIndex: r.Index))
                    .ToList();

                string outputRoot = textBoxOutput.Text;
                string formatLocal = comboBoxFormat.SelectedItem?.ToString() ?? "mp3";
                var outputPreset = OutputPresetCatalog.Get(formatLocal);
                bool outputFullPath = checkBoxOutputFolder.Checked;
                var outputPathResolver = new OutputPathResolver();
                var reservedOutputPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int bitrate = Decimal.ToInt32(numericUpDownBitrate.Value);
                int sampleRate = int.Parse(comboBoxSampleRate.SelectedItem?.ToString() ?? "44100");
                bool useSourceSampleRate = checkBoxUseSourceSampleRate.Checked;
                AudioChannelMode channelMode = GetSelectedChannelMode();
                int bitDepth = outputPreset.SupportsBitDepth
                    ? int.Parse(comboBoxBitDepth.SelectedItem?.ToString() ?? "16")
                    : 16;
                int maxDegree = Decimal.ToInt32(numericUpDownThreads.Value) > 0
                    ? Decimal.ToInt32(numericUpDownThreads.Value)
                    : Environment.ProcessorCount;

                // Filter to existing files and compute totals for progress and throughput
                var filteredRows = rows.Where(r => !string.IsNullOrEmpty(r.inPath) && File.Exists(r.inPath)).ToList();
                _totalFilesProcessing = filteredRows.Count;
                var inputSizes = new Dictionary<int, long>();
                long sumBefore = 0;
                foreach (var r in filteredRows)
                {
                    try
                    {
                        long size = new FileInfo(r.inPath).Length;
                        inputSizes[r.rowIndex] = size;
                        sumBefore += size;
                    }
                    catch
                    {
                        inputSizes[r.rowIndex] = 1;
                    }
                }
                Interlocked.Exchange(ref _totalBeforeSize, sumBefore);
                ResetProgressState(sumBefore > 0 ? sumBefore : filteredRows.Count);
                Invoke(() =>
                {
                    foreach (var row in filteredRows)
                        SetRowStatus(row.rowIndex, "Pending", "0%");
                });

                // Configure ThreadPool min threads
                ThreadPool.GetMinThreads(out int minWorker, out int minIO);
                if (minWorker < maxDegree + 2)
                {
                    ThreadPool.SetMinThreads(maxDegree + 2, minIO);
                }

                // Create bounded channel
                int channelCapacity = Math.Max(1, maxDegree * 4);
                _jobChannel = Channel.CreateBounded<AudioConversionJob>(
                    new BoundedChannelOptions(channelCapacity)
                    {
                        SingleWriter = true,
                        SingleReader = false,
                        FullMode = BoundedChannelFullMode.Wait
                    });

                // Start UI update timers
                _uiUpdateTimer = new System.Windows.Forms.Timer();
                _uiUpdateTimer.Interval = 250;
                _uiUpdateTimer.Tick += (s, ev) => UpdateUI();
                _uiUpdateTimer.Start();

                _elapsedTimeTimer = new System.Windows.Forms.Timer();
                _elapsedTimeTimer.Interval = 33;
                _elapsedTimeTimer.Tick += (s, ev) => UpdateElapsedTime();
                _elapsedTimeTimer.Start();

                // Start consumers
                var consumers = Enumerable.Range(0, maxDegree).Select(_ => System.Threading.Tasks.Task.Run(async () =>
                {
                    await foreach (var job in _jobChannel.Reader.ReadAllAsync(_cts.Token).ConfigureAwait(false))
                    {
                        try
                        {
                            ProcessSingleFile(job, _cts.Token);
                        }
                        catch (OperationCanceledException) { break; }
                    }
                }, _cts.Token)).ToArray();

                // Start producer with audio analysis
                var producer = System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        foreach (var row in filteredRows)
                        {
                            _cts.Token.ThrowIfCancellationRequested();

                            // Analyze input file for fast path detection
                            var inputInfo = AudioAnalyzer.AnalyzeFile(row.inPath);
                            var outputPath = outputPathResolver.Resolve(new OutputPathRequest(
                                row.inPath,
                                row.relativePath,
                                outputRoot,
                                outputPreset.Extension,
                                outputFullPath,
                                ExistingFilePolicy.AutoRename,
                                reservedOutputPaths));
                            reservedOutputPaths.Add(outputPath.FinalOutputPath);

                            var job = new AudioConversionJob
                            {
                                InputPath = row.inPath,
                                OutputPath = outputPath.FinalOutputPath,
                                TemporaryOutputPath = outputPath.TemporaryOutputPath,
                                OutputWasAutoRenamed = outputPath.WasAutoRenamed,
                                Format = formatLocal,
                                Bitrate = bitrate,
                                SampleRate = useSourceSampleRate && inputInfo != null && inputInfo.SampleRate > 0
                                    ? inputInfo.SampleRate
                                    : sampleRate,
                                BitDepth = bitDepth,
                                ChannelMode = channelMode,
                                RowIndex = row.rowIndex,
                                InputBytes = inputSizes.TryGetValue(row.rowIndex, out var inputBytes) ? inputBytes : 1,
                                ProgressWeight = Math.Max(1, inputSizes.TryGetValue(row.rowIndex, out var weight) ? weight : 1),
                                CancellationToken = _cts.Token,
                                PreserveMetadata = true,
                                UseFastPath = false,
                                InputInfo = inputInfo
                            };

                            await _jobChannel.Writer.WriteAsync(job, _cts.Token).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) { }
                    finally
                    {
                        _jobChannel?.Writer.TryComplete();
                    }
                }, _cts.Token);

                await System.Threading.Tasks.Task.WhenAll(consumers.Append(producer)).ConfigureAwait(false);

                _sw.Stop();
                double finalElapsed = _sw.Elapsed.TotalSeconds;
                string elapsedText = finalElapsed > 0 ? finalElapsed.ToString("N2") + " s" : "0.00 s";
                double finalFilesPerSec = finalElapsed > 0 ? _completedCount / finalElapsed : 0.0;

                Invoke(() =>
                {
                    elapsedLabel.Text = elapsedText;
                    filesPerSecLabel.Text = finalFilesPerSec.ToString("N2") + " files/s";
                    UpdateUI();
                    buttonConvert.Text = "Convert";
                });
            }
            catch (OperationCanceledException)
            {
                Invoke(() => MessageBox.Show("Operation was Cancelled."));
            }
            finally
            {
                _sw.Stop();
                _cts.Dispose();
            }
        }

        private void ProcessSingleFile(AudioConversionJob job, CancellationToken token)
        {
            if (string.IsNullOrEmpty(job.InputPath))
                return;

            token.ThrowIfCancellationRequested();

            string finalPath = job.OutputPath;
            string tempPath = job.TemporaryOutputPath;
            if (string.IsNullOrWhiteSpace(tempPath))
            {
                string extension = Path.GetExtension(finalPath).TrimStart('.');
                tempPath = Path.Combine(
                    Path.GetDirectoryName(finalPath) ?? string.Empty,
                    $".{Path.GetFileNameWithoutExtension(finalPath)}.{Guid.NewGuid():N}.tmp.{extension}");
            }

            // Ensure output directory exists
            string? dir = Path.GetDirectoryName(finalPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string? tempDir = Path.GetDirectoryName(tempPath);
            if (!string.IsNullOrEmpty(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            SafeDelete(tempPath);

            try
            {
                long beforeSize = new FileInfo(job.InputPath).Length;
                _progressWeights[job.RowIndex] = Math.Max(1, job.ProgressWeight);
                SetRowStatus(job.RowIndex, "Converting", "0%");

                var request = new ConversionRequest(
                    job.InputPath,
                    tempPath,
                    job.Format,
                    job.Bitrate,
                    job.SampleRate,
                    job.BitDepth,
                    job.ChannelMode,
                    job.PreserveMetadata,
                    job.UseFastPath,
                    job.InputInfo);
                var progress = new AnonymousProgress<ConversionProgress>(p => _fileProgress[job.RowIndex] = Math.Clamp(p.Fraction, 0, 1));
                var result = FFmpegConverter.Convert(request, progress, token);

                token.ThrowIfCancellationRequested();

                if (!result.Success)
                    throw new InvalidOperationException(result.ErrorMessage ?? "Conversion failed.");

                if (!File.Exists(tempPath) || new FileInfo(tempPath).Length == 0)
                    throw new InvalidOperationException("Conversion produced no output.");

                MoveTempToFinal(tempPath, finalPath);

                // Preserve timestamp
                DateTime desiredTime = File.GetLastWriteTime(job.InputPath);
                SetFileTimestampWithRetries(finalPath, desiredTime, token);

                // Get output file size with retries
                long afterSize = GetFileSizeWithRetries(finalPath, token);

                Interlocked.Increment(ref _completedCount);
                Interlocked.Add(ref _totalAfterSize, afterSize);
                Interlocked.Add(ref _processedInputBytes, beforeSize);
                CompleteProgress(job.RowIndex, job.ProgressWeight);
                SetRowStatus(job.RowIndex, "Done", "100%");

                if (result.Warnings.Count > 0 || job.OutputWasAutoRenamed)
                {
                    var warnings = result.Warnings.ToList();
                    if (job.OutputWasAutoRenamed)
                        warnings.Add($"Output renamed to avoid overwriting: {Path.GetFileName(finalPath)}");

                    Invoke(() =>
                    {
                        if (!tabControl.TabPages.Contains(tabPageFailedFiles))
                        {
                            tabControl.TabPages.Add(tabPageFailedFiles);
                        }
                        dataGridViewFailed.Rows.Add(job.InputPath, string.Join("; ", warnings));
                    });
                }
            }
            catch (OperationCanceledException)
            {
                SafeDelete(tempPath);
                _fileProgress.TryRemove(job.RowIndex, out _);
                _progressWeights.TryRemove(job.RowIndex, out _);
                SetRowStatus(job.RowIndex, "Cancelled", null);
                throw;
            }
            catch (Exception ex)
            {
                SafeDelete(tempPath);
                _fileProgress.TryRemove(job.RowIndex, out _);
                CompleteProgress(job.RowIndex, job.ProgressWeight);
                SetRowStatus(job.RowIndex, "Failed", null);

                lock (_lockObj)
                {
                    _failedCount++;
                }

                Invoke(() =>
                {
                    if (!tabControl.TabPages.Contains(tabPageFailedFiles))
                    {
                        tabControl.TabPages.Add(tabPageFailedFiles);
                    }
                    dataGridViewFailed.Rows.Add(job.InputPath, ex.Message);
                    tabControl.SelectedTab = tabPageFailedFiles;
                });
            }
        }

        private static void SafeDelete(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch { }
        }

        private static void MoveTempToFinal(string tempPath, string finalPath)
        {
            File.Move(tempPath, finalPath, overwrite: false);
        }

        private void ResetProgressState(double totalWeight)
        {
            _fileProgress.Clear();
            _progressWeights.Clear();
            lock (_progressLock)
            {
                _totalProgressWeight = totalWeight;
                _finishedProgressWeight = 0;
            }
        }

        private void CompleteProgress(int rowIndex, double weight)
        {
            _fileProgress.TryRemove(rowIndex, out _);
            _progressWeights.TryRemove(rowIndex, out _);
            lock (_progressLock)
            {
                _finishedProgressWeight += Math.Max(1, weight);
            }
        }

        private void SetRowStatus(int rowIndex, string status, string? progress)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)(() => SetRowStatus(rowIndex, status, progress)));
                return;
            }

            if (rowIndex < 0 || rowIndex >= dataGridView1.Rows.Count)
                return;

            var row = dataGridView1.Rows[rowIndex];
            SetCellValue(row, "Status", status);
            if (progress != null)
                SetCellValue(row, "Progress", progress);
        }

        private long GetFileSizeWithRetries(string path, CancellationToken token)
        {
            const int maxRetries = 8;
            int attempt = 0;
            for (; attempt < maxRetries; attempt++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var fi = new FileInfo(path);
                    if (fi.Exists && fi.Length > 0)
                    {
                        return fi.Length;
                    }
                }
                catch { }
                Thread.Sleep(30);
            }
            try { return new FileInfo(path).Length; } catch { return 0; }
        }

        private void SetFileTimestampWithRetries(string path, DateTime timestamp, CancellationToken token)
        {
            const int maxRetries = 8;
            int attempt = 0;
            for (; attempt < maxRetries; attempt++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    if (File.Exists(path))
                    {
                        // Ensure file handle is free by opening with shared read
                        using (var fsCheck = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) { }
                        File.SetLastWriteTime(path, timestamp);
                        return;
                    }
                }
                catch (IOException) { /* file busy, retry */ }
                catch (UnauthorizedAccessException) { /* file busy/locked, retry */ }
                Thread.Sleep(50);
            }
            try { File.SetLastWriteTime(path, timestamp); } catch { }
        }

        private void UpdateElapsedTime()
        {
            try
            {
                double elapsed = _sw.Elapsed.TotalSeconds;
                string elapsedText = elapsed > 0 ? elapsed.ToString("N2") + " s" : "0.00 s";
                elapsedLabel.Text = elapsedText;
            }
            catch { }
        }

        private void UpdateUI()
        {
            if (InvokeRequired)
            {
                BeginInvoke((System.Windows.Forms.MethodInvoker)delegate { UpdateUI(); });
                return;
            }
            try
            {
                var completed = Interlocked.CompareExchange(ref _completedCount, 0, 0);
                var failed = Interlocked.CompareExchange(ref _failedCount, 0, 0);
                foreach (var progress in _fileProgress)
                {
                    double fraction = Math.Clamp(progress.Value, 0, 1);
                    if (progress.Key >= 0 && progress.Key < dataGridView1.Rows.Count)
                    {
                        SetCellValue(dataGridView1.Rows[progress.Key], "Progress", $"{(int)Math.Round(fraction * 100)}%");
                    }
                }

                double totalWeight;
                double finishedWeight;
                lock (_progressLock)
                {
                    totalWeight = _totalProgressWeight;
                    finishedWeight = _finishedProgressWeight;
                }

                double inFlightWeight = ProgressAggregator.GetInFlightWeight(_fileProgress, _progressWeights);
                var size = Interlocked.Read(ref _totalAfterSize);
                double elapsed = _sw.Elapsed.TotalSeconds;
                double filesPerSec = elapsed > 0 ? completed / elapsed : 0.0;
                double mbSec = elapsed > 0 ? ((Interlocked.Read(ref _processedInputBytes) + inFlightWeight) / 1024.0 / 1024.0) / elapsed : 0.0;
                double inBytes = Interlocked.Read(ref _totalBeforeSize);
                double saved = inBytes > 0 ? 100.0 * (1.0 - (double)size / inBytes) : 0.0;
                int pct = ProgressAggregator.GetPercentage(totalWeight, finishedWeight, inFlightWeight);

                completedFiles.Text = completed.ToString();
                afterSizeLabel.Text = FormatBytes(size);
                filesPerSecLabel.Text = filesPerSec.ToString("N2") + " files/s";
                mbPerSecLabel.Text = mbSec.ToString("N2") + " MB/s";
                percentSavedLabel.Text = saved.ToString("N2") + "%";
                processingProgress.Value = Math.Max(0, Math.Min(100, pct));
            }
            catch { }
        }

        private void UpdateBitDepthVisibility()
        {
            var format = comboBoxFormat.SelectedItem?.ToString() ?? string.Empty;
            bool supportsBitDepth = OutputPresetCatalog.Get(format).SupportsBitDepth;

            comboBoxBitDepth.Visible = supportsBitDepth;
            labelBitDepth.Visible = supportsBitDepth;
            comboBoxBitDepth.Enabled = supportsBitDepth;
            labelBitDepth.Enabled = supportsBitDepth;
            numericUpDownBitrate.Visible = !supportsBitDepth;
            labelBitrate.Visible = !supportsBitDepth;
            numericUpDownBitrate.Enabled = !supportsBitDepth;
            labelBitrate.Enabled = !supportsBitDepth;

            if (supportsBitDepth)
            {
                if (comboBoxBitDepth.Items.Count != SupportedBitDepths.Length)
                {
                    comboBoxBitDepth.Items.Clear();
                    comboBoxBitDepth.Items.AddRange(SupportedBitDepths);
                }

                var selected = comboBoxBitDepth.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selected) || !SupportedBitDepths.Contains(selected))
                {
                    if (!_isFirstLaunch && SupportedBitDepths.Contains(_lastSupportedBitDepth))
                    {
                        comboBoxBitDepth.SelectedItem = _lastSupportedBitDepth;
                    }
                    else
                    {
                        comboBoxBitDepth.SelectedItem = "16";
                    }
                }

                _lastSupportedBitDepth = comboBoxBitDepth.SelectedItem?.ToString() ?? "16";
            }
        }

        private void UpdateSampleRateOptions()
        {
            var format = comboBoxFormat.SelectedItem?.ToString() ?? "mp3";
            var previous = comboBoxSampleRate.SelectedItem?.ToString();
            var rates = GetSupportedSampleRates(format);

            comboBoxSampleRate.Items.Clear();
            foreach (var rate in rates)
            {
                comboBoxSampleRate.Items.Add(rate.ToString());
            }

            if (!string.IsNullOrEmpty(previous) && comboBoxSampleRate.Items.Contains(previous))
            {
                comboBoxSampleRate.SelectedItem = previous;
                return;
            }

            if (comboBoxSampleRate.Items.Contains("44100"))
            {
                comboBoxSampleRate.SelectedItem = "44100";
            }
            else if (comboBoxSampleRate.Items.Count > 0)
            {
                comboBoxSampleRate.SelectedIndex = 0;
            }
        }

        private void UpdateSampleRateMode()
        {
            bool useSource = checkBoxUseSourceSampleRate.Checked;
            comboBoxSampleRate.Enabled = !useSource;
            labelSampleRate.Enabled = !useSource;

            if (!useSource)
            {
                UpdateSampleRateOptions();
            }
        }

        private unsafe static int[] GetSupportedSampleRates(string format)
        {
            try
            {
                var codecId = OutputPresetCatalog.GetCodecId(format, 16);
                AVCodec* codec = avcodec_find_encoder(codecId);
                if (codec == null || codec->supported_samplerates == null)
                {
                    return DefaultSampleRates;
                }

                var rates = new List<int>();
                for (int* p = codec->supported_samplerates; *p != 0; p++)
                {
                    rates.Add(*p);
                }

                if (rates.Count == 0)
                {
                    return DefaultSampleRates;
                }

                rates.Sort();
                return rates.ToArray();
            }
            catch
            {
                return DefaultSampleRates;
            }
        }

        private void cancelButton_Click(object? sender, EventArgs e)
        {
            if (!_isProcessing)
                return;

            try
            {
                if (!_cts.IsCancellationRequested)
                    _cts.Cancel();
                buttonConvert.Text = "Cancelling...";
                cancelButton.Enabled = false;
            }
            catch (ObjectDisposedException) { }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            _uiUpdateTimer?.Stop();
            _elapsedTimeTimer?.Stop();
        }

        private void clearListButton_Click(object? sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            dataGridViewFailed.Rows.Clear();
            UpdateFileCount();
            _completedCount = 0;
            _failedCount = 0;
            _totalBeforeSize = 0;
            _totalAfterSize = 0;
            _processedInputBytes = 0;
            ResetProgressState(0);
            completedFiles.Text = "0";
            beforeSizeLabel.Text = "0";
            afterSizeLabel.Text = "0";
            percentSavedLabel.Text = "0.00%";
            elapsedLabel.Text = "00:00:00";
            filesPerSecLabel.Text = "0.00 files/s";
            mbPerSecLabel.Text = "0.00 MB/s";
            processingProgress.Value = 0;

            // Remove Failed Files tab
            if (tabControl.TabPages.Contains(tabPageFailedFiles))
            {
                tabControl.TabPages.Remove(tabPageFailedFiles);
            }

            // Switch back to All Files tab
            tabControl.SelectedTab = tabPageAllFiles;
        }

        private void dataGridViewFailed_DoubleClick(object? sender, EventArgs e)
        {
            if (dataGridViewFailed.CurrentRow == null)
                return;

            var cellValue = dataGridViewFailed.CurrentRow.Cells[0].Value;
            if (cellValue == null)
                return;

            string filePath = cellValue.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private string ShowOptimizedSettingsDialog()
        {
            using var dialog = new Form
            {
                Text = "Select Optimized Settings",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false
            };

            var highQualityButton = new Button
            {
                Text = "High Quality",
                Width = 200,
                Height = 40,
                Top = 20,
                Left = 20
            };

            var standardButton = new Button
            {
                Text = "Standard",
                Width = 200,
                Height = 40,
                Top = 70,
                Left = 20
            };

            var compactButton = new Button
            {
                Text = "Compact",
                Width = 200,
                Height = 40,
                Top = 120,
                Left = 20
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Width = 200,
                Height = 40,
                Top = 170,
                Left = 20,
                DialogResult = DialogResult.Cancel
            };

            dialog.ClientSize = new Size(240, 230);
            dialog.Controls.AddRange(new Control[] { highQualityButton, standardButton, compactButton, cancelButton });

            highQualityButton.Click += (s, ev) => { dialog.DialogResult = DialogResult.OK; dialog.Tag = "High Quality"; dialog.Close(); };
            standardButton.Click += (s, ev) => { dialog.DialogResult = DialogResult.OK; dialog.Tag = "Standard"; dialog.Close(); };
            compactButton.Click += (s, ev) => { dialog.DialogResult = DialogResult.OK; dialog.Tag = "Compact"; dialog.Close(); };
            cancelButton.Click += (s, ev) => { dialog.Close(); };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                return dialog.Tag?.ToString() ?? "Standard";
            }
            return "Standard";
        }

        private void ShowAboutDialog()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            string licensePath = Path.Combine(AppContext.BaseDirectory, "LICENSES.md");
            if (!File.Exists(licensePath))
                licensePath = Path.Combine(AppContext.BaseDirectory, "docs", "LICENSES.md");

            MessageBox.Show(
                $"AudioConverter v{version}{Environment.NewLine}{Environment.NewLine}" +
                "Uses FFmpeg shared libraries via FFmpeg.AutoGen." + Environment.NewLine +
                FFmpegConverter.GetRuntimeInfo() + Environment.NewLine + Environment.NewLine +
                $"License notices: {licensePath}",
                "About AudioConverter",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isProcessing)
            {
                var result = MessageBox.Show(
                    "Conversion is in progress. Are you sure you want to exit?",
                    "Confirm Exit",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                try { _cts.Cancel(); } catch (ObjectDisposedException) { }
            }

            SaveSettings();
            try { _cts.Dispose(); } catch (ObjectDisposedException) { }
            _uiUpdateTimer?.Dispose();
            _elapsedTimeTimer?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
