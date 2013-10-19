using System;
using System.Runtime.InteropServices;
using System.IO;

namespace Native68000
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int VdpCallback(int i);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate uint ReadCallback(uint a);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void WriteCallback(uint a, uint v);

    public class Musashi
    {
        [DllImport("MusashiDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterVdpCallback(IntPtr callback);

        [DllImport("MusashiDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterRead8(IntPtr callback);

        [DllImport("MusashiDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterRead16(IntPtr callback);

        [DllImport("MusashiDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterRead32(IntPtr callback);

        [DllImport("MusashiDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterWrite8(IntPtr callback);

        [DllImport("MusashiDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterWrite16(IntPtr callback);

        [DllImport("MusashiDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterWrite32(IntPtr callback);

        [DllImport("MusashiDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Init();

        [DllImport("MusashiDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Reset();

        [DllImport("MusashiDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetIRQ(int level);

        [DllImport("MusashiDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Execute(int cycles);

        [DllImport("MusashiDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int QueryCpuState(int regcode);

        [DllImport("MusashiDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetCpuState(int regcode, int value);

        [DllImport("MusashiDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetCyclesRemaining();

        public static int D0 { get { return QueryCpuState(0); } }
        public static int D1 { get { return QueryCpuState(1); } }
        public static int D2 { get { return QueryCpuState(2); } }
        public static int D3 { get { return QueryCpuState(3); } }
        public static int D4 { get { return QueryCpuState(4); } }
        public static int D5 { get { return QueryCpuState(5); } }
        public static int D6 { get { return QueryCpuState(6); } }
        public static int D7 { get { return QueryCpuState(7); } }

        public static int A0 { get { return QueryCpuState(8); } }
        public static int A1 { get { return QueryCpuState(9); } }
        public static int A2 { get { return QueryCpuState(10); } }
        public static int A3 { get { return QueryCpuState(11); } }
        public static int A4 { get { return QueryCpuState(12); } }
        public static int A5 { get { return QueryCpuState(13); } }
        public static int A6 { get { return QueryCpuState(14); } }
        public static int A7 { get { return QueryCpuState(15); } }

        public static int PC { get { return QueryCpuState(16); } }
        public static int SR { get { return QueryCpuState(17); } }
        public static int SP { get { return QueryCpuState(18); } }

        public static void SaveStateBinary(BinaryWriter writer)
        {
            for (int i=0; i<31; i++)
                writer.Write(QueryCpuState(i));
        }

        public static void LoadStateBinary(BinaryReader reader)
        {
            for (int i = 0; i < 31; i++)
                SetCpuState(i, reader.ReadInt32());
        }
    }
}