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
			GetSRAM(data, length);
			return data;
		}

		public void StoreSaveRam(byte[] data)
		{
			SetSRAM(data, data.Length);
		}

		[DllImport(dllPath, EntryPoint = "melonds_getsramlength")]
		private static extern int GetSRAMLength();
		[DllImport(dllPath, EntryPoint = "melonds_getsramdirtyflag")]
		private static extern bool IsSRAMModified();
		[DllImport(dllPath, EntryPoint = "melonds_exportsram")]
		private static extern void GetSRAM(byte[] dst, int length);
		[DllImport(dllPath, EntryPoint = "melonds_importsram")]
		private static extern void SetSRAM(byte[] src, int length);
	}
}
