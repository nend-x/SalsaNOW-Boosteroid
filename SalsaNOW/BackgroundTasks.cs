#if WINDOWS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace RuntimeApp
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
                        AppLogger.Warn("CloseHandlesLaunchersHelper: 0 handles closed (process not running, path mismatch, or run elevated).");
                    else
                        AppLogger.Info($"Closed {n} handle(s) in LaunchersHelper (all types, System Informer style).");
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"CloseHandlesLaunchersHelper: {ex.Message}");
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

                AppLogger.Info("Cleared launchershelper.log.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to clear launchershelper.log: {ex.Message}");
            }

            return Task.CompletedTask;
        }


        // Monitors Desktop and Start Menu shortcuts, syncing them to the persistent application directory
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
                AppLogger.Info("Initial Desktop shortcut sync completed.");
            }
            catch (Exception ex) { AppLogger.Error($"Initial shortcut sync failed: {ex.Message}"); }

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
                                    AppLogger.Info($"Backed up new shortcut: {Path.GetFileName(file)}");
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
                                    AppLogger.Info($"Copied shortcut over to Start Menu: {Path.GetFileName(file)}");
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
                                    AppLogger.Info($"Moved deleted shortcut to long-term backup: {fileName}");
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
                        AppLogger.Warn($"Restored missing core component: {name}");
                        new Thread(() => MessageBox.Show($"{Path.GetFileNameWithoutExtension(name)} is a core component and cannot be removed.", RuntimeIdentity.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information)).Start();
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
                    AppLogger.Info("Removed Local Group Policy cache folders (System32\\GroupPolicy, GroupPolicyUsers).");

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
                        AppLogger.Info("Ran gpupdate /force.");
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"Failed to reset local Group Policy cache or gpupdate: {ex.Message}");
                }

                if (token.IsCancellationRequested) return;

                try
                {
                    const string explorerAdvanced = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
                    using (var adv = Registry.CurrentUser.CreateSubKey(explorerAdvanced, true))
                    {
                        if (adv != null)
                        {
                            adv.SetValue("TaskbarAutoHide", 0, RegistryValueKind.DWord);
                            adv.SetValue("TaskbarAutoHideInTabletMode", 0, RegistryValueKind.DWord);
                        }
                    }

                    // StuckRects* Settings blob: offset 8 (9th byte) often ties to auto-hide; values differ by Windows build (0x02 / 0x03 / 0x22).
                    byte hideOffByte = GetStuckRectsAlwaysShowByte();
                    TryPatchStuckRectsSettings(@"Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3", hideOffByte);
                    TryPatchStuckRectsSettings(@"Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects2", hideOffByte);

                    AppLogger.Info($"Taskbar: Advanced DWORDs + StuckRects Settings[8]=0x{hideOffByte:X2} (Explorer restart applies).");
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"Failed to apply taskbar visibility registry: {ex.Message}");
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
                    AppLogger.Info("Terminated explorer.exe.");
                }
                catch (Exception ex) { AppLogger.Error($"Failed to terminate explorer.exe: {ex.Message}"); }

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
                    AppLogger.Info("Restarted explorer.exe.");
                }
                catch (Exception ex) { AppLogger.Error($"Failed to restart explorer.exe: {ex.Message}"); }

                if (token.IsCancellationRequested) return;

                Thread.Sleep(2000);

                if (token.IsCancellationRequested) return;

                try
                {
                    string localWallpaper = Path.Combine(Path.GetTempPath(), "boosteroid_wp.png");
                    using (var wc = new WebClient())
                    {
                        wc.DownloadFile(RuntimeEndpoints.BoosteroidWallpaperUrl, localWallpaper);
                    }
                    if (NativeMethods.SetDesktopWallpaper(localWallpaper))
                        AppLogger.Info("Desktop wallpaper set (Boosteroid).");
                    else
                        AppLogger.Warn("Desktop wallpaper API returned false.");
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"Failed to set desktop wallpaper: {ex.Message}");
                }
            }, token);
        }

        static bool TryGetWindowsBuildNumber(out int build)
        {
            build = 0;
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (k == null) return false;
                    object v = k.GetValue("CurrentBuild");
                    if (v is int i) { build = i; return true; }
                    if (v is string s && int.TryParse(s, out int b)) { build = b; return true; }
                    v = k.GetValue("CurrentBuildNumber");
                    if (v is string s2 && int.TryParse(s2, out int b2)) { build = b2; return true; }
                }
            }
            catch { }
            return false;
        }

        static byte GetStuckRectsAlwaysShowByte()
        {
            // Offset 8 in Settings is widely cited but values conflict (0x02 vs 0x03 vs 0x22). We avoid 0x03 (reported ineffective here).
            if (TryGetWindowsBuildNumber(out int build) && build >= 22000)
                return 0x22;
            return 0x02;
        }

        static void TryPatchStuckRectsSettings(string explorerSubKey, byte byteAtOffset8)
        {
            try
            {
                using (var stuck = Registry.CurrentUser.OpenSubKey(explorerSubKey, true))
                {
                    if (stuck == null) return;
                    var settings = stuck.GetValue("Settings") as byte[];
                    if (settings == null || settings.Length <= 8) return;
                    byte[] copy = (byte[])settings.Clone();
                    copy[8] = byteAtOffset8;
                    stuck.SetValue("Settings", copy, RegistryValueKind.Binary);
                }
            }
            catch { }
        }

    }
}
#endif
