using System;
using System.IO;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64 : IStatable
	{
		public bool BinarySaveStatesPreferred { get { return true; } }

		// these functions are all exact copy paste from gambatte.
		// if something's wrong here, it's probably wrong there too
		public void SaveStateText(TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHexFast(writer);

			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(TextReader reader)
		{
			var hex = reader.ReadLine();
			var state = new byte[hex.Length / 2];
			state.ReadFromHexFast(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			byte[] data = SaveStatePrivateBuff;
			int bytes_used = api.SaveState(data);

			writer.Write(bytes_used);
			writer.Write(data, 0, bytes_used);

			byte[] saveram = api.SaveSaveram();
			writer.Write(saveram);
			if (saveram.Length != mupen64plusApi.kSaveramSize)
			{
				throw new InvalidOperationException("Unexpected N64 SaveRam size");
			}

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			if ((_disableExpansionSlot && length >= 16788288) || (!_disableExpansionSlot && length < 16788288))
			{
				throw new SavestateSizeMismatchException("Wrong N64 savestate size");
			}

			reader.Read(SaveStatePrivateBuff, 0, length);
			byte[] data = SaveStatePrivateBuff;

			api.LoadState(data);

			reader.Read(SaveStatePrivateBuff, 0, mupen64plusApi.kSaveramSize);
			api.LoadSaveram(SaveStatePrivateBuff);

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public byte[] SaveStateBinary()
		{
			// WELCOME TO THE HACK ZONE
			byte[] saveram = api.SaveSaveram();

			int lenwant = 4 + SaveStatePrivateBuff.Length + saveram.Length + 1 + 4 + 4;
			if (SaveStateBinaryPrivateBuff.Length != lenwant)
			{
				Console.WriteLine("Allocating new N64 private buffer size {0}", lenwant);
				SaveStateBinaryPrivateBuff = new byte[lenwant];
			}

			var ms = new MemoryStream(SaveStateBinaryPrivateBuff);
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();

			if (ms.Length != SaveStateBinaryPrivateBuff.Length)
			{
				throw new Exception("Unexpected Length");
			}

			return SaveStateBinaryPrivateBuff;
		}

		private byte[] SaveStatePrivateBuff = new byte[16788288 + 1024];
		private byte[] SaveStateBinaryPrivateBuff = new byte[0];
	}
}
