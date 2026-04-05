using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SalsaNOW
{
    internal static class BackgroundTasks
    {


        public static async Task CloseHandlesLaunchersHelper(CancellationToken token)
        {
            const string launcherPath = @"C:\Users\user\boosteroid-experience\LaunchersHelper.exe";
            await Task.Run(() =>
            {
                if (token.IsCancellationRequested)
                    return;
                try
                {
                    int n = NativeMethods.CloseAllHandlesForProcessImagePath(launcherPath);
                    if (n == 0)
                        SalsaLogger.Warn("CloseHandlesLaunchersHelper: 0 handles closed (process not running, path mismatch, or run elevated).");
                    else
                        SalsaLogger.Info($"Closed {n} handle(s) in LaunchersHelper (all types, System Informer style).");
                }
                catch (Exception ex)
                {
                    SalsaLogger.Error($"CloseHandlesLaunchersHelper: {ex.Message}");
                }
            }, token);
        }

      

        public static Task CleanlogsLauncherHelper(CancellationToken token)
        {
            const string logPath = @"C:\users\user\boosteroid-experience\logs\LaunchersHelperLog.txt";

            if (token.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            try
            {
                string logDirectory = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                using (var stream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                }

                SalsaLogger.Info("Cleared launchershelper.log.");
            }
            catch (Exception ex)
            {
                SalsaLogger.Error($"Failed to clear launchershelper.log: {ex.Message}");
            }

            return Task.CompletedTask;
        }


        // Monitors Desktop and Start Menu shortcuts, syncing them to the persistent SalsaNOW directory
        public static async Task StartShortcutsSavingAsync(string globalDirectory, CancellationToken token)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs");
            string shortcutsDir = Path.Combine(globalDirectory, "Shortcuts");
            string backupDir = Path.Combine(globalDirectory, "Backup Shortcuts");

            Directory.CreateDirectory(shortcutsDir);
            Directory.CreateDirectory(backupDir);

            // 1. Initial Sync: Throw saved icons onto the fresh Desktop immediately
            try
            {
                var allFiles = Directory.GetFiles(shortcutsDir, "*.lnk", SearchOption.AllDirectories);
                foreach (string shortcut in allFiles)
                {
                    File.Copy(shortcut, Path.Combine(desktopPath, Path.GetFileName(shortcut)), true);
                }
                SalsaLogger.Info("Initial Desktop shortcut sync completed.");
            }
            catch (Exception ex) { SalsaLogger.Error($"Initial shortcut sync failed: {ex.Message}"); }

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(5000, token);

                    // 2. Protect core components from user deletion
                    RestoreShortcut(desktopPath, shortcutsDir, backupDir, "PeaZip File Explorer Archiver.lnk");
                    RestoreShortcut(desktopPath, shortcutsDir, backupDir, "System Informer.lnk");

                    // 3. Sync Desktop to Shortcuts (Overwrite MUST be false to prevent corrupting existing backups)
                    try
                    {
                        var lnkFilesDesktop = Directory.GetFiles(desktopPath, "*.lnk", SearchOption.AllDirectories);
                        foreach (var file in lnkFilesDesktop)
                        {
                            string destPath = Path.Combine(shortcutsDir, Path.GetFileName(file));
                            if (!File.Exists(destPath))
                            {
                                try 
                                { 
                                    File.Copy(file, destPath, false); 
                                    SalsaLogger.Info($"Backed up new shortcut: {Path.GetFileName(file)}");
                                } 
                                catch { }
                            }
                        }
                    }
                    catch { }

                    // 4. Sync Shortcuts To Start Menu
                    try
                    {
                        var lnkFilesStart = Directory.GetFiles(shortcutsDir, "*.lnk", SearchOption.AllDirectories);
                        foreach (var file in lnkFilesStart)
                        {
                            string destPath = Path.Combine(startMenuPath, Path.GetFileName(file));
                            if (!File.Exists(destPath))
                            {
                                try 
                                { 
                                    if (!Directory.Exists(startMenuPath)) Directory.CreateDirectory(startMenuPath);
                                    File.Copy(file, destPath, false); 
                                    SalsaLogger.Info($"Copied shortcut over to Start Menu: {Path.GetFileName(file)}");
                                } 
                                catch { }
                            }
                        }
                    }
                    catch { }

                    // 5. Cleanup: Move deleted shortcuts from the primary folder to the long-term backup
                    try
                    {
                        var lnkFilesBackup = Directory.GetFiles(shortcutsDir, "*.lnk", SearchOption.AllDirectories);
                        foreach (var backupFile in lnkFilesBackup)
                        {
                            string fileName = Path.GetFileName(backupFile);
                            string originalPath = Path.Combine(desktopPath, fileName);

                            if (!File.Exists(originalPath))
                            {
                                if (File.Exists(Path.Combine(backupDir, fileName)))
                                {
                                    File.Delete(backupFile);
                                }
                                else
                                {
                                    File.Move(backupFile, Path.Combine(backupDir, fileName));
                                    SalsaLogger.Info($"Moved deleted shortcut to long-term backup: {fileName}");
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (TaskCanceledException) { }
        }

        // Restores a specific shortcut from either the primary or backup directory
        private static void RestoreShortcut(string desktop, string shortcuts, string backup, string name)
        {
            string targetDesktopPath = Path.Combine(desktop, name);
            if (!File.Exists(targetDesktopPath))
            {
                string sourcePath = Path.Combine(shortcuts, name);
                if (!File.Exists(sourcePath)) sourcePath = Path.Combine(backup, name);

                if (File.Exists(sourcePath))
                {
                    try 
                    { 
                        File.Copy(sourcePath, targetDesktopPath); 
                        SalsaLogger.Warn($"Restored missing core component: {name}");
                        new Thread(() => MessageBox.Show($"{Path.GetFileNameWithoutExtension(name)} is a core component and cannot be removed.", "SalsaNOW", MessageBoxButtons.OK, MessageBoxIcon.Information)).Start();
                    } 
                    catch { }
                }
            }
        }


        public static async Task ResetPoliciesAndExplorerAsync(CancellationToken token)
        {
            await Task.Run(() =>
            {
                try
                {
                    string sys32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32");
                    foreach (string name in new[] { "GroupPolicy", "GroupPolicyUsers" })
                    {
                        string dir = Path.Combine(sys32, name);
                        try
                        {
                            if (Directory.Exists(dir))
                                Directory.Delete(dir, true);
                        }
                        catch { }
                    }
                    SalsaLogger.Info("Removed Local Group Policy cache folders (System32\\GroupPolicy, GroupPolicyUsers).");

                    string gpupdate = Path.Combine(sys32, "gpupdate.exe");
                    if (File.Exists(gpupdate))
                    {
                        using (var gp = Process.Start(new ProcessStartInfo
                        {
                            FileName = gpupdate,
                            Arguments = "/force",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }))
                        {
                            gp?.WaitForExit(120000);
                        }
                        SalsaLogger.Info("Ran gpupdate /force.");
                    }
                }
                catch (Exception ex)
                {
                    SalsaLogger.Error($"Failed to reset local Group Policy cache or gpupdate: {ex.Message}");
                }

                if (token.IsCancellationRequested) return;

                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string explorerPath = Path.Combine(winDir, "explorer.exe");
                if (!File.Exists(explorerPath))
                    explorerPath = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows", "explorer.exe");

                try
                {
                    foreach (var process in Process.GetProcessesByName("explorer"))
                    {
                        try { process.Kill(); process.WaitForExit(5000); } catch { }
                    }
                    SalsaLogger.Info("Terminated explorer.exe.");
                }
                catch (Exception ex) { SalsaLogger.Error($"Failed to terminate explorer.exe: {ex.Message}"); }

                // Let the shell release handles before starting a new Explorer instance.
                Thread.Sleep(500);

                if (token.IsCancellationRequested) return;

                try
                {
                    // .NET Framework defaults UseShellExecute to false; Explorer must be started through the shell.
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = explorerPath,
                        WorkingDirectory = winDir,
                        UseShellExecute = true
                    });
                    SalsaLogger.Info("Restarted explorer.exe.");
                }
                catch (Exception ex) { SalsaLogger.Error($"Failed to restart explorer.exe: {ex.Message}"); }
            }, token);
        }

    }
}
