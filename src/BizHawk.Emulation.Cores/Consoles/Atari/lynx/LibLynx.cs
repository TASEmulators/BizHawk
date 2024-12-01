using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Lynx
{
	public abstract class LibLynx
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		[BizImport(cc)]
		public abstract IntPtr Create(byte[] game, int gamesize, byte[] bios, int biossize, int pagesize0, int pagesize1, bool lowpass);

		[BizImport(cc)]
		public abstract void Destroy(IntPtr s);

		[BizImport(cc)]
		public abstract void Reset(IntPtr s);

		[BizImport(cc)]
		public abstract bool Advance(IntPtr s, Buttons buttons, int[] vbuff, short[] sbuff, ref int sbuffsize);

		[BizImport(cc)]
		public abstract void SetRotation(IntPtr s, int value);

		[BizImport(cc)]
		public abstract bool GetSaveRamPtr(IntPtr s, out int size, out IntPtr data);

		[BizImport(cc)]
		public abstract void GetReadOnlyCartPtrs(IntPtr s, out int s0, out IntPtr p0, out int s1, out IntPtr p1);


		[BizImport(cc)]
		public abstract int BinStateSize(IntPtr s);
		[BizImport(cc)]
		public abstract bool BinStateSave(IntPtr s, byte[] data, int length);
		[BizImport(cc)]
		public abstract bool BinStateLoad(IntPtr s, byte[] data, int length);
		[BizImport(cc)]
		public abstract void TxtStateSave(IntPtr s, [In]ref TextStateFPtrs ff);
		[BizImport(cc)]
		public abstract void TxtStateLoad(IntPtr s, [In]ref TextStateFPtrs ff);

		[BizImport(cc)]
		public abstract IntPtr GetRamPointer(IntPtr s);

		[Flags]
		public enum Buttons : ushort
		{
			Up = 0x0040,
			Down = 0x0080,
			Left = 0x0010,
			Right = 0x0020,
			Option_1 = 0x008,
			Option_2 = 0x004,
			B = 0x002,
			A = 0x001,
			Pause = 0x100,
		}
	}
}
