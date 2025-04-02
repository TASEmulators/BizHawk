using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	public partial class PPSSPP : IStatable
	{
		private byte[] _stateBuf = Array.Empty<byte>();

		public bool AvoidRewind => true;

		public void SaveStateBinary(BinaryWriter writer)
		{
			/*
			var stateLen = _core.Encore_StartSaveState(_context);
			writer.Write(stateLen);

			if (stateLen > _stateBuf.Length)
			{
				_stateBuf = new byte[stateLen];
			}

			_core.Encore_FinishSaveState(_context, _stateBuf);
			writer.Write(_stateBuf, 0, stateLen);
			*/

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			/*
			var stateLen = reader.ReadInt32();

			if (stateLen > _stateBuf.Length)
			{
				_stateBuf = new byte[stateLen];
			}

			reader.Read(_stateBuf, 0, stateLen);
			_core.Encore_LoadState(_context, _stateBuf, stateLen);
			*/

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();

			// memory domain pointers are no longer valid, reset them
			// WireMemoryDomains();
		}
	}
}