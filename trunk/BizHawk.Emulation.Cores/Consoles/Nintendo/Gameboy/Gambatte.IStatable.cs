using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

using BizHawk.Emulation.Common;


namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : IStatable
	{
		public bool BinarySaveStatesPreferred { get { return true; } }

		public void SaveStateText(System.IO.TextWriter writer)
		{
			var s = SaveState();
			ser.Serialize(writer, s);
			// write extra copy of stuff we don't use
			writer.WriteLine();
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
			var s = (TextState<TextStateData>)ser.Deserialize(reader, typeof(TextState<TextStateData>));
			LoadState(s);
		}

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
			if (!LibGambatte.gambatte_newstatesave(GambatteState, savebuff, savebuff.Length))
				throw new Exception("gambatte_newstatesave() returned false");

			writer.Write(savebuff.Length);
			writer.Write(savebuff);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
			writer.Write(frameOverflow);
			writer.Write(_cycleCount);
		}

		public void LoadStateBinary(System.IO.BinaryReader reader)
		{
			int length = reader.ReadInt32();
			if (length != savebuff.Length)
				throw new InvalidOperationException("Savestate buffer size mismatch!");

			reader.Read(savebuff, 0, savebuff.Length);

			if (!LibGambatte.gambatte_newstateload(GambatteState, savebuff, savebuff.Length))
				throw new Exception("gambatte_newstateload() returned false");

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
			frameOverflow = reader.ReadUInt32();
			_cycleCount = reader.ReadUInt64();
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream(savebuff2);
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			if (ms.Position != savebuff2.Length)
				throw new InvalidOperationException();
			ms.Close();
			return savebuff2;
		}

		private byte[] savebuff;
		private byte[] savebuff2;

		private void NewSaveCoreSetBuff()
		{
			savebuff = new byte[LibGambatte.gambatte_newstatelen(GambatteState)];
			savebuff2 = new byte[savebuff.Length + 4 + 21];
		}

		private JsonSerializer ser = new JsonSerializer { Formatting = Formatting.Indented };

		// other data in the text state besides core
		internal class TextStateData
		{
			public int Frame;
			public int LagCount;
			public bool IsLagFrame;
			public ulong _cycleCount;
			public uint frameOverflow;
		}

		internal TextState<TextStateData> SaveState()
		{
			var s = new TextState<TextStateData>();
			s.Prepare();
			var ff = s.GetFunctionPointersSave();
			LibGambatte.gambatte_newstatesave_ex(GambatteState, ref ff);
			s.ExtraData.IsLagFrame = IsLagFrame;
			s.ExtraData.LagCount = LagCount;
			s.ExtraData.Frame = Frame;
			s.ExtraData.frameOverflow = frameOverflow;
			s.ExtraData._cycleCount = _cycleCount;
			return s;
		}

		internal void LoadState(TextState<TextStateData> s)
		{
			s.Prepare();
			var ff = s.GetFunctionPointersLoad();
			LibGambatte.gambatte_newstateload_ex(GambatteState, ref ff);
			IsLagFrame = s.ExtraData.IsLagFrame;
			LagCount = s.ExtraData.LagCount;
			Frame = s.ExtraData.Frame;
			frameOverflow = s.ExtraData.frameOverflow;
			_cycleCount = s.ExtraData._cycleCount;
		}
	}
}
