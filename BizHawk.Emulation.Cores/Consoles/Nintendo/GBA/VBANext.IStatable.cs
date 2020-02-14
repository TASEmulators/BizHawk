using System;
using System.IO;
using Newtonsoft.Json;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class VBANext : IStatable
	{
		public void SaveStateBinary(BinaryWriter writer)
		{
			if (!LibVBANext.BinStateSave(Core, _savebuff, _savebuff.Length))
				throw new InvalidOperationException($"Core's {nameof(LibVBANext.BinStateSave)}() returned false!");
			writer.Write(_savebuff.Length);
			writer.Write(_savebuff);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			if (length != _savebuff.Length)
				throw new InvalidOperationException("Save buffer size mismatch!");
			reader.Read(_savebuff, 0, length);
			if (!LibVBANext.BinStateLoad(Core, _savebuff, _savebuff.Length))
				throw new InvalidOperationException($"Core's {nameof(LibVBANext.BinStateLoad)}() returned false!");

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public byte[] SaveStateBinary()
		{
			var ms = new MemoryStream(_savebuff2, true);
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			if (ms.Position != _savebuff2.Length)
				throw new InvalidOperationException();
			ms.Close();
			return _savebuff2;
		}

		private JsonSerializer ser = new JsonSerializer { Formatting = Formatting.Indented };
		private readonly byte[] _savebuff;
		private readonly byte[] _savebuff2;

		private class TextStateData
		{
			public int Frame;
			public int LagCount;
			public bool IsLagFrame;
		}
	}
}
