using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;

namespace RuntimeApp
{
    internal class Program
    {
        private static string globalDirectory = "";
        private static string currentPath = Directory.GetCurrentDirectory();
        private static readonly CancellationTokenSource cts = new CancellationTokenSource();
        private static string customAppsJsonPath = null;

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            Console.Title = RuntimeIdentity.AppName;

            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "--apps-json" || args[i] == "-a") && i + 1 < args.Length)
                {
                    customAppsJsonPath = args[i + 1]; i++;
                }
            }

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, errors) => true;

            await Startup();
            
            // Load configuration once to share settings across modules
            AppSettings.Load(globalDirectory);

            // Fire and forget non-blocking background services
            //_ = BackgroundTasks.StartShortcutsSavingAsync(globalDirectory, cts.Token);
            //_ = BackgroundTasks.StartTerminateGFNExplorerShellAsync(cts.Token);
            //_ = BackgroundTasks.StartEacWatcherAsync(cts.Token);
            //_ = BackgroundTasks.StartBrickPreventionAsync(cts.Token);
            _ = BackgroundTasks.CloseHandlesLaunchersHelper(cts.Token);
            _ = BackgroundTasks.CleanlogsLauncherHelper(cts.Token);
            _ = BackgroundTasks.ResetPoliciesAndExplorerAsync(cts.Token);


            // Execute deployment modules
            await AppInstaller.AppsInstallAsync(globalDirectory, customAppsJsonPath);
            //await AppInstaller.DesktopInstallAsync(globalDirectory);
            //await AppInstaller.AppsInstallSilentAsync(globalDirectory);
            
            //await SteamManager.ShutdownServerAsync(globalDirectory);
            
            // Apply Nvidia optimizations if enabled
            //if (AppSettings.NvidiaRaytracing) NvidiaManager.EnableRTX();

            NativeMethods.ShowWindow(NativeMethods.GetConsoleWindow(), NativeMethods.SW_HIDE);
            
            // _ = SteamManager.SetupGameSavesAsync(globalDirectory);
            
            string batch = Path.Combine(globalDirectory, "StartupBatch.bat");
            if (File.Exists(batch)) Process.Start(new ProcessStartInfo { FileName = batch, UseShellExecute = true });

            try { await Task.Delay(Timeout.Infinite, cts.Token); } catch (TaskCanceledException) { }
        }

        static async Task Startup()
        {
            try
            {
                if (!File.Exists(@"C:\Users\user\boosteroid-experience\LaunchersHelper.exe")) 
                { 
                    Console.WriteLine("[!] Not a Boosteroid environment. Exiting..."); 
                    await Task.Delay(5000); Environment.Exit(0); 
                }
                
                using (var wc = new WebClient())
                {
                    var dir = JsonConvert.DeserializeObject<System.Collections.Generic.List<SavePath>>(await wc.DownloadStringTaskAsync(RuntimeEndpoints.DirectoryJsonUrl))[0];
                    globalDirectory = dir.directoryCreate;
                    Directory.CreateDirectory(globalDirectory);
                    
                    // Initialize Logger here so it knows the global directory path
                    AppLogger.Initialize(globalDirectory);
                    AppLogger.Info($"Main directory created {globalDirectory}");
                    
                    string cfg = Path.Combine(globalDirectory, RuntimeIdentity.ConfigFileName);
                    if (!System.IO.File.Exists(cfg)) await wc.DownloadFileTaskAsync(new Uri(RuntimeEndpoints.ConfigUrl), cfg);
                }
            }
            // Upload Crashlogs to paste.rs and show the user a link to forward to the Devs
            catch (Exception ex) 
            { 
                AppLogger.UploadLogAndShowError(ex.Message);
                Environment.Exit(0);
            }
        }
    }
}