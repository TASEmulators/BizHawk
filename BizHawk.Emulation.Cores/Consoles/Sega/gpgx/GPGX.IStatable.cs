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
#if true

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
			if (!Core.gpgx_state_load(_savebuff, _savebuff.Length))
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
			if (!Core.gpgx_state_save(_savebuff, _savebuff.Length))
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

		private void InitStateBuffers()
		{
			byte[] tmp = new byte[Core.gpgx_state_max_size()];
			int size = Core.gpgx_state_size(tmp, tmp.Length);
			if (size <= 0)
				throw new Exception("Couldn't Determine GPGX internal state size!");
			_savebuff = new byte[size];
			_savebuff2 = new byte[_savebuff.Length + 13];
			Console.WriteLine("GPGX Internal State Size: {0}", size);
		}

#else
		public void LoadStateBinary(BinaryReader reader)
		{
			var elf = (ElfRunner)NativeData;
			elf.LoadStateBinary(reader);
			// other variables
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
			// any managed pointers that we sent to the core need to be resent now!
			// TODO: sega cd won't work until we fix that!
			Core.gpgx_set_input_callback(InputCallback);
			RefreshMemCallbacks();

			UpdateVideo();
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			var elf = (ElfRunner)NativeData;
			elf.SaveStateBinary(writer);
			// other variables
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
		}

		public byte[] SaveStateBinary()
		{
			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			ms.Close();
			return ms.ToArray();
		}

		private void InitStateBuffers()
		{
		}
#endif
	}
}
