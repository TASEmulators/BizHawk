using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.SNK
{
	public abstract class LibNeoGeoPort : LibWaterboxCore
	{
		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public long FrontendTime;
			public int SkipRendering;
			public int Buttons;
		}
		public enum Language
		{
			Japanese, English
		}
		[UnmanagedFunctionPointer(CC)]
		public delegate void SaveRamCallback(IntPtr data, int length);

		[BizImport(CC)]
		public abstract bool LoadSystem(byte[] rom, int romlength, Language language);
		[BizImport(CC)]
		public abstract void SetLayers(int enable); // 1, 2, 4  bg,fg,sprites
		[BizImport(CC)]
		public abstract void HardReset();
		[BizImport(CC)]
		public abstract void SetCommsCallbacks(IntPtr readcb, IntPtr pollcb, IntPtr writecb);
		[BizImport(CC)]
		public abstract bool HasSaveRam();
		[BizImport(CC)]
		public abstract bool PutSaveRam(byte[] data, int length);
		[BizImport(CC)]
		public abstract void GetSaveRam(SaveRamCallback callback);
	}
}
