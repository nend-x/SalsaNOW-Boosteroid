using System;

namespace RuntimeApp
{
    internal static class RuntimeIdentity
    {
        public static readonly string AppName;

        static RuntimeIdentity()
        {
            AppName = GenerateAppName();
        }

        private static string GenerateAppName()
        {
            string[] prefixes = { "host", "core", "app", "sys", "svc", "node", "engine", "daemon" };
            string[] suffixes = { "alpha", "beta", "prime", "nova", "flux", "pulse", "oxide", "zero" };
            var random = new Random();
            string prefix = prefixes[random.Next(prefixes.Length)];
            string suffix = suffixes[random.Next(suffixes.Length)];
            string id = Guid.NewGuid().ToString("N").Substring(0, 8);
            return $"{prefix}_{suffix}_{id}";
        }

        public static string ConfigFileName => $"{AppName}.ini";
        public static string LogFileName => $"{AppName}.log";
        public static string CrashReporterUserAgent => $"{AppName}-CrashReporter";
        public static string FatalErrorTitle => $"{AppName} - Fatal Error";
        public static string SessionLogHeader => $"--- {AppName} Session Log [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ---\n";
        public static string CrashReportHeader(string errorMessage) => $"--- {AppName} CRASH REPORT ---\nTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nError: {errorMessage}\n\n";
    }
}
