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
    /// Главная форма приложения для анализа аппаратных затрат
    public partial class MainForm : Form
    {
        // Объект для отмены асинхронных операций
        private CancellationTokenSource _cancellationTokenSource;
        // Флаг, показывающий включен ли режим мониторинга
        private bool _isMonitoring = false;
        // Путь к выбранному исполняемому файлу
        private string _selectedExecutablePath = string.Empty;
        // Отслеживаемый процесс
        private Process _monitoredProcess = null;
        // Список снимков состояния ресурсов
        private List<ResourceSnapshot> _resourceSnapshots = new List<ResourceSnapshot>();
        // Буфер для хранения результатов статического анализа
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
            this.Text = "Детальный анализатор программ";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Controls
            this.btnSelectFile = new Button();
            this.btnSelectFile.Text = "Выбрать файл";
            this.btnSelectFile.Location = new Point(15, 15);
            this.btnSelectFile.Size = new Size(100, 30);
            this.btnSelectFile.Click += new EventHandler(btnSelectFile_Click);

            this.txtFilePath = new TextBox();
            this.txtFilePath.Location = new Point(125, 15);
            this.txtFilePath.Size = new Size(650, 30);
            this.txtFilePath.ReadOnly = true;

            this.btnStartAnalysis = new Button();
            this.btnStartAnalysis.Text = "Начать анализ";
            this.btnStartAnalysis.Location = new Point(15, 55);
            this.btnStartAnalysis.Size = new Size(120, 30);
            this.btnStartAnalysis.Click += new EventHandler(btnStartAnalysis_Click);
            this.btnStartAnalysis.Enabled = false;

            this.btnStopAnalysis = new Button();
            this.btnStopAnalysis.Text = "Остановить";
            this.btnStopAnalysis.Location = new Point(145, 55);
            this.btnStopAnalysis.Size = new Size(120, 30);
            this.btnStopAnalysis.Click += new EventHandler(btnStopAnalysis_Click);
            this.btnStopAnalysis.Enabled = false;

            this.btnSaveReport = new Button();
            this.btnSaveReport.Text = "Сохранить отчет";
            this.btnSaveReport.Location = new Point(275, 55);
            this.btnSaveReport.Size = new Size(120, 30);
            this.btnSaveReport.Click += new EventHandler(btnSaveReport_Click);
            this.btnSaveReport.Enabled = false;

            this.btnAnalyzeDeeper = new Button();
            this.btnAnalyzeDeeper.Text = "Глубокий анализ кода";
            this.btnAnalyzeDeeper.Location = new Point(405, 55);
            this.btnAnalyzeDeeper.Size = new Size(150, 30);
            this.btnAnalyzeDeeper.Click += new EventHandler(btnAnalyzeDeeper_Click);
            this.btnAnalyzeDeeper.Enabled = false;

            this.lblStatus = new Label();
            this.lblStatus.Text = "Готов к работе";
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
            this.tabDynamic.Text = "Динамический анализ";
            this.lblCpu = new Label();
            this.lblCpu.Text = "Использование CPU: -";
            this.lblCpu.Location = new Point(10, 20);
            this.lblCpu.AutoSize = true;
            this.lblMemory = new Label();
            this.lblMemory.Text = "Использование RAM: -";
            this.lblMemory.Location = new Point(10, 50);
            this.lblMemory.AutoSize = true;
            this.lblDiskIO = new Label();
            this.lblDiskIO.Text = "Дисковые операции: -";
            this.lblDiskIO.Location = new Point(10, 80);
            this.lblDiskIO.AutoSize = true;
            this.tabDynamic.Controls.Add(this.lblCpu);
            this.tabDynamic.Controls.Add(this.lblMemory);
            this.tabDynamic.Controls.Add(this.lblDiskIO);

            this.tabStatic = new TabPage();
            this.tabStatic.Text = "Статический анализ";
            this.lblFileSize = new Label();
            this.lblFileSize.Text = "Размер файла: -";
            this.lblFileSize.Location = new Point(10, 20);
            this.lblFileSize.AutoSize = true;
            this.lblFileType = new Label();
            this.lblFileType.Text = "Тип файла: -";
            this.lblFileType.Location = new Point(10, 50);
            this.lblFileType.AutoSize = true;
            this.lblDependencies = new Label();
            this.lblDependencies.Text = "Зависимости: -";
            this.lblDependencies.Location = new Point(10, 80);
            this.lblDependencies.AutoSize = true;
            this.lblStaticAnalysis = new Label();
            this.lblStaticAnalysis.Text = "Результаты статического анализа:";
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
            this.tabDisassembly.Text = "Дизассемблированный код";
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

        /// Обработчик нажатия кнопки выбора файла
        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                // Настраиваем диалог выбора файла
                openFileDialog.Filter = "Исполняемые файлы (*.exe;*.dll)|*.exe;*.dll|Все файлы (*.*)|*.*";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedExecutablePath = openFileDialog.FileName;
                    txtFilePath.Text = _selectedExecutablePath;
                    btnStartAnalysis.Enabled = true;
                    btnAnalyzeDeeper.Enabled = true;

                    // Выполняем базовый статический анализ при выборе файла
                    PerformBasicStaticAnalysis(_selectedExecutablePath);
                }
            }
        }

        /// Обработчик нажатия кнопки запуска анализа
        private void btnStartAnalysis_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedExecutablePath))
            {
                MessageBox.Show("Пожалуйста, выберите исполняемый файл для анализа.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Инициализация для нового анализа
                _cancellationTokenSource = new CancellationTokenSource();
                _resourceSnapshots.Clear();
                _isMonitoring = true;

                // Обновляем состояние UI
                btnStartAnalysis.Enabled = false;
                btnStopAnalysis.Enabled = true;
                btnSelectFile.Enabled = false;
                btnAnalyzeDeeper.Enabled = false;
                lblStatus.Text = "Анализ запущен...";

                // Запускаем процесс для мониторинга
                _monitoredProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _selectedExecutablePath,
                        UseShellExecute = true
                    }
                };
                _monitoredProcess.Start();

                // Запускаем мониторинг в отдельном потоке
                Task.Run(() => MonitorResourcesAsync(_monitoredProcess, _cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске процесса: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                saveFileDialog.FileName = "ДетальныйАнализПрограммы.txt";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    SaveReport(saveFileDialog.FileName);
                    MessageBox.Show("Отчет успешно сохранен!", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// Выполнение глубокого анализа программного кода
        private void btnAnalyzeDeeper_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedExecutablePath))
            {
                MessageBox.Show("Пожалуйста, выберите исполняемый файл для анализа.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnAnalyzeDeeper.Enabled = false;
            lblStatus.Text = "Выполняется глубокий анализ кода...";
            progressBar.Visible = true;
            progressBar.Value = 0;

            // Запускаем анализ в фоновом потоке
            Task.Run(() =>
            {
                try
                {
                    // Выполняем глубокий анализ кода
                    PerformDeepCodeAnalysis(_selectedExecutablePath);

                    // Обновляем UI после завершения
                    BeginInvoke(new Action(() =>
                    {
                        lblStatus.Text = "Глубокий анализ завершен";
                        progressBar.Visible = false;
                        btnAnalyzeDeeper.Enabled = true;
                        tabControl.SelectedTab = tabDisassembly;
                    }));
                }
                catch (Exception ex)
                {
                    // Обрабатываем ошибки
                    BeginInvoke(new Action(() =>
                    {
                        lblStatus.Text = "Ошибка при выполнении глубокого анализа";
                        progressBar.Visible = false;
                        btnAnalyzeDeeper.Enabled = true;
                        MessageBox.Show($"Ошибка при анализе: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            });
        }

        /// Асинхронный мониторинг использования ресурсов процессом
        private async Task MonitorResourcesAsync(Process process, CancellationToken cancellationToken)
        {
            try
            {
                int processId = process.Id;
                DateTime startTime = DateTime.Now;

                // Цикл мониторинга, выполняется до отмены или завершения процесса
                while (!cancellationToken.IsCancellationRequested && !process.HasExited)
                {
                    // Задержка для сбора данных
                    await Task.Delay(1000, cancellationToken);

                    if (process.HasExited)
                        break;

                    try
                    {
                        process.Refresh();

                        // Получение загрузки CPU через WMI
                        double cpuUsage = GetCpuUsageForProcess(processId);

                        // Получение использования памяти
                        double memoryUsageMB = process.WorkingSet64 / (1024 * 1024.0);

                        // Информация о дисковых операциях
                        double diskReadKBs = 0;
                        double diskWriteKBs = 0;

                        // Попытка получить статистику I/O через WMI
                        var ioStats = GetIOStatsForProcess(processId);
                        if (ioStats != null)
                        {
                            diskReadKBs = ioStats.Item1 / 1024.0;
                            diskWriteKBs = ioStats.Item2 / 1024.0;
                        }

                        // Создание снимка ресурсов
                        var snapshot = new ResourceSnapshot
                        {
                            Timestamp = DateTime.Now,
                            CpuUsage = cpuUsage,
                            MemoryUsageMB = memoryUsageMB,
                            DiskReadKBs = diskReadKBs,
                            DiskWriteKBs = diskWriteKBs
                        };

                        _resourceSnapshots.Add(snapshot);

                        // Обновление UI в основном потоке
                        BeginInvoke(new Action(() =>
                        {
                            lblCpu.Text = $"Использование CPU: {cpuUsage:F2}%";
                            lblMemory.Text = $"Использование RAM: {memoryUsageMB:F2} МБ";
                            lblDiskIO.Text = $"Дисковые операции: Чтение {diskReadKBs:F2} КБ/с, Запись {diskWriteKBs:F2} КБ/с";
                        }));
                    }
                    catch (Exception ex)
                    {
                        // Процесс мог завершиться во время сбора данных
                        if (!process.HasExited)
                        {
                            BeginInvoke(new Action(() =>
                            {
                                lblStatus.Text = $"Ошибка при сборе данных: {ex.Message}";
                            }));
                        }
                        break;
                    }
                }

                // Process completed or monitoring stopped
                BeginInvoke(new Action(() =>
                {
                    lblStatus.Text = "Анализ завершен";
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

                        lblCpu.Text = $"Среднее использование CPU: {avgCpu:F2}%, Макс: {maxCpu:F2}%";
                        lblMemory.Text = $"Среднее использование RAM: {avgMem:F2} МБ, Макс: {maxMem:F2} МБ";
                        lblDiskIO.Text = $"Среднее дисковых операций: Чтение {avgDiskRead:F2} КБ/с, Запись {avgDiskWrite:F2} КБ/с";
                    }
                }));
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() =>
                {
                    lblStatus.Text = $"Ошибка мониторинга: {ex.Message}";
                    ResetUI();
                }));
            }
        }

        /// Получение загрузки CPU для процесса через WMI
        private double GetCpuUsageForProcess(int processId)
        {
            try
            {
                // Используем WMI для получения использования CPU
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
                // Резервный метод, если WMI не работает
                try
                {
                    Process p = Process.GetProcessById(processId);
                    return p.TotalProcessorTime.TotalMilliseconds /
                           (Environment.ProcessorCount * 10.0 * (DateTime.Now - p.StartTime).TotalMilliseconds);
                }
                catch
                {
                    // Если все методы не сработали, возвращаем 0
                }
            }
            return 0;
        }

        /// Получение статистики дисковых операций для процесса через WMI
        /// возвращает Кортеж (чтение, запись) в байтах/сек или null если не удалось получить данные
        private Tuple<double, double> GetIOStatsForProcess(int processId)
        {
            try
            {
                // Запрос WMI для получения данных о дисковых операциях
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
                // Если WMI не работает, возвращаем null
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

        /// Выполнение базового статического анализа файла
        private void PerformBasicStaticAnalysis(string filePath)
        {
            try
            {
                _staticAnalysisOutput.Clear();
                FileInfo fileInfo = new FileInfo(filePath);

                // Получение базовой информации о файле
                long fileSizeBytes = fileInfo.Length;
                string fileExtension = fileInfo.Extension.ToLower();

                // Вывод базовой информации
                _staticAnalysisOutput.AppendLine($"=== БАЗОВАЯ ИНФОРМАЦИЯ О ФАЙЛЕ ===");
                _staticAnalysisOutput.AppendLine($"Полный путь: {filePath}");
                _staticAnalysisOutput.AppendLine($"Размер: {FormatFileSize(fileSizeBytes)}");
                _staticAnalysisOutput.AppendLine($"Тип файла: {fileExtension}");
                _staticAnalysisOutput.AppendLine($"Дата создания: {fileInfo.CreationTime}");
                _staticAnalysisOutput.AppendLine($"Дата последнего изменения: {fileInfo.LastWriteTime}");
                _staticAnalysisOutput.AppendLine();

                // Выполнение анализа PE-заголовка для exe и dll файлов
                if (fileExtension == ".exe" || fileExtension == ".dll")
                {
                    AnalyzePEFile(filePath);
                }
                else
                {
                    _staticAnalysisOutput.AppendLine("Детальный анализ доступен только для .exe и .dll файлов.");
                }

                // Обновление интерфейса пользователя
                BeginInvoke(new Action(() =>
                {
                    lblFileSize.Text = $"Размер файла: {FormatFileSize(fileSizeBytes)}";
                    lblFileType.Text = $"Тип файла: {fileExtension.ToUpper()}";
                    rtbResults.Text = _staticAnalysisOutput.ToString();
                }));
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() =>
                {
                    _staticAnalysisOutput.AppendLine($"Ошибка при выполнении статического анализа: {ex.Message}");
                    rtbResults.Text = _staticAnalysisOutput.ToString();
                }));
            }
        }

        /// Анализ PE (Portable Executable) файла
        private void AnalyzePEFile(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    // Чтение первых 4KB файла для анализа заголовков
                    byte[] buffer = new byte[4096]; // Более чем достаточно для DOS и PE заголовков

                    // Чтение начала файла
                    fs.Read(buffer, 0, buffer.Length);

                    // Проверка сигнатуры MZ (DOS-заголовок)
                    if (buffer[0] != 'M' || buffer[1] != 'Z')
                    {
                        _staticAnalysisOutput.AppendLine("Файл не является корректным PE-файлом (отсутствует сигнатура MZ).");
                        return;
                    }

                    // Получение смещения PE-заголовка
                    int peOffset = BitConverter.ToInt32(buffer, 0x3C);

                    // Проверка сигнатуры PE
                    if (peOffset >= buffer.Length - 4 ||
                        buffer[peOffset] != 'P' ||
                        buffer[peOffset + 1] != 'E' ||
                        buffer[peOffset + 2] != 0 ||
                        buffer[peOffset + 3] != 0)
                    {
                        _staticAnalysisOutput.AppendLine("Файл не является корректным PE-файлом (отсутствует сигнатура PE).");
                        return;
                    }

                    // Получение типа машины (архитектуры)
                    ushort machineType = BitConverter.ToUInt16(buffer, peOffset + 4);
                    string architecture;
                    switch (machineType)
                    {
                        case 0x014c: architecture = "x86 (32-bit)"; break;
                        case 0x8664: architecture = "x64 (64-bit)"; break;
                        case 0x0200: architecture = "Intel Itanium"; break;
                        case 0x01c4: architecture = "ARM"; break;
                        case 0xAA64: architecture = "ARM64"; break;
                        default: architecture = $"Другая ({machineType:X4})"; break;
                    }

                    // Получение количества секций
                    ushort numSections = BitConverter.ToUInt16(buffer, peOffset + 6);

                    // Получение метки времени (время компиляции)
                    uint timestamp = BitConverter.ToUInt32(buffer, peOffset + 8);
                    DateTime compileTime = new DateTime(1970, 1, 1).AddSeconds(timestamp);

                    // Получение характеристик файла
                    ushort characteristics = BitConverter.ToUInt16(buffer, peOffset + 22);
                    bool isDll = (characteristics & 0x2000) != 0;
                    bool isExecutableFile = (characteristics & 0x0002) != 0;
                    bool isSystem = (characteristics & 0x1000) != 0;

                    
                    ushort optionalHeaderSize = BitConverter.ToUInt16(buffer, peOffset + 20);
                    ushort optionalHeaderMagic = BitConverter.ToUInt16(buffer, peOffset + 24);
                    bool isPE32Plus = optionalHeaderMagic == 0x20b; // PE32+ (64-bit)

                    // Вывод информации о заголовке PE
                    _staticAnalysisOutput.AppendLine($"=== АНАЛИЗ PE-ЗАГОЛОВКА ===");
                    _staticAnalysisOutput.AppendLine($"Архитектура: {architecture}");
                    _staticAnalysisOutput.AppendLine($"Количество секций: {numSections}");
                    _staticAnalysisOutput.AppendLine($"Время компиляции: {compileTime}");
                    _staticAnalysisOutput.AppendLine($"Тип файла: {(isDll ? "Динамическая библиотека (DLL)" : "Исполняемый файл (EXE)")}");
                    _staticAnalysisOutput.AppendLine($"Системный файл: {(isSystem ? "Да" : "Нет")}");
                    _staticAnalysisOutput.AppendLine($"Исполняемый файл: {(isExecutableFile ? "Да" : "Нет")}");
                    _staticAnalysisOutput.AppendLine($"PE32+: {(isPE32Plus ? "Да (64-bit)" : "Нет (32-bit)")}");
                    _staticAnalysisOutput.AppendLine();

                    // Рассчитать смещение к таблице секций
                    int sectionTableOffset = peOffset + 24 + optionalHeaderSize;

                    // Получить подсистему
                    ushort subsystem = 0;
                    if (optionalHeaderSize >= 68) // PE32
                    {
                        subsystem = BitConverter.ToUInt16(buffer, peOffset + 24 + 68);
                    }

                    string subsystemName = "Неизвестно";
                    switch (subsystem)
                    {
                        case 1: subsystemName = "Драйвер устройства"; break;
                        case 2: subsystemName = "Windows GUI"; break;
                        case 3: subsystemName = "Windows CUI (консоль)"; break;
                        case 5: subsystemName = "OS/2 CUI"; break;
                        case 7: subsystemName = "POSIX CUI"; break;
                        case 8: subsystemName = "Нативное окно"; break;
                        case 11: subsystemName = "Windows CE GUI"; break;
                        case 14: subsystemName = "Xbox"; break;
                    }
                    _staticAnalysisOutput.AppendLine($"Подсистема: {subsystemName} ({subsystem})");

                    // Попытаться получить информацию о каталоге импорта
                    try
                    {
                        int importDirOffset;
                        if (isPE32Plus)
                        {
                            // В PE32+ (64-бит) каталог импорта находится по другому смещению
                            importDirOffset = peOffset + 24 + 112;
                        }
                        else
                        {
                            // В PE32 (32-бит) каталог импорта находится по смещению 96
                            importDirOffset = peOffset + 24 + 96;
                        }

                        // Каталог импорта - это первый элемент каталога данных
                        uint importDirRVA = BitConverter.ToUInt32(buffer, importDirOffset);
                        uint importDirSize = BitConverter.ToUInt32(buffer, importDirOffset + 4);

                        if (importDirRVA != 0 && importDirSize != 0)
                        {
                            _staticAnalysisOutput.AppendLine($"Импорт: RVA=0x{importDirRVA:X8}, Размер={importDirSize}");
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки разбора каталога импорта
                    }

                    // Read sections
                    _staticAnalysisOutput.AppendLine();
                    _staticAnalysisOutput.AppendLine("=== СЕКЦИИ ===");

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

                        // Информация о выходной секции
                        _staticAnalysisOutput.AppendLine($"Секция: {sectionName}");
                        _staticAnalysisOutput.AppendLine($"  Виртуальный размер: {FormatFileSize(virtualSize)}");
                        _staticAnalysisOutput.AppendLine($"  Адрес: 0x{virtualAddress:X8}");
                        _staticAnalysisOutput.AppendLine($"  Размер данных: {FormatFileSize(rawDataSize)}");
                        _staticAnalysisOutput.AppendLine($"  Тип: {(isCode ? "Код" : "")} {(isInitializedData ? "Инициализированные данные" : "")} {(isUninitializedData ? "Неинициализированные данные" : "")}");
                        _staticAnalysisOutput.AppendLine($"  Права: {(isReadable ? "R" : "-")}{(isWritable ? "W" : "-")}{(isSectionExecutable ? "X" : "-")}");
                        _staticAnalysisOutput.AppendLine();
                    }

                    // Вывод оценок использования ресурсов
                    _staticAnalysisOutput.AppendLine("=== ОЦЕНКА РЕСУРСОВ ===");
                    _staticAnalysisOutput.AppendLine($"Размер кода: {FormatFileSize(codeSize)}");
                    _staticAnalysisOutput.AppendLine($"Размер данных: {FormatFileSize(dataSize)}");
                    if (resourceSize > 0)
                    {
                        _staticAnalysisOutput.AppendLine($"Размер ресурсов: {FormatFileSize(resourceSize)}");
                    }

                    // Простая схема эвристики для определения потребностей в ресурсах
                    int totalSize = codeSize + dataSize + resourceSize;
                    string cpuEstimate = "Низкие";
                    string ramEstimate = "Низкие";

                    if (totalSize > 10 * 1024 * 1024) // > 10 MB
                    {
                        cpuEstimate = "Высокие";
                        ramEstimate = "Высокие";
                    }
                    else if (totalSize > 1 * 1024 * 1024) // > 1 MB
                    {
                        cpuEstimate = "Средние";
                        ramEstimate = "Средние";
                    }

                    _staticAnalysisOutput.AppendLine();
                    _staticAnalysisOutput.AppendLine("=== ПРОГНОЗ ТРЕБОВАНИЙ ===");
                    _staticAnalysisOutput.AppendLine($"Оценка CPU: {cpuEstimate}");
                    _staticAnalysisOutput.AppendLine($"Оценка RAM: {ramEstimate}");
                }

                // Пробуем проанализировать импорт с помощью dumpbin
                AnalyzeImports(filePath);
            }
            catch (Exception ex)
            {
                _staticAnalysisOutput.AppendLine($"Ошибка при анализе PE-файла: {ex.Message}");
            }
        }

        private void AnalyzeImports(string filePath)
        {
            try
            {
                List<string> imports = new List<string>();
                List<string> exports = new List<string>();

                // Первая попытка с dumpbin
                bool usedDumpBin = TryAnalyzeWithDumpBin(filePath, imports, exports);

                if (!usedDumpBin)
                {
                    // Если dumpbin не работает или недоступен, используем базовое сканирование двоичных файлов
                    ScanFileForImports(filePath, imports);
                }

                _staticAnalysisOutput.AppendLine();
                _staticAnalysisOutput.AppendLine("=== ИМПОРТИРУЕМЫЕ БИБЛИОТЕКИ ===");
                if (imports.Count > 0)
                {
                    foreach (var import in imports)
                    {
                        _staticAnalysisOutput.AppendLine($"- {import}");
                    }

                    // Определяем потенциальное использование ресурсов на основе импортированных DLL
                    DetectResourceUsageFromImports(imports);
                }
                else
                {
                    _staticAnalysisOutput.AppendLine("Не удалось определить импортируемые библиотеки");
                }

                // Обновление метки зависимостей
                BeginInvoke(new Action(() =>
                {
                    lblDependencies.Text = $"Зависимости: {imports.Count} библиотек";
                }));

                if (exports.Count > 0)
                {
                    _staticAnalysisOutput.AppendLine();
                    _staticAnalysisOutput.AppendLine("=== ЭКСПОРТИРУЕМЫЕ ФУНКЦИИ ===");
                    foreach (var export in exports.Take(20)) // Ограничение до первых 20
                    {
                        _staticAnalysisOutput.AppendLine($"- {export}");
                    }

                    if (exports.Count > 20)
                    {
                        _staticAnalysisOutput.AppendLine($"... и еще {exports.Count - 20} функций");
                    }
                }
            }
            catch (Exception ex)
            {
                _staticAnalysisOutput.AppendLine($"Ошибка при анализе импортов: {ex.Message}");
            }
        }

        private bool TryAnalyzeWithDumpBin(string filePath, List<string> imports, List<string> exports)
        {
            try
            {
                // пробуем использовать dumpbin для анализа импорта
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
                        // Разбор импортированных DLL
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

                        // Также попытаемся получить экспорт
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
                                    // Это экспортная строка
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
                // Dumpbin недоступен или не работает
            }

            return false;
        }

        private void ScanFileForImports(string filePath, List<string> imports)
        {
            try
            {
                // Очень простой сканер ссылок на DLL
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[4096];
                    StringBuilder sb = new StringBuilder();

                    while (fs.Read(buffer, 0, buffer.Length) > 0)
                    {
                        // Преобразование в строку и поиск ссылок на DLL
                        string chunk = Encoding.ASCII.GetString(buffer);
                        sb.Append(chunk);
                    }

                    // Ищем общие паттерны DLL
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
            _staticAnalysisOutput.AppendLine("=== ОПРЕДЕЛЕНИЕ ИСПОЛЬЗОВАНИЯ РЕСУРСОВ ===");

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

            if (usesGUI) _staticAnalysisOutput.AppendLine("- Графический интерфейс пользователя (GUI)");
            if (usesNetwork) _staticAnalysisOutput.AppendLine("- Сетевые операции");
            if (usesDatabase) _staticAnalysisOutput.AppendLine("- Работа с базами данных");
            if (usesMultimedia) _staticAnalysisOutput.AppendLine("- Мультимедиа (аудио/видео)");
            if (uses3D) _staticAnalysisOutput.AppendLine("- 3D графика");
            if (usesHardwareAcceleration) _staticAnalysisOutput.AppendLine("- Аппаратное ускорение");

            if (!usesGUI && !usesNetwork && !usesDatabase && !usesMultimedia && !uses3D && !usesHardwareAcceleration)
            {
                _staticAnalysisOutput.AppendLine("- Не удалось определить специфические аппаратные требования");
            }
        }

        /// Выполнение глубокого анализа программного кода
        private void PerformDeepCodeAnalysis(string filePath)
        {
            StringBuilder disassemblyOutput = new StringBuilder();
            disassemblyOutput.AppendLine("=== ДИЗАССЕМБЛИРОВАННЫЙ КОД ===");
            disassemblyOutput.AppendLine();

            try
            {
                // Сначала пытаемся использовать dumpbin для дизассемблирования
                if (!TryDisassembleWithDumpBin(filePath, disassemblyOutput))
                {
                    // Если dumpbin не сработал, пробуем objdump
                    if (!TryDisassembleWithObjDump(filePath, disassemblyOutput))
                    {
                        // Если все инструменты не сработали, выполняем базовый анализ шестнадцатеричного дампа
                        PerformHexDumpAnalysis(filePath, disassemblyOutput);
                    }
                }

                // Анализ шаблонов инструкций в дизассемблированном коде
                AnalyzeCodeComplexity(disassemblyOutput.ToString());

                // Обновление интерфейса с результатами дизассемблирования
                BeginInvoke(new Action(() =>
                {
                    rtbDisassembly.Text = disassemblyOutput.ToString();
                }));
            }
            catch (Exception ex)
            {
                disassemblyOutput.AppendLine($"Ошибка при выполнении глубокого анализа кода: {ex.Message}");

                BeginInvoke(new Action(() =>
                {
                    rtbDisassembly.Text = disassemblyOutput.ToString();
                }));
            }
        }

        /// Попытка дизассемблирования с помощью инструмента dumpbin
        /// возвращаем true если дизассемблирование успешно, иначе false
        private bool TryDisassembleWithDumpBin(string filePath, StringBuilder output)
        {
            try
            {
                using (var process = new Process())
                {
                    // Настройка параметров запуска dumpbin
                    process.StartInfo.FileName = "dumpbin.exe";
                    process.StartInfo.Arguments = $"/DISASM \"{filePath}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();

                    // Чтение вывода dumpbin частями для обработки больших файлов
                    using (StreamReader reader = process.StandardOutput)
                    {
                        // Отображение прогресса
                        BeginInvoke(new Action(() =>
                        {
                            progressBar.Value = 30;
                        }));

                        string line;
                        bool inCodeSection = false;
                        int lineCount = 0;

                        // Обработка вывода dumpbin построчно
                        while ((line = reader.ReadLine()) != null)
                        {
                            // Определяем начало секции кода
                            if (line.Contains("SECTION HEADER") && line.Contains("code"))
                            {
                                inCodeSection = true;
                                output.AppendLine(line);
                                output.AppendLine("-------------------------------------------");
                                continue;
                            }

                            // Если мы в секции кода, записываем каждую строку
                            if (inCodeSection)
                            {
                                if (line.Trim().Length > 0)
                                {
                                    output.AppendLine(line);
                                    lineCount++;

                                    // Ограничение на разумное количество строк
                                    if (lineCount > 1000)
                                    {
                                        output.AppendLine("...");
                                        output.AppendLine("(Вывод ограничен первыми 1000 строками)");
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    process.WaitForExit();
                    // Проверяем успешность выполнения и наличие вывода
                    return process.ExitCode == 0 && output.Length > 100;
                }
            }
            catch
            {
                // dumpbin недоступен или другая ошибка
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

                    // Считывание вывода кусками для обработки больших файлов
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

                                // Ограничение до разумного количества строк
                                if (lineCount > 1000)
                                {
                                    output.AppendLine("...");
                                    output.AppendLine("(Вывод ограничен первыми 1000 строками)");
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

                output.AppendLine("Примечание: Инструменты дизассемблирования недоступны.");
                output.AppendLine("Выполняется базовый анализ шестнадцатеричного дампа.");
                output.AppendLine();

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    // Находим текстовый раздел в PE-файле
                    byte[] buffer = new byte[4096];
                    fs.Read(buffer, 0, buffer.Length);

                    // Находим PE header
                    int peOffset = BitConverter.ToInt32(buffer, 0x3C);

                    // Проверяем, есть ли у нас действительный PE-файл
                    if (peOffset < buffer.Length - 4 &&
                        buffer[peOffset] == 'P' &&
                        buffer[peOffset + 1] == 'E' &&
                        buffer[peOffset + 2] == 0 &&
                        buffer[peOffset + 3] == 0)
                    {
                        // Получение количества секций
                        ushort numSections = BitConverter.ToUInt16(buffer, peOffset + 6);

                        // Получение размера необязательного заголовка
                        ushort optionalHeaderSize = BitConverter.ToUInt16(buffer, peOffset + 20);

                        // Рассчитывем смещение таблицы секций
                        int sectionTableOffset = peOffset + 24 + optionalHeaderSize;

                        // Ищем текстовый раздел
                        int textSectionRVA = 0;
                        int textSectionOffset = 0;
                        int textSectionSize = 0;

                        for (int i = 0; i < numSections && sectionTableOffset + i * 40 + 40 <= buffer.Length; i++)
                        {
                            int sectionOffset = sectionTableOffset + i * 40;

                            // Чтение имени раздела (8 байт)
                            string sectionName = Encoding.ASCII.GetString(buffer, sectionOffset, 8).TrimEnd('\0');

                            // Проверяем, является ли это разделом кода
                            if (sectionName == ".text" || sectionName == "CODE")
                            {
                                // Получение виртуального адреса секции (RVA)
                                textSectionRVA = BitConverter.ToInt32(buffer, sectionOffset + 12);

                                // Получение смещения файла раздела
                                textSectionOffset = BitConverter.ToInt32(buffer, sectionOffset + 20);

                                // Получение размера секции
                                textSectionSize = BitConverter.ToInt32(buffer, sectionOffset + 16);

                                break;
                            }
                        }

                        if (textSectionOffset > 0 && textSectionSize > 0)
                        {

                            // Мы нашли текстовый раздел, выгружаем его
                            output.AppendLine($"Text section found: RVA=0x{textSectionRVA:X8}, Offset=0x{textSectionOffset:X8}, Size={textSectionSize}");
                            output.AppendLine();

                            // Ограничить размер для анализа
                            int sizeToAnalyze = Math.Min(textSectionSize, 4096);

                            // Переход к текстовому разделу
                            fs.Seek(textSectionOffset, SeekOrigin.Begin);

                            // Прочитать текстовый раздел
                            byte[] textSection = new byte[sizeToAnalyze];
                            fs.Read(textSection, 0, sizeToAnalyze);

                            // Выполнение анализа паттернов инструкций
                            AnalyzeInstructionPatterns(textSection, output);
                        }
                        else
                        {
                            // Текстовый раздел не найден, просто дампим первые несколько КБ
                            output.AppendLine("Text section not found, dumping first 4KB");
                            output.AppendLine();

                            // Идти к началу
                            fs.Seek(0, SeekOrigin.Begin);

                            // Чтение первых 4 КБ
                            byte[] firstChunk = new byte[4096];
                            fs.Read(firstChunk, 0, 4096);

                            // Dump hex
                            DumpHex(firstChunk, output);
                        }
                    }
                    else
                    {
                        // Не файл PE, а просто дамп первых нескольких КБ.
                        output.AppendLine("Не является корректным PE-файлом, дамп первых 4КБ");
                        output.AppendLine();

                        // Dump hex
                        DumpHex(buffer, output);
                    }
                }
            }
            catch (Exception ex)
            {
                output.AppendLine($"Ошибка при анализе файла: {ex.Message}");
            }
        }

        private void AnalyzeInstructionPatterns(byte[] data, StringBuilder output)
        {
            // Это очень базовое сопоставление паттернов для обычных инструкций x86/x64
            output.AppendLine("Анализ шаблонов инструкций:");
            output.AppendLine();

            // Общие паттерны инструкций x86/x64
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

            // Подсчитаем количество вхождений каждого паттерна
            Dictionary<string, int> patternCounts = new Dictionary<string, int>();
            foreach (var pattern in patterns)
            {
                patternCounts[pattern.Key] = 0;
            }

            // Сканирование для поиска паттернов
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

            // Количество выходных паттернов
            foreach (var count in patternCounts)
            {
                output.AppendLine($"{count.Key}: {count.Value} раз");
            }

            // Подсчет функционально-подобных структур (PUSH EBP с последующим MOV EBP,ESP)
            int functionCount = 0;
            for (int i = 0; i < data.Length - 3; i++)
            {
                if (data[i] == 0x55 && i + 2 < data.Length && data[i + 1] == 0x8B && data[i + 2] == 0xEC)
                {
                    functionCount++;
                }
            }

            output.AppendLine($"Примерное количество функций: {functionCount}");

            // Оцениваем циклы, считая прыжки назад
            int loopCount = 0;
            for (int i = 0; i < data.Length - 6; i++)
            {
                // JL/JLE/JG/JGE с отрицательным смещением
                if ((data[i] == 0x7C || data[i] == 0x7E || data[i] == 0x7F || data[i] == 0x7D) &&
                    (sbyte)data[i + 1] < 0)
                {
                    loopCount++;
                }
            }

            output.AppendLine($"Примерное количество циклов: {loopCount}");

            // Оценка сложности на основе паттернов
            int complexity = functionCount * 2 + loopCount * 3 + patternCounts["CALL"] + patternCounts["JMP"];
            string complexityLevel;

            if (complexity > 500)
                complexityLevel = "Очень высокая";
            else if (complexity > 200)
                complexityLevel = "Высокая";
            else if (complexity > 100)
                complexityLevel = "Средняя";
            else if (complexity > 50)
                complexityLevel = "Низкая";
            else
                complexityLevel = "Очень низкая";

            output.AppendLine();
            output.AppendLine($"Оценка сложности кода: {complexityLevel} (score: {complexity})");
            output.AppendLine();

            // Выгружаем фактическую разборку или hex
            output.AppendLine("Дамп первых байтов кода:");
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

                // Ограничиваем output, чтобы он был управляемым
                if (i >= 256)
                {
                    output.AppendLine("...");
                    break;
                }
            }
        }

        /// Анализ сложности кода на основе дизассемблированного листинга
        private void AnalyzeCodeComplexity(string disassembly)
        {
            // Отображение прогресса
            BeginInvoke(new Action(() =>
            {
                progressBar.Value = 90;
            }));

            StringBuilder complexityAnalysis = new StringBuilder();
            complexityAnalysis.AppendLine();
            complexityAnalysis.AppendLine("=== АНАЛИЗ СЛОЖНОСТИ КОДА ===");

            try
            {
                // Подсчет типовых инструкций
                int callCount = CountOccurrences(disassembly, "call ");           // Вызовы функций
                int jmpCount = CountOccurrences(disassembly, "jmp ");             // Безусловные переходы
                int condJumpCount = CountOccurrences(disassembly, "je ") +        // Условные переходы
                                  CountOccurrences(disassembly, "jne ") +
                                  CountOccurrences(disassembly, "jg ") +
                                  CountOccurrences(disassembly, "jl ") +
                                  CountOccurrences(disassembly, "jge ") +
                                  CountOccurrences(disassembly, "jle ");
                int loopCount = CountOccurrences(disassembly, "loop");            // Циклы
                int movCount = CountOccurrences(disassembly, "mov ");             // Перемещение данных
                int pushPopCount = CountOccurrences(disassembly, "push ") +       // Операции со стеком
                                 CountOccurrences(disassembly, "pop ");

                // Определение функций по характерным инструкциям в прологе
                int functionCount = CountOccurrences(disassembly, "push ebp") +   // Стандартный пролог функции
                                  CountOccurrences(disassembly, "push rbp");

                // Операции с плавающей точкой
                int fpCount = CountOccurrences(disassembly, "fld") +              // Загрузка/сохранение FP
                            CountOccurrences(disassembly, "fst") +
                            CountOccurrences(disassembly, "fadd") +               // Арифметика FP
                            CountOccurrences(disassembly, "fmul") +
                            CountOccurrences(disassembly, "fdiv");

                // Векторные операции (SIMD)
                int simdCount = CountOccurrences(disassembly, "movaps") +         // Операции SSE
                              CountOccurrences(disassembly, "movups") +
                              CountOccurrences(disassembly, "movdqa") +           // Операции AVX
                              CountOccurrences(disassembly, "paddd") +
                              CountOccurrences(disassembly, "psubd") +
                              CountOccurrences(disassembly, "pmulld");

                // Вывод статистики
                complexityAnalysis.AppendLine($"Обнаруженные функции: {functionCount}");
                complexityAnalysis.AppendLine($"Вызовы функций (call): {callCount}");
                complexityAnalysis.AppendLine($"Безусловные переходы (jmp): {jmpCount}");
                complexityAnalysis.AppendLine($"Условные переходы (je, jne и т.д.): {condJumpCount}");
                complexityAnalysis.AppendLine($"Циклы (loop): {loopCount}");
                complexityAnalysis.AppendLine();

                complexityAnalysis.AppendLine("Характеристики кода:");
                complexityAnalysis.AppendLine($"- Операции перемещения данных (mov): {movCount}");
                complexityAnalysis.AppendLine($"- Операции со стеком (push/pop): {pushPopCount}");
                complexityAnalysis.AppendLine($"- Операции с плавающей точкой: {fpCount}");
                complexityAnalysis.AppendLine($"- SIMD/векторные операции: {simdCount}");
                complexityAnalysis.AppendLine();

                // Расчет метрик сложности
                int cyclomaticComplexity = 1 + condJumpCount;
                double branchingFactor = (double)(condJumpCount + jmpCount) / Math.Max(1, movCount);

                // Оценка использования CPU на основе состава инструкций
                int cpuComplexity = callCount * 5 + condJumpCount * 3 + fpCount * 4 + simdCount * 2;
                string cpuUsage;

                // Определение уровня нагрузки на CPU
                if (cpuComplexity > 1000)
                    cpuUsage = "Очень высокая";
                else if (cpuComplexity > 500)
                    cpuUsage = "Высокая";
                else if (cpuComplexity > 200)
                    cpuUsage = "Средняя";
                else if (cpuComplexity > 50)
                    cpuUsage = "Низкая";
                else
                    cpuUsage = "Очень низкая";

                complexityAnalysis.AppendLine("Метрики сложности:");
                complexityAnalysis.AppendLine($"- Цикломатическая сложность: {cyclomaticComplexity}");
                complexityAnalysis.AppendLine($"- Коэффициент ветвления: {branchingFactor:F2}");
                complexityAnalysis.AppendLine();

                complexityAnalysis.AppendLine("Оценка требований:");
                complexityAnalysis.AppendLine($"- Нагрузка на CPU: {cpuUsage}");

                // Дополнительные характеристики на основе обнаруженных инструкций
                if (fpCount > 100)
                    complexityAnalysis.AppendLine("- Высокая вычислительная нагрузка (операции с плавающей точкой)");

                if (simdCount > 50)
                    complexityAnalysis.AppendLine("- Использование векторных вычислений (SIMD)");

                // Обновление UI
                BeginInvoke(new Action(() =>
                {
                    rtbDisassembly.Text += complexityAnalysis.ToString();
                    progressBar.Value = 100;
                }));
            }
            catch (Exception ex)
            {
                complexityAnalysis.AppendLine($"Ошибка при анализе сложности: {ex.Message}");

                BeginInvoke(new Action(() =>
                {
                    rtbDisassembly.Text += complexityAnalysis.ToString();
                    progressBar.Value = 100;
                }));
            }
        }

        /// Подсчет количества вхождений паттерна в тексте
        /// text:Текст для поиска
        /// pattern:Искомый паттерн
        /// возвращаем количество найденных вхождений
        private int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;

            // Поиск всех вхождений паттерна в тексте без учета регистра
            while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += pattern.Length;
            }

            return count;
        }

        /// Сохранение отчета с результатами анализа
        private void SaveReport(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // Заголовок отчета
                    writer.WriteLine("=== ДЕТАЛЬНЫЙ АНАЛИЗ ПРОГРАММНОГО ПРОДУКТА ===");
                    writer.WriteLine($"Файл: {_selectedExecutablePath}");
                    writer.WriteLine($"Дата анализа: {DateTime.Now}");
                    writer.WriteLine();

                    // Раздел со статическим анализом
                    writer.WriteLine("--- СТАТИЧЕСКИЙ АНАЛИЗ ---");
                    writer.WriteLine(rtbResults.Text);
                    writer.WriteLine();

                    // Раздел с дизассемблированным кодом
                    writer.WriteLine("--- ДИЗАССЕМБЛИРОВАННЫЙ КОД И АНАЛИЗ СЛОЖНОСТИ ---");
                    writer.WriteLine(rtbDisassembly.Text);
                    writer.WriteLine();

                    // Раздел с динамическим анализом
                    writer.WriteLine("--- ДИНАМИЧЕСКИЙ АНАЛИЗ ---");
                    if (_resourceSnapshots.Count > 0)
                    {
                        // Расчет средних и максимальных значений
                        double avgCpu = _resourceSnapshots.Average(s => s.CpuUsage);
                        double maxCpu = _resourceSnapshots.Max(s => s.CpuUsage);
                        double avgMem = _resourceSnapshots.Average(s => s.MemoryUsageMB);
                        double maxMem = _resourceSnapshots.Max(s => s.MemoryUsageMB);
                        double avgDiskRead = _resourceSnapshots.Average(s => s.DiskReadKBs);
                        double avgDiskWrite = _resourceSnapshots.Average(s => s.DiskWriteKBs);

                        writer.WriteLine($"Продолжительность анализа: {_resourceSnapshots.Count} секунд");
                        writer.WriteLine($"Среднее использование CPU: {avgCpu:F2}%, Максимум: {maxCpu:F2}%");
                        writer.WriteLine($"Среднее использование RAM: {avgMem:F2} МБ, Максимум: {maxMem:F2} МБ");
                        writer.WriteLine($"Среднее дисковых операций: Чтение {avgDiskRead:F2} КБ/с, Запись {avgDiskWrite:F2} КБ/с");
                        writer.WriteLine();

                        writer.WriteLine("Детальные измерения (по секундам):");
                        writer.WriteLine("Время\tCPU(%)\tRAM(МБ)\tЧтение(КБ/с)\tЗапись(КБ/с)");

                        foreach (var snapshot in _resourceSnapshots)
                        {
                            writer.WriteLine($"{snapshot.Timestamp.ToString("HH:mm:ss")}\t{snapshot.CpuUsage:F2}\t{snapshot.MemoryUsageMB:F2}\t{snapshot.DiskReadKBs:F2}\t{snapshot.DiskWriteKBs:F2}");
                        }
                    }
                    else
                    {
                        writer.WriteLine("Динамический анализ не был выполнен или не содержит данных.");
                    }

                    writer.WriteLine();
                    writer.WriteLine("=== КОНЕЦ ОТЧЕТА ===");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении отчета: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
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

        // Частные поля для элементов управления пользовательского интерфейса
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

    /// Класс для хранения данных о потреблении ресурсов в конкретный момент времени
    public class ResourceSnapshot
    {
        /// Время снятия показаний
        public DateTime Timestamp { get; set; }

        /// Использование процессора в процентах
        public double CpuUsage { get; set; }

        /// Использование оперативной памяти в МБ
        public double MemoryUsageMB { get; set; }

        /// Скорость чтения с диска в КБ/с
        public double DiskReadKBs { get; set; }

        /// Скорость записи на диск в КБ/с
        public double DiskWriteKBs { get; set; }
    }

    /// Основной класс программы
    static class Program
    {
        /// Главная точка входа в приложение
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}