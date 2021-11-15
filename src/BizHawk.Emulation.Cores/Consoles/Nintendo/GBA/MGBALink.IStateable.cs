using System;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBALink : IStatable
	{
		public void SaveStateBinary(BinaryWriter writer)
		{
			writer.Write(_numCores);
			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].SaveStateBinary(writer);
				writer.Write(_frameOverflow[i]);
				writer.Write(_stepOverflow[i]);
				writer.Write((int)_connectionStatus[i]);
				writer.Write(_stepTransferCount[i]);
			}
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			if (_numCores != reader.ReadInt32())
			{
				throw new InvalidOperationException("Core number mismatch!");
			}
			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].LoadStateBinary(reader);
				_frameOverflow[i] = reader.ReadInt32();
				_stepOverflow[i] = reader.ReadInt32();
				_connectionStatus[i] = (ConnectionStatus)reader.ReadInt32();
				_stepTransferCount[i] = reader.ReadInt32();
			}
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}
	}
}
