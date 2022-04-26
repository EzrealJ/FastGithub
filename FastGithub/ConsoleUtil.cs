using Serilog;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace FastGithub
{
    static class ConsoleUtil
    {
        private const uint ENABLE_QUICK_EDIT = 0x0040;

        private const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        [SupportedOSPlatform("windows")]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SupportedOSPlatform("windows")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SupportedOSPlatform("windows")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        /// <summary>
        /// 禁用快速编辑模式
        /// </summary>
        /// <returns></returns>
        public static bool DisableQuickEdit()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var hwnd = GetStdHandle(STD_INPUT_HANDLE);
                    if (GetConsoleMode(hwnd, out uint mode))
                    {
                        mode &= ~ENABLE_QUICK_EDIT;
                        return SetConsoleMode(hwnd, mode);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "禁用快速编辑模式，没有管理员权限时，这种情况是正常的");
            }

            return false;
        }
    }
}
