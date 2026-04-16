using System;
using System.IO;
using System.Net;
using System.Net.Http;
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
        private static HttpClient httpClient;

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            // Initialize HttpClient with TLS 1.2/1.3 support
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true
            };
            httpClient = new HttpClient(handler);

            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "--apps-json" || args[i] == "-a") && i + 1 < args.Length)
                {
                    customAppsJsonPath = args[i + 1]; i++;
                }
            }

            await Startup();
            
            // Load configuration once to share settings across modules
            AppSettings.Load(globalDirectory);

#if WINDOWS
            // Fire and forget non-blocking background services (Windows only)
            _ = BackgroundTasks.CloseHandlesLaunchersHelper(cts.Token);
            _ = BackgroundTasks.CleanlogsLauncherHelper(cts.Token);
            _ = BackgroundTasks.ResetPoliciesAndExplorerAsync(cts.Token);

            // Execute deployment modules (Windows only)
            await AppInstaller.AppsInstallAsync(globalDirectory, customAppsJsonPath);

            NativeMethods.ShowWindow(NativeMethods.GetConsoleWindow(), NativeMethods.SW_HIDE);
            
            string batch = Path.Combine(globalDirectory, "StartupBatch.bat");
            if (File.Exists(batch)) Process.Start(new ProcessStartInfo { FileName = batch, UseShellExecute = true });
#else
            Console.WriteLine("[i] Running in non-Windows mode. Windows-specific features are disabled.");
            Console.WriteLine($"[i] Global directory: {globalDirectory}");
            
            // Keep running for demo purposes
            Console.WriteLine("[i] Press any key to exit...");
            Console.ReadKey();
#endif

            try { await Task.Delay(Timeout.Infinite, cts.Token); } catch (TaskCanceledException) { }
        }

        static async Task Startup()
        {
            try
            {
#if WINDOWS
                // Check for Boosteroid environment (Windows only)
                string launcherPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                    "boosteroid-experience", 
                    "LaunchersHelper.exe");
                
                if (!File.Exists(launcherPath)) 
                { 
                    Console.WriteLine("[!] Not a Boosteroid environment. Exiting..."); 
                    await Task.Delay(5000); 
                    Environment.Exit(0); 
                }
#else
                Console.WriteLine("[i] Skipping Boosteroid environment check (non-Windows platform).");
#endif
                
                // Fetch directory configuration
                var response = await httpClient.GetStringAsync(RuntimeEndpoints.DirectoryJsonUrl);
                var dirs = JsonConvert.DeserializeObject<System.Collections.Generic.List<SavePath>>(response);
                
                if (dirs == null || dirs.Count == 0)
                {
                    throw new InvalidOperationException("Could not retrieve directory configuration.");
                }
                
                globalDirectory = dirs[0].directoryCreate;
                Directory.CreateDirectory(globalDirectory);
                
                // Initialize Logger here so it knows the global directory path
                AppLogger.Initialize(globalDirectory);
                AppLogger.Info($"Main directory created {globalDirectory}");
                
                // Download config if it doesn't exist
                string cfg = Path.Combine(globalDirectory, RuntimeIdentity.ConfigFileName);
                if (!File.Exists(cfg)) 
                {
                    var configData = await httpClient.GetByteArrayAsync(RuntimeEndpoints.ConfigUrl);
                    await File.WriteAllBytesAsync(cfg, configData);
                }
            }
            catch (Exception ex) 
            { 
                AppLogger.UploadLogAndShowError(ex.Message);
                Environment.Exit(0);
            }
        }
    }
}