using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class VBANext : IStatable
	{
		public bool BinarySaveStatesPreferred
		{
			get { return true; }
		}

		public void SaveStateText(TextWriter writer)
		{
			var s = new TextState<TextStateData>();
			s.Prepare();
			var ff = s.GetFunctionPointersSave();
			LibVBANext.TxtStateSave(Core, ref ff);
			s.ExtraData.IsLagFrame = IsLagFrame;
			s.ExtraData.LagCount = LagCount;
			s.ExtraData.Frame = Frame;

			ser.Serialize(writer, s);
			// write extra copy of stuff we don't use
			writer.WriteLine();
			writer.WriteLine("Frame {0}", Frame);

			//Console.WriteLine(BizHawk.Common.BufferExtensions.BufferExtensions.HashSHA1(SaveStateBinary()));
		}

		public void LoadStateText(TextReader reader)
		{
			var s = (TextState<TextStateData>)ser.Deserialize(reader, typeof(TextState<TextStateData>));
			s.Prepare();
			var ff = s.GetFunctionPointersLoad();
			LibVBANext.TxtStateLoad(Core, ref ff);
			IsLagFrame = s.ExtraData.IsLagFrame;
			LagCount = s.ExtraData.LagCount;
			Frame = s.ExtraData.Frame;
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			if (!LibVBANext.BinStateSave(Core, savebuff, savebuff.Length))
				throw new InvalidOperationException("Core's BinStateSave() returned false!");
			writer.Write(savebuff.Length);
			writer.Write(savebuff);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			if (length != savebuff.Length)
				throw new InvalidOperationException("Save buffer size mismatch!");
			reader.Read(savebuff, 0, length);
			if (!LibVBANext.BinStateLoad(Core, savebuff, savebuff.Length))
				throw new InvalidOperationException("Core's BinStateLoad() returned false!");

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public byte[] SaveStateBinary()
		{
			var ms = new MemoryStream(savebuff2, true);
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			if (ms.Position != savebuff2.Length)
				throw new InvalidOperationException();
			ms.Close();
			return savebuff2;
		}

		private JsonSerializer ser = new JsonSerializer() { Formatting = Formatting.Indented };
		private byte[] savebuff;
		private byte[] savebuff2;

		private class TextStateData
		{
			public int Frame;
			public int LagCount;
			public bool IsLagFrame;
		}
	}
}
