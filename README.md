# 🚀 Multi-Tool Downloader

A lightweight and efficient C# console application to download and silently install your favorite Windows applications from a simple, configurable JSON file.

### ✨ Features

* **Modular by Design:** Easily add, remove, or update applications by editing the `apps.json` file. No need to recompile!
* **Automated Installation:** Automatically installs applications silently after downloading them, using pre-configured command-line arguments.
* **Efficient Downloading:** Checks if an installer already exists before downloading to save bandwidth and time.
* **Customizable:**
    * Set a default console window size for a consistent look.
    * Enjoy a stylish, static gradient title using modern terminal colors.
    * View applications in a clean, aligned column layout.
* **Built with Modern C#:** Uses `HttpClient` for downloads and is compatible with modern .NET environments.
* **No Dependencies:** Runs on a clean Windows installation with just the .NET runtime.

### 🎬 Getting Started

1.  **Configure:** Edit the `apps.json` file to add the applications you want, along with their download URLs and silent installation arguments.
2.  **Compile:** Build the project using Visual Studio.
3.  **Run:** Execute the compiled `.exe` as an administrator for the best results.

### 🔧 How It Works

The application reads the `apps.json` file to populate a list of available software. When you select an application to install, the tool:

1.  Checks if the installer file already exists in the `Downloads` folder.
2.  If not, it downloads the file using the specified `DownloadUrl`.
3.  Executes the installer with the silent arguments (`SilentArgs`) to automate the installation process without user intervention.
