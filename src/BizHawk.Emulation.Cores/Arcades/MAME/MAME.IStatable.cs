using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : IStatable
	{
		public bool AvoidRewind => false;

		public void SaveStateBinary(BinaryWriter writer)
		{
			using (_exe.EnterExit())
			{
				_exe.SaveStateBinary(writer);
			}

			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			using (_exe.EnterExit())
			{
				_exe.LoadStateBinary(reader);
			}

			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();

			DiscardSamples();
		}
	}
}