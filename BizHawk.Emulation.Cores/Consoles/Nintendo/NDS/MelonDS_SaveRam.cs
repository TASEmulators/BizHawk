using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	unsafe partial class MelonDS : ISaveRam
	{
		public bool SaveRamModified => IsSRAMModified();

		public byte[] CloneSaveRam()
		{
			int length = GetSRAMLength();
			byte[] data = new byte[length];
			fixed (byte* dst = data)
			{
				GetSRAM(dst, length);
			}
			return data;
		}

		public void StoreSaveRam(byte[] data)
		{
			fixed (byte* src = data)
			{
				SetSRAM(src, data.Length);
			}
		}

		[DllImport(dllPath)]
		private static extern int GetSRAMLength();
		[DllImport(dllPath)]
		private static extern bool IsSRAMModified();
		[DllImport(dllPath)]
		private static extern void GetSRAM(byte* dst, int length);
		[DllImport(dllPath)]
		private static extern void SetSRAM(byte* src, int length);

	}
}
