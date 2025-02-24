using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public static class XInput2Imports
	{
		private const string XI2 = "libXi.so.6";

		[DllImport(XI2)]
		public static extern XlibImports.Status XIQueryVersion(IntPtr display, ref int major_version_inout, ref int minor_version_inout);

		public enum XIEvents
		{
			XI_DeviceChanged = 1,
			XI_KeyPress = 2,
			XI_KeyRelease = 3,
			XI_ButtonPress = 4,
			XI_ButtonRelease = 5,
			XI_Motion = 6,
			XI_Enter = 7,
			XI_Leave = 8,
			XI_FocusIn = 9,
			XI_FocusOut = 10,
			XI_HierarchyChanged = 11,
			XI_PropertyEvent = 12,
			XI_RawKeyPress = 13,
			XI_RawKeyRelease = 14,
			XI_RawButtonPress = 15,
			XI_RawButtonRelease = 16,
			XI_RawMotion = 17,
			XI_TouchBegin = 18, // XI 2.2
			XI_TouchUpdate = 19,
			XI_TouchEnd = 20,
			XI_TouchOwnership = 21,
			XI_RawTouchBegin = 22,
			XI_RawTouchUpdate = 23,
			XI_RawTouchEnd = 24,
			XI_BarrierHit = 25, // XI 2.3
			XI_BarrierLeave = 26,
			XI_GesturePinchBegin = 27, // XI 2.4
			XI_GesturePinchUpdate = 28,
			XI_GesturePinchEnd = 29,
			XI_GestureSwipeBegin = 30,
			XI_GestureSwipeUpdate = 31,
			XI_GestureSwipeEnd = 32,
			XI_LASTEVENT = XI_GestureSwipeEnd
		}

		// these are normally macros in XI2.h
		public static void XISetMask(Span<byte> maskBuf, int evt)
			=> maskBuf[evt >> 3] |= (byte)(1 << (evt & 7));

		public static bool XIMaskIsSet(Span<byte> maskBuf, int evt)
			=> (maskBuf[evt >> 3] & (byte)(1 << (evt & 7))) != 0;

		public const int XIAllDevices = 0;
		public const int XIAllMasterDevices = 1;

		[StructLayout(LayoutKind.Sequential)]
		public struct XIEventMask
		{
			public int deviceid;
			public int mask_len;
			public IntPtr mask;
		}

		// weird status XISelectEvents uses...
		public enum XIStatus : int
		{
			Success = 0,
			NoSuchExtension = 1,
			MiscError = -1,
		}

		[DllImport(XI2)]
		public static extern XIStatus XISelectEvents(IntPtr display, IntPtr win, ref XIEventMask masks, int num_masks);

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct XIValuatorState
		{
			public int mask_len;
			public byte* mask;
			public double* values;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct XIRawEvent
		{
			public int type;
			public nuint serial;
			public int send_event; // Bool
			public IntPtr display;
			public int extension;
			public int evtype;
			public nuint time;
			public int deviceid;
			public int sourceid;
			public int detail;
			public int flags;
			public XIValuatorState valuators;
			public unsafe double* raw_values;
		}
	}
}
