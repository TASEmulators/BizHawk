using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public static class Win32PInvokes
	{
		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsProcessorFeaturePresent(ProcessorFeature processorFeature);

		public enum ProcessorFeature : uint
		{
			/// <summary>
			/// On a Pentium, a floating-point precision error can occur in rare circumstances
			/// </summary>
			FloatingPointPrecisionErrata = 0,
			/// <summary>
			/// Floating-point operations are emulated using a software emulator.
			/// This function returns a nonzero value if floating-point operations are emulated; otherwise, it returns zero.
			/// </summary>
			FloatingPointEmulated = 1,
			/// <summary>
			/// The atomic compare and exchange operation (cmpxchg) is available
			/// </summary>
			CompareExchangeDouble = 2,
			/// <summary>
			/// The MMX instruction set is available
			/// </summary>
			InstructionsMMXAvailable = 3,
			/// <summary>
			/// The SSE instruction set is available
			/// </summary>
			InstructionsXMMIAvailable = 6,
			/// <summary>
			/// The 3D-Now instruction set is available.
			/// </summary>
			Instruction3DNowAvailable = 7,
			/// <summary>
			/// The RDTSC instruction is available
			/// </summary>
			InstructionRDTSCAvailable = 8,
			/// <summary>
			/// The processor is PAE-enabled
			/// </summary>
			PAEEnabled = 9,
			/// <summary>
			/// The SSE2 instruction set is available
			/// </summary>
			InstructionsXMMI64Available = 10,
			/// <summary>
			/// Data execution prevention is enabled. (This feature is not supported until Windows XP SP2 and Windows Server 2003 SP1)
			/// </summary>
			NXEnabled = 12,
			/// <summary>
			/// The SSE3 instruction set is available. (This feature is not supported until Windows Vista)
			/// </summary>
			InstructionsSSE3Available = 13,
			/// <summary>
			/// The atomic compare and exchange 128-bit operation (cmpxchg16b) is available. (This feature is not supported until Windows Vista)
			/// </summary>
			CompareExchange128 = 14,
			/// <summary>
			/// The atomic compare 64 and exchange 128-bit operation (cmp8xchg16) is available (This feature is not supported until Windows Vista.)
			/// </summary>
			Compare64Exchange128 = 15,
			/// <summary>
			/// TBD
			/// </summary>
			ChannelsEnabled = 16,
		}

	}

	//largely from https://raw.githubusercontent.com/noserati/tpl/master/ThreadAffinityTaskScheduler.cs (MIT license)
	public static class Win32ThreadHacks
	{
		internal static class NativeMethods
		{
			[DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
			public static extern Int32 WaitForSingleObject(Microsoft.Win32.SafeHandles.SafeWaitHandle handle, uint milliseconds);

			public const uint QS_KEY = 0x0001;
			public const uint QS_MOUSEMOVE = 0x0002;
			public const uint QS_MOUSEBUTTON = 0x0004;
			public const uint QS_POSTMESSAGE = 0x0008;
			public const uint QS_TIMER = 0x0010;
			public const uint QS_PAINT = 0x0020;
			public const uint QS_SENDMESSAGE = 0x0040;
			public const uint QS_HOTKEY = 0x0080;
			public const uint QS_ALLPOSTMESSAGE = 0x0100;
			public const uint QS_RAWINPUT = 0x0400;

			public const uint QS_MOUSE = (QS_MOUSEMOVE | QS_MOUSEBUTTON);
			public const uint QS_INPUT = (QS_MOUSE | QS_KEY | QS_RAWINPUT);
			public const uint QS_ALLEVENTS = (QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY);
			public const uint QS_ALLINPUT = (QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY | QS_SENDMESSAGE);

			public const uint MWMO_INPUTAVAILABLE = 0x0004;
			public const uint MWMO_WAITALL = 0x0001;

			public const uint PM_REMOVE = 0x0001;
			public const uint PM_NOREMOVE = 0;

			public const uint WAIT_TIMEOUT = 0x00000102;
			public const uint WAIT_FAILED = 0xFFFFFFFF;
			public const uint INFINITE = 0xFFFFFFFF;
			public const uint WAIT_OBJECT_0 = 0;
			public const uint WAIT_ABANDONED_0 = 0x00000080;
			public const uint WAIT_IO_COMPLETION = 0x000000C0;

			[StructLayout(LayoutKind.Sequential)]
			public struct MSG
			{
				public IntPtr hwnd;
				public uint message;
				public IntPtr wParam;
				public IntPtr lParam;
				public uint time;
				public int x;
				public int y;
			}

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

			[DllImport("user32.dll")]
			public static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool TranslateMessage([In] ref MSG lpMsg);

			[DllImport("ole32.dll", PreserveSig = false)]
			public static extern void OleInitialize(IntPtr pvReserved);

			[DllImport("ole32.dll", PreserveSig = true)]
			public static extern void OleUninitialize();

			[DllImport("kernel32.dll")]
			public static extern uint GetTickCount();

			[DllImport("user32.dll")]
			public static extern uint GetQueueStatus(uint flags);

			[DllImport("user32.dll", SetLastError = true)]
			public static extern uint MsgWaitForMultipleObjectsEx(
					uint nCount, IntPtr[] pHandles, uint dwMilliseconds, uint dwWakeMask, uint dwFlags);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern uint WaitForMultipleObjects(
					uint nCount, IntPtr[] lpHandles, bool bWaitAll, uint dwMilliseconds);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool SetEvent(IntPtr hEvent);

			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool CloseHandle(IntPtr hObject);
		}

		/// <summary>
		/// Analyze the result of the native wait API
		/// </summary>
		static bool IsNativeWaitSuccessful(uint count, uint nativeResult, out int managedResult)
		{
			if (nativeResult == (NativeMethods.WAIT_OBJECT_0 + count))
			{
				// a is message pending, only valid for MsgWaitForMultipleObjectsEx
				managedResult = unchecked((int)nativeResult);
				return false;
			}

			if (nativeResult >= NativeMethods.WAIT_OBJECT_0 && nativeResult < (NativeMethods.WAIT_OBJECT_0 + count))
			{
				managedResult = unchecked((int)(nativeResult - NativeMethods.WAIT_OBJECT_0));
				return true;
			}

			if (nativeResult >= NativeMethods.WAIT_ABANDONED_0 && nativeResult < (NativeMethods.WAIT_ABANDONED_0 + count))
			{
				managedResult = unchecked((int)(nativeResult - NativeMethods.WAIT_ABANDONED_0));
				throw new AbandonedMutexException();
			}

			if (nativeResult == NativeMethods.WAIT_TIMEOUT)
			{
				managedResult = WaitHandle.WaitTimeout;
				return false;
			}

			throw new InvalidOperationException();
		}

		/// <summary>
		/// functionally the same as WaitOne, but does not message pump
		/// </summary>
		public static void HackyPinvokeWaitOne(WaitHandle handle)
		{
			NativeMethods.WaitForSingleObject(handle.SafeWaitHandle, 0xFFFFFFFF);
		}

		/// <summary>
		/// Functionally the same as WaitOne(), but pumps com messa
		/// </summary>
		/// <param name="handle"></param>
		public static void HackyComWaitOne(WaitHandle handle)
		{
			uint nativeResult; // result of the native wait API (WaitForMultipleObjects or MsgWaitForMultipleObjectsEx)
			int managedResult; // result to return from WaitHelper

			IntPtr[] waitHandles = new IntPtr[]{
				handle.SafeWaitHandle.DangerousGetHandle() };
			uint count = 1;

			uint QS_MASK = NativeMethods.QS_ALLINPUT; // message queue status
			QS_MASK = 0; //bizhawk edit?? did we need any messages here?? apparently not???

			// the core loop
			var msg = new NativeMethods.MSG();
			while (true)
			{
				// MsgWaitForMultipleObjectsEx with MWMO_INPUTAVAILABLE returns,
				// even if there's a message already seen but not removed in the message queue
				nativeResult = NativeMethods.MsgWaitForMultipleObjectsEx(
						count, waitHandles,
						(uint)0xFFFFFFFF,
						QS_MASK,
						NativeMethods.MWMO_INPUTAVAILABLE);

				if (IsNativeWaitSuccessful(count, nativeResult, out managedResult) || WaitHandle.WaitTimeout == managedResult)
					break;
				// there is a message, pump and dispatch it
				if (NativeMethods.PeekMessage(out msg, IntPtr.Zero, 0, 0, NativeMethods.PM_REMOVE))
				{
					NativeMethods.TranslateMessage(ref msg);
					NativeMethods.DispatchMessage(ref msg);
				}
			}
			//m64pFrameComplete.WaitOne();
		}

	}

}