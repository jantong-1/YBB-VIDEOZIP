using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VideoCompressorUI
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (!Environment.Is64BitOperatingSystem)
            {
                MessageBox.Show("当前软件仅支持 64 位 Windows。", "YBBvideozip", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Application.Run(new MainForm());
        }
    }

    internal enum CodecChoice
    {
        H264,
        H265
    }

    internal enum EngineChoice
    {
        Cpu,
        Gpu
    }

    internal enum QualityChoice
    {
        High,
        Balanced,
        Small
    }

    internal sealed class VideoJob
    {
        public string InputPath;
        public string OutputPath;
        public string Status;
        public int Progress;
        public double DurationSeconds;
        public string ErrorMessage;
    }

    internal sealed class TimeoutWebClient : WebClient
    {
        public int TimeoutMs = 300000;

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            if (request != null)
            {
                request.Timeout = TimeoutMs;
            }
            return request;
        }
    }

    internal sealed class MainForm : Form
    {
        private const string DefaultRuntimeArchiveName = "ffmpeg-runtime-win64-gpl-8.1.1-ybb.zip";
        private const string RuntimeArchiveSha256 = "457a01bdb3b98c16af8aa210adcda9b1fd3fc0ad09d0d3c299c436d1bbe713ab";
        private const string RuntimeDownloadUrl = "https://shenlouar.cn/YBBvideozipFFmpeg/ffmpeg-runtime-win64-gpl-8.1.1-ybb.zip";
        private const string RuntimeProductDirName = "YBBvideozip";

        private static readonly string[] SupportedExtensions =
        {
            ".mp4", ".mov", ".mkv", ".avi", ".webm", ".m4v"
        };

        private readonly List<VideoJob> jobs = new List<VideoJob>();
        private readonly RoundedPanel mainPanel;
        private readonly DashedPanel dropPanel;
        private readonly Label hintLabel;
        private readonly DataGridView jobGrid;
        private readonly RoundedButton h264Button;
        private readonly RoundedButton h265Button;
        private readonly RoundedButton cpuButton;
        private readonly RoundedButton gpuButton;
        private readonly ComboBox qualityCombo;
        private readonly RoundedButton outputDirButton;
        private readonly Label statusLabel;
        private readonly RoundedButton executeButton;

        private string ffmpegPath;
        private string ffprobePath;
        private string ffmpegVersionLine;
        private string encoderList;
        private string customOutputDirectory;
        private bool toolsReady;
        private bool isRunning;
        private bool queueFinished;
        private CodecChoice selectedCodec = CodecChoice.H264;
        private EngineChoice selectedEngine = EngineChoice.Cpu;
        private QualityChoice selectedQuality = QualityChoice.Balanced;

        public MainForm()
        {
            Text = "YBB视频压缩";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(910, 615);
            MinimumSize = new Size(820, 560);
            BackColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            ShowIcon = false;
            AllowDrop = true;

            mainPanel = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                Radius = 0,
                BorderWidth = 0,
                DrawBorder = false,
                BorderColor = Color.Transparent,
                BackColor = Color.White
            };
            Controls.Add(mainPanel);

            dropPanel = new DashedPanel
            {
                Location = new Point(24, 24),
                Size = new Size(862, 420),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Radius = 15,
                BorderWidth = 2,
                BorderColor = Color.Black,
                BackColor = Color.FromArgb(218, 218, 218),
                AllowDrop = true
            };
            mainPanel.Controls.Add(dropPanel);

            hintLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(140, 140, 140),
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei UI", 19F, FontStyle.Regular, GraphicsUnit.Point),
                Text = DefaultHintText()
            };
            dropPanel.Controls.Add(hintLabel);

            jobGrid = new DataGridView
            {
                Location = new Point(28, 28),
                Size = new Size(806, 364),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Visible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.FromArgb(218, 218, 218),
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(200, 200, 200),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowDrop = true
            };
            jobGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(205, 205, 205);
            jobGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(70, 70, 70);
            jobGrid.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
            jobGrid.EnableHeadersVisualStyles = false;
            jobGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "文件", FillWeight = 34 });
            jobGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "状态", FillWeight = 14 });
            jobGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "进度", FillWeight = 12 });
            jobGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "输出位置", FillWeight = 40 });
            dropPanel.Controls.Add(jobGrid);

            h264Button = CreateOptionButton("H.264", new Point(34, 470), new Size(120, 48));
            h265Button = CreateOptionButton("H.265", new Point(166, 470), new Size(120, 48));
            cpuButton = CreateOptionButton("CPU", new Point(318, 470), new Size(120, 48));
            gpuButton = CreateOptionButton("GPU", new Point(450, 470), new Size(120, 48));
            mainPanel.Controls.Add(h264Button);
            mainPanel.Controls.Add(h265Button);
            mainPanel.Controls.Add(cpuButton);
            mainPanel.Controls.Add(gpuButton);

            qualityCombo = new ComboBox
            {
                Location = new Point(34, 535),
                Size = new Size(252, 42),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Regular, GraphicsUnit.Point)
            };
            qualityCombo.Items.Add("高质量");
            qualityCombo.Items.Add("均衡");
            qualityCombo.Items.Add("小体积");
            qualityCombo.SelectedIndex = 1;
            mainPanel.Controls.Add(qualityCombo);

            outputDirButton = CreateOptionButton("自定义保存", new Point(318, 535), new Size(120, 48));
            outputDirButton.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            mainPanel.Controls.Add(outputDirButton);

            executeButton = new RoundedButton
            {
                Text = "执行",
                Location = new Point(732, 470),
                Size = new Size(128, 108),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                BackColor = Color.FromArgb(210, 210, 210),
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Regular, GraphicsUnit.Point),
                Cursor = Cursors.Hand
            };
            executeButton.Radius = 15;
            mainPanel.Controls.Add(executeButton);

            statusLabel = new Label
            {
                AutoSize = false,
                Location = new Point(450, 535),
                Size = new Size(260, 44),
                ForeColor = Color.FromArgb(120, 120, 120),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Text = OutputLocationText()
            };
            mainPanel.Controls.Add(statusLabel);

            h264Button.Click += delegate { SelectCodec(CodecChoice.H264); };
            h265Button.Click += delegate { SelectCodec(CodecChoice.H265); };
            cpuButton.Click += delegate { SelectEngine(EngineChoice.Cpu); };
            gpuButton.Click += delegate { SelectEngine(EngineChoice.Gpu); };
            qualityCombo.SelectedIndexChanged += delegate { SelectQualityFromCombo(); };
            outputDirButton.Click += OutputDirButtonClick;
            executeButton.Click += ExecuteButtonClick;

            DragEnter += FilesDragEnter;
            DragDrop += FilesDragDrop;
            dropPanel.DragEnter += FilesDragEnter;
            dropPanel.DragDrop += FilesDragDrop;
            hintLabel.DragEnter += FilesDragEnter;
            hintLabel.DragDrop += FilesDragDrop;
            jobGrid.DragEnter += FilesDragEnter;
            jobGrid.DragDrop += FilesDragDrop;

            Resize += delegate { LayoutControls(); };
            LayoutControls();
            RefreshOptionButtons();
            Task.Run(new Action(CheckToolsAtStartup));
        }

        private RoundedButton CreateOptionButton(string text, Point location, Size size)
        {
            RoundedButton button = new RoundedButton
            {
                Text = text,
                Location = location,
                Size = size,
                Radius = 15,
                BackColor = Color.FromArgb(214, 214, 214),
                ForeColor = Color.FromArgb(55, 55, 55),
                Font = new Font("Microsoft YaHei UI", 17F, FontStyle.Regular, GraphicsUnit.Point),
                Cursor = Cursors.Hand
            };
            return button;
        }

        private void LayoutControls()
        {
            const int margin = 24;
            const int bottomAreaHeight = 160;
            int panelWidth = Math.Max(0, mainPanel.ClientSize.Width);
            int panelHeight = Math.Max(0, mainPanel.ClientSize.Height);
            int dropHeight = Math.Max(280, panelHeight - margin - bottomAreaHeight);

            dropPanel.Location = new Point(margin, margin);
            dropPanel.Size = new Size(Math.Max(360, panelWidth - margin * 2), dropHeight);

            jobGrid.Location = new Point(28, 28);
            jobGrid.Size = new Size(Math.Max(300, dropPanel.ClientSize.Width - 56), Math.Max(200, dropPanel.ClientSize.Height - 56));

            int controlY = dropPanel.Bottom + 30;
            int secondRowY = controlY + 65;
            h264Button.Location = new Point(34, controlY);
            h265Button.Location = new Point(166, controlY);
            cpuButton.Location = new Point(318, controlY);
            gpuButton.Location = new Point(450, controlY);
            qualityCombo.Location = new Point(34, secondRowY);

            executeButton.Location = new Point(Math.Max(610, panelWidth - 34 - executeButton.Width), controlY);
            outputDirButton.Location = new Point(318, secondRowY);
            statusLabel.Location = new Point(450, secondRowY);
            statusLabel.Size = new Size(Math.Max(120, executeButton.Left - statusLabel.Left - 26), 44);
        }

        private static string DefaultHintText()
        {
            return "拖入视频文件，进行压缩。\r\n压缩完成后，文件将保存在源文件旁。";
        }

        private string OutputLocationText()
        {
            if (String.IsNullOrWhiteSpace(customOutputDirectory))
            {
                return "保存到：源文件旁";
            }

            return "保存到：" + customOutputDirectory;
        }

        private void OutputDirButtonClick(object sender, EventArgs e)
        {
            if (isRunning)
            {
                return;
            }

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择压缩后视频的保存目录";
                dialog.ShowNewFolderButton = true;
                if (!String.IsNullOrWhiteSpace(customOutputDirectory) && Directory.Exists(customOutputDirectory))
                {
                    dialog.SelectedPath = customOutputDirectory;
                }
                else if (jobs.Count > 0)
                {
                    dialog.SelectedPath = Path.GetDirectoryName(jobs[0].InputPath);
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    customOutputDirectory = dialog.SelectedPath;
                    statusLabel.Text = OutputLocationText();
                    RefreshJobGrid();
                }
            }
        }

        private void CheckToolsAtStartup()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                ffmpegPath = ResolveToolPath(baseDir, "ffmpeg.exe");
                ffprobePath = ResolveToolPath(baseDir, "ffprobe.exe");

                if (!File.Exists(ffmpegPath) || !File.Exists(ffprobePath))
                {
                    if (!InstallFfmpegRuntime(baseDir))
                    {
                        toolsReady = false;
                        string message = "未找到 ffmpeg\\bin\\ffmpeg.exe 或 ffprobe.exe。";
                        Ui(delegate
                        {
                            statusLabel.Text = message;
                            SetHint(message);
                        });
                        return;
                    }

                    ffmpegPath = ResolveToolPath(baseDir, "ffmpeg.exe");
                    ffprobePath = ResolveToolPath(baseDir, "ffprobe.exe");
                    if (!File.Exists(ffmpegPath) || !File.Exists(ffprobePath))
                    {
                        toolsReady = false;
                        string message = "FFmpeg 组件安装后仍未找到必要文件。";
                        Ui(delegate
                        {
                            statusLabel.Text = message;
                            SetHint(message);
                        });
                        return;
                    }
                }

                ffmpegVersionLine = RunProcessCapture(ffmpegPath, "-hide_banner -version", 8000).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).FirstOrDefault();
                encoderList = RunProcessCapture(ffmpegPath, "-hide_banner -encoders", 12000);

                bool hasX264 = HasEncoder("libx264");
                bool hasX265 = HasEncoder("libx265");
                int major = ParseFfmpegMajor(ffmpegVersionLine);
                toolsReady = hasX264 && hasX265;

                Ui(delegate
                {
                    if (!toolsReady)
                    {
                        statusLabel.Text = "FFmpeg 缺少 libx264 或 libx265。";
                        SetHint("当前 FFmpeg 缺少必要编码器，不能开始压缩。");
                        return;
                    }

                    statusLabel.Text = OutputLocationText();
                    if (major > 0 && major < 6)
                    {
                        SetHint("当前 FFmpeg 版本偏旧，建议使用 6.1 或更新版本。");
                    }
                    else
                    {
                        SetHint(DefaultHintText());
                    }
                });
            }
            catch (Exception ex)
            {
                toolsReady = false;
                Ui(delegate
                {
                    statusLabel.Text = "FFmpeg 检查失败";
                    SetHint("FFmpeg 检查失败：" + ex.Message);
                });
            }
        }

        private bool InstallFfmpegRuntime(string baseDir)
        {
            bool accepted = UiCall(delegate
            {
                DialogResult result = MessageBox.Show(
                    this,
                    "首次运行需要下载 FFmpeg 运行组件。\r\n\r\n点击“是”后将自动下载并安装。",
                    "安装 FFmpeg 组件",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                return result == DialogResult.Yes;
            });

            if (!accepted)
            {
                Ui(delegate
                {
                    statusLabel.Text = "未安装 FFmpeg 组件";
                    SetHint("未安装 FFmpeg 组件，不能开始压缩。");
                });
                return false;
            }

            string installRoot = GetRuntimeInstallRoot(baseDir);
            string tempRoot = Path.Combine(Path.GetTempPath(), RuntimeProductDirName, Guid.NewGuid().ToString("N"));
            string archivePath = Path.Combine(tempRoot, DefaultRuntimeArchiveName);

            try
            {
                Directory.CreateDirectory(tempRoot);
                Ui(delegate
                {
                    statusLabel.Text = "正在下载 FFmpeg 组件...";
                    SetHint("正在下载 FFmpeg 组件，请稍候。");
                });

                DownloadFile(RuntimeDownloadUrl, archivePath);

                string actualHash = ComputeSha256(archivePath);
                if (!String.Equals(actualHash, RuntimeArchiveSha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("FFmpeg 组件 SHA256 校验失败。");
                }

                Ui(delegate
                {
                    statusLabel.Text = "正在解压 FFmpeg 组件...";
                    SetHint("正在解压 FFmpeg 组件，请稍候。");
                });

                ExtractZipSafely(archivePath, installRoot);

                string installedFfmpeg = Path.Combine(installRoot, "bin", "ffmpeg.exe");
                string installedFfprobe = Path.Combine(installRoot, "bin", "ffprobe.exe");
                if (!File.Exists(installedFfmpeg) || !File.Exists(installedFfprobe))
                {
                    throw new FileNotFoundException("压缩包内缺少 ffmpeg.exe 或 ffprobe.exe。");
                }

                Ui(delegate
                {
                    statusLabel.Text = "FFmpeg 组件安装完成";
                    SetHint(DefaultHintText());
                });
                return true;
            }
            catch (Exception ex)
            {
                Ui(delegate
                {
                    statusLabel.Text = "FFmpeg 组件安装失败";
                    SetHint("FFmpeg 组件安装失败：" + ex.Message);
                });
                return false;
            }
            finally
            {
                TryDeleteDirectory(tempRoot);
            }
        }

        private static string ResolveToolPath(string baseDir, string fileName)
        {
            string nested = Path.Combine(baseDir, "ffmpeg", "bin", fileName);
            if (File.Exists(nested))
            {
                return nested;
            }

            string userNested = Path.Combine(GetUserRuntimeRoot(), "bin", fileName);
            if (File.Exists(userNested))
            {
                return userNested;
            }

            return Path.Combine(baseDir, fileName);
        }

        private static string GetRuntimeInstallRoot(string baseDir)
        {
            if (CanWriteDirectory(baseDir))
            {
                return Path.Combine(baseDir, "ffmpeg");
            }

            return GetUserRuntimeRoot();
        }

        private static string GetUserRuntimeRoot()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (String.IsNullOrWhiteSpace(localAppData))
            {
                localAppData = Path.GetTempPath();
            }

            return Path.Combine(localAppData, RuntimeProductDirName, "ffmpeg");
        }

        private static bool CanWriteDirectory(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                string testPath = Path.Combine(directory, ".write-test-" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(testPath, "test");
                File.Delete(testPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void DownloadFile(string url, string destinationPath)
        {
            ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | (SecurityProtocolType)3072;
            using (TimeoutWebClient client = new TimeoutWebClient())
            {
                client.DownloadFile(new Uri(url), destinationPath);
            }
        }

        private static string ComputeSha256(string path)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = sha256.ComputeHash(stream);
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash)
                {
                    builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                }
                return builder.ToString();
            }
        }

        private static void ExtractZipSafely(string archivePath, string targetRoot)
        {
            Directory.CreateDirectory(targetRoot);
            string normalizedRoot = Path.GetFullPath(targetRoot);
            if (!normalizedRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                normalizedRoot += Path.DirectorySeparatorChar;
            }

            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string destinationPath = Path.GetFullPath(Path.Combine(targetRoot, entry.FullName));
                    if (!destinationPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException("压缩包内存在非法路径。");
                    }

                    if (String.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    string parentDirectory = Path.GetDirectoryName(destinationPath);
                    if (!String.IsNullOrEmpty(parentDirectory))
                    {
                        Directory.CreateDirectory(parentDirectory);
                    }
                    entry.ExtractToFile(destinationPath, true);
                }
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch
            {
            }
        }

        private static int ParseFfmpegMajor(string line)
        {
            if (String.IsNullOrWhiteSpace(line))
            {
                return 0;
            }

            Match match = Regex.Match(line, @"ffmpeg version\s+(\d+)");
            if (!match.Success)
            {
                return 0;
            }

            int major;
            return Int32.TryParse(match.Groups[1].Value, out major) ? major : 0;
        }

        private bool HasEncoder(string encoderName)
        {
            return !String.IsNullOrEmpty(encoderList) &&
                   encoderList.IndexOf(encoderName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SelectCodec(CodecChoice codec)
        {
            if (isRunning)
            {
                return;
            }

            selectedCodec = codec;
            RefreshOptionButtons();
            if (selectedEngine == EngineChoice.Gpu)
            {
                BeginGpuCheck();
            }
        }

        private void SelectEngine(EngineChoice engine)
        {
            if (isRunning)
            {
                return;
            }

            selectedEngine = engine;
            RefreshOptionButtons();
            if (engine == EngineChoice.Gpu)
            {
                BeginGpuCheck();
            }
            else
            {
                SetHint(jobs.Count == 0 ? DefaultHintText() : "");
            }
        }

        private void SelectQualityFromCombo()
        {
            if (qualityCombo.SelectedIndex == 0)
            {
                selectedQuality = QualityChoice.High;
            }
            else if (qualityCombo.SelectedIndex == 2)
            {
                selectedQuality = QualityChoice.Small;
            }
            else
            {
                selectedQuality = QualityChoice.Balanced;
            }
        }

        private async void BeginGpuCheck()
        {
            if (!toolsReady)
            {
                selectedEngine = EngineChoice.Cpu;
                RefreshOptionButtons();
                SetHint("FFmpeg 未通过检查，暂不能使用 GPU。");
                return;
            }

            string encoder = GetGpuEncoder();
            SetHint("正在检测 GPU 编码器...");
            bool ok = await Task.Run(new Func<bool>(delegate { return IsGpuEncoderUsable(encoder); }));
            if (selectedEngine != EngineChoice.Gpu)
            {
                return;
            }

            if (!ok)
            {
                selectedEngine = EngineChoice.Cpu;
                RefreshOptionButtons();
                SetHint("当前机器未检测到可用 NVIDIA GPU 编码器，建议使用 CPU。");
            }
            else
            {
                SetHint(jobs.Count == 0 ? "GPU 编码器可用，可以拖入视频开始压缩。" : "");
            }
        }

        private bool IsGpuEncoderUsable(string encoder)
        {
            if (!HasEncoder(encoder))
            {
                return false;
            }

            string args = "-hide_banner -loglevel error -f lavfi -i testsrc2=size=64x64:rate=1 -t 0.1 -an -c:v " +
                          encoder + " -f null -";
            try
            {
                using (Process process = StartProcess(ffmpegPath, args, true))
                {
                    return process.WaitForExit(10000) && process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private void RefreshOptionButtons()
        {
            SetButtonSelected(h264Button, selectedCodec == CodecChoice.H264);
            SetButtonSelected(h265Button, selectedCodec == CodecChoice.H265);
            SetButtonSelected(cpuButton, selectedEngine == EngineChoice.Cpu);
            SetButtonSelected(gpuButton, selectedEngine == EngineChoice.Gpu);
        }

        private static void SetButtonSelected(RoundedButton button, bool selected)
        {
            button.BackColor = selected ? Color.FromArgb(55, 55, 55) : Color.FromArgb(214, 214, 214);
            button.ForeColor = selected ? Color.White : Color.FromArgb(55, 55, 55);
        }

        private void ExecuteButtonClick(object sender, EventArgs e)
        {
            if (queueFinished)
            {
                ClearJobs();
                return;
            }

            if (isRunning)
            {
                return;
            }

            if (!toolsReady)
            {
                SetHint("FFmpeg 未通过检查，不能开始压缩。");
                return;
            }

            if (jobs.Count == 0)
            {
                SetHint("请先拖入一个或多个视频文件。");
                return;
            }

            StartQueue();
        }

        private void StartQueue()
        {
            isRunning = true;
            queueFinished = false;
            executeButton.Text = "执行中";
            executeButton.Enabled = false;
            h264Button.Enabled = false;
            h265Button.Enabled = false;
            cpuButton.Enabled = false;
            gpuButton.Enabled = false;
            qualityCombo.Enabled = false;
            outputDirButton.Enabled = false;
            SetHint("");

            Task.Run(new Action(RunQueue));
        }

        private void RunQueue()
        {
            int failures = 0;
            foreach (VideoJob job in jobs)
            {
                if (job.Status == "完成")
                {
                    continue;
                }

                job.Status = "进行中";
                job.Progress = 0;
                job.ErrorMessage = "";
                job.OutputPath = CreateOutputPath(job.InputPath);
                UpdateJobRow(job);

                bool ok = RunEncode(job);
                if (!ok)
                {
                    failures++;
                }
            }

            Ui(delegate
            {
                isRunning = false;
                queueFinished = true;
                executeButton.Text = "完成";
                executeButton.Enabled = true;
                h264Button.Enabled = true;
                h265Button.Enabled = true;
                cpuButton.Enabled = true;
                gpuButton.Enabled = true;
                qualityCombo.Enabled = true;
                outputDirButton.Enabled = true;
                statusLabel.Text = failures == 0 ? "全部完成。点击“完成”清空列表。" : "任务结束，部分文件失败。点击“完成”清空列表。";
                SetHint("");
            });
        }

        private bool RunEncode(VideoJob job)
        {
            job.DurationSeconds = ProbeDuration(job.InputPath);
            string args = BuildEncodeArguments(job.InputPath, job.OutputPath);
            List<string> errorLines = new List<string>();

            try
            {
                using (Process process = StartProcess(ffmpegPath, args, false))
                {
                    process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                    {
                        if (!String.IsNullOrWhiteSpace(e.Data))
                        {
                            HandleProgressLine(job, e.Data);
                        }
                    };
                    process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                    {
                        if (!String.IsNullOrWhiteSpace(e.Data))
                        {
                            lock (errorLines)
                            {
                                if (errorLines.Count > 8)
                                {
                                    errorLines.RemoveAt(0);
                                }
                                errorLines.Add(e.Data);
                            }
                        }
                    };

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    if (process.ExitCode == 0 && File.Exists(job.OutputPath))
                    {
                        job.Progress = 100;
                        job.Status = "完成";
                        UpdateJobRow(job);
                        return true;
                    }

                    job.Status = "失败";
                    job.Progress = 0;
                    lock (errorLines)
                    {
                        job.ErrorMessage = errorLines.Count == 0 ? "FFmpeg 返回失败。" : errorLines[errorLines.Count - 1];
                    }
                    UpdateJobRow(job);
                    return false;
                }
            }
            catch (Exception ex)
            {
                job.Status = "失败";
                job.Progress = 0;
                job.ErrorMessage = ex.Message;
                UpdateJobRow(job);
                return false;
            }
        }

        private void HandleProgressLine(VideoJob job, string line)
        {
            if (line.StartsWith("out_time=", StringComparison.OrdinalIgnoreCase))
            {
                double seconds = ParseOutTimeSeconds(line.Substring("out_time=".Length));
                if (seconds >= 0 && job.DurationSeconds > 0)
                {
                    int progress = (int)Math.Floor(seconds * 100.0 / job.DurationSeconds);
                    if (progress > 99)
                    {
                        progress = 99;
                    }
                    if (progress > job.Progress)
                    {
                        job.Progress = progress;
                        UpdateJobRow(job);
                    }
                }
            }
            else if (line.Trim().Equals("progress=end", StringComparison.OrdinalIgnoreCase))
            {
                job.Progress = 100;
                UpdateJobRow(job);
            }
        }

        private double ProbeDuration(string inputPath)
        {
            try
            {
                string args = "-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 " + QuoteArg(inputPath);
                string output = RunProcessCapture(ffprobePath, args, 15000).Trim();
                double value;
                if (Double.TryParse(output, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    return value;
                }
            }
            catch
            {
            }

            return 0;
        }

        private string BuildEncodeArguments(string inputPath, string outputPath)
        {
            List<string> args = new List<string>();
            args.Add("-y");
            args.Add("-hide_banner");
            args.Add("-nostats");
            args.Add("-progress");
            args.Add("pipe:1");
            args.Add("-i");
            args.Add(inputPath);

            if (selectedEngine == EngineChoice.Cpu)
            {
                if (selectedCodec == CodecChoice.H264)
                {
                    args.AddRange(new[] { "-c:v", "libx264", "-preset", "medium", "-crf", GetH264Crf().ToString(CultureInfo.InvariantCulture), "-threads", "0", "-pix_fmt", "yuv420p" });
                }
                else
                {
                    args.AddRange(new[] { "-c:v", "libx265", "-preset", "medium", "-crf", GetH265Crf().ToString(CultureInfo.InvariantCulture), "-x265-params", "aq-mode=2:ref=3:bframes=3:rc-lookahead=15", "-threads", "0", "-tag:v", "hvc1", "-pix_fmt", "yuv420p" });
                }
            }
            else
            {
                if (selectedCodec == CodecChoice.H264)
                {
                    args.AddRange(new[] { "-c:v", "h264_nvenc", "-preset", "p5", "-cq", GetGpuCq().ToString(CultureInfo.InvariantCulture), "-b:v", "0", "-pix_fmt", "yuv420p" });
                }
                else
                {
                    args.AddRange(new[] { "-c:v", "hevc_nvenc", "-preset", "p5", "-cq", GetGpuCq().ToString(CultureInfo.InvariantCulture), "-b:v", "0", "-tag:v", "hvc1", "-pix_fmt", "yuv420p" });
                }
            }

            args.AddRange(new[] { "-c:a", "aac", "-b:a", "128k", "-movflags", "+faststart", outputPath });
            return JoinArgs(args);
        }

        private int GetH264Crf()
        {
            if (selectedQuality == QualityChoice.High)
            {
                return 21;
            }
            return selectedQuality == QualityChoice.Small ? 29 : 25;
        }

        private int GetH265Crf()
        {
            if (selectedQuality == QualityChoice.High)
            {
                return 26;
            }
            return selectedQuality == QualityChoice.Small ? 34 : 30;
        }

        private int GetGpuCq()
        {
            if (selectedQuality == QualityChoice.High)
            {
                return 28;
            }
            return selectedQuality == QualityChoice.Small ? 36 : 32;
        }

        private string GetGpuEncoder()
        {
            return selectedCodec == CodecChoice.H264 ? "h264_nvenc" : "hevc_nvenc";
        }

        private string CreateOutputPath(string inputPath)
        {
            string folder = String.IsNullOrWhiteSpace(customOutputDirectory)
                ? Path.GetDirectoryName(inputPath)
                : customOutputDirectory;
            if (String.IsNullOrWhiteSpace(folder))
            {
                throw new IOException("无法确定输出目录。");
            }
            Directory.CreateDirectory(folder);

            string name = Path.GetFileNameWithoutExtension(inputPath);
            string suffix = selectedCodec == CodecChoice.H264 ? "_h264" : "_h265";
            string candidate = Path.Combine(folder, name + suffix + ".mp4");

            if (!File.Exists(candidate))
            {
                return candidate;
            }

            for (int i = 1; i < 1000; i++)
            {
                string numbered = Path.Combine(folder, name + suffix + "_" + i.ToString(CultureInfo.InvariantCulture) + ".mp4");
                if (!File.Exists(numbered))
                {
                    return numbered;
                }
            }

            throw new IOException("无法生成唯一输出文件名。");
        }

        private void FilesDragEnter(object sender, DragEventArgs e)
        {
            if (!isRunning && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void FilesDragDrop(object sender, DragEventArgs e)
        {
            if (isRunning || !e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            string[] dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddFiles(dropped);
        }

        private void AddFiles(IEnumerable<string> paths)
        {
            int added = 0;
            foreach (string path in ExpandVideoPaths(paths))
            {
                if (jobs.Any(j => String.Equals(j.InputPath, path, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                jobs.Add(new VideoJob
                {
                    InputPath = path,
                    OutputPath = "",
                    Status = "待执行",
                    Progress = 0,
                    DurationSeconds = 0
                });
                added++;
            }

            RefreshJobGrid();
            queueFinished = false;
            executeButton.Text = "执行";
            if (added == 0)
            {
                SetHint("没有识别到可压缩的视频文件。支持 mp4、mov、mkv、avi、webm、m4v。");
            }
            else
            {
                SetHint("");
            }
        }

        private IEnumerable<string> ExpandVideoPaths(IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                if (File.Exists(path) && IsSupportedVideo(path))
                {
                    yield return Path.GetFullPath(path);
                }
                else if (Directory.Exists(path))
                {
                    foreach (string file in Directory.GetFiles(path).Where(IsSupportedVideo))
                    {
                        yield return Path.GetFullPath(file);
                    }
                }
            }
        }

        private static bool IsSupportedVideo(string path)
        {
            string extension = Path.GetExtension(path);
            return SupportedExtensions.Any(x => String.Equals(x, extension, StringComparison.OrdinalIgnoreCase));
        }

        private void RefreshJobGrid()
        {
            jobGrid.Rows.Clear();
            foreach (VideoJob job in jobs)
            {
                jobGrid.Rows.Add(Path.GetFileName(job.InputPath), StatusText(job), ProgressText(job), job.OutputPath);
            }

            bool hasJobs = jobs.Count > 0;
            jobGrid.Visible = hasJobs;
            hintLabel.Visible = !hasJobs;
        }

        private void UpdateJobRow(VideoJob job)
        {
            Ui(delegate
            {
                int index = jobs.IndexOf(job);
                if (index < 0 || index >= jobGrid.Rows.Count)
                {
                    return;
                }

                DataGridViewRow row = jobGrid.Rows[index];
                row.Cells[0].Value = Path.GetFileName(job.InputPath);
                row.Cells[1].Value = StatusText(job);
                row.Cells[2].Value = ProgressText(job);
                row.Cells[3].Value = String.IsNullOrEmpty(job.OutputPath) ? "" : job.OutputPath;

                if (job.Status == "失败")
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(170, 40, 40);
                }
                else if (job.Status == "完成")
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(40, 120, 70);
                }
                else
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(50, 50, 50);
                }
            });
        }

        private static string StatusText(VideoJob job)
        {
            if (job.Status == "失败" && !String.IsNullOrWhiteSpace(job.ErrorMessage))
            {
                return "失败";
            }
            return job.Status;
        }

        private static string ProgressText(VideoJob job)
        {
            if (job.Status == "待执行")
            {
                return "待执行";
            }

            if (job.Status == "失败")
            {
                return "失败";
            }

            return Math.Max(0, Math.Min(100, job.Progress)).ToString(CultureInfo.InvariantCulture) + "%";
        }

        private void ClearJobs()
        {
            jobs.Clear();
            RefreshJobGrid();
            queueFinished = false;
            executeButton.Text = "执行";
            statusLabel.Text = OutputLocationText();
            SetHint(DefaultHintText());
        }

        private void SetHint(string text)
        {
            if (!String.IsNullOrEmpty(text) && jobs.Count > 0)
            {
                hintLabel.Visible = false;
                jobGrid.Visible = true;
                statusLabel.Text = text;
                return;
            }

            if (String.IsNullOrEmpty(text) && jobs.Count > 0)
            {
                hintLabel.Visible = false;
                jobGrid.Visible = true;
                return;
            }

            hintLabel.Text = text;
            hintLabel.Visible = true;
            jobGrid.Visible = jobs.Count > 0 && String.IsNullOrEmpty(text);
        }

        private void Ui(Action action)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(action);
            }
            else
            {
                action();
            }
        }

        private T UiCall<T>(Func<T> action)
        {
            if (IsDisposed)
            {
                return default(T);
            }

            if (InvokeRequired)
            {
                return (T)Invoke(action);
            }

            return action();
        }

        private static Process StartProcess(string fileName, string arguments, bool redirectErrorOnly)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = !redirectErrorOnly,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            Process process = new Process { StartInfo = psi };
            process.Start();
            if (redirectErrorOnly)
            {
                process.BeginErrorReadLine();
            }
            return process;
        }

        private static string RunProcessCapture(string fileName, string arguments, int timeoutMs)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                if (!process.WaitForExit(timeoutMs))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }
                    throw new TimeoutException("命令执行超时。");
                }

                return output + "\n" + error;
            }
        }

        private static string JoinArgs(IEnumerable<string> args)
        {
            return String.Join(" ", args.Select(QuoteArg).ToArray());
        }

        private static string QuoteArg(string arg)
        {
            if (arg == null)
            {
                return "\"\"";
            }

            if (arg.Length > 0 && arg.IndexOfAny(new[] { ' ', '\t', '\r', '\n', '"' }) < 0)
            {
                return arg;
            }

            return "\"" + arg.Replace("\"", "\\\"") + "\"";
        }

        private static double ParseOutTimeSeconds(string value)
        {
            string[] parts = value.Trim().Split(':');
            if (parts.Length != 3)
            {
                return -1;
            }

            int hours;
            int minutes;
            double seconds;
            if (!Int32.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out hours) ||
                !Int32.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes) ||
                !Double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out seconds))
            {
                return -1;
            }

            return hours * 3600 + minutes * 60 + seconds;
        }
    }

    internal class RoundedPanel : Panel
    {
        public int Radius { get; set; }
        public int BorderWidth { get; set; }
        public Color BorderColor { get; set; }
        public bool DrawBorder { get; set; }

        public RoundedPanel()
        {
            Radius = 18;
            BorderWidth = 2;
            BorderColor = Color.Black;
            DrawBorder = true;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!DrawBorder || BorderWidth <= 0)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(BorderWidth / 2, BorderWidth / 2, Width - BorderWidth - 1, Height - BorderWidth - 1);
            using (GraphicsPath path = RoundedRectangle(rect, Radius))
            using (Pen pen = new Pen(BorderColor, BorderWidth))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        protected static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            if (radius <= 0)
            {
                GraphicsPath square = new GraphicsPath();
                square.AddRectangle(bounds);
                square.CloseFigure();
                return square;
            }

            int diameter = radius * 2;
            diameter = Math.Min(diameter, Math.Min(bounds.Width, bounds.Height));
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class DashedPanel : RoundedPanel
    {
        public DashedPanel()
        {
            DrawBorder = false;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? Color.White : Parent.BackColor);
            if (BorderWidth <= 0)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            int inset = Math.Max(2, BorderWidth);
            Rectangle rect = new Rectangle(inset, inset, Width - inset * 2 - 1, Height - inset * 2 - 1);
            using (GraphicsPath path = RoundedRectangle(rect, Radius))
            using (SolidBrush brush = new SolidBrush(BackColor))
            using (Pen pen = new Pen(BorderColor, BorderWidth))
            {
                e.Graphics.FillPath(brush, path);
                pen.DashStyle = DashStyle.Dash;
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                e.Graphics.DrawPath(pen, path);
            }
        }
    }

    internal sealed class RoundedButton : Control
    {
        public int Radius { get; set; }
        public int BorderWidth { get; set; }
        public Color BorderColor { get; set; }
        private bool isMouseDown;

        public RoundedButton()
        {
            Radius = 15;
            BorderWidth = 3;
            BorderColor = Color.Black;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            TabStop = false;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            Invalidate();
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (isMouseDown)
            {
                isMouseDown = false;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (isMouseDown)
            {
                isMouseDown = false;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? Color.White : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            int inset = Math.Max(2, BorderWidth);
            Rectangle rect = new Rectangle(inset, inset, Width - inset * 2 - 1, Height - inset * 2 - 1);
            Color fill = Enabled ? BackColor : Color.FromArgb(225, 225, 225);
            if (Enabled && isMouseDown)
            {
                fill = ControlPaint.Dark(fill, 0.04f);
            }

            Color text = Enabled ? ForeColor : Color.FromArgb(150, 150, 150);

            using (GraphicsPath path = RoundedRectangle(rect, Radius))
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(BorderColor, BorderWidth))
            {
                e.Graphics.FillPath(brush, path);
                pen.LineJoin = LineJoin.Round;
                e.Graphics.DrawPath(pen, path);
            }

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                rect,
                text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            GraphicsPath path = new GraphicsPath();
            if (diameter <= 0)
            {
                path.AddRectangle(bounds);
                path.CloseFigure();
                return path;
            }

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
