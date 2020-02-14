using System;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : IStatable
	{
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
	}
}
