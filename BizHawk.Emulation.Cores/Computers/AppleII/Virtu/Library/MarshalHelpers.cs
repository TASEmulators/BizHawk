using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;

namespace Jellyfish.Library
{
    public static class MarshalHelpers
    {
        [SecurityCritical]
        public static void FillMemory(IntPtr buffer, int bufferSize, byte value)
        {
            NativeMethods.FillMemory(buffer, (IntPtr)bufferSize, value);
        }

        [SecurityCritical]
        public static void ZeroMemory(IntPtr buffer, int bufferSize)
        {
            NativeMethods.ZeroMemory(buffer, (IntPtr)bufferSize);
        }

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern void FillMemory(IntPtr destination, IntPtr length, byte fill);

            [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern void ZeroMemory(IntPtr destination, IntPtr length);
        }
    }
}
