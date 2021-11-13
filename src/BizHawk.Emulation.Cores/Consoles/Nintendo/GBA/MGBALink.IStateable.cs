using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBALink : IStatable
	{
		public void SaveStateBinary(BinaryWriter writer)
		{
			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].SaveStateBinary(writer);
			}
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].LoadStateBinary(reader);
			}
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}
	}
}
