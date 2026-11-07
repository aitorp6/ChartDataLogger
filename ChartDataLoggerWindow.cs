#region Using declarations
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.AddOns.ChartDataLogger
{
    public class ChartDataLoggerWindow : NTWindow, IWorkspacePersistence
    {
        private ComboBox   cbInstrument;
        private ComboBox   cbIntervalType;
        private TextBox    tbIntervalValue;
        private ComboBox   cbRangePreset;
        private DatePicker dpFrom;
        private DatePicker dpTo;

        private TextBox    tbOutputPath;
        private TextBox    tbFilenameBase;
        private CheckBox   cbChronological;
        private CheckBox   cbIncludeMs;

        private Button      bExport;
        private Button      bOpenFolder;
        private ProgressBar pbProgress;
        private Label       lStatus;
        private TextBox     tbConsole;

        private BarsRequest activeBarsRequest;
        private bool isBusy = false;

        public ChartDataLoggerWindow()
        {
            Caption   = "📊 Chart Data Logger (Add-On)";
            Width     = 860;
            Height    = 600;
            MinWidth  = 740;
            MinHeight = 480;

            Content = BuildContent();

            Loaded += (o, e) =>
            {
                if (WorkspaceOptions == null)
                    WorkspaceOptions = new WorkspaceOptions("ChartDataLoggerAddOn_" + Guid.NewGuid().ToString("N"), this);

                PopulateInstrumentSuggestions();

                Log("════════════════════════════════════════════════════════════════════════");
                Log("📊 Chart Data Logger v2.1 (Add-On) ready.");
                Log(" • Mode: Direct historical OHLCV data download via BarsRequest.");
                Log(" • Standalone: No open chart windows required and zero UI overhead.");
                Log(" • Output: Strict chronological CSV export.");
                Log("════════════════════════════════════════════════════════════════════════");
            };

            Closing += (o, e) =>
            {
                CleanupBarsRequest();
            };
        }

        public WorkspaceOptions WorkspaceOptions { get; set; }
        public void Restore(XDocument document, XElement element) { }
        public void Save(XDocument document, XElement element) { }

        private DependencyObject BuildContent()
        {
            double margin = 8;

            // Main container with clean light theme
            Grid mainGrid = new Grid
            {
                Margin     = new Thickness(margin * 1.5),
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)) // Slate 50
            };

            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Settings (Data + File)
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Actions & Progress
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Splitter
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star), MinHeight = 120 }); // 4: Console

            // ── 0. Header ────────────────────────────────────────────────────────
            StackPanel headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, margin) };
            headerPanel.Children.Add(new TextBlock
            {
                Text       = "📊 Chart Data Logger",
                FontSize   = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 138)), // Institutional navy blue
                Margin     = new Thickness(0, 0, 0, 2)
            });
            headerPanel.Children.Add(new TextBlock
            {
                Text       = "Direct historical OHLCV bar download to high-precision CSV format",
                FontSize   = 12.5,
                Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)) // Slate 600
            });
            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);

            // ── 1. Settings (2 Columns) ──────────────────────────────────────────
            Grid topGrid = new Grid { Margin = new Thickness(0, 0, 0, margin) };
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.05, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.95, GridUnitType.Star) });

            // ── 1A. Left Column: Instrument, Timeframe & Date Range ──────────────
            GroupBox gbData = new GroupBox
            {
                Header      = " 🌐 1. Instrument, Timeframe & Date Range ",
                FontWeight  = FontWeights.SemiBold,
                Foreground  = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Slate 900
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)), // Slate 300
                Background  = new SolidColorBrush(Colors.White),
                Margin      = new Thickness(0, 0, margin / 2, 0),
                Padding     = new Thickness(6)
            };
            StackPanel spData = new StackPanel { Margin = new Thickness(margin) };

            // Instrument + Bar Interval Row
            Grid instGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            instGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
            instGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            instGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.6, GridUnitType.Star) });

            StackPanel spInst = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            spInst.Children.Add(CreateFieldLabel("Instrument (e.g. NQ 12-26 or NQ):"));
            cbInstrument = new ComboBox
            {
                IsEditable      = true,
                Text            = "NQ 12-26",
                Height          = 27,
                Background      = new SolidColorBrush(Colors.White),
                Foreground      = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(203, 213, 225))
            };
            spInst.Children.Add(cbInstrument);
            Grid.SetColumn(spInst, 0);
            instGrid.Children.Add(spInst);

            StackPanel spType = new StackPanel { Margin = new Thickness(4, 0, 4, 0) };
            spType.Children.Add(CreateFieldLabel("Bar Type:"));
            cbIntervalType = new ComboBox
            {
                Height          = 27,
                Background      = new SolidColorBrush(Colors.White),
                Foreground      = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(203, 213, 225))
            };
            cbIntervalType.Items.Add("Minute");
            cbIntervalType.Items.Add("Second");
            cbIntervalType.Items.Add("Tick");
            cbIntervalType.Items.Add("Day");
            cbIntervalType.SelectedIndex = 0;
            spType.Children.Add(cbIntervalType);
            Grid.SetColumn(spType, 1);
            instGrid.Children.Add(spType);

            StackPanel spVal = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
            spVal.Children.Add(CreateFieldLabel("Value:"));
            tbIntervalValue = CreateStyledTextBox("1");
            spVal.Children.Add(tbIntervalValue);
            Grid.SetColumn(spVal, 2);
            instGrid.Children.Add(spVal);

            spData.Children.Add(instGrid);

            // Date Range Row (Quick Range + Start Date + End Date)
            Grid dateGrid = new Grid();
            dateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
            dateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            dateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

            StackPanel spPreset = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            spPreset.Children.Add(CreateFieldLabel("Quick Range:"));
            cbRangePreset = new ComboBox
            {
                Height          = 27,
                Background      = new SolidColorBrush(Colors.White),
                Foreground      = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(203, 213, 225))
            };
            cbRangePreset.Items.Add("Last 7 days");
            cbRangePreset.Items.Add("Last 30 days");
            cbRangePreset.Items.Add("Last 60 days");
            cbRangePreset.Items.Add("Last 90 days");
            cbRangePreset.Items.Add("Last 6 months");
            cbRangePreset.Items.Add("Last 1 year");
            cbRangePreset.Items.Add("Custom");
            cbRangePreset.SelectedIndex = 1;
            cbRangePreset.SelectionChanged += OnRangePresetChanged;
            spPreset.Children.Add(cbRangePreset);
            Grid.SetColumn(spPreset, 0);
            dateGrid.Children.Add(spPreset);

            StackPanel spFrom = new StackPanel { Margin = new Thickness(4, 0, 4, 0) };
            spFrom.Children.Add(CreateFieldLabel("Start Date:"));
            dpFrom = new DatePicker
            {
                SelectedDate    = DateTime.Today.AddDays(-30),
                Height          = 27,
                BorderBrush     = new SolidColorBrush(Color.FromRgb(203, 213, 225))
            };
            spFrom.Children.Add(dpFrom);
            Grid.SetColumn(spFrom, 1);
            dateGrid.Children.Add(spFrom);

            StackPanel spTo = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
            spTo.Children.Add(CreateFieldLabel("End Date:"));
            dpTo = new DatePicker
            {
                SelectedDate    = DateTime.Today,
                Height          = 27,
                BorderBrush     = new SolidColorBrush(Color.FromRgb(203, 213, 225))
            };
            spTo.Children.Add(dpTo);
            Grid.SetColumn(spTo, 2);
            dateGrid.Children.Add(spTo);

            spData.Children.Add(dateGrid);
            gbData.Content = spData;
            Grid.SetColumn(gbData, 0);
            topGrid.Children.Add(gbData);

            // ── 1B. Right Column: Output File & Format Options ───────────────────
            GroupBox gbFile = new GroupBox
            {
                Header      = " 💾 2. Output File & Format Options ",
                FontWeight  = FontWeights.SemiBold,
                Foreground  = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                Background  = new SolidColorBrush(Colors.White),
                Margin      = new Thickness(margin / 2, 0, 0, 0),
                Padding     = new Thickness(6)
            };
            StackPanel spFile = new StackPanel { Margin = new Thickness(margin) };

            spFile.Children.Add(CreateFieldLabel("Output Directory:"));
            string defaultExportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NinjaTrader 8", "Export");
            tbOutputPath = CreateStyledTextBox(defaultExportDir);
            tbOutputPath.Margin = new Thickness(0, 0, 0, 8);
            spFile.Children.Add(tbOutputPath);

            spFile.Children.Add(CreateFieldLabel("Base Filename:"));
            tbFilenameBase = CreateStyledTextBox("PriceData");
            tbFilenameBase.Margin = new Thickness(0, 0, 0, 10);
            spFile.Children.Add(tbFilenameBase);

            StackPanel checksRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
            cbChronological = new CheckBox
            {
                Content    = "Chronological Order (Oldest → Newest)",
                IsChecked  = true,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                FontWeight = FontWeights.Medium,
                Margin     = new Thickness(0, 0, 20, 0)
            };
            cbIncludeMs = new CheckBox
            {
                Content    = "Include Milliseconds (.fff)",
                IsChecked  = false,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                FontWeight = FontWeights.Medium
            };
            checksRow.Children.Add(cbChronological);
            checksRow.Children.Add(cbIncludeMs);
            spFile.Children.Add(checksRow);

            gbFile.Content = spFile;
            Grid.SetColumn(gbFile, 1);
            topGrid.Children.Add(gbFile);

            Grid.SetRow(topGrid, 1);
            mainGrid.Children.Add(topGrid);

            // ── 2. Action & Progress Bar ─────────────────────────────────────────
            Grid actionGrid = new Grid { Margin = new Thickness(0, 4, 0, margin) };
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            bExport = new Button
            {
                Content         = "🚀 Download & Export CSV",
                Width           = 225,
                Height          = 36,
                FontWeight      = FontWeights.Bold,
                FontSize        = 13.5,
                Background      = new SolidColorBrush(Color.FromRgb(37, 99, 235)), // Vibrant Blue
                Foreground      = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Margin          = new Thickness(0, 0, 10, 0),
                Cursor          = System.Windows.Input.Cursors.Hand
            };
            bExport.Click += OnExportClick;
            Grid.SetColumn(bExport, 0);
            actionGrid.Children.Add(bExport);

            bOpenFolder = new Button
            {
                Content         = "📂 Open Folder",
                Width           = 135,
                Height          = 36,
                FontWeight      = FontWeights.SemiBold,
                Background      = new SolidColorBrush(Colors.White),
                Foreground      = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(0, 0, 14, 0),
                Cursor          = System.Windows.Input.Cursors.Hand
            };
            bOpenFolder.Click += OnOpenFolderClick;
            Grid.SetColumn(bOpenFolder, 1);
            actionGrid.Children.Add(bOpenFolder);

            StackPanel spProg = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            lStatus = new Label
            {
                Content    = "Status: Ready to download.",
                Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                FontWeight = FontWeights.Medium,
                Padding    = new Thickness(0, 0, 0, 3)
            };
            pbProgress = new ProgressBar
            {
                Height          = 10,
                Minimum         = 0,
                Maximum         = 100,
                Value           = 0,
                Background      = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Foreground      = new SolidColorBrush(Color.FromRgb(22, 163, 74)), // Success Green
                BorderThickness = new Thickness(0)
            };
            spProg.Children.Add(lStatus);
            spProg.Children.Add(pbProgress);
            Grid.SetColumn(spProg, 2);
            actionGrid.Children.Add(spProg);

            Grid.SetRow(actionGrid, 2);
            mainGrid.Children.Add(actionGrid);

            // ── 3. GridSplitter ──────────────────────────────────────────────────
            GridSplitter splitter = new GridSplitter
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment   = VerticalAlignment.Center,
                Height              = 6,
                Margin              = new Thickness(0, 2, 0, 2),
                Background          = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                ShowsPreview        = true,
                Cursor              = System.Windows.Input.Cursors.SizeNS
            };
            Grid.SetRow(splitter, 3);
            mainGrid.Children.Add(splitter);

            // ── 4. Console Log (White Background, Slate 900 Text) ────────────────
            tbConsole = new TextBox
            {
                IsReadOnly                    = true,
                Background                    = new SolidColorBrush(Colors.White),
                Foreground                    = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Slate 900
                FontFamily                    = new FontFamily("Consolas, Courier New"),
                FontSize                      = 12,
                Padding                       = new Thickness(10),
                VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderBrush                   = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness               = new Thickness(1)
            };
            Grid.SetRow(tbConsole, 4);
            mainGrid.Children.Add(tbConsole);

            return mainGrid;
        }

        private Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Content    = text,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)), // Slate 700
                Padding    = new Thickness(0, 0, 0, 3)
            };
        }

        private TextBox CreateStyledTextBox(string initialText)
        {
            return new TextBox
            {
                Text            = initialText,
                Height          = 27,
                Padding         = new Thickness(6, 4, 6, 4),
                Background      = new SolidColorBrush(Colors.White),
                Foreground      = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(1)
            };
        }

        private void PopulateInstrumentSuggestions()
        {
            try
            {
                HashSet<string> suggestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                string dbRoot = Path.Combine(Globals.UserDataDir, "db");
                foreach (string sub in new[] { "minute", "day", "tick" })
                {
                    string dir = Path.Combine(dbRoot, sub);
                    if (!Directory.Exists(dir)) continue;

                    foreach (string folder in Directory.GetDirectories(dir))
                    {
                        string name = Path.GetFileName(folder);
                        if (!string.IsNullOrWhiteSpace(name))
                            suggestions.Add(name);
                    }
                }

                var sorted = suggestions.OrderBy(s => s).ToList();
                string currentText = cbInstrument.Text;
                cbInstrument.Items.Clear();
                foreach (string item in sorted)
                    cbInstrument.Items.Add(item);

                if (sorted.Count > 0 && string.IsNullOrWhiteSpace(currentText))
                    cbInstrument.Text = sorted[0];
                else
                    cbInstrument.Text = currentText;
            }
            catch { }
        }

        private void OnRangePresetChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpFrom == null || dpTo == null) return;
            string preset = cbRangePreset.SelectedItem?.ToString() ?? "";
            DateTime today = DateTime.Today;

            if (preset.Contains("7 days"))
            {
                dpFrom.SelectedDate = today.AddDays(-7);
                dpTo.SelectedDate   = today;
            }
            else if (preset.Contains("30 days"))
            {
                dpFrom.SelectedDate = today.AddDays(-30);
                dpTo.SelectedDate   = today;
            }
            else if (preset.Contains("60 days"))
            {
                dpFrom.SelectedDate = today.AddDays(-60);
                dpTo.SelectedDate   = today;
            }
            else if (preset.Contains("90 days"))
            {
                dpFrom.SelectedDate = today.AddDays(-90);
                dpTo.SelectedDate   = today;
            }
            else if (preset.Contains("6 months"))
            {
                dpFrom.SelectedDate = today.AddMonths(-6);
                dpTo.SelectedDate   = today;
            }
            else if (preset.Contains("1 year"))
            {
                dpFrom.SelectedDate = today.AddYears(-1);
                dpTo.SelectedDate   = today;
            }
        }

        private void OnOpenFolderClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = tbOutputPath.Text.Trim();
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                Log("❌ Error opening folder: " + ex.Message);
            }
        }

        private Instrument ResolveInstrument(string inputName, out string resolvedNote)
        {
            resolvedNote = null;
            if (string.IsNullOrWhiteSpace(inputName))
                return null;

            inputName = inputName.Trim();

            // 1. Direct attempt
            try
            {
                Instrument direct = Instrument.GetInstrument(inputName);
                if (direct != null)
                {
                    resolvedNote = direct.FullName;
                    return direct;
                }
            }
            catch { }

            // 2. If user provided root (e.g. "NQ" or "ES"), look up the latest contract in db\minute
            try
            {
                string minDir = Path.Combine(Globals.UserDataDir, "db", "minute");
                if (Directory.Exists(minDir))
                {
                    var matchingFolders = Directory.GetDirectories(minDir)
                        .Select(Path.GetFileName)
                        .Where(name => name != null && name.StartsWith(inputName + " ", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(name => Directory.GetLastWriteTime(Path.Combine(minDir, name)))
                        .ToList();

                    foreach (string candidate in matchingFolders)
                    {
                        Instrument inst = Instrument.GetInstrument(candidate);
                        if (inst != null)
                        {
                            resolvedNote = $"{inst.FullName} (auto-resolved from '{inputName}')";
                            return inst;
                        }
                    }
                }
            }
            catch { }

            // 3. Continuous wildcard attempt
            try
            {
                Instrument cont = Instrument.GetInstrument(inputName + " ##-##");
                if (cont != null)
                {
                    resolvedNote = cont.FullName;
                    return cont;
                }
            }
            catch { }

            return null;
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            if (isBusy) return;

            string rawInstrument = cbInstrument.Text.Trim();
            string intervalTypeStr = cbIntervalType.SelectedItem?.ToString() ?? "Minute";
            if (!int.TryParse(tbIntervalValue.Text.Trim(), out int intervalValue) || intervalValue < 1)
            {
                Log("❌ Error: Bar interval value must be an integer >= 1.");
                return;
            }

            DateTime fromDate = dpFrom.SelectedDate ?? DateTime.Today.AddDays(-30);
            DateTime toDate   = dpTo.SelectedDate   ?? DateTime.Today;
            DateTime toDateEndOfDay = toDate.Date.AddDays(1).AddTicks(-1);

            if (fromDate > toDateEndOfDay)
            {
                Log("❌ Error: Start date cannot be after end date.");
                return;
            }

            string outputDir = tbOutputPath.Text.Trim();
            string fileBase  = tbFilenameBase.Text.Trim();
            if (string.IsNullOrWhiteSpace(fileBase))
                fileBase = "PriceData";

            bool chronological = cbChronological.IsChecked == true;
            bool includeMs     = cbIncludeMs.IsChecked == true;

            Instrument instrument = ResolveInstrument(rawInstrument, out string resolvedNote);
            if (instrument == null)
            {
                Log($"❌ Error: Instrument '{rawInstrument}' could not be found. Try entering the full contract name (e.g. 'NQ 12-26' or 'ES 12-26').");
                lStatus.Content = "Instrument not found.";
                return;
            }

            BarsPeriodType periodType = BarsPeriodType.Minute;
            Enum.TryParse(intervalTypeStr, true, out periodType);

            isBusy = true;
            bExport.IsEnabled = false;
            pbProgress.Value = 20;
            lStatus.Content = $"Requesting historical bars for {instrument.FullName}...";

            Log("────────────────────────────────────────────────────────────────────────");
            Log($"▶ Starting historical data request (BarsRequest):");
            Log($"  • Instrument:       {resolvedNote}");
            Log($"  • Timeframe:        {intervalValue} {periodType}");
            Log($"  • Requested Range:  {fromDate:yyyy-MM-dd} → {toDate:yyyy-MM-dd}");

            CleanupBarsRequest();

            try
            {
                // Direct exact range request (no warmup required)
                activeBarsRequest = new BarsRequest(instrument, fromDate.Date, toDateEndOfDay)
                {
                    BarsPeriod   = new BarsPeriod { BarsPeriodType = periodType, Value = intervalValue },
                    TradingHours = instrument.MasterInstrument.TradingHours
                };

                activeBarsRequest.Request((request, errorCode, errorMessage) =>
                {
                    OnBarsReceived(
                        request,
                        errorCode,
                        errorMessage,
                        instrument,
                        periodType,
                        intervalValue,
                        fromDate.Date,
                        toDateEndOfDay,
                        outputDir,
                        fileBase,
                        chronological,
                        includeMs);
                });
            }
            catch (Exception ex)
            {
                Log("❌ Exception while initiating BarsRequest: " + ex.Message);
                ResetBusyState("Request error.");
            }
        }

        private void OnBarsReceived(
            BarsRequest request,
            ErrorCode errorCode,
            string errorMessage,
            Instrument instrument,
            BarsPeriodType periodType,
            int intervalValue,
            DateTime fromDate,
            DateTime toDateEndOfDay,
            string outputDir,
            string fileBase,
            bool chronological,
            bool includeMs)
        {
            if (errorCode != ErrorCode.NoError)
            {
                Log($"❌ NinjaTrader error loading bars: {errorCode} - {errorMessage}");
                ResetBusyState($"Error: {errorCode}");
                return;
            }

            if (request?.Bars == null || request.Bars.Count == 0)
            {
                Log("⚠️ No bars returned for the selected instrument and date range.");
                ResetBusyState("No data available.");
                return;
            }

            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                Bars bars = request.Bars;
                int totalBarsLoaded = bars.Count;

                UpdateProgress(60, $"Bars loaded ({totalBarsLoaded:N0}). Writing CSV file...");
                Log($"✓ Bars loaded from NT8 engine: {totalBarsLoaded:N0} total bars");
                Log($"  • Oldest bar (index 0):            {bars.GetTime(0):yyyy-MM-dd HH:mm:ss}");
                Log($"  • Newest bar (index {totalBarsLoaded - 1}): {bars.GetTime(totalBarsLoaded - 1):yyyy-MM-dd HH:mm:ss}");

                // Ensure output directory exists
                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                string intervalLabel = GetBarIntervalString(periodType, intervalValue);
                string fileName = $"{instrument.MasterInstrument.Name}_{fileBase}_{intervalLabel}.csv";
                string fullFilePath = Path.Combine(outputDir, fileName);

                string timeFormat = includeMs ? "yyyy-MM-dd HH:mm:ss.fff" : "yyyy-MM-dd HH:mm:ss";

                int exportedCount = 0;
                DateTime firstExportedTime = DateTime.MinValue;
                DateTime lastExportedTime  = DateTime.MinValue;

                using (StreamWriter writer = new StreamWriter(fullFilePath, append: false, Encoding.UTF8, bufferSize: 65536))
                {
                    // Standard OHLCV header
                    writer.WriteLine("Time,Open,High,Low,Close,Volume");

                    StringBuilder lineSb = new StringBuilder(128);

                    if (chronological)
                    {
                        // 0 (oldest) -> totalBarsLoaded - 1 (newest)
                        for (int i = 0; i < totalBarsLoaded; i++)
                        {
                            DateTime barTime = bars.GetTime(i);
                            if (barTime < fromDate || barTime > toDateEndOfDay)
                                continue;

                            lineSb.Clear();
                            lineSb.Append(barTime.ToString(timeFormat, CultureInfo.InvariantCulture)).Append(',')
                                  .Append(bars.GetOpen(i).ToString(CultureInfo.InvariantCulture)).Append(',')
                                  .Append(bars.GetHigh(i).ToString(CultureInfo.InvariantCulture)).Append(',')
                                  .Append(bars.GetLow(i).ToString(CultureInfo.InvariantCulture)).Append(',')
                                  .Append(bars.GetClose(i).ToString(CultureInfo.InvariantCulture)).Append(',')
                                  .Append(bars.GetVolume(i).ToString(CultureInfo.InvariantCulture));

                            writer.WriteLine(lineSb.ToString());

                            if (exportedCount == 0)
                                firstExportedTime = barTime;
                            lastExportedTime = barTime;
                            exportedCount++;
                        }
                    }
                    else
                    {
                        // totalBarsLoaded - 1 (newest) -> 0 (oldest)
                        for (int i = totalBarsLoaded - 1; i >= 0; i--)
                        {
                            DateTime barTime = bars.GetTime(i);
                            if (barTime < fromDate || barTime > toDateEndOfDay)
                                continue;

                            lineSb.Clear();
                            lineSb.Append(barTime.ToString(timeFormat, CultureInfo.InvariantCulture)).Append(',')
                                  .Append(bars.GetOpen(i).ToString(CultureInfo.InvariantCulture)).Append(',')
                                  .Append(bars.GetHigh(i).ToString(CultureInfo.InvariantCulture)).Append(',')
                                  .Append(bars.GetLow(i).ToString(CultureInfo.InvariantCulture)).Append(',')
                                  .Append(bars.GetClose(i).ToString(CultureInfo.InvariantCulture)).Append(',')
                                  .Append(bars.GetVolume(i).ToString(CultureInfo.InvariantCulture));

                            writer.WriteLine(lineSb.ToString());

                            if (exportedCount == 0)
                                firstExportedTime = barTime;
                            lastExportedTime = barTime;
                            exportedCount++;
                        }
                    }
                }

                sw.Stop();
                UpdateProgress(100, $"Export completed! ({exportedCount:N0} rows in {sw.ElapsedMilliseconds} ms)");

                Log($"✅ EXPORT COMPLETED SUCCESSFULLY in {sw.ElapsedMilliseconds} ms:");
                Log($"  • File:          {fullFilePath}");
                Log($"  • Rows written:  {exportedCount:N0}");
                Log($"  • Columns:       6 (Time, Open, High, Low, Close, Volume)");
                if (exportedCount > 0)
                {
                    Log($"  • First CSV row: {firstExportedTime:yyyy-MM-dd HH:mm:ss}");
                    Log($"  • Last CSV row:  {lastExportedTime:yyyy-MM-dd HH:mm:ss}");
                }
                Log("────────────────────────────────────────────────────────────────────────");
            }
            catch (Exception ex)
            {
                Log("❌ Error during export: " + ex.Message);
                ResetBusyState("Error exporting CSV.");
                return;
            }
            finally
            {
                CleanupBarsRequest();
                Dispatcher.InvokeAsync(() =>
                {
                    isBusy = false;
                    bExport.IsEnabled = true;
                });
            }
        }

        private string GetBarIntervalString(BarsPeriodType periodType, int periodValue)
        {
            switch (periodType)
            {
                case BarsPeriodType.Minute: return $"{periodValue}min";
                case BarsPeriodType.Second: return $"{periodValue}sec";
                case BarsPeriodType.Tick:   return $"{periodValue}tick";
                case BarsPeriodType.Day:    return $"{periodValue}day";
                case BarsPeriodType.Month:  return $"{periodValue}month";
                default:                    return periodType.ToString();
            }
        }

        private void CleanupBarsRequest()
        {
            try
            {
                if (activeBarsRequest != null)
                {
                    activeBarsRequest.Dispose();
                    activeBarsRequest = null;
                }
            }
            catch { }
        }

        private void UpdateProgress(double value, string statusText)
        {
            Dispatcher.InvokeAsync(() =>
            {
                pbProgress.Value = value;
                lStatus.Content  = statusText;
            });
        }

        private void ResetBusyState(string statusText)
        {
            CleanupBarsRequest();
            Dispatcher.InvokeAsync(() =>
            {
                isBusy = false;
                bExport.IsEnabled = true;
                pbProgress.Value  = 0;
                lStatus.Content   = statusText;
            });
        }

        private void Log(string message)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (tbConsole == null) return;
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                tbConsole.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
                tbConsole.ScrollToEnd();
            });
        }
    }
}
