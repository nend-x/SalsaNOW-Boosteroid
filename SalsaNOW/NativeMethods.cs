#if WINDOWS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace RuntimeApp
{
    internal static class NativeMethods
    {
        // Window message and state constants
        public const int WM_CLOSE = 0x0010;
        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public const uint ProcessQueryLimitedInformation = 0x1000;
        public const uint ProcessDupHandle = 0x0040;
        public const uint ProcessTerminate = 0x0001;
        public const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
        public const uint DUPLICATE_SAME_ACCESS = 0x00000002;
        public const uint STATUS_SUCCESS = 0x00000000;
        public const uint STATUS_BUFFER_OVERFLOW = 0x80000005;
        public const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
        public const uint STATUS_BUFFER_TOO_SMALL = 0xC0000023;
        public const int SystemExtendedHandleInformation = 64;
        public const int ObjectNameInformation = 1;
        public const int ObjectTypeInformation = 2;
        public const uint FILE_NAME_NORMALIZED = 0x00000008;
        public const uint FILE_TYPE_DISK = 0x00000001;
        public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const uint TOKEN_QUERY = 0x0008;
        public const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
        {
            public IntPtr Object;
            public IntPtr UniqueProcessId;
            public IntPtr HandleValue;
            public uint GrantedAccess;
            public ushort CreatorBackTraceIndex;
            public ushort ObjectTypeIndex;
            public uint HandleAttributes;
            public uint Reserved;
        }

        // --- user32.dll ---



        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        public static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const uint SPI_SETDESKWALLPAPER = 0x0014;
        public const uint SPIF_UPDATEINIFILE = 0x01;
        public const uint SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

        public static bool SetDesktopWallpaper(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                return false;
            return SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, Path.GetFullPath(fullPath), SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }

        // --- kernel32.dll ---

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle, uint dwDesiredAccess, bool bInheritHandle, uint dwOptions);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("kernel32.dll")]
        public static extern uint GetFileType(IntPtr hFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint GetFinalPathNameByHandle(IntPtr hFile, StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

        [DllImport("ntdll.dll")]
        public static extern uint NtQuerySystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength, out int ReturnLength);

        [DllImport("ntdll.dll")]
        public static extern uint NtQueryObject(IntPtr Handle, int ObjectInformationClass, IntPtr ObjectInformation, int ObjectInformationLength, out int ReturnLength);

        public static int CloseAllHandlesForProcessImagePath(string targetPath)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
                return 0;

            string targetComparable = NormalizeComparablePath(Path.GetFullPath(targetPath));
            if (string.IsNullOrEmpty(targetComparable))
                return 0;

            string exeName = Path.GetFileNameWithoutExtension(targetPath);
            var targetPids = new HashSet<int>();
            foreach (var p in Process.GetProcessesByName(exeName))
            {
                try
                {
                    string procPath = null;
                    if (TryGetProcessImagePath(p.Id, out string nativePath))
                        procPath = nativePath;
                    else
                    {
                        try { procPath = p.MainModule.FileName; } catch { }
                    }
                    if (procPath != null && string.Equals(NormalizeComparablePath(procPath), targetComparable, StringComparison.OrdinalIgnoreCase))
                        targetPids.Add(p.Id);
                }
                catch { }
                finally { try { p.Dispose(); } catch { } }
            }

            if (targetPids.Count == 0)
                return 0;

            TryEnableSeDebugPrivilege();

            int closed = 0;
            int size = 0x100000;
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                while (true)
                {
                    uint status = NtQuerySystemInformation(SystemExtendedHandleInformation, buffer, size, out int needed);
                    if (status == STATUS_INFO_LENGTH_MISMATCH || status == STATUS_BUFFER_TOO_SMALL)
                    {
                        Marshal.FreeHGlobal(buffer);
                        size = Math.Max(needed, size * 2);
                        buffer = Marshal.AllocHGlobal(size);
                        continue;
                    }
                    if (status != STATUS_SUCCESS)
                        return closed;
                    break;
                }

                ulong numHandles = (ulong)(IntPtr.Size == 8 ? Marshal.ReadInt64(buffer) : Marshal.ReadInt32(buffer));
                int entrySize = Marshal.SizeOf(typeof(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX));
                IntPtr entryPtr = IntPtr.Add(buffer, IntPtr.Size * 2);

                for (ulong i = 0; i < numHandles; i++)
                {
                    var entry = (SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX)Marshal.PtrToStructure(
                        IntPtr.Add(entryPtr, (int)(i * (ulong)entrySize)), typeof(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX));

                    int pid = HandleOwnerPidFromSystemHandle(entry.UniqueProcessId);
                    if (pid == 0 || !targetPids.Contains(pid))
                        continue;

                    IntPtr hRemote = OpenProcess(ProcessDupHandle, false, pid);
                    if (hRemote == IntPtr.Zero)
                        continue;

                    try
                    {
                        if (DuplicateHandle(hRemote, entry.HandleValue, GetCurrentProcess(), out IntPtr hClosed, 0, false, DUPLICATE_CLOSE_SOURCE))
                        {
                            if (hClosed != IntPtr.Zero)
                                CloseHandle(hClosed);
                            closed++;
                        }
                    }
                    finally
                    {
                        CloseHandle(hRemote);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return closed;
        }

        static int HandleOwnerPidFromSystemHandle(IntPtr uniqueProcessId)
        {
            if (uniqueProcessId == IntPtr.Zero)
                return 0;
            if (IntPtr.Size == 4)
                return uniqueProcessId.ToInt32();
            return unchecked((int)(uint)(uniqueProcessId.ToInt64() & 0xFFFFFFFFL));
        }

        static void TryEnableSeDebugPrivilege()
        {
            IntPtr hToken = IntPtr.Zero;
            try
            {
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hToken))
                    return;
                if (!LookupPrivilegeValue(null, "SeDebugPrivilege", out LUID luid))
                    return;
                var tp = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Privileges = new LUID_AND_ATTRIBUTES { Luid = luid, Attributes = SE_PRIVILEGE_ENABLED }
                };
                AdjustTokenPrivileges(hToken, false, ref tp, (uint)Marshal.SizeOf(typeof(TOKEN_PRIVILEGES)), IntPtr.Zero, IntPtr.Zero);
            }
            catch { }
            finally
            {
                if (hToken != IntPtr.Zero)
                    CloseHandle(hToken);
            }
        }

        static bool TryGetProcessImagePath(int pid, out string path)
        {
            path = null;
            IntPtr h = OpenProcess(ProcessQueryLimitedInformation, false, pid);
            if (h == IntPtr.Zero)
                return false;
            try
            {
                var sb = new StringBuilder(32767);
                int size = sb.Capacity;
                if (QueryFullProcessImageName(h, 0, sb, ref size))
                {
                    path = sb.ToString();
                    return true;
                }
            }
            finally
            {
                CloseHandle(h);
            }
            return false;
        }

        static string NormalizeComparablePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            string p = path.Trim();
            if (p.StartsWith(@"\\?\", StringComparison.Ordinal))
            {
                if (p.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
                    p = @"\\" + p.Substring(8);
                else
                    p = p.Substring(4);
            }

            try
            {
                return Path.GetFullPath(p).TrimEnd('\\').ToLowerInvariant();
            }
            catch
            {
                return p.TrimEnd('\\').ToLowerInvariant();
            }
        }
    }
}
#endif