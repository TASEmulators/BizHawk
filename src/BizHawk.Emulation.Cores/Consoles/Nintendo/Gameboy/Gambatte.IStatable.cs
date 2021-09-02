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
			writer.Write(IsSgb);

			if (IsSgb)
			{
				if (LibGambatte.gambatte_savespcstate(GambatteState, _spcsavebuff) != 0)
				{
					throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_savespcstate)}() returned non-zero (spc state error???)");
				}
				writer.Write(_spcsavebuff);
			}
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
			IsSgb = reader.ReadBoolean();

			if (IsSgb)
			{
				reader.Read(_spcsavebuff, 0, _spcsavebuff.Length);
				if (LibGambatte.gambatte_loadspcstate(GambatteState, _spcsavebuff) != 0)
				{
					throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_loadspcstate)}() returned non-zero (spc state error???)");
				}
			}
		}

		private byte[] _savebuff;
		private byte[] _spcsavebuff; // sgb only

		private void NewSaveCoreSetBuff()
		{
			_savebuff = new byte[LibGambatte.gambatte_newstatelen(GambatteState)];
			_spcsavebuff = new byte[67 * 1024L]; // enum { spc_state_size = 67 * 1024L }; /* maximum space needed when saving */
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
			public bool IsSgb;
			public byte[] _spcsavebuff = new byte[67 * 1024L]; // idk how to serialize this so let's just slap this here
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
			s.ExtraData.IsSgb = IsSgb;
			if (IsSgb)
			{
				if (LibGambatte.gambatte_savespcstate(GambatteState, s.ExtraData._spcsavebuff) != 0)
				{
					throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_savespcstate)}() returned non-zero (spc state error???)");
				}
			}
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
			IsSgb = s.ExtraData.IsSgb;
			if (IsSgb)
			{
				if (LibGambatte.gambatte_loadspcstate(GambatteState, s.ExtraData._spcsavebuff) != 0)
				{
					throw new InvalidOperationException($"{nameof(LibGambatte.gambatte_loadspcstate)}() returned non-zero (spc state error???)");
				}
			}
		}
	}
}
