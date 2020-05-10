using System;
using System.IO;
using Newtonsoft.Json;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : ITextStatable
	{
		public void SaveStateText(TextWriter writer)
		{
			var s = SaveState();
			ser.Serialize(writer, s);
		}

		public void LoadStateText(TextReader reader)
		{
			var s = (TextState<TextStateData>)ser.Deserialize(reader, typeof(TextState<TextStateData>));
			LoadState(s);
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			if (!LibGambatte.gambatte_newstatesave(GambatteState, _savebuff, _savebuff.Length))
			{
				throw new Exception($"{nameof(LibGambatte.gambatte_newstatesave)}() returned false");
			}

			writer.Write(_savebuff.Length);
			writer.Write(_savebuff);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
			writer.Write(frameOverflow);
			writer.Write(_cycleCount);
			writer.Write(IsCgb);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			if (length != _savebuff.Length)
			{
				throw new InvalidOperationException("Savestate buffer size mismatch!");
			}

			reader.Read(_savebuff, 0, _savebuff.Length);

			if (!LibGambatte.gambatte_newstateload(GambatteState, _savebuff, _savebuff.Length))
			{
				throw new Exception($"{nameof(LibGambatte.gambatte_newstateload)}() returned false");
			}

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
			frameOverflow = reader.ReadUInt32();
			_cycleCount = reader.ReadUInt64();
			IsCgb = reader.ReadBoolean();
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream(_savebuff2);
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			if (ms.Position != _savebuff2.Length)
			{
				throw new InvalidOperationException();
			}

			ms.Close();
			return _savebuff2;
		}

		private byte[] _savebuff;
		private byte[] _savebuff2;

		private void NewSaveCoreSetBuff()
		{
			_savebuff = new byte[LibGambatte.gambatte_newstatelen(GambatteState)];
			_savebuff2 = new byte[_savebuff.Length + 4 + 21 + 1];
		}

		private readonly JsonSerializer ser = new JsonSerializer { Formatting = Formatting.Indented };

		// other data in the text state besides core
		internal class TextStateData
		{
			public int Frame;
			public int LagCount;
			public bool IsLagFrame;
			public ulong _cycleCount;
			public uint frameOverflow;
			public bool IsCgb;
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
			s.ExtraData.IsCgb = IsCgb;
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
			IsCgb = s.ExtraData.IsCgb;
		}
	}
}
