using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators.Emu83
{
	public partial class Emu83 : IStatable
	{
		private readonly byte[] _stateBuf = new byte[LibEmu83.TI83_GetStateSize()];

		public bool AvoidRewind => false;

		public void SaveStateBinary(BinaryWriter writer)
		{
			if (!LibEmu83.TI83_SaveState(Context, _stateBuf))
			{
				throw new Exception($"{nameof(LibEmu83.TI83_SaveState)}() returned false");
			}

			writer.Write(_stateBuf.Length);
			writer.Write(_stateBuf);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			if (length != _stateBuf.Length)
			{
				throw new InvalidOperationException("Savestate buffer size mismatch!");
			}

			reader.Read(_stateBuf, 0, _stateBuf.Length);

			if (!LibEmu83.TI83_LoadState(Context, _stateBuf))
			{
				throw new Exception($"{nameof(LibEmu83.TI83_LoadState)}() returned false");
			}

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}
	}
}
