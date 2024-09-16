using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public partial class Stella : IStatable
	{
		public bool AvoidRewind => false;

		public void LoadStateBinary(BinaryReader reader)
		{
			_elf.LoadStateBinary(reader);
			// other variables
			_leftDifficultyToggled = reader.ReadBoolean();
			_rightDifficultyToggled = reader.ReadBoolean();
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
			// any managed pointers that we sent to the core need to be resent now!
			Core.stella_set_input_callback(_inputCallback);
			UpdateVideo();
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			_elf.SaveStateBinary(writer);
			// other variables
			writer.Write(_leftDifficultyToggled);
			writer.Write(_rightDifficultyToggled);
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
		}
	}
}
