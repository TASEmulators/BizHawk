using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
    static class LogConsole
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();

        public static bool ConsoleVisible { get; private set; }

        public static void ShowConsole()
        {
            if (ConsoleVisible) return;
            AllocConsole();
            ConsoleVisible = true;
            var sout = new StreamWriter(Console.OpenStandardOutput());
            sout.AutoFlush = true;
            Console.SetOut(sout);
        }

        public static void HideConsole()
        {
            if (ConsoleVisible == false) return;
            FreeConsole();
            Console.SetOut(TextWriter.Null);
            ConsoleVisible = false;
        }
    }
}