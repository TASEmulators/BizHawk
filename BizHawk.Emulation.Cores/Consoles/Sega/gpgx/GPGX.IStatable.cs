using System;
using System.IO;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : IStatable
	{
		public bool BinarySaveStatesPreferred
		{
			get { return true; }
		}

		public void SaveStateText(TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHexFast(writer);
			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHexFast(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		public byte[] SaveStateBinary()
		{
			var ms = new MemoryStream(_savebuff2, true);
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			ms.Close();
			return _savebuff2;
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int newlen = reader.ReadInt32();
			if (newlen != _savebuff.Length)
			{
				throw new Exception("Unexpected state size");
			}

			reader.Read(_savebuff, 0, _savebuff.Length);
			if (!LibGPGX.gpgx_state_load(_savebuff, _savebuff.Length))
			{
				throw new Exception("gpgx_state_load() returned false");
			}

			// other variables
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
			UpdateVideo();
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			if (!LibGPGX.gpgx_state_save(_savebuff, _savebuff.Length))
				throw new Exception("gpgx_state_save() returned false");

			writer.Write(_savebuff.Length);
			writer.Write(_savebuff);
			// other variables
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
		}

		private byte[] _savebuff;
		private byte[] _savebuff2;
	}
}
