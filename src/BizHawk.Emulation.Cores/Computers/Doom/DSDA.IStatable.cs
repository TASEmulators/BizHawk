using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IStatable
	{
		public bool AvoidRewind => false;

		public void LoadStateBinary(BinaryReader reader)
		{
			_elf.LoadStateBinary(reader);

			_turnHeld[0]    = reader.ReadInt32();
			_turnHeld[1]    = reader.ReadInt32();
			_turnHeld[2]    = reader.ReadInt32();
			_turnHeld[3]    = reader.ReadInt32();
			_turnCarry      = reader.ReadInt32();
			_lastGammaInput = reader.ReadBoolean();
			Frame           = reader.ReadInt32();
			LagCount        = reader.ReadInt32();
			IsLagFrame      = reader.ReadBoolean();

			// any managed pointers that we sent to the core need to be resent now!
			UpdateVideo();
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			_elf.SaveStateBinary(writer);

			writer.Write(_turnHeld[0]);
			writer.Write(_turnHeld[1]);
			writer.Write(_turnHeld[2]);
			writer.Write(_turnHeld[3]);
			writer.Write(_turnCarry);
			writer.Write(_lastGammaInput);
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
		}
	}
}
