# Chart Data Logger for NinjaTrader 8 📊

[![NinjaTrader 8](https://img.shields.io/badge/NinjaTrader-8.0%20%7C%208.1-blue.svg)](https://ninjatrader.com)
[![Platform](https://img.shields.io/badge/Platform-Windows%20.NET%20Framework%204.8-lightgrey.svg)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A lightweight, high-performance **Add-On for NinjaTrader 8** designed to download and export historical **OHLCV market data** directly into clean, standardized CSV files without requiring open chart windows.

---

## ✨ Features

- **🚀 Standalone Headless Data Loading**: Uses NinjaTrader's native `BarsRequest` asynchronous API to request historical bars directly from connected providers or local cache. No open chart tabs, drawing overhead, or UI lag required.
- **⚡ Blazing Fast Streaming Export**: Uses buffered stream writing (`StreamWriter`) directly to disk. Exports 100,000+ bars in under 100 milliseconds with minimal memory footprint.
- **🌐 Smart Instrument Resolution**:
  - Auto-populates available instruments directly from your local NinjaTrader historical database (`db/minute`, `db/day`, `db/tick`).
  - Supports both full contract names (e.g., `NQ 12-26`, `ES 03-26`) and root tickers (e.g., entering `NQ` automatically resolves to the latest active contract in your database).
- **⏱️ Flexible Timeframes & Bar Types**:
  - Supported bar period types: **Minute**, **Second**, **Tick**, and **Day**.
  - Any custom value multiplier (e.g., 1-minute, 5-minute, 15-minute, 60-minute, 1-tick, etc.).
- **📅 Date Range Presets**:
  - One-click presets: *Last 7 days*, *Last 30 days*, *Last 60 days*, *Last 90 days*, *Last 6 months*, *Last 1 year*.
  - Full custom calendar date picker for exact historical date boundaries.
- **📑 Clean, Standardized CSV Output**:
  - `CultureInfo.InvariantCulture` formatting guarantees period (`.`) decimal separators regardless of your Windows locale.
  - Optional chronological order (*Oldest → Newest*) or reverse (*Newest → Oldest*).
  - Optional sub-second timestamp formatting (`yyyy-MM-dd HH:mm:ss.fff`).
  - Directly compatible with **Python (Pandas / Polars)**, **Excel**, **R**, **TradingView**, and institutional backtesting frameworks.
- **🎨 Modern UI**: Clean, responsive light theme with real-time progress bar, live execution logs, and quick directory access.

---

## 📂 CSV Format Specification

The exported CSV files strictly follow standard quantitative finance tabular formatting:

```csv
Time,Open,High,Low,Close,Volume
2026-10-15 09:30:00,19540.25,19552.5,19538.75,19548,2450
2026-10-15 09:31:00,19548.25,19560,19545.5,19557.25,1892
2026-10-15 09:32:00,19557.5,19565,19551.75,19561.5,1420
```

- **Filename convention**: `[Instrument]_[BaseFilename]_[Interval].csv` (e.g., `NQ_PriceData_1min.csv`).
- **Default export directory**: `Documents\NinjaTrader 8\Export\` (customizable from the UI).

---

## 🛠️ Installation

### Option 1: 1-Click Import via NinjaTrader (Recommended & Easiest)

1. Go to the **[Releases](../../releases)** section of this repository.
2. Download the pre-packaged **`ChartDataLogger.zip`** file.
3. In NinjaTrader 8, open the **Control Center** and click on:
   ```
   Tools → Import → NinjaScript Add-On...
   ```
4. Select the downloaded `ChartDataLogger.zip` file.
5. If prompted with a warning regarding trusted scripts, click **Yes**.
6. NinjaTrader will automatically unpack, install, and compile the Add-On. A confirmation pop-up will appear once completed.

---

### Option 2: Manual Installation (From Source)

1. Clone or download this repository.
2. Copy the `ChartDataLogger` folder into your NinjaTrader 8 custom AddOns directory:
   ```
   Documents\NinjaTrader 8\bin\Custom\AddOns\ChartDataLogger\
   ```
3. Folder structure should look like:
   ```text
   NinjaTrader 8\bin\Custom\AddOns\ChartDataLogger\
   ├── ChartDataLoggerAddOn.cs
   └── ChartDataLoggerWindow.cs
   ```
4. Open NinjaTrader 8:
   - If not compiled automatically, open the **NinjaScript Editor** (`New` → `NinjaScript Editor`) and press **F5** to compile.

---

## 🚀 How to Use

1. In the NinjaTrader 8 **Control Center**, click on the **`New`** menu.
2. Select **`📊 Chart Data Logger`**.
3. In the window:
   - **Instrument**: Select or type your instrument (e.g., `NQ 12-26`).
   - **Bar Type & Value**: Choose your timeframe (e.g., `Minute` with value `1`).
   - **Date Range**: Select a quick preset (e.g., `Last 30 days`) or pick custom dates.
   - **Output Options**: Set your preferred output directory and filename base.
4. Click **`🚀 Download & Export CSV`**.
5. Once complete, click **`📂 Open Folder`** to jump straight to your generated CSV files.

---

## 💻 Tech Stack & Requirements

- **Platform**: NinjaTrader 8 (v8.0.x or v8.1.x+)
- **OS**: Windows 10 / Windows 11
- **Framework**: .NET Framework 4.8 / WPF
- **APIs**: `NinjaTrader.Data.BarsRequest`, `NinjaTrader.Gui.Tools.NTWindow`, `NinjaTrader.NinjaScript.AddOnBase`

---

## 📄 License

This project is open-source under the [MIT License](LICENSE). Feel free to use, modify, and distribute it for personal and commercial trading research.
