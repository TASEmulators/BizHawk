using System;
using System.IO;
using System.Runtime.InteropServices;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;


namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class GBA : IStatable
	{
		public bool BinarySaveStatesPreferred { get { return true; } }

		public void SaveStateText(System.IO.TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHex(writer);
			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHex(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
			byte[] data = SaveCoreBinary();
			writer.Write(data.Length);
			writer.Write(data);
			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(System.IO.BinaryReader reader)
		{
			int length = reader.ReadInt32();
			byte[] data = reader.ReadBytes(length);
			LoadCoreBinary(data);
			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		private byte[] SaveCoreBinary()
		{
			IntPtr ndata = IntPtr.Zero;
			uint nsize = 0;
			if (!LibMeteor.libmeteor_savestate(ref ndata, ref nsize))
				throw new Exception("libmeteor_savestate() failed!");
			if (ndata == IntPtr.Zero || nsize == 0)
				throw new Exception("libmeteor_savestate() returned bad!");

			byte[] ret = new byte[nsize];
			Marshal.Copy(ndata, ret, 0, (int)nsize);
			LibMeteor.libmeteor_savestate_destroy(ndata);
			return ret;
		}

		private void LoadCoreBinary(byte[] data)
		{
			if (!LibMeteor.libmeteor_loadstate(data, (uint)data.Length))
				throw new Exception("libmeteor_loadstate() failed!");
		}
	}
}
