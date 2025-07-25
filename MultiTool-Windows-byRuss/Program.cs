// Program.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices; // Necesario para la API de Windows
using System.Text.Json;
using System.Threading.Tasks;

public class Program
{
    private static List<AppInfo> allApps = new();
    private static readonly string downloadsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads");
    private static readonly HttpClient httpClient = new();

    public static async Task Main(string[] args)
    {
        // Activa el modo de terminal virtual para los colores ANSI en el ejecutable final.
        WindowsApi.EnableVirtualTerminalProcessing();

        Console.Title = "Multi-Tool Downloader & Installer (Consola)";

        // Establece un tamaño de ventana predeterminado.
        try
        {
            int width = 165;
            int height = 40;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.SetWindowSize(width, height);
                Console.SetBufferSize(width, height);
            }
        }
        catch (Exception)
        {
            Console.WriteLine("No se pudo establecer el tamaño de la ventana. Usando el tamaño por defecto.");
        }

        if (!LoadApps()) { Console.ReadKey(); return; }
        Directory.CreateDirectory(downloadsPath);

        while (true)
        {
            DisplayMenu();
            string input = Console.ReadLine() ?? "";

            if (await ProcessUserCommand(input))
            {
                break;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nProceso finalizado. Presiona cualquier tecla para volver al menú...");
            Console.ResetColor();
            Console.ReadKey();
        }
        Console.Clear();
        Console.WriteLine("Saliendo de la aplicación...");
    }

    private static async Task<bool> ProcessUserCommand(string input)
    {
        if (input.Equals("salir", StringComparison.OrdinalIgnoreCase) || input.Equals("q", StringComparison.OrdinalIgnoreCase))
        {
            return true; // Salir
        }

        if (int.TryParse(input, out int choice))
        {
            Console.Clear();
            if (choice == 0)
            {
                await ProcessMultipleApps(allApps);
            }
            else if (choice > 0 && choice <= allApps.Count)
            {
                await ProcessMultipleApps(new List<AppInfo> { allApps[choice - 1] });
            }
            else
            {
                ShowError("Opción no válida.");
            }
        }
        else
        {
            ShowError("Entrada inválida. Introduce un número.");
        }
        return false; // No salir
    }

    private static void DisplayMenu()
    {
        Console.Clear();
        WriteGradientTitle();

        Console.WriteLine("\n[ADVERTENCIA] Ejecuta esta herramienta como Administrador para mejores resultados.\n");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" [0] Descargar e Instalar TODO");
        Console.ResetColor();
        Console.WriteLine("\nO selecciona una aplicación individual:");

        if (allApps.Any())
        {
            const int columns = 3;
            int maxNameLength = allApps.Max(app => app.Name?.Length ?? 0);
            int columnWidth = maxNameLength + 8;

            for (int i = 0; i < allApps.Count; i++)
            {
                string appName = allApps[i]?.Name ?? "Nombre no disponible";
                string formattedText = $" [{i + 1,2}] {appName}";

                Console.Write(formattedText.PadRight(columnWidth));

                if ((i + 1) % columns == 0 || i == allApps.Count - 1)
                {
                    Console.WriteLine();
                }
            }
        }

        Console.WriteLine("\n-----------------------------------------------------------------------------------------------------------------------------------------------------------------");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Escribe un número (o 'salir' para terminar): ");
        Console.ResetColor();
    }

    #region Lógica Principal
    private static async Task ProcessMultipleApps(List<AppInfo> appsToProcess)
    {
        Console.CursorVisible = true;
        for (int i = 0; i < appsToProcess.Count; i++)
        {
            var app = appsToProcess[i];
            if (app?.Name == null) continue;

            string header = $"Procesando ({i + 1}/{appsToProcess.Count}): {app.Name}";
            Console.WriteLine(header);
            Console.WriteLine(new string('=', header.Length));

            string? filePath = await DownloadApp(app);
            if (string.IsNullOrEmpty(filePath)) continue;

            await InstallApp(app, filePath);
            Console.WriteLine("\n");
        }
        Console.CursorVisible = false;
    }

    // =================================================================
    // FUNCIÓN MODIFICADA PARA VERIFICAR ARCHIVOS EXISTENTES
    // =================================================================
    private static async Task<string?> DownloadApp(AppInfo app)
    {
        if (string.IsNullOrEmpty(app.DownloadUrl) || string.IsNullOrEmpty(app.Name))
        {
            ShowError($"\nError: La URL de descarga o el nombre para '{app.Name}' es inválido.");
            return null;
        }

        try
        {
            var uri = new Uri(app.DownloadUrl);
            string fileName = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(Path.GetExtension(fileName)))
            {
                fileName = $"{app.Name.Replace(" ", "")}_Setup{Path.GetExtension(uri.LocalPath) ?? ".exe"}";
            }

            string fullPath = Path.Combine(downloadsPath, fileName);

            // *** NUEVA VERIFICACIÓN ***
            if (File.Exists(fullPath))
            {
                Console.Write("-> Descargando... ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"¡Archivo ya existe! Se omite la descarga.");
                Console.ResetColor();
                return fullPath; // Devuelve la ruta para que la instalación continúe.
            }

            Console.Write("-> Descargando... ");
            using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("¡Completado!");
            Console.ResetColor();
            return fullPath;
        }
        catch (Exception ex)
        {
            ShowError($"\nError al descargar {app.Name}: {ex.Message}");
            return null;
        }
    }

    private static async Task InstallApp(AppInfo app, string filePath)
    {
        Console.Write("-> Instalando... ");
        if (string.IsNullOrWhiteSpace(app.SilentArgs))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No hay argumentos silenciosos. Se abrirá el instalador estándar.");
            Console.ResetColor();
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            return;
        }
        try
        {
            var startInfo = new ProcessStartInfo();
            if (Path.GetExtension(filePath).Equals(".msi", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.FileName = "msiexec.exe";
                startInfo.Arguments = $"/i \"{filePath}\" {app.SilentArgs}";
            }
            else
            {
                startInfo.FileName = filePath;
                startInfo.Arguments = app.SilentArgs;
            }
            startInfo.UseShellExecute = true;
            var process = Process.Start(startInfo);
            if (process != null) await process.WaitForExitAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("¡Completado!");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            ShowError($"\nError durante la instalación de {app.Name ?? "App desconocida"}: {ex.Message}");
        }
    }
    #endregion

    #region Título y Métodos de UI (Sin cambios)
    private static void WriteGradientTitle()
    {
        string[] title =
        {
            @"                                                                                    ",
            @"                                                                                    ",
            @"                                                                                    ",
            @"         ███▄ ▄███▓ █    ██  ██▓  ▄▄▄█████▓ ██▓▄▄▄█████▓ ▒█████   ▒█████   ██▓      ",
            @"        ▓██▒▀█▀ ██▒ ██  ▓██▒▓██▒  ▓  ██▒ ▓▒▓██▒▓  ██▒ ▓▒▒██▒  ██▒▒██▒  ██▒▓██▒      ",
            @"        ▓██    ▓██░▓██  ▒██░▒██░  ▒ ▓██░ ▒░▒██▒▒ ▓██░ ▒░▒██░  ██▒▒██░  ██▒▒██░      ",
            @"        ▒██    ▒██ ▓▓█  ░██░▒██░  ░ ▓██▓ ░ ░██░░ ▓██▓ ░ ▒██   ██░▒██   ██░▒██░      ",
            @"        ▒██▒   ░██▒▒▒█████▓ ░██████▒▒██▒ ░ ░██░  ▒██▒ ░ ░ ████▓▒░░ ████▓▒░░██████▒  ",
            @"        ░ ▒░   ░  ░░▒▓▒ ▒ ▒ ░ ▒░▓  ░▒ ░░   ░▓    ▒ ░░   ░ ▒░▒░▒░ ░ ▒░▒░▒░ ░ ▒░▓  ░  ",
            @"        ░  ░      ░░░▒░ ░ ░ ░ ░ ▒  ░  ░     ▒ ░    ░      ░ ▒ ▒░   ░ ▒ ▒░ ░ ░ ▒  ░  ",
            @"        ░      ░    ░░░ ░ ░   ░ ░   ░       ▒ ░  ░      ░ ░ ░ ▒  ░ ░ ░ ▒    ░ ░     ",
            @"               ░      ░         ░  ░        ░               ░ ░      ░ ░      ░  ░  ",
            @"                                                                                    ",
            @"         ▄▄▄▄ ▓██   ██▓    ██▀███   █    ██   ██████   ██████                       ",
            @"        ▓█████▄▒██  ██▒   ▓██ ▒ ██▒ ██  ▓██▒▒██    ▒ ▒██    ▒                       ",
            @"        ▒██▒ ▄██▒██ ██░   ▓██ ░▄█ ▒▓██  ▒██░░ ▓██▄   ░ ▓██▄                         ",
            @"        ▒██░█▀  ░ ▐██▓░   ▒██▀▀█▄  ▓▓█  ░██░  ▒   ██▒  ▒   ██▒                      ",
            @"        ░▓█  ▀█▓░ ██▒▓░   ░██▓ ▒██▒▒▒█████▓ ▒██████▒▒▒██████▒▒                      ",
            @"        ░▒▓███▀▒ ██▒▒▒    ░ ▒▓ ░▒▓░░▒▓▒ ▒ ▒ ▒ ▒▓▒ ▒ ░▒ ▒▓▒ ▒ ░                      ",
            @"        ▒░▒   ░▓██ ░▒░      ░▒ ░ ▒░░░▒░ ░ ░ ░ ░▒  ░ ░░ ░▒  ░ ░                      ",
            @"         ░    ░▒ ▒ ░░       ░░   ░  ░░░ ░ ░ ░  ░  ░  ░  ░  ░                        ",
            @"         ░     ░ ░           ░        ░           ░        ░                        ",
            @"              ░░ ░                                                                  "
        };
        string[] hexColors = { "8A0303", "700404", "560505", "3C0606", "220707" };
        int totalRows = title.Length;
        string ansiReset = "\x1b[0m";

        for (int i = 0; i < totalRows; i++)
        {
            int colorIndex = (i * hexColors.Length) / totalRows;
            var (r, g, b) = HexToRgb(hexColors[colorIndex]);
            string ansiColor = $"\x1b[38;2;{r};{g};{b}m";
            Console.WriteLine($"{ansiColor}{title[i]}{ansiReset}");
        }
    }

    private static (int R, int G, int B) HexToRgb(string hex)
    {
        hex = hex.TrimStart('#');
        return (
            int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
            int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
            int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber)
        );
    }

    private static bool LoadApps()
    {
        string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apps.json");
        if (!File.Exists(jsonPath)) { ShowError("Error: No se encontró el archivo 'apps.json'."); return false; }
        try
        {
            var appList = JsonSerializer.Deserialize<List<AppInfo>>(File.ReadAllText(jsonPath));
            if (appList != null) allApps = appList;
            return true;
        }
        catch (Exception ex) { ShowError($"Error al procesar 'apps.json': {ex.Message}"); return false; }
    }

    private static void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    #endregion

    #region API de Windows para Colores
    private static class WindowsApi
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        public static void EnableVirtualTerminalProcessing()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var handle = GetStdHandle(STD_OUTPUT_HANDLE);
                if (handle == IntPtr.Zero) return;
                GetConsoleMode(handle, out uint mode);
                mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                SetConsoleMode(handle, mode);
            }
        }
    }
    #endregion
}