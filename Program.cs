using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace AudioConverter
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Logger.LogInfo("=== Application Starting ===");
                string? baseDir = AppDomain.CurrentDomain.BaseDirectory;
                Logger.LogInfo($"Command Line Args: {string.Join(", ", args)}");
                Logger.LogInfo($"Working Directory: {Environment.CurrentDirectory}");
                Logger.LogInfo($"Base Directory: {baseDir}");
                Logger.LogInfo($"Executable Path: {Application.ExecutablePath}");

                if (args.Length >= 2 && args[0] == "--apply-update")
                {
                    Logger.LogInfo("Applying update");
                    RunApplyUpdate(args[1]);
                    return;
                }

                Logger.LogInfo("Initializing VisualStyles");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Logger.LogInfo("Creating main form");
                Logger.LogInfo($"Assembly Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");

                Application.Run(new Form1());

                Logger.LogInfo("=== Application Exiting Normally ===");
            }
            catch (Exception ex)
            {
                Logger.LogError("Unhandled exception in Main", ex);
                MessageBox.Show(
                    $"Application failed to start:\n\n{ex.Message}\n\nLog file: {Logger.GetLogPath()}",
                    "Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        static void RunApplyUpdate(string targetPath)
        {
            string sourcePath = Environment.ProcessPath ?? Application.ExecutablePath;
            targetPath = Path.GetFullPath(targetPath);
            if (!targetPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Update target must be an .exe file.");

            Thread.Sleep(1000);
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    File.Copy(sourcePath, targetPath, overwrite: true);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetPath,
                        UseShellExecute = true
                    });
                    return;
                }
                catch (IOException)
                {
                    Thread.Sleep(500);
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(500);
                }
            }

            throw new IOException($"Could not replace {targetPath}.");
        }
    }
}
