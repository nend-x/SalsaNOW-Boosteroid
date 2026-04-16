using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RuntimeApp
{
    internal static class AppLogger
    {
        private static string _logFilePath;
        private static HttpClient _httpClient;

        // Initializes the local log file in the global directory
        public static void Initialize(string globalDirectory)
        {
            _logFilePath = Path.Combine(globalDirectory, RuntimeIdentity.LogFileName);
            _httpClient = new HttpClient();
            try
            {
                File.WriteAllText(_logFilePath, RuntimeIdentity.SessionLogHeader);
            }
            catch { }
        }

        public static void Info(string message) => Write("[+]", message, ConsoleColor.Gray);
        public static void Warn(string message) => Write("[!]", message, ConsoleColor.Yellow);
        public static void Error(string message) => Write("[ERR]", message, ConsoleColor.Red);

        // Outputs formatted messages to the console and appends them to the local log file
        private static void Write(string prefix, string message, ConsoleColor color)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"{prefix} {message}");
            Console.ForegroundColor = original;

            if (!string.IsNullOrEmpty(_logFilePath))
            {
                try { File.AppendAllText(_logFilePath, $"[{DateTime.Now:HH:mm:ss}] {prefix} {message}\n"); }
                catch { } 
            }
        }

        // Uploads crash logs to paste.rs upon fatal exceptions
        public static void UploadLogAndShowError(string fatalErrorMessage)
        {
            // Ensure the fatal error is written to the console and local log
            Error(fatalErrorMessage);

            string pasteUrl = "Could not be uploaded.";
            try
            {
                // 1. Construct the crash report payload
                string logContent = RuntimeIdentity.CrashReportHeader(fatalErrorMessage);
                
                if (!string.IsNullOrEmpty(_logFilePath) && File.Exists(_logFilePath))
                {
                    logContent += "--- LOG HISTORY ---\n" + File.ReadAllText(_logFilePath);
                }

                // 2. Upload to paste.rs
                var request = new HttpRequestMessage(HttpMethod.Post, "https://paste.rs/")
                {
                    Content = new StringContent(logContent)
                };
                request.Headers.Add("User-Agent", RuntimeIdentity.CrashReporterUserAgent);
                
                var response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    pasteUrl = response.Content.ReadAsStringAsync().GetAwaiter().GetResult().Trim();
                }
            }
            catch { }

#if WINDOWS
            // On Windows, we could show a message box (requires additional setup)
            // For now, use console output on all platforms
#endif
            // Display the fatal error and the crash log link to the user
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n========== APPLICATION CRASHED ==========");
            Console.WriteLine($"Error: {fatalErrorMessage}");
            Console.WriteLine($"Crash log link: {pasteUrl}");
            Console.WriteLine("==========================================\n");
            Console.ResetColor();
            
            // Pause to let user see the error
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}