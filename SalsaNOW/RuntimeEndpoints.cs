using System;

namespace RuntimeApp
{
    internal static class RuntimeEndpoints
    {
        public const string ApiBaseUrl = "https://cdn.runtimeapp.example.com";

        public static string AppsJsonUrl => ApiBaseUrl + "/jsons/apps.json";
        public static string SilentAppsJsonUrl => ApiBaseUrl + "/jsons/silentapps.json";
        public static string DesktopJsonUrl => ApiBaseUrl + "/jsons/desktop.json";
        public static string GameSavesJsonUrl => ApiBaseUrl + "/jsons/GameSavesPaths.json";
        public static string SteamProxyJsonUrl => ApiBaseUrl + "/jsons/kaka.json";
        public static string UsgExeUrl => ApiBaseUrl + "/USG/bleh.exe";
        public static string BoosteroidWallpaperUrl => ApiBaseUrl + "/Boosteroid/boosteroid_wp.png";
        public static string ConfigUrl => ApiBaseUrl + "/jsons/config.ini";
        public static string DirectoryJsonUrl => ApiBaseUrl + "/bdjson/directory.json";
    }
}
