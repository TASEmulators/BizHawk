using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;
using System.IO;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	unsafe partial class MelonDS : IStatable
	{
		private byte[] _stateBuffer = new byte[0];

		public void LoadStateBinary(BinaryReader reader)
		{
			var mStream = new MemoryStream();
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

		public void SaveStateBinary(BinaryWriter writer)
		{
			int len = GetSavestateSize();
			if (len > _stateBuffer.Length)
			{
				_stateBuffer = new byte[len];
			}

			fixed (byte* ptr = _stateBuffer)
			{
				GetSavestateData(ptr, len);
			}

			writer.Write(_stateBuffer, 0, len);
		}

		[DllImport(dllPath)]
		private static extern bool UseSavestate(byte* data, int len);

		[DllImport(dllPath)]
		private static extern int GetSavestateSize();

		[DllImport(dllPath)]
		private static extern void GetSavestateData(byte* data, int size);
	}
}
