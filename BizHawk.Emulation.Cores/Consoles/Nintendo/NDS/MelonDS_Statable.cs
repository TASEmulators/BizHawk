using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;
using System.IO;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	unsafe partial class MelonDS : IStatable
	{
		public void LoadStateBinary(BinaryReader reader)
		{
			MemoryStream mStream = new MemoryStream();
			reader.BaseStream.CopyTo(mStream);

			LoadStateByteArray(mStream.GetBuffer(), (int)mStream.Length);
		}

		private void LoadStateByteArray(byte[] data, int length = -1)
		{
			if (length == -1) length = data.Length;
			fixed (byte* ptr = data)
			{
				if (!UseSavestate(ptr, length))
					CoreComm.Notify("Savestate load failed! See log window for details.");
			}
		}

		public byte[] SaveStateBinary()
		{
			int len = GetSavestateSize();
			byte[] ret = new byte[len];
			fixed (byte* ptr = ret)
			{
				GetSavestateData(ptr, len);
			}
			return ret;
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			byte[] data = SaveStateBinary();
			writer.Write(data);
		}

		[DllImport(dllPath)]
		private static extern bool UseSavestate(byte* data, int len);
		[DllImport(dllPath)]
		private static extern int GetSavestateSize();
		[DllImport(dllPath)]
		private static extern void GetSavestateData(byte* data, int size);
	}
}
