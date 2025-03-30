using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdvancedCodeAnalyzer
{
    /// ������� ����� ���������� ��� ������� ���������� ������
    public partial class MainForm : Form
    {
        // ������ ��� ������ ����������� ��������
        private CancellationTokenSource _cancellationTokenSource;
        // ����, ������������ ������� �� ����� �����������
        private bool _isMonitoring = false;
        // ���� � ���������� ������������ �����
        private string _selectedExecutablePath = string.Empty;
        // ������������� �������
        private Process _monitoredProcess = null;
        // ������ ������� ��������� ��������
        private List<ResourceSnapshot> _resourceSnapshots = new List<ResourceSnapshot>();
        // ����� ��� �������� ����������� ������������ �������
        private StringBuilder _staticAnalysisOutput = new StringBuilder();

        public MainForm()
        {
            InitializeComponent();
            this.FormClosing += MainForm_FormClosing;
        }

        private void InitializeComponent()
        {
            this.btnSelectFile = new Button();
            this.btnStartAnalysis = new Button();
            this.btnStopAnalysis = new Button();
            this.btnSaveReport = new Button();
            this.txtFilePath = new TextBox();
            this.lblStatus = new Label();
            this.tabControl = new TabControl();
            this.tabDynamic = new TabPage();
            this.tabStatic = new TabPage();
            this.tabDisassembly = new TabPage();
            this.lblCpu = new Label();
            this.lblMemory = new Label();
            this.lblDiskIO = new Label();
            this.lblFileSize = new Label();
            this.lblFileType = new Label();
            this.lblDependencies = new Label();
            this.lblStaticAnalysis = new Label();
            this.rtbResults = new RichTextBox();
            this.rtbDisassembly = new RichTextBox();
            this.btnAnalyzeDeeper = new Button();
            this.progressBar = new ProgressBar();

            // Form
            this.Text = "��������� ���������� ��������";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Controls
            this.btnSelectFile = new Button();
            this.btnSelectFile.Text = "������� ����";
            this.btnSelectFile.Location = new Point(15, 15);
            this.btnSelectFile.Size = new Size(100, 30);
            this.btnSelectFile.Click += new EventHandler(btnSelectFile_Click);

            this.txtFilePath = new TextBox();
            this.txtFilePath.Location = new Point(125, 15);
            this.txtFilePath.Size = new Size(650, 30);
            this.txtFilePath.ReadOnly = true;

            this.btnStartAnalysis = new Button();
            this.btnStartAnalysis.Text = "������ ������";
            this.btnStartAnalysis.Location = new Point(15, 55);
            this.btnStartAnalysis.Size = new Size(120, 30);
            this.btnStartAnalysis.Click += new EventHandler(btnStartAnalysis_Click);
            this.btnStartAnalysis.Enabled = false;

            this.btnStopAnalysis = new Button();
            this.btnStopAnalysis.Text = "����������";
            this.btnStopAnalysis.Location = new Point(145, 55);
            this.btnStopAnalysis.Size = new Size(120, 30);
            this.btnStopAnalysis.Click += new EventHandler(btnStopAnalysis_Click);
            this.btnStopAnalysis.Enabled = false;

            this.btnSaveReport = new Button();
            this.btnSaveReport.Text = "��������� �����";
            this.btnSaveReport.Location = new Point(275, 55);
            this.btnSaveReport.Size = new Size(120, 30);
            this.btnSaveReport.Click += new EventHandler(btnSaveReport_Click);
            this.btnSaveReport.Enabled = false;

            this.btnAnalyzeDeeper = new Button();
            this.btnAnalyzeDeeper.Text = "�������� ������ ����";
            this.btnAnalyzeDeeper.Location = new Point(405, 55);
            this.btnAnalyzeDeeper.Size = new Size(150, 30);
            this.btnAnalyzeDeeper.Click += new EventHandler(btnAnalyzeDeeper_Click);
            this.btnAnalyzeDeeper.Enabled = false;

            this.lblStatus = new Label();
            this.lblStatus.Text = "����� � ������";
            this.lblStatus.Location = new Point(15, 95);
            this.lblStatus.AutoSize = true;

            this.progressBar = new ProgressBar();
            this.progressBar.Location = new Point(205, 95);
            this.progressBar.Size = new Size(570, 23);
            this.progressBar.Visible = false;

            this.tabControl = new TabControl();
            this.tabControl.Location = new Point(15, 130);
            this.tabControl.Size = new Size(760, 420);

            this.tabDynamic = new TabPage();
            this.tabDynamic.Text = "������������ ������";
            this.lblCpu = new Label();
            this.lblCpu.Text = "������������� CPU: -";
            this.lblCpu.Location = new Point(10, 20);
            this.lblCpu.AutoSize = true;
            this.lblMemory = new Label();
            this.lblMemory.Text = "������������� RAM: -";
            this.lblMemory.Location = new Point(10, 50);
            this.lblMemory.AutoSize = true;
            this.lblDiskIO = new Label();
            this.lblDiskIO.Text = "�������� ��������: -";
            this.lblDiskIO.Location = new Point(10, 80);
            this.lblDiskIO.AutoSize = true;
            this.tabDynamic.Controls.Add(this.lblCpu);
            this.tabDynamic.Controls.Add(this.lblMemory);
            this.tabDynamic.Controls.Add(this.lblDiskIO);

            this.tabStatic = new TabPage();
            this.tabStatic.Text = "����������� ������";
            this.lblFileSize = new Label();
            this.lblFileSize.Text = "������ �����: -";
            this.lblFileSize.Location = new Point(10, 20);
            this.lblFileSize.AutoSize = true;
            this.lblFileType = new Label();
            this.lblFileType.Text = "��� �����: -";
            this.lblFileType.Location = new Point(10, 50);
            this.lblFileType.AutoSize = true;
            this.lblDependencies = new Label();
            this.lblDependencies.Text = "�����������: -";
            this.lblDependencies.Location = new Point(10, 80);
            this.lblDependencies.AutoSize = true;
            this.lblStaticAnalysis = new Label();
            this.lblStaticAnalysis.Text = "���������� ������������ �������:";
            this.lblStaticAnalysis.Location = new Point(10, 110);
            this.lblStaticAnalysis.AutoSize = true;
            this.rtbResults = new RichTextBox();
            this.rtbResults.Location = new Point(10, 130);
            this.rtbResults.Size = new Size(735, 250);
            this.rtbResults.ReadOnly = true;
            this.rtbResults.Font = new Font("Consolas", 9.0f);
            this.tabStatic.Controls.Add(this.lblFileSize);
            this.tabStatic.Controls.Add(this.lblFileType);
            this.tabStatic.Controls.Add(this.lblDependencies);
            this.tabStatic.Controls.Add(this.lblStaticAnalysis);
            this.tabStatic.Controls.Add(this.rtbResults);

            this.tabDisassembly = new TabPage();
            this.tabDisassembly.Text = "������������������� ���";
            this.rtbDisassembly = new RichTextBox();
            this.rtbDisassembly.Location = new Point(10, 10);
            this.rtbDisassembly.Size = new Size(735, 370);
            this.rtbDisassembly.ReadOnly = true;
            this.rtbDisassembly.Font = new Font("Consolas", 9.0f);
            this.rtbDisassembly.WordWrap = false;
            this.tabDisassembly.Controls.Add(this.rtbDisassembly);

            this.tabControl.Controls.Add(this.tabStatic);
            this.tabControl.Controls.Add(this.tabDynamic);
            this.tabControl.Controls.Add(this.tabDisassembly);

            // Form controls
            this.Controls.Add(this.btnSelectFile);
            this.Controls.Add(this.txtFilePath);
            this.Controls.Add(this.btnStartAnalysis);
            this.Controls.Add(this.btnStopAnalysis);
            this.Controls.Add(this.btnSaveReport);
            this.Controls.Add(this.btnAnalyzeDeeper);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.tabControl);
        }

        /// ���������� ������� ������ ������ �����
        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                // ����������� ������ ������ �����
                openFileDialog.Filter = "����������� ����� (*.exe;*.dll)|*.exe;*.dll|��� ����� (*.*)|*.*";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedExecutablePath = openFileDialog.FileName;
                    txtFilePath.Text = _selectedExecutablePath;
                    btnStartAnalysis.Enabled = true;
                    btnAnalyzeDeeper.Enabled = true;

                    // ��������� ������� ����������� ������ ��� ������ �����
                    PerformBasicStaticAnalysis(_selectedExecutablePath);
                }
            }
        }

        /// ���������� ������� ������ ������� �������
        private void btnStartAnalysis_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedExecutablePath))
            {
                MessageBox.Show("����������, �������� ����������� ���� ��� �������.", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // ������������� ��� ������ �������
                _cancellationTokenSource = new CancellationTokenSource();
                _resourceSnapshots.Clear();
                _isMonitoring = true;

                // ��������� ��������� UI
                btnStartAnalysis.Enabled = false;
                btnStopAnalysis.Enabled = true;
                btnSelectFile.Enabled = false;
                btnAnalyzeDeeper.Enabled = false;
                lblStatus.Text = "������ �������...";

                // ��������� ������� ��� �����������
                _monitoredProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _selectedExecutablePath,
                        UseShellExecute = true
                    }
                };
                _monitoredProcess.Start();

                // ��������� ���������� � ��������� ������
                Task.Run(() => MonitorResourcesAsync(_monitoredProcess, _cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"������ ��� ������� ��������: {ex.Message}", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUI();
            }
        }

        private void btnStopAnalysis_Click(object sender, EventArgs e)
        {
            StopMonitoring();
        }

        private void btnSaveReport_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.DefaultExt = "txt";
                saveFileDialog.FileName = "������������������������.txt";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    SaveReport(saveFileDialog.FileName);
                    MessageBox.Show("����� ������� ��������!", "����������", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// ���������� ��������� ������� ������������ ����
        private void btnAnalyzeDeeper_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedExecutablePath))
            {
                MessageBox.Show("����������, �������� ����������� ���� ��� �������.", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnAnalyzeDeeper.Enabled = false;
            lblStatus.Text = "����������� �������� ������ ����...";
            progressBar.Visible = true;
            progressBar.Value = 0;

            // ��������� ������ � ������� ������
            Task.Run(() =>
            {
                try
                {
                    // ��������� �������� ������ ����
                    PerformDeepCodeAnalysis(_selectedExecutablePath);

                    // ��������� UI ����� ����������
                    BeginInvoke(new Action(() =>
                    {
                        lblStatus.Text = "�������� ������ ��������";
                        progressBar.Visible = false;
                        btnAnalyzeDeeper.Enabled = true;
                        tabControl.SelectedTab = tabDisassembly;
                    }));
                }
                catch (Exception ex)
                {
                    // ������������ ������
                    BeginInvoke(new Action(() =>
                    {
                        lblStatus.Text = "������ ��� ���������� ��������� �������";
                        progressBar.Visible = false;
                        btnAnalyzeDeeper.Enabled = true;
                        MessageBox.Show($"������ ��� �������: {ex.Message}", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            });
        }

        /// ����������� ���������� ������������� �������� ���������
        private async Task MonitorResourcesAsync(Process process, CancellationToken cancellationToken)
        {
            try
            {
                int processId = process.Id;
                DateTime startTime = DateTime.Now;

                // ���� �����������, ����������� �� ������ ��� ���������� ��������
                while (!cancellationToken.IsCancellationRequested && !process.HasExited)
                {
                    // �������� ��� ����� ������
                    await Task.Delay(1000, cancellationToken);

                    if (process.HasExited)
                        break;

                    try
                    {
                        process.Refresh();

                        // ��������� �������� CPU ����� WMI
                        double cpuUsage = GetCpuUsageForProcess(processId);

                        // ��������� ������������� ������
                        double memoryUsageMB = process.WorkingSet64 / (1024 * 1024.0);

                        // ���������� � �������� ���������
                        double diskReadKBs = 0;
                        double diskWriteKBs = 0;

                        // ������� �������� ���������� I/O ����� WMI
                        var ioStats = GetIOStatsForProcess(processId);
                        if (ioStats != null)
                        {
                            diskReadKBs = ioStats.Item1 / 1024.0;
                            diskWriteKBs = ioStats.Item2 / 1024.0;
                        }

                        // �������� ������ ��������
                        var snapshot = new ResourceSnapshot
                        {
                            Timestamp = DateTime.Now,
                            CpuUsage = cpuUsage,
                            MemoryUsageMB = memoryUsageMB,
                            DiskReadKBs = diskReadKBs,
                            DiskWriteKBs = diskWriteKBs
                        };

                        _resourceSnapshots.Add(snapshot);

                        // ���������� UI � �������� ������
                        BeginInvoke(new Action(() =>
                        {
                            lblCpu.Text = $"������������� CPU: {cpuUsage:F2}%";
                            lblMemory.Text = $"������������� RAM: {memoryUsageMB:F2} ��";
                            lblDiskIO.Text = $"�������� ��������: ������ {diskReadKBs:F2} ��/�, ������ {diskWriteKBs:F2} ��/�";
                        }));
                    }
                    catch (Exception ex)
                    {
                        // ������� ��� ����������� �� ����� ����� ������
                        if (!process.HasExited)
                        {
                            BeginInvoke(new Action(() =>
                            {
                                lblStatus.Text = $"������ ��� ����� ������: {ex.Message}";
                            }));
                        }
                        break;
                    }
                }

                // Process completed or monitoring stopped
                BeginInvoke(new Action(() =>
                {
                    lblStatus.Text = "������ ��������";
                    btnSaveReport.Enabled = true;
                    btnStartAnalysis.Enabled = true;
                    btnStopAnalysis.Enabled = false;
                    btnSelectFile.Enabled = true;
                    btnAnalyzeDeeper.Enabled = true;
                    _isMonitoring = false;

                    // Show summary
                    if (_resourceSnapshots.Count > 0)
                    {
                        double avgCpu = _resourceSnapshots.Average(s => s.CpuUsage);
                        double maxCpu = _resourceSnapshots.Max(s => s.CpuUsage);
                        double avgMem = _resourceSnapshots.Average(s => s.MemoryUsageMB);
                        double maxMem = _resourceSnapshots.Max(s => s.MemoryUsageMB);
                        double avgDiskRead = _resourceSnapshots.Average(s => s.DiskReadKBs);
                        double avgDiskWrite = _resourceSnapshots.Average(s => s.DiskWriteKBs);

                        lblCpu.Text = $"������� ������������� CPU: {avgCpu:F2}%, ����: {maxCpu:F2}%";
                        lblMemory.Text = $"������� ������������� RAM: {avgMem:F2} ��, ����: {maxMem:F2} ��";
                        lblDiskIO.Text = $"������� �������� ��������: ������ {avgDiskRead:F2} ��/�, ������ {avgDiskWrite:F2} ��/�";
                    }
                }));
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() =>
                {
                    lblStatus.Text = $"������ �����������: {ex.Message}";
                    ResetUI();
                }));
            }
        }

        /// ��������� �������� CPU ��� �������� ����� WMI
        private double GetCpuUsageForProcess(int processId)
        {
            try
            {
                // ���������� WMI ��� ��������� ������������� CPU
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT PercentProcessorTime FROM Win32_PerfFormattedData_PerfProc_Process WHERE IDProcess = " + processId))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return Convert.ToDouble(obj["PercentProcessorTime"]);
                    }
                }
            }
            catch
            {
                // ��������� �����, ���� WMI �� ��������
                try
                {
                    Process p = Process.GetProcessById(processId);
                    return p.TotalProcessorTime.TotalMilliseconds /
                           (Environment.ProcessorCount * 10.0 * (DateTime.Now - p.StartTime).TotalMilliseconds);
                }
                catch
                {
                    // ���� ��� ������ �� ���������, ���������� 0
                }
            }
            return 0;
        }

        /// ��������� ���������� �������� �������� ��� �������� ����� WMI
        /// ���������� ������ (������, ������) � ������/��� ��� null ���� �� ������� �������� ������
        private Tuple<double, double> GetIOStatsForProcess(int processId)
        {
            try
            {
                // ������ WMI ��� ��������� ������ � �������� ���������
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT ReadTransferCount, WriteTransferCount FROM Win32_PerfFormattedData_PerfProc_Process WHERE IDProcess = " + processId))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        double readBytes = Convert.ToDouble(obj["ReadTransferCount"]);
                        double writeBytes = Convert.ToDouble(obj["WriteTransferCount"]);
                        return new Tuple<double, double>(readBytes, writeBytes);
                    }
                }
            }
            catch
            {
                // ���� WMI �� ��������, ���������� null
            }
            return null;
        }

        private void StopMonitoring()
        {
            if (_isMonitoring)
            {
                _cancellationTokenSource?.Cancel();

                if (_monitoredProcess != null && !_monitoredProcess.HasExited)
                {
                    try
                    {
                        _monitoredProcess.CloseMainWindow();
                        if (!_monitoredProcess.WaitForExit(3000))
                        {
                            _monitoredProcess.Kill();
                        }
                    }
                    catch { /* Ignore errors when trying to kill process */ }
                }

                _isMonitoring = false;
                btnSaveReport.Enabled = true;
            }
        }

        /// ���������� �������� ������������ ������� �����
        private void PerformBasicStaticAnalysis(string filePath)
        {
            try
            {
                _staticAnalysisOutput.Clear();
                FileInfo fileInfo = new FileInfo(filePath);

                // ��������� ������� ���������� � �����
                long fileSizeBytes = fileInfo.Length;
                string fileExtension = fileInfo.Extension.ToLower();

                // ����� ������� ����������
                _staticAnalysisOutput.AppendLine($"=== ������� ���������� � ����� ===");
                _staticAnalysisOutput.AppendLine($"������ ����: {filePath}");
                _staticAnalysisOutput.AppendLine($"������: {FormatFileSize(fileSizeBytes)}");
                _staticAnalysisOutput.AppendLine($"��� �����: {fileExtension}");
                _staticAnalysisOutput.AppendLine($"���� ��������: {fileInfo.CreationTime}");
                _staticAnalysisOutput.AppendLine($"���� ���������� ���������: {fileInfo.LastWriteTime}");
                _staticAnalysisOutput.AppendLine();

                // ���������� ������� PE-��������� ��� exe � dll ������
                if (fileExtension == ".exe" || fileExtension == ".dll")
                {
                    AnalyzePEFile(filePath);
                }
                else
                {
                    _staticAnalysisOutput.AppendLine("��������� ������ �������� ������ ��� .exe � .dll ������.");
                }

                // ���������� ���������� ������������
                BeginInvoke(new Action(() =>
                {
                    lblFileSize.Text = $"������ �����: {FormatFileSize(fileSizeBytes)}";
                    lblFileType.Text = $"��� �����: {fileExtension.ToUpper()}";
                    rtbResults.Text = _staticAnalysisOutput.ToString();
                }));
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() =>
                {
                    _staticAnalysisOutput.AppendLine($"������ ��� ���������� ������������ �������: {ex.Message}");
                    rtbResults.Text = _staticAnalysisOutput.ToString();
                }));
            }
        }

        /// ������ PE (Portable Executable) �����
        private void AnalyzePEFile(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    // ������ ������ 4KB ����� ��� ������� ����������
                    byte[] buffer = new byte[4096]; // ����� ��� ���������� ��� DOS � PE ����������

                    // ������ ������ �����
                    fs.Read(buffer, 0, buffer.Length);

                    // �������� ��������� MZ (DOS-���������)
                    if (buffer[0] != 'M' || buffer[1] != 'Z')
                    {
                        _staticAnalysisOutput.AppendLine("���� �� �������� ���������� PE-������ (����������� ��������� MZ).");
                        return;
                    }

                    // ��������� �������� PE-���������
                    int peOffset = BitConverter.ToInt32(buffer, 0x3C);

                    // �������� ��������� PE
                    if (peOffset >= buffer.Length - 4 ||
                        buffer[peOffset] != 'P' ||
                        buffer[peOffset + 1] != 'E' ||
                        buffer[peOffset + 2] != 0 ||
                        buffer[peOffset + 3] != 0)
                    {
                        _staticAnalysisOutput.AppendLine("���� �� �������� ���������� PE-������ (����������� ��������� PE).");
                        return;
                    }

                    // ��������� ���� ������ (�����������)
                    ushort machineType = BitConverter.ToUInt16(buffer, peOffset + 4);
                    string architecture;
                    switch (machineType)
                    {
                        case 0x014c: architecture = "x86 (32-bit)"; break;
                        case 0x8664: architecture = "x64 (64-bit)"; break;
                        case 0x0200: architecture = "Intel Itanium"; break;
                        case 0x01c4: architecture = "ARM"; break;
                        case 0xAA64: architecture = "ARM64"; break;
                        default: architecture = $"������ ({machineType:X4})"; break;
                    }

                    // ��������� ���������� ������
                    ushort numSections = BitConverter.ToUInt16(buffer, peOffset + 6);

                    // ��������� ����� ������� (����� ����������)
                    uint timestamp = BitConverter.ToUInt32(buffer, peOffset + 8);
                    DateTime compileTime = new DateTime(1970, 1, 1).AddSeconds(timestamp);

                    // ��������� ������������� �����
                    ushort characteristics = BitConverter.ToUInt16(buffer, peOffset + 22);
                    bool isDll = (characteristics & 0x2000) != 0;
                    bool isExecutableFile = (characteristics & 0x0002) != 0;
                    bool isSystem = (characteristics & 0x1000) != 0;

                    
                    ushort optionalHeaderSize = BitConverter.ToUInt16(buffer, peOffset + 20);
                    ushort optionalHeaderMagic = BitConverter.ToUInt16(buffer, peOffset + 24);
                    bool isPE32Plus = optionalHeaderMagic == 0x20b; // PE32+ (64-bit)

                    // ����� ���������� � ��������� PE
                    _staticAnalysisOutput.AppendLine($"=== ������ PE-��������� ===");
                    _staticAnalysisOutput.AppendLine($"�����������: {architecture}");
                    _staticAnalysisOutput.AppendLine($"���������� ������: {numSections}");
                    _staticAnalysisOutput.AppendLine($"����� ����������: {compileTime}");
                    _staticAnalysisOutput.AppendLine($"��� �����: {(isDll ? "������������ ���������� (DLL)" : "����������� ���� (EXE)")}");
                    _staticAnalysisOutput.AppendLine($"��������� ����: {(isSystem ? "��" : "���")}");
                    _staticAnalysisOutput.AppendLine($"����������� ����: {(isExecutableFile ? "��" : "���")}");
                    _staticAnalysisOutput.AppendLine($"PE32+: {(isPE32Plus ? "�� (64-bit)" : "��� (32-bit)")}");
                    _staticAnalysisOutput.AppendLine();

                    // ���������� �������� � ������� ������
                    int sectionTableOffset = peOffset + 24 + optionalHeaderSize;

                    // �������� ����������
                    ushort subsystem = 0;
                    if (optionalHeaderSize >= 68) // PE32
                    {
                        subsystem = BitConverter.ToUInt16(buffer, peOffset + 24 + 68);
                    }

                    string subsystemName = "����������";
                    switch (subsystem)
                    {
                        case 1: subsystemName = "������� ����������"; break;
                        case 2: subsystemName = "Windows GUI"; break;
                        case 3: subsystemName = "Windows CUI (�������)"; break;
                        case 5: subsystemName = "OS/2 CUI"; break;
                        case 7: subsystemName = "POSIX CUI"; break;
                        case 8: subsystemName = "�������� ����"; break;
                        case 11: subsystemName = "Windows CE GUI"; break;
                        case 14: subsystemName = "Xbox"; break;
                    }
                    _staticAnalysisOutput.AppendLine($"����������: {subsystemName} ({subsystem})");

                    // ���������� �������� ���������� � �������� �������
                    try
                    {
                        int importDirOffset;
                        if (isPE32Plus)
                        {
                            // � PE32+ (64-���) ������� ������� ��������� �� ������� ��������
                            importDirOffset = peOffset + 24 + 112;
                        }
                        else
                        {
                            // � PE32 (32-���) ������� ������� ��������� �� �������� 96
                            importDirOffset = peOffset + 24 + 96;
                        }

                        // ������� ������� - ��� ������ ������� �������� ������
                        uint importDirRVA = BitConverter.ToUInt32(buffer, importDirOffset);
                        uint importDirSize = BitConverter.ToUInt32(buffer, importDirOffset + 4);

                        if (importDirRVA != 0 && importDirSize != 0)
                        {
                            _staticAnalysisOutput.AppendLine($"������: RVA=0x{importDirRVA:X8}, ������={importDirSize}");
                        }
                    }
                    catch
                    {
                        // ���������� ������ ������� �������� �������
                    }

                    // Read sections
                    _staticAnalysisOutput.AppendLine();
                    _staticAnalysisOutput.AppendLine("=== ������ ===");

                    int codeSize = 0;
                    int dataSize = 0;
                    int resourceSize = 0;

                    for (int i = 0; i < numSections && sectionTableOffset + i * 40 + 40 <= buffer.Length; i++)
                    {
                        int sectionOffset = sectionTableOffset + i * 40;

                        // Read section name (8 bytes)
                        string sectionName = Encoding.ASCII.GetString(buffer, sectionOffset, 8).TrimEnd('\0');

                        // Read section size
                        uint virtualSize = BitConverter.ToUInt32(buffer, sectionOffset + 8);
                        uint virtualAddress = BitConverter.ToUInt32(buffer, sectionOffset + 12);
                        uint rawDataSize = BitConverter.ToUInt32(buffer, sectionOffset + 16);

                        // Read section characteristics
                        uint characteristics2 = BitConverter.ToUInt32(buffer, sectionOffset + 36);
                        bool isCode = (characteristics2 & 0x00000020) != 0;
                        bool isInitializedData = (characteristics2 & 0x00000040) != 0;
                        bool isUninitializedData = (characteristics2 & 0x00000080) != 0;
                        bool isReadable = (characteristics2 & 0x40000000) != 0;
                        bool isWritable = (characteristics2 & 0x80000000) != 0;
                        bool isSectionExecutable = (characteristics2 & 0x20000000) != 0;

                        // Track section sizes
                        if (isCode) codeSize += (int)virtualSize;
                        if (isInitializedData || isUninitializedData) dataSize += (int)virtualSize;
                        if (sectionName == ".rsrc") resourceSize = (int)virtualSize;

                        // ���������� � �������� ������
                        _staticAnalysisOutput.AppendLine($"������: {sectionName}");
                        _staticAnalysisOutput.AppendLine($"  ����������� ������: {FormatFileSize(virtualSize)}");
                        _staticAnalysisOutput.AppendLine($"  �����: 0x{virtualAddress:X8}");
                        _staticAnalysisOutput.AppendLine($"  ������ ������: {FormatFileSize(rawDataSize)}");
                        _staticAnalysisOutput.AppendLine($"  ���: {(isCode ? "���" : "")} {(isInitializedData ? "������������������ ������" : "")} {(isUninitializedData ? "�������������������� ������" : "")}");
                        _staticAnalysisOutput.AppendLine($"  �����: {(isReadable ? "R" : "-")}{(isWritable ? "W" : "-")}{(isSectionExecutable ? "X" : "-")}");
                        _staticAnalysisOutput.AppendLine();
                    }

                    // ����� ������ ������������� ��������
                    _staticAnalysisOutput.AppendLine("=== ������ �������� ===");
                    _staticAnalysisOutput.AppendLine($"������ ����: {FormatFileSize(codeSize)}");
                    _staticAnalysisOutput.AppendLine($"������ ������: {FormatFileSize(dataSize)}");
                    if (resourceSize > 0)
                    {
                        _staticAnalysisOutput.AppendLine($"������ ��������: {FormatFileSize(resourceSize)}");
                    }

                    // ������� ����� ��������� ��� ����������� ������������ � ��������
                    int totalSize = codeSize + dataSize + resourceSize;
                    string cpuEstimate = "������";
                    string ramEstimate = "������";

                    if (totalSize > 10 * 1024 * 1024) // > 10 MB
                    {
                        cpuEstimate = "�������";
                        ramEstimate = "�������";
                    }
                    else if (totalSize > 1 * 1024 * 1024) // > 1 MB
                    {
                        cpuEstimate = "�������";
                        ramEstimate = "�������";
                    }

                    _staticAnalysisOutput.AppendLine();
                    _staticAnalysisOutput.AppendLine("=== ������� ���������� ===");
                    _staticAnalysisOutput.AppendLine($"������ CPU: {cpuEstimate}");
                    _staticAnalysisOutput.AppendLine($"������ RAM: {ramEstimate}");
                }

                // ������� ���������������� ������ � ������� dumpbin
                AnalyzeImports(filePath);
            }
            catch (Exception ex)
            {
                _staticAnalysisOutput.AppendLine($"������ ��� ������� PE-�����: {ex.Message}");
            }
        }

        private void AnalyzeImports(string filePath)
        {
            try
            {
                List<string> imports = new List<string>();
                List<string> exports = new List<string>();

                // ������ ������� � dumpbin
                bool usedDumpBin = TryAnalyzeWithDumpBin(filePath, imports, exports);

                if (!usedDumpBin)
                {
                    // ���� dumpbin �� �������� ��� ����������, ���������� ������� ������������ �������� ������
                    ScanFileForImports(filePath, imports);
                }

                _staticAnalysisOutput.AppendLine();
                _staticAnalysisOutput.AppendLine("=== ������������� ���������� ===");
                if (imports.Count > 0)
                {
                    foreach (var import in imports)
                    {
                        _staticAnalysisOutput.AppendLine($"- {import}");
                    }

                    // ���������� ������������� ������������� �������� �� ������ ��������������� DLL
                    DetectResourceUsageFromImports(imports);
                }
                else
                {
                    _staticAnalysisOutput.AppendLine("�� ������� ���������� ������������� ����������");
                }

                // ���������� ����� ������������
                BeginInvoke(new Action(() =>
                {
                    lblDependencies.Text = $"�����������: {imports.Count} ���������";
                }));

                if (exports.Count > 0)
                {
                    _staticAnalysisOutput.AppendLine();
                    _staticAnalysisOutput.AppendLine("=== �������������� ������� ===");
                    foreach (var export in exports.Take(20)) // ����������� �� ������ 20
                    {
                        _staticAnalysisOutput.AppendLine($"- {export}");
                    }

                    if (exports.Count > 20)
                    {
                        _staticAnalysisOutput.AppendLine($"... � ��� {exports.Count - 20} �������");
                    }
                }
            }
            catch (Exception ex)
            {
                _staticAnalysisOutput.AppendLine($"������ ��� ������� ��������: {ex.Message}");
            }
        }

        private bool TryAnalyzeWithDumpBin(string filePath, List<string> imports, List<string> exports)
        {
            try
            {
                // ������� ������������ dumpbin ��� ������� �������
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "dumpbin.exe";
                    process.StartInfo.Arguments = $"/IMPORTS \"{filePath}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        // ������ ��������������� DLL
                        string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        string currentDll = null;

                        foreach (var line in lines)
                        {
                            if (line.Contains(".dll") && !line.Contains(" "))
                            {
                                currentDll = line.Trim();
                                if (!imports.Contains(currentDll))
                                {
                                    imports.Add(currentDll);
                                }
                            }
                        }

                        // ����� ���������� �������� �������
                        process.StartInfo.Arguments = $"/EXPORTS \"{filePath}\"";
                        process.Start();
                        output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            bool inExportSection = false;

                            foreach (var line in lines)
                            {
                                if (line.Contains("ordinal hint"))
                                {
                                    inExportSection = true;
                                    continue;
                                }

                                if (inExportSection && line.Trim().Length > 0 && char.IsDigit(line.Trim()[0]))
                                {
                                    // ��� ���������� ������
                                    string[] parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length >= 4)
                                    {
                                        exports.Add(parts[3]); // Export name
                                    }
                                }
                            }
                        }

                        return true;
                    }
                }
            }
            catch
            {
                // Dumpbin ���������� ��� �� ��������
            }

            return false;
        }

        private void ScanFileForImports(string filePath, List<string> imports)
        {
            try
            {
                // ����� ������� ������ ������ �� DLL
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[4096];
                    StringBuilder sb = new StringBuilder();

                    while (fs.Read(buffer, 0, buffer.Length) > 0)
                    {
                        // �������������� � ������ � ����� ������ �� DLL
                        string chunk = Encoding.ASCII.GetString(buffer);
                        sb.Append(chunk);
                    }

                    // ���� ����� �������� DLL
                    string fileContent = sb.ToString();
                    Regex dllRegex = new Regex(@"([a-zA-Z0-9_-]+\.dll)", RegexOptions.IgnoreCase);

                    foreach (Match match in dllRegex.Matches(fileContent))
                    {
                        string dll = match.Groups[1].Value;
                        if (!imports.Contains(dll))
                        {
                            imports.Add(dll);
                        }
                    }
                }
            }
            catch
            {
                // Ignore scanning errors
            }
        }

        private void DetectResourceUsageFromImports(List<string> imports)
        {
            _staticAnalysisOutput.AppendLine();
            _staticAnalysisOutput.AppendLine("=== ����������� ������������� �������� ===");

            bool usesGUI = false;
            bool usesNetwork = false;
            bool usesDatabase = false;
            bool usesMultimedia = false;
            bool uses3D = false;
            bool usesHardwareAcceleration = false;

            foreach (var import in imports)
            {
                string lowerImport = import.ToLower();

                // GUI usage
                if (lowerImport.Contains("user32") || lowerImport.Contains("gdi32") ||
                    lowerImport.Contains("comctl32") || lowerImport.Contains("shell32"))
                {
                    usesGUI = true;
                }

                // Network usage
                if (lowerImport.Contains("ws2_32") || lowerImport.Contains("wininet") ||
                    lowerImport.Contains("winhttp") || lowerImport.Contains("urlmon"))
                {
                    usesNetwork = true;
                }

                // Database usage
                if (lowerImport.Contains("odbc") || lowerImport.Contains("sqlite") ||
                    lowerImport.Contains("sql") || lowerImport.Contains("msjet"))
                {
                    usesDatabase = true;
                }

                // Multimedia usage
                if (lowerImport.Contains("winmm") || lowerImport.Contains("avifil32") ||
                    lowerImport.Contains("msvfw32") || lowerImport.Contains("dsound"))
                {
                    usesMultimedia = true;
                }

                // 3D graphics
                if (lowerImport.Contains("d3d") || lowerImport.Contains("opengl") ||
                    lowerImport.Contains("dxgi") || lowerImport.Contains("vulkan"))
                {
                    uses3D = true;
                }

                // Hardware acceleration
                if (lowerImport.Contains("cuda") || lowerImport.Contains("opencl") ||
                    lowerImport.Contains("dxva") || uses3D)
                {
                    usesHardwareAcceleration = true;
                }
            }

            if (usesGUI) _staticAnalysisOutput.AppendLine("- ����������� ��������� ������������ (GUI)");
            if (usesNetwork) _staticAnalysisOutput.AppendLine("- ������� ��������");
            if (usesDatabase) _staticAnalysisOutput.AppendLine("- ������ � ������ ������");
            if (usesMultimedia) _staticAnalysisOutput.AppendLine("- ����������� (�����/�����)");
            if (uses3D) _staticAnalysisOutput.AppendLine("- 3D �������");
            if (usesHardwareAcceleration) _staticAnalysisOutput.AppendLine("- ���������� ���������");

            if (!usesGUI && !usesNetwork && !usesDatabase && !usesMultimedia && !uses3D && !usesHardwareAcceleration)
            {
                _staticAnalysisOutput.AppendLine("- �� ������� ���������� ������������� ���������� ����������");
            }
        }

        /// ���������� ��������� ������� ������������ ����
        private void PerformDeepCodeAnalysis(string filePath)
        {
            StringBuilder disassemblyOutput = new StringBuilder();
            disassemblyOutput.AppendLine("=== ������������������� ��� ===");
            disassemblyOutput.AppendLine();

            try
            {
                // ������� �������� ������������ dumpbin ��� ������������������
                if (!TryDisassembleWithDumpBin(filePath, disassemblyOutput))
                {
                    // ���� dumpbin �� ��������, ������� objdump
                    if (!TryDisassembleWithObjDump(filePath, disassemblyOutput))
                    {
                        // ���� ��� ����������� �� ���������, ��������� ������� ������ ������������������ �����
                        PerformHexDumpAnalysis(filePath, disassemblyOutput);
                    }
                }

                // ������ �������� ���������� � ������������������� ����
                AnalyzeCodeComplexity(disassemblyOutput.ToString());

                // ���������� ���������� � ������������ ������������������
                BeginInvoke(new Action(() =>
                {
                    rtbDisassembly.Text = disassemblyOutput.ToString();
                }));
            }
            catch (Exception ex)
            {
                disassemblyOutput.AppendLine($"������ ��� ���������� ��������� ������� ����: {ex.Message}");

                BeginInvoke(new Action(() =>
                {
                    rtbDisassembly.Text = disassemblyOutput.ToString();
                }));
            }
        }

        /// ������� ������������������ � ������� ����������� dumpbin
        /// ���������� true ���� ������������������ �������, ����� false
        private bool TryDisassembleWithDumpBin(string filePath, StringBuilder output)
        {
            try
            {
                using (var process = new Process())
                {
                    // ��������� ���������� ������� dumpbin
                    process.StartInfo.FileName = "dumpbin.exe";
                    process.StartInfo.Arguments = $"/DISASM \"{filePath}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();

                    // ������ ������ dumpbin ������� ��� ��������� ������� ������
                    using (StreamReader reader = process.StandardOutput)
                    {
                        // ����������� ���������
                        BeginInvoke(new Action(() =>
                        {
                            progressBar.Value = 30;
                        }));

                        string line;
                        bool inCodeSection = false;
                        int lineCount = 0;

                        // ��������� ������ dumpbin ���������
                        while ((line = reader.ReadLine()) != null)
                        {
                            // ���������� ������ ������ ����
                            if (line.Contains("SECTION HEADER") && line.Contains("code"))
                            {
                                inCodeSection = true;
                                output.AppendLine(line);
                                output.AppendLine("-------------------------------------------");
                                continue;
                            }

                            // ���� �� � ������ ����, ���������� ������ ������
                            if (inCodeSection)
                            {
                                if (line.Trim().Length > 0)
                                {
                                    output.AppendLine(line);
                                    lineCount++;

                                    // ����������� �� �������� ���������� �����
                                    if (lineCount > 1000)
                                    {
                                        output.AppendLine("...");
                                        output.AppendLine("(����� ��������� ������� 1000 ��������)");
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    process.WaitForExit();
                    // ��������� ���������� ���������� � ������� ������
                    return process.ExitCode == 0 && output.Length > 100;
                }
            }
            catch
            {
                // dumpbin ���������� ��� ������ ������
                return false;
            }
        }

        private bool TryDisassembleWithObjDump(string filePath, StringBuilder output)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "objdump.exe";
                    process.StartInfo.Arguments = $"-d \"{filePath}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();

                    // Report progress
                    BeginInvoke(new Action(() =>
                    {
                        progressBar.Value = 50;
                    }));

                    // ���������� ������ ������� ��� ��������� ������� ������
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string line;
                        int lineCount = 0;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.Contains(":") && (line.Contains("push") || line.Contains("mov") || line.Contains("call")))
                            {
                                output.AppendLine(line);
                                lineCount++;

                                // ����������� �� ��������� ���������� �����
                                if (lineCount > 1000)
                                {
                                    output.AppendLine("...");
                                    output.AppendLine("(����� ��������� ������� 1000 ��������)");
                                    break;
                                }
                            }
                        }
                    }

                    process.WaitForExit();
                    return process.ExitCode == 0 && output.Length > 100;
                }
            }
            catch
            {
                // objdump not available
                return false;
            }
        }

        private void PerformHexDumpAnalysis(string filePath, StringBuilder output)
        {
            try
            {
                // Report progress
                BeginInvoke(new Action(() =>
                {
                    progressBar.Value = 70;
                }));

                output.AppendLine("����������: ����������� ������������������ ����������.");
                output.AppendLine("����������� ������� ������ ������������������ �����.");
                output.AppendLine();

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    // ������� ��������� ������ � PE-�����
                    byte[] buffer = new byte[4096];
                    fs.Read(buffer, 0, buffer.Length);

                    // ������� PE header
                    int peOffset = BitConverter.ToInt32(buffer, 0x3C);

                    // ���������, ���� �� � ��� �������������� PE-����
                    if (peOffset < buffer.Length - 4 &&
                        buffer[peOffset] == 'P' &&
                        buffer[peOffset + 1] == 'E' &&
                        buffer[peOffset + 2] == 0 &&
                        buffer[peOffset + 3] == 0)
                    {
                        // ��������� ���������� ������
                        ushort numSections = BitConverter.ToUInt16(buffer, peOffset + 6);

                        // ��������� ������� ��������������� ���������
                        ushort optionalHeaderSize = BitConverter.ToUInt16(buffer, peOffset + 20);

                        // ����������� �������� ������� ������
                        int sectionTableOffset = peOffset + 24 + optionalHeaderSize;

                        // ���� ��������� ������
                        int textSectionRVA = 0;
                        int textSectionOffset = 0;
                        int textSectionSize = 0;

                        for (int i = 0; i < numSections && sectionTableOffset + i * 40 + 40 <= buffer.Length; i++)
                        {
                            int sectionOffset = sectionTableOffset + i * 40;

                            // ������ ����� ������� (8 ����)
                            string sectionName = Encoding.ASCII.GetString(buffer, sectionOffset, 8).TrimEnd('\0');

                            // ���������, �������� �� ��� �������� ����
                            if (sectionName == ".text" || sectionName == "CODE")
                            {
                                // ��������� ������������ ������ ������ (RVA)
                                textSectionRVA = BitConverter.ToInt32(buffer, sectionOffset + 12);

                                // ��������� �������� ����� �������
                                textSectionOffset = BitConverter.ToInt32(buffer, sectionOffset + 20);

                                // ��������� ������� ������
                                textSectionSize = BitConverter.ToInt32(buffer, sectionOffset + 16);

                                break;
                            }
                        }

                        if (textSectionOffset > 0 && textSectionSize > 0)
                        {

                            // �� ����� ��������� ������, ��������� ���
                            output.AppendLine($"Text section found: RVA=0x{textSectionRVA:X8}, Offset=0x{textSectionOffset:X8}, Size={textSectionSize}");
                            output.AppendLine();

                            // ���������� ������ ��� �������
                            int sizeToAnalyze = Math.Min(textSectionSize, 4096);

                            // ������� � ���������� �������
                            fs.Seek(textSectionOffset, SeekOrigin.Begin);

                            // ��������� ��������� ������
                            byte[] textSection = new byte[sizeToAnalyze];
                            fs.Read(textSection, 0, sizeToAnalyze);

                            // ���������� ������� ��������� ����������
                            AnalyzeInstructionPatterns(textSection, output);
                        }
                        else
                        {
                            // ��������� ������ �� ������, ������ ������ ������ ��������� ��
                            output.AppendLine("Text section not found, dumping first 4KB");
                            output.AppendLine();

                            // ���� � ������
                            fs.Seek(0, SeekOrigin.Begin);

                            // ������ ������ 4 ��
                            byte[] firstChunk = new byte[4096];
                            fs.Read(firstChunk, 0, 4096);

                            // Dump hex
                            DumpHex(firstChunk, output);
                        }
                    }
                    else
                    {
                        // �� ���� PE, � ������ ���� ������ ���������� ��.
                        output.AppendLine("�� �������� ���������� PE-������, ���� ������ 4��");
                        output.AppendLine();

                        // Dump hex
                        DumpHex(buffer, output);
                    }
                }
            }
            catch (Exception ex)
            {
                output.AppendLine($"������ ��� ������� �����: {ex.Message}");
            }
        }

        private void AnalyzeInstructionPatterns(byte[] data, StringBuilder output)
        {
            // ��� ����� ������� ������������� ��������� ��� ������� ���������� x86/x64
            output.AppendLine("������ �������� ����������:");
            output.AppendLine();

            // ����� �������� ���������� x86/x64
            Dictionary<string, byte[]> patterns = new Dictionary<string, byte[]>
            {
                { "CALL", new byte[] { 0xE8 } },                // CALL relative
                { "JMP", new byte[] { 0xE9 } },                 // JMP relative
                { "PUSH EBP", new byte[] { 0x55 } },            // PUSH EBP (function prologue)
                { "MOV EBP,ESP", new byte[] { 0x8B, 0xEC } },   // MOV EBP,ESP (function prologue)
                { "POP EBP", new byte[] { 0x5D } },             // POP EBP (function epilogue)
                { "RET", new byte[] { 0xC3 } },                 // RET (function return)
                { "XOR EAX,EAX", new byte[] { 0x33, 0xC0 } },   // XOR EAX,EAX (zero out EAX)
                { "REP MOVS", new byte[] { 0xF3, 0xA4 } },      // REP MOVS (memory copy)
                { "REP STOS", new byte[] { 0xF3, 0xAA } }       // REP STOS (memory set)
            };

            // ���������� ���������� ��������� ������� ��������
            Dictionary<string, int> patternCounts = new Dictionary<string, int>();
            foreach (var pattern in patterns)
            {
                patternCounts[pattern.Key] = 0;
            }

            // ������������ ��� ������ ���������
            for (int i = 0; i < data.Length - 5; i++)
            {
                foreach (var pattern in patterns)
                {
                    bool match = true;
                    for (int j = 0; j < pattern.Value.Length && i + j < data.Length; j++)
                    {
                        if (data[i + j] != pattern.Value[j])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        patternCounts[pattern.Key]++;
                    }
                }
            }

            // ���������� �������� ���������
            foreach (var count in patternCounts)
            {
                output.AppendLine($"{count.Key}: {count.Value} ���");
            }

            // ������� �������������-�������� �������� (PUSH EBP � ����������� MOV EBP,ESP)
            int functionCount = 0;
            for (int i = 0; i < data.Length - 3; i++)
            {
                if (data[i] == 0x55 && i + 2 < data.Length && data[i + 1] == 0x8B && data[i + 2] == 0xEC)
                {
                    functionCount++;
                }
            }

            output.AppendLine($"��������� ���������� �������: {functionCount}");

            // ��������� �����, ������ ������ �����
            int loopCount = 0;
            for (int i = 0; i < data.Length - 6; i++)
            {
                // JL/JLE/JG/JGE � ������������� ���������
                if ((data[i] == 0x7C || data[i] == 0x7E || data[i] == 0x7F || data[i] == 0x7D) &&
                    (sbyte)data[i + 1] < 0)
                {
                    loopCount++;
                }
            }

            output.AppendLine($"��������� ���������� ������: {loopCount}");

            // ������ ��������� �� ������ ���������
            int complexity = functionCount * 2 + loopCount * 3 + patternCounts["CALL"] + patternCounts["JMP"];
            string complexityLevel;

            if (complexity > 500)
                complexityLevel = "����� �������";
            else if (complexity > 200)
                complexityLevel = "�������";
            else if (complexity > 100)
                complexityLevel = "�������";
            else if (complexity > 50)
                complexityLevel = "������";
            else
                complexityLevel = "����� ������";

            output.AppendLine();
            output.AppendLine($"������ ��������� ����: {complexityLevel} (score: {complexity})");
            output.AppendLine();

            // ��������� ����������� �������� ��� hex
            output.AppendLine("���� ������ ������ ����:");
            output.AppendLine();
            DumpHex(data.Take(512).ToArray(), output);
        }

        private void DumpHex(byte[] data, StringBuilder output)
        {
            int bytesPerLine = 16;

            for (int i = 0; i < data.Length; i += bytesPerLine)
            {
                // Address
                output.Append($"{i:X8}: ");

                // Hex values
                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (i + j < data.Length)
                        output.Append($"{data[i + j]:X2} ");
                    else
                        output.Append("   ");

                    if (j == 7)
                        output.Append(" ");
                }

                // ASCII representation
                output.Append(" | ");
                for (int j = 0; j < bytesPerLine && i + j < data.Length; j++)
                {
                    char c = (char)data[i + j];
                    output.Append(char.IsControl(c) ? '.' : c);
                }

                output.AppendLine();

                // ������������ output, ����� �� ��� �����������
                if (i >= 256)
                {
                    output.AppendLine("...");
                    break;
                }
            }
        }

        /// ������ ��������� ���� �� ������ �������������������� ��������
        private void AnalyzeCodeComplexity(string disassembly)
        {
            // ����������� ���������
            BeginInvoke(new Action(() =>
            {
                progressBar.Value = 90;
            }));

            StringBuilder complexityAnalysis = new StringBuilder();
            complexityAnalysis.AppendLine();
            complexityAnalysis.AppendLine("=== ������ ��������� ���� ===");

            try
            {
                // ������� ������� ����������
                int callCount = CountOccurrences(disassembly, "call ");           // ������ �������
                int jmpCount = CountOccurrences(disassembly, "jmp ");             // ����������� ��������
                int condJumpCount = CountOccurrences(disassembly, "je ") +        // �������� ��������
                                  CountOccurrences(disassembly, "jne ") +
                                  CountOccurrences(disassembly, "jg ") +
                                  CountOccurrences(disassembly, "jl ") +
                                  CountOccurrences(disassembly, "jge ") +
                                  CountOccurrences(disassembly, "jle ");
                int loopCount = CountOccurrences(disassembly, "loop");            // �����
                int movCount = CountOccurrences(disassembly, "mov ");             // ����������� ������
                int pushPopCount = CountOccurrences(disassembly, "push ") +       // �������� �� ������
                                 CountOccurrences(disassembly, "pop ");

                // ����������� ������� �� ����������� ����������� � �������
                int functionCount = CountOccurrences(disassembly, "push ebp") +   // ����������� ������ �������
                                  CountOccurrences(disassembly, "push rbp");

                // �������� � ��������� ������
                int fpCount = CountOccurrences(disassembly, "fld") +              // ��������/���������� FP
                            CountOccurrences(disassembly, "fst") +
                            CountOccurrences(disassembly, "fadd") +               // ���������� FP
                            CountOccurrences(disassembly, "fmul") +
                            CountOccurrences(disassembly, "fdiv");

                // ��������� �������� (SIMD)
                int simdCount = CountOccurrences(disassembly, "movaps") +         // �������� SSE
                              CountOccurrences(disassembly, "movups") +
                              CountOccurrences(disassembly, "movdqa") +           // �������� AVX
                              CountOccurrences(disassembly, "paddd") +
                              CountOccurrences(disassembly, "psubd") +
                              CountOccurrences(disassembly, "pmulld");

                // ����� ����������
                complexityAnalysis.AppendLine($"������������ �������: {functionCount}");
                complexityAnalysis.AppendLine($"������ ������� (call): {callCount}");
                complexityAnalysis.AppendLine($"����������� �������� (jmp): {jmpCount}");
                complexityAnalysis.AppendLine($"�������� �������� (je, jne � �.�.): {condJumpCount}");
                complexityAnalysis.AppendLine($"����� (loop): {loopCount}");
                complexityAnalysis.AppendLine();

                complexityAnalysis.AppendLine("�������������� ����:");
                complexityAnalysis.AppendLine($"- �������� ����������� ������ (mov): {movCount}");
                complexityAnalysis.AppendLine($"- �������� �� ������ (push/pop): {pushPopCount}");
                complexityAnalysis.AppendLine($"- �������� � ��������� ������: {fpCount}");
                complexityAnalysis.AppendLine($"- SIMD/��������� ��������: {simdCount}");
                complexityAnalysis.AppendLine();

                // ������ ������ ���������
                int cyclomaticComplexity = 1 + condJumpCount;
                double branchingFactor = (double)(condJumpCount + jmpCount) / Math.Max(1, movCount);

                // ������ ������������� CPU �� ������ ������� ����������
                int cpuComplexity = callCount * 5 + condJumpCount * 3 + fpCount * 4 + simdCount * 2;
                string cpuUsage;

                // ����������� ������ �������� �� CPU
                if (cpuComplexity > 1000)
                    cpuUsage = "����� �������";
                else if (cpuComplexity > 500)
                    cpuUsage = "�������";
                else if (cpuComplexity > 200)
                    cpuUsage = "�������";
                else if (cpuComplexity > 50)
                    cpuUsage = "������";
                else
                    cpuUsage = "����� ������";

                complexityAnalysis.AppendLine("������� ���������:");
                complexityAnalysis.AppendLine($"- ��������������� ���������: {cyclomaticComplexity}");
                complexityAnalysis.AppendLine($"- ����������� ���������: {branchingFactor:F2}");
                complexityAnalysis.AppendLine();

                complexityAnalysis.AppendLine("������ ����������:");
                complexityAnalysis.AppendLine($"- �������� �� CPU: {cpuUsage}");

                // �������������� �������������� �� ������ ������������ ����������
                if (fpCount > 100)
                    complexityAnalysis.AppendLine("- ������� �������������� �������� (�������� � ��������� ������)");

                if (simdCount > 50)
                    complexityAnalysis.AppendLine("- ������������� ��������� ���������� (SIMD)");

                // ���������� UI
                BeginInvoke(new Action(() =>
                {
                    rtbDisassembly.Text += complexityAnalysis.ToString();
                    progressBar.Value = 100;
                }));
            }
            catch (Exception ex)
            {
                complexityAnalysis.AppendLine($"������ ��� ������� ���������: {ex.Message}");

                BeginInvoke(new Action(() =>
                {
                    rtbDisassembly.Text += complexityAnalysis.ToString();
                    progressBar.Value = 100;
                }));
            }
        }

        /// ������� ���������� ��������� �������� � ������
        /// text:����� ��� ������
        /// pattern:������� �������
        /// ���������� ���������� ��������� ���������
        private int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;

            // ����� ���� ��������� �������� � ������ ��� ����� ��������
            while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += pattern.Length;
            }

            return count;
        }

        /// ���������� ������ � ������������ �������
        private void SaveReport(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // ��������� ������
                    writer.WriteLine("=== ��������� ������ ������������ �������� ===");
                    writer.WriteLine($"����: {_selectedExecutablePath}");
                    writer.WriteLine($"���� �������: {DateTime.Now}");
                    writer.WriteLine();

                    // ������ �� ����������� ��������
                    writer.WriteLine("--- ����������� ������ ---");
                    writer.WriteLine(rtbResults.Text);
                    writer.WriteLine();

                    // ������ � ������������������� �����
                    writer.WriteLine("--- ������������������� ��� � ������ ��������� ---");
                    writer.WriteLine(rtbDisassembly.Text);
                    writer.WriteLine();

                    // ������ � ������������ ��������
                    writer.WriteLine("--- ������������ ������ ---");
                    if (_resourceSnapshots.Count > 0)
                    {
                        // ������ ������� � ������������ ��������
                        double avgCpu = _resourceSnapshots.Average(s => s.CpuUsage);
                        double maxCpu = _resourceSnapshots.Max(s => s.CpuUsage);
                        double avgMem = _resourceSnapshots.Average(s => s.MemoryUsageMB);
                        double maxMem = _resourceSnapshots.Max(s => s.MemoryUsageMB);
                        double avgDiskRead = _resourceSnapshots.Average(s => s.DiskReadKBs);
                        double avgDiskWrite = _resourceSnapshots.Average(s => s.DiskWriteKBs);

                        writer.WriteLine($"����������������� �������: {_resourceSnapshots.Count} ������");
                        writer.WriteLine($"������� ������������� CPU: {avgCpu:F2}%, ��������: {maxCpu:F2}%");
                        writer.WriteLine($"������� ������������� RAM: {avgMem:F2} ��, ��������: {maxMem:F2} ��");
                        writer.WriteLine($"������� �������� ��������: ������ {avgDiskRead:F2} ��/�, ������ {avgDiskWrite:F2} ��/�");
                        writer.WriteLine();

                        writer.WriteLine("��������� ��������� (�� ��������):");
                        writer.WriteLine("�����\tCPU(%)\tRAM(��)\t������(��/�)\t������(��/�)");

                        foreach (var snapshot in _resourceSnapshots)
                        {
                            writer.WriteLine($"{snapshot.Timestamp.ToString("HH:mm:ss")}\t{snapshot.CpuUsage:F2}\t{snapshot.MemoryUsageMB:F2}\t{snapshot.DiskReadKBs:F2}\t{snapshot.DiskWriteKBs:F2}");
                        }
                    }
                    else
                    {
                        writer.WriteLine("������������ ������ �� ��� �������� ��� �� �������� ������.");
                    }

                    writer.WriteLine();
                    writer.WriteLine("=== ����� ������ ===");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"������ ��� ���������� ������: {ex.Message}", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "�", "��", "��", "��", "��" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:N2} {suffixes[counter]}";
        }

        private void ResetUI()
        {
            btnStartAnalysis.Enabled = !string.IsNullOrEmpty(_selectedExecutablePath);
            btnStopAnalysis.Enabled = false;
            btnSelectFile.Enabled = true;
            btnAnalyzeDeeper.Enabled = !string.IsNullOrEmpty(_selectedExecutablePath);
            _isMonitoring = false;
            progressBar.Visible = false;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopMonitoring();
        }

        // ������� ���� ��� ��������� ���������� ����������������� ����������
        private Button btnSelectFile;
        private Button btnStartAnalysis;
        private Button btnStopAnalysis;
        private Button btnSaveReport;
        private Button btnAnalyzeDeeper;
        private TextBox txtFilePath;
        private Label lblStatus;
        private TabControl tabControl;
        private TabPage tabDynamic;
        private TabPage tabStatic;
        private TabPage tabDisassembly;
        private Label lblCpu;
        private Label lblMemory;
        private Label lblDiskIO;
        private Label lblFileSize;
        private Label lblFileType;
        private Label lblDependencies;
        private Label lblStaticAnalysis;
        private RichTextBox rtbResults;
        private RichTextBox rtbDisassembly;
        private ProgressBar progressBar;
    }

    /// ����� ��� �������� ������ � ����������� �������� � ���������� ������ �������
    public class ResourceSnapshot
    {
        /// ����� ������ ���������
        public DateTime Timestamp { get; set; }

        /// ������������� ���������� � ���������
        public double CpuUsage { get; set; }

        /// ������������� ����������� ������ � ��
        public double MemoryUsageMB { get; set; }

        /// �������� ������ � ����� � ��/�
        public double DiskReadKBs { get; set; }

        /// �������� ������ �� ���� � ��/�
        public double DiskWriteKBs { get; set; }
    }

    /// �������� ����� ���������
    static class Program
    {
        /// ������� ����� ����� � ����������
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}